namespace RaspberryDotNet.FrameBuffer;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public sealed class PixelBufferBgr565 : PixelBuffer
{
    public PixelBufferBgr565(int width, int height)
        : base(CreateBuffer(width, height), width, height)
    {
    }

    public override void SetPixel(int x, int y, byte r, byte g, byte b)
    {
        var offset = ((y * Width) + x) * 2;
        var rgb = (ushort)(((r & 0xF8) << 8) | ((g & 0xFC) << 3) | (b >> 3));
        Unsafe.WriteUnaligned(ref Data[offset], rgb);
    }

    public override void Clear(byte r = 0, byte g = 0, byte b = 0)
    {
        var rgb = (ushort)(((r & 0xF8) << 8) | ((g & 0xFC) << 3) | (b >> 3));
        var lo = (byte)(rgb & 0xFF);
        if (lo == (byte)(rgb >> 8))
        {
            Data.Fill(lo);
        }
        else
        {
            MemoryMarshal.Cast<byte, ushort>(Data).Fill(rgb);
        }
    }

    private static byte[] CreateBuffer(int width, int height)
    {
        return new byte[checked(width * height * 2)];
    }
}
