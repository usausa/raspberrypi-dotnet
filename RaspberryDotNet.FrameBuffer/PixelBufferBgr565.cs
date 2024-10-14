namespace RaspberryDotNet.FrameBuffer;

using System.Runtime.CompilerServices;

public sealed class PixelBufferBgr565 : PixelBuffer
{
    public PixelBufferBgr565(int width, int height)
        : base(new byte[width * height * 2], width, height)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (byte Byte0, byte Byte1) CalcPixelBytes(byte r, byte g, byte b)
    {
        var rgb = ((r & 0xF8) << 8) | ((g & 0xFC) << 3) | (b >> 3);
        return ((byte)(rgb & 0xFF), (byte)((rgb >> 8) & 0xFF));
    }

    public override void SetPixel(int x, int y, byte r, byte g, byte b)
    {
        var offset = ((y * Width) + x) * 2;
        var (b0, b1) = CalcPixelBytes(r, g, b);
        var span = Data[offset..];
        span[0] = b0;
        span[1] = b1;
    }

    public override void Clear(byte r = 0, byte g = 0, byte b = 0)
    {
        var (b0, b1) = CalcPixelBytes(r, g, b);
        if (b0 == b1)
        {
            Data.Fill(b0);
        }
        else
        {
            var span = Data;
            for (var offset = 0; offset < Data.Length; offset += 2)
            {
                span[offset] = b0;
                span[offset + 1] = b1;
            }
        }
    }
}
