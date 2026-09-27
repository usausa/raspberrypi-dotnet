namespace RaspberryDotNet.BuildHat;

public sealed class BuildHatMotor
{
    private readonly BuildHatController controller;

    public BuildHatPort Port { get; }

    public BuildHatPortState State => Port.State;

    internal BuildHatMotor(BuildHatController controller, BuildHatPort port)
    {
        this.controller = controller;
        Port = port;
    }

    public void SetSpeed(int speed) => controller.SetSpeed(Port.Index, speed);

    public void SetPower(double power) => controller.SetPower(Port.Index, power);

    public void MoveTo(int position, int speed) => controller.MoveTo(Port.Index, position, speed);

    public void Coast() => controller.Coast(Port.Index);

    public void Brake() => controller.Brake(Port.Index);

    public void Hold() => controller.Hold(Port.Index);

    public void SetPowerLimit(double limit) => Port.SetPowerLimit(limit);

    public void SetPwmParameters(double threshold, double minimum) => controller.SetPwmParameters(Port.Index, threshold, minimum);

    public Task RunForSecondsAsync(double seconds, int speed, BuildHatStopMode stop = BuildHatStopMode.Coast, CancellationToken token = default) =>
        controller.RunForSecondsAsync(Port.Index, seconds, speed, stop, token);

    public Task RunForDegreesAsync(int degrees, int speed, BuildHatStopMode stop = BuildHatStopMode.Coast, CancellationToken token = default) =>
        controller.RunForDegreesAsync(Port.Index, degrees, speed, stop, token);

    public Task RunToPositionAsync(int position, int speed, BuildHatStopMode stop = BuildHatStopMode.Coast, CancellationToken token = default) =>
        controller.RunToPositionAsync(Port.Index, position, speed, stop, token);

    public Task RunToAbsolutePositionAsync(int degrees, int speed, BuildHatDirection direction = BuildHatDirection.Shortest, BuildHatStopMode stop = BuildHatStopMode.Coast, CancellationToken token = default) =>
        controller.RunToAbsolutePositionAsync(Port.Index, degrees, speed, direction, stop, token);
}
