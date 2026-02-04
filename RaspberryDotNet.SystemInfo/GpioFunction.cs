namespace RaspberryDotNet.SystemInfo;

#pragma warning disable CA1028
public enum GpioFunction : uint
{
    In = 0,
    Out = 1,
    Alt5 = 2,
    Alt4 = 3,
    Alt0 = 4,
    Alt1 = 5,
    Alt2 = 6,
    Alt3 = 7,
    Unknown = 0xFFFF
}
#pragma warning restore CA1028
