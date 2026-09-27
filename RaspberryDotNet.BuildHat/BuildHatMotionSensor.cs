namespace RaspberryDotNet.BuildHat;

using System.Globalization;

public sealed class BuildHatMotionSensor
{
    private readonly BuildHatController controller;

    public BuildHatPort Port { get; }

    internal BuildHatMotionSensor(BuildHatController controller, BuildHatPort port)
    {
        this.controller = controller;
        Port = port;
    }

    public Task<int> ReadDistanceAsync(CancellationToken token = default) => ReadAsync(BuildHatMotionSensorMode.Distance, token);

    public Task<int> ReadCountAsync(CancellationToken token = default) => ReadAsync(BuildHatMotionSensorMode.Count, token);

    public void Select(BuildHatMotionSensorMode mode)
    {
        controller.PrepareMotionSensor(Port.Index);
        Port.SelectMode(ToFirmwareMode(mode));
    }

    public void Deselect() => Port.Deselect();

    private async Task<int> ReadAsync(BuildHatMotionSensorMode mode, CancellationToken token)
    {
        controller.PrepareMotionSensor(Port.Index);
        var values = await Port.ReadOnceAsync(ToFirmwareMode(mode), token).ConfigureAwait(false);
        if (values.Count < 1)
        {
            throw new IOException(String.Create(CultureInfo.InvariantCulture, $"Unexpected data. port=[{Port.Name}], mode=[{mode}], count=[{values.Count}]"));
        }

        return (int)values[0];
    }

    private static int ToFirmwareMode(BuildHatMotionSensorMode mode) => mode switch
    {
        BuildHatMotionSensorMode.Distance => 0,
        BuildHatMotionSensorMode.Count => 1,
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };
}
