namespace RaspberryDotNet.BuildHat;

using System.Globalization;

public sealed class BuildHatColorSensor
{
    private readonly BuildHatController controller;

    public BuildHatPort Port { get; }

    internal BuildHatColorSensor(BuildHatController controller, BuildHatPort port)
    {
        this.controller = controller;
        Port = port;
    }

    public async Task<int> ReadReflectedLightAsync(CancellationToken token = default)
    {
        var values = await ReadAsync(BuildHatColorSensorMode.ReflectedLight, 1, token).ConfigureAwait(false);
        return (int)values[0];
    }

    public async Task<int> ReadAmbientLightAsync(CancellationToken token = default)
    {
        var values = await ReadAsync(BuildHatColorSensorMode.AmbientLight, 1, token).ConfigureAwait(false);
        return (int)values[0];
    }

    public async Task<BuildHatRgb> ReadRgbAsync(CancellationToken token = default)
    {
        var values = await ReadAsync(BuildHatColorSensorMode.Rgb, 3, token).ConfigureAwait(false);
        return new BuildHatRgb((int)values[0], (int)values[1], (int)values[2]);
    }

    public async Task<BuildHatHsv> ReadHsvAsync(CancellationToken token = default)
    {
        var values = await ReadAsync(BuildHatColorSensorMode.Hsv, 3, token).ConfigureAwait(false);
        return new BuildHatHsv((int)values[0], (int)values[1], (int)values[2]);
    }

    public void Select(BuildHatColorSensorMode mode)
    {
        controller.PrepareColorSensor(Port.Index);
        Port.SelectMode(ToFirmwareMode(mode));
    }

    public void Deselect() => Port.Deselect();

    private async Task<IReadOnlyList<double>> ReadAsync(BuildHatColorSensorMode mode, int count, CancellationToken token)
    {
        controller.PrepareColorSensor(Port.Index);
        var values = await Port.ReadOnceAsync(ToFirmwareMode(mode), token).ConfigureAwait(false);
        if (values.Count < count)
        {
            throw new IOException(String.Create(CultureInfo.InvariantCulture, $"Unexpected data. port=[{Port.Name}], mode=[{mode}], count=[{values.Count}]"));
        }

        return values;
    }

    private static int ToFirmwareMode(BuildHatColorSensorMode mode) => mode switch
    {
        BuildHatColorSensorMode.ReflectedLight => 1,
        BuildHatColorSensorMode.AmbientLight => 2,
        BuildHatColorSensorMode.Rgb => 5,
        BuildHatColorSensorMode.Hsv => 6,
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };
}
