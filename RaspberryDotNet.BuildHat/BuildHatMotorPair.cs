namespace RaspberryDotNet.BuildHat;

public sealed class BuildHatMotorPair
{
    private readonly BuildHatController controller;

    public BuildHatPort Left { get; }

    public BuildHatPort Right { get; }

    internal BuildHatMotorPair(BuildHatController controller, BuildHatPort left, BuildHatPort right)
    {
        this.controller = controller;
        Left = left;
        Right = right;
    }

    public void SetSpeed(int left, int right) => controller.SetPairSpeed(Left.Index, left, Right.Index, right);

    public void Coast() => controller.CoastPair(Left.Index, Right.Index);

    public void Brake() => controller.BrakePair(Left.Index, Right.Index);

    public Task RunForSecondsAsync(double seconds, int left, int right, BuildHatStopMode stop = BuildHatStopMode.Coast, CancellationToken token = default) =>
        controller.RunPairForSecondsAsync(Left.Index, left, Right.Index, right, seconds, stop, token);

    public Task RunForDegreesAsync(int degrees, int left, int right, BuildHatStopMode stop = BuildHatStopMode.Coast, CancellationToken token = default) =>
        controller.RunPairForDegreesAsync(Left.Index, left, Right.Index, right, degrees, stop, token);
}
