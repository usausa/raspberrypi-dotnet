namespace RaspberryDotNet.BuildHat;

using System.Globalization;

public sealed class BuildHatLight
{
    private const int MaxBrightness = 100;

    private readonly BuildHatController controller;

    public BuildHatPort Port { get; }

    internal BuildHatLight(BuildHatController controller, BuildHatPort port)
    {
        this.controller = controller;
        Port = port;
    }

    public void SetBrightness(int brightness)
    {
        if (brightness == 0)
        {
            Off();
            return;
        }

        controller.WriteLight(Port.Index, String.Create(CultureInfo.InvariantCulture, $"on ; set {(double)brightness / MaxBrightness:0.##}"));
    }

    public void Off() => controller.WriteLight(Port.Index, "coast");
}
