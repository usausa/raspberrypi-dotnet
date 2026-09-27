namespace RaspberryDotNet.BuildHat;

using System.Globalization;

public sealed class BuildHatForceSensor
{
    private readonly BuildHatController controller;

    public BuildHatPort Port { get; }

    internal BuildHatForceSensor(BuildHatController controller, BuildHatPort port)
    {
        this.controller = controller;
        Port = port;
    }

    public Task<int> ReadForceAsync(CancellationToken token = default) => ReadAsync(BuildHatForceSensorMode.Force, token);

    public async Task<bool> ReadPressedAsync(CancellationToken token = default)
    {
        var value = await ReadAsync(BuildHatForceSensorMode.Pressed, token).ConfigureAwait(false);
        return value == 1;
    }

    public Task<int> ReadPeakForceAsync(CancellationToken token = default) => ReadAsync(BuildHatForceSensorMode.PeakForce, token);

    public void Select(BuildHatForceSensorMode mode)
    {
        controller.PrepareForceSensor(Port.Index);
        Port.SelectMode(ToFirmwareMode(mode));
    }

    public void Deselect() => Port.Deselect();

    private async Task<int> ReadAsync(BuildHatForceSensorMode mode, CancellationToken token)
    {
        controller.PrepareForceSensor(Port.Index);
        var values = await Port.ReadOnceAsync(ToFirmwareMode(mode), token).ConfigureAwait(false);
        if (values.Count < 1)
        {
            throw new IOException(String.Create(CultureInfo.InvariantCulture, $"Unexpected data. port=[{Port.Name}], mode=[{mode}], count=[{values.Count}]"));
        }

        return (int)values[0];
    }

    private static int ToFirmwareMode(BuildHatForceSensorMode mode) => mode switch
    {
        BuildHatForceSensorMode.Force => 0,
        BuildHatForceSensorMode.Pressed => 1,
        BuildHatForceSensorMode.PeakForce => 3,
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };
}
