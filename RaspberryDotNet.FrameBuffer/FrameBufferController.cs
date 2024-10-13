namespace RaspberryDotNet.FrameBuffer;

// TODO
public sealed class FrameBufferController
{
    public PixelBuffer Buffer { get; }

    public FrameBufferController(PixelBuffer buffer)
    {
        Buffer = buffer;
    }
}
