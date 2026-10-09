namespace RaspberryDotNet.BuildHat;

using System.Globalization;

public sealed class BuildHatPassiveMotor
{
    private readonly BuildHatController controller;

    public BuildHatPort Port { get; }

    internal BuildHatPassiveMotor(BuildHatController controller, BuildHatPort port)
    {
        this.controller = controller;
        Port = port;
    }

    public void SetPower(double power)
    {
        controller.WritePassiveMotor(Port.Index, String.Create(CultureInfo.InvariantCulture, $"pwm ; set {power:0.###}"));
    }

    public void Coast() => controller.WritePassiveMotor(Port.Index, "coast");

    public void Brake() => controller.WritePassiveMotor(Port.Index, "off");

    public void SetPowerLimit(double limit) => Port.SetPowerLimit(limit);
}
