namespace RaspberryDotNet.BuildHat;

public readonly record struct BuildHatMatrixPixel(
    BuildHatMatrixColor Color,
    int Brightness);
