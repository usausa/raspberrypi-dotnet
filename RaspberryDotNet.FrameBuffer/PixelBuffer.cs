namespace RaspberryDotNet.FrameBuffer;

using System.Drawing;
using System.Runtime.CompilerServices;

public abstract class PixelBuffer
{
    private readonly byte[] data;

    public int Width { get; }

    public int Height { get; }

    public Span<byte> Data => data;

    protected PixelBuffer(byte[] data, int width, int height)
    {
        this.data = data;
        Width = width;
        Height = height;
    }

    public abstract void SetPixel(int x, int y, byte r, byte g, byte b);

    public virtual void Clear(byte r = 0, byte g = 0, byte b = 0)
    {
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                SetPixel(x, y, 0, 0, 0);
            }
        }
    }
}

public static class PixelBufferExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetPixel(this PixelBuffer buffer, int x, int y, Color c) =>
        buffer.SetPixel(x, y, c.R, c.G, c.B);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Clear(this PixelBuffer buffer, Color c) =>
        buffer.Clear(c.R, c.G, c.B);
}
