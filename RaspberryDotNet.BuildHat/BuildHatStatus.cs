namespace RaspberryDotNet.BuildHat;

public readonly record struct BuildHatStatus(
    string Firmware,
    bool FirmwareLoaded,
    double? Voltage,
    bool HasPowerFault);
