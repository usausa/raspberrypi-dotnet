namespace RaspberryDotNet.SystemInfo;

[Flags]
#pragma warning disable CA1028
#pragma warning disable CA1711
#pragma warning disable CA2217
public enum ThrottledFlags : uint
{
    None = 0,

    // Current status (bits 0-15)
    UnderVoltageDetected = 1 << 0,
    ArmFrequencyCapped = 1 << 1,
    CurrentlyThrottled = 1 << 2,
    SoftTemperatureLimitActive = 1 << 3,

    // Historical status (bits 16-31)
    UnderVoltageHasOccurred = 1 << 16,
    ArmFrequencyCappingHasOccurred = 1 << 17,
    ThrottlingHasOccurred = 1 << 18,
    SoftTemperatureLimitHasOccurred = 1 << 19,

    Unknown = 0xFFFFFFFF
}
#pragma warning restore CA2217
#pragma warning restore CA1711
#pragma warning restore CA1028
