namespace RaspberryDotNet.BuildHat;

public sealed class BuildHatPort
{
    private readonly BuildHatController controller;

    public int Index { get; }

    public char Name => (char)('A' + Index);

    public BuildHatPortState State => controller.GetState(Index);

    internal BuildHatPort(BuildHatController controller, int index)
    {
        this.controller = controller;
        Index = index;
    }

    public void SelectMode(int mode) => controller.SelectMode(Index, mode);

    public void SelectCombi(params int[] modes) => controller.SelectCombi(Index, modes);

    public void Deselect() => controller.Deselect(Index);

    public Task<IReadOnlyList<double>> ReadOnceAsync(int mode, CancellationToken token = default) => controller.ReadOnceAsync(Index, mode, token);

    public void Write(ReadOnlySpan<byte> data) => controller.WriteData(Index, data);

    public void PowerOn() => controller.PowerOn(Index);

    public void PowerOff() => controller.PowerOff(Index);

    public void SetPowerLimit(double limit) => controller.SetPowerLimit(Index, limit);
}
