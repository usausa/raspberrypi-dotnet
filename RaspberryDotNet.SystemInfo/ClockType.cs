namespace RaspberryDotNet.SystemInfo;

// ReSharper disable IdentifierTypo
#pragma warning disable CA1008
#pragma warning disable CA1028
public enum ClockType : uint
{
    Emmc = 1,
    Uart = 2,
    Arm = 3,
    Core = 4,
    V3d = 5,
    H264 = 6,
    Isp = 7,
    Sdram = 8,
    Pixel = 9,
    Pwm = 10
}
#pragma warning restore CA1028
#pragma warning restore CA1008
// ReSharper restore IdentifierTypo
