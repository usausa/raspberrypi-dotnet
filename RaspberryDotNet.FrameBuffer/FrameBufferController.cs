namespace RaspberryDotNet.FrameBuffer;

using System;
using System.IO;

using static RaspberryDotNet.FrameBuffer.NativeMethods;

public sealed class FrameBufferController : IDisposable
{
    private readonly string name;

    private IntPtr map = IntPtr.Zero;
    private nuint mapLength;

    private Stream? stream;

    public PixelBuffer Buffer { get; }

    public bool IsOpen => (map != IntPtr.Zero) || (stream is not null);

    public FrameBufferController(string name, PixelBuffer buffer)
    {
        Buffer = buffer;
        this.name = name;
    }

    public void Dispose()
    {
        if (IsOpen)
        {
            Close();
        }
    }

    public void Open()
    {
        if (IsOpen)
        {
            return;
        }

        var expected = ReadFrameBufferSize(Path.GetFileName(name));
        if (expected.HasValue && (Buffer.Data.Length != expected.Value))
        {
            throw new InvalidOperationException($"Buffer size does not match framebuffer. buffer=[{Buffer.Data.Length}], framebuffer=[{expected.Value}]");
        }

        var length = expected ?? Buffer.Data.Length;
        if (TryOpenMap(length))
        {
            return;
        }

        // Fall back to stream
        stream = new FileStream(name, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
    }

    private bool TryOpenMap(int length)
    {
        var fd = open(name, O_RDWR);
        if (fd < 0)
        {
            return false;
        }

        try
        {
            var ptr = mmap(IntPtr.Zero, (nuint)length, PROT_READ | PROT_WRITE, MAP_SHARED, fd, IntPtr.Zero);
            if (ptr == MAP_FAILED)
            {
                return false;
            }

            map = ptr;
            mapLength = (nuint)length;
            return true;
        }
        finally
        {
            // The mapping stays valid after the descriptor is closed.
            _ = close(fd);
        }
    }

    private static int? ReadFrameBufferSize(string deviceName)
    {
        var basePath = $"/sys/class/graphics/{deviceName}";
        try
        {
            var parts = File.ReadAllText(Path.Combine(basePath, "virtual_size")).Trim().Split(',');
            if ((parts.Length != 2) || !Int32.TryParse(parts[0], out var width) || !Int32.TryParse(parts[1], out var height))
            {
                return null;
            }

            // stride (line length) accounts for row padding when available
            var stridePath = Path.Combine(basePath, "stride");
            if (File.Exists(stridePath) &&
                Int32.TryParse(File.ReadAllText(stridePath).AsSpan().Trim(), out var stride) &&
                (stride > 0))
            {
                var strideSize = (long)stride * height;
                return strideSize <= Int32.MaxValue ? (int)strideSize : null;
            }

            if (!Int32.TryParse(File.ReadAllText(Path.Combine(basePath, "bits_per_pixel")).AsSpan().Trim(), out var bpp))
            {
                return null;
            }

            var size = (long)width * height * bpp / 8;
            return size <= Int32.MaxValue ? (int)size : null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    public void Close()
    {
        if (map != IntPtr.Zero)
        {
            _ = munmap(map, mapLength);
            map = IntPtr.Zero;
            mapLength = 0;
        }

        stream?.Dispose();
        stream = null;
    }

    public unsafe void Update(bool flush = true)
    {
        if (map != IntPtr.Zero)
        {
            // Direct memcpy into the mapped framebuffer; no syscall per frame.
            Buffer.Data.CopyTo(new Span<byte>((void*)map, (int)mapLength));
            return;
        }

        if (stream is null)
        {
            return;
        }

        stream.Seek(0, SeekOrigin.Begin);
        stream.Write(Buffer.Data);
        if (flush)
        {
            stream.Flush();
        }
    }
}
