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

        stream = File.OpenWrite(name);
    }

    public void Close()
    {
        stream?.Dispose();
        stream = null;
    }

    public void Update()
    {
        if (stream is null)
        {
            return;
        }

        stream.Seek(0, SeekOrigin.Begin);
        stream.Write(Buffer.Data);
        stream.Flush();
    }
}
