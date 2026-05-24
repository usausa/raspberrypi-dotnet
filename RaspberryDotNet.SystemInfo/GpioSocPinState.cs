namespace RaspberryDotNet.SystemInfo;

public readonly record struct GpioSocPinState(int Pin, GpioFunction Function, int Level);
