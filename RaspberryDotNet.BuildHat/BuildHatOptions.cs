namespace RaspberryDotNet.BuildHat;

public sealed class BuildHatOptions
{
    public string Device { get; set; } = "/dev/serial0";

    public BuildHatFirmware? Firmware { get; set; }

    public int ResetPin { get; set; } = 4;

    public int Boot0Pin { get; set; } = 22;

    public int DataInterval { get; set; } = 100;

    public double PowerLimit { get; set; } = 0.7;

    public double PwmThreshold { get; set; } = 0.65;

    public double MinimumPwm { get; set; } = 0.01;

    public TimeSpan OperationTimeoutMargin { get; set; } = TimeSpan.FromSeconds(2);

    public TimeSpan ReadTimeout { get; set; } = TimeSpan.FromSeconds(1);
}
