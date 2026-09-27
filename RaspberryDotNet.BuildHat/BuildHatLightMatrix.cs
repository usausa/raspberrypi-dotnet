namespace RaspberryDotNet.BuildHat;

using System.Globalization;

public sealed class BuildHatLightMatrix
{
    public const int PixelCount = 9;

    private const int LevelMode = 0;

    private const int PixelMode = 2;

    private const int TransitionMode = 3;

    private const byte LevelHeader = 0xC0;

    private const byte PixelHeader = 0xC2;

    private const byte TransitionHeader = 0xC3;

    private const int MaxLevel = 9;

    private const int MaxBrightness = 10;

    private readonly BuildHatController controller;

    public BuildHatPort Port { get; }

    internal BuildHatLightMatrix(BuildHatController controller, BuildHatPort port)
    {
        this.controller = controller;
        Port = port;
    }

    public void SetPixels(ReadOnlySpan<BuildHatMatrixPixel> pixels)
    {
        if (pixels.Length != PixelCount)
        {
            throw new ArgumentException(String.Create(CultureInfo.InvariantCulture, $"Pixels must have {PixelCount} items. count=[{pixels.Length}]"), nameof(pixels));
        }

        Span<byte> data = stackalloc byte[PixelCount + 1];
        data[0] = PixelHeader;
        for (var i = 0; i < PixelCount; i++)
        {
            data[i + 1] = ToByte(pixels[i], nameof(pixels));
        }

        Write(PixelMode, data);
    }

    public void Fill(BuildHatMatrixPixel pixel)
    {
        Span<byte> data = stackalloc byte[PixelCount + 1];
        data[0] = PixelHeader;
        data[1..].Fill(ToByte(pixel, nameof(pixel)));
        Write(PixelMode, data);
    }

    public void Clear() => Fill(default);

    public void SetLevel(int level)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(level);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(level, MaxLevel);
        Write(LevelMode, [LevelHeader, (byte)level]);
    }

    public void SetTransition(BuildHatMatrixTransition transition)
    {
        ArgumentOutOfRangeException.ThrowIfNegative((int)transition, nameof(transition));
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)transition, (int)BuildHatMatrixTransition.Fade, nameof(transition));
        Write(TransitionMode, [TransitionHeader, (byte)transition]);
    }

    private void Write(int mode, ReadOnlySpan<byte> data)
    {
        controller.PrepareLightMatrix(Port.Index);
        controller.WriteModeData(Port.Index, mode, data);
    }

    private static byte ToByte(BuildHatMatrixPixel pixel, string name)
    {
        ArgumentOutOfRangeException.ThrowIfNegative((int)pixel.Color, name);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)pixel.Color, (int)BuildHatMatrixColor.White, name);
        ArgumentOutOfRangeException.ThrowIfNegative(pixel.Brightness, name);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pixel.Brightness, MaxBrightness, name);
        return (byte)((pixel.Brightness << 4) | (int)pixel.Color);
    }
}
