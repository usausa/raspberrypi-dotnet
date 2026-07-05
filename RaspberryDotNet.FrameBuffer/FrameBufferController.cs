namespace RaspberryDotNet.FrameBuffer;

using System;
using System.IO;

public sealed class FrameBufferController : IDisposable
{
    private readonly string name;

    private Stream? stream;

    public PixelBuffer Buffer { get; }

    public bool IsOpen => stream != null;

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

        stream = new FileStream(name, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
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
        stream?.Dispose();
        stream = null;
    }

    public void Update(bool flush = true)
    {
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
