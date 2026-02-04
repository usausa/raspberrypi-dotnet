namespace RaspberryDotNet.SystemInfo;

public sealed class GpioSocPinState
{
    public int Pin { get; }

    public GpioFunction Function { get; }

    public int Level { get; }

    public GpioSocPinState(int pin, GpioFunction function, int level)
    {
        Pin = pin;
        Function = function;
        Level = level;
    }
}
