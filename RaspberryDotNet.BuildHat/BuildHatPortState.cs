namespace RaspberryDotNet.BuildHat;

public readonly record struct BuildHatPortState(
    BuildHatDeviceType? DeviceType,
    int Speed,
    int Position,
    int? AbsolutePosition,
    IReadOnlyList<double> Values);
