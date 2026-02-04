namespace RaspberryDotNet.SystemInfo;

public sealed class GpioHeaderPinState
{
    public int PhysicalPin { get; }

    public int SocPin { get; }

    public GpioFunction Function { get; }

    public int Level { get; }

    public GpioHeaderPinState(int physicalPin, int socPin, GpioFunction function, int level)
    {
        PhysicalPin = physicalPin;
        SocPin = socPin;
        Function = function;
        Level = level;
    }
}
