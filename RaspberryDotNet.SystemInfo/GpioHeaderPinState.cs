namespace RaspberryDotNet.SystemInfo;

public readonly record struct GpioHeaderPinState(int PhysicalPin, int SocPin, GpioFunction Function, int Level);
