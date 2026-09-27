namespace RaspberryDotNet.BuildHat;

using System.Globalization;

public sealed class BuildHatColorDistanceSensor
{
    private readonly BuildHatController controller;

    public BuildHatPort Port { get; }

    internal BuildHatColorDistanceSensor(BuildHatController controller, BuildHatPort port)
    {
        this.controller = controller;
        Port = port;
    }

    public async Task<BuildHatColor> ReadColorAsync(CancellationToken token = default)
    {
        var values = await ReadAsync(BuildHatColorDistanceMode.Color, 1, token).ConfigureAwait(false);
        return (BuildHatColor)(int)values[0];
    }

    public async Task<int> ReadDistanceAsync(CancellationToken token = default)
    {
        var values = await ReadAsync(BuildHatColorDistanceMode.Distance, 1, token).ConfigureAwait(false);
        return (int)values[0];
    }

    public async Task<int> ReadCountAsync(CancellationToken token = default)
    {
        var values = await ReadAsync(BuildHatColorDistanceMode.Count, 1, token).ConfigureAwait(false);
        return (int)values[0];
    }

    public async Task<int> ReadReflectedLightAsync(CancellationToken token = default)
    {
        var values = await ReadAsync(BuildHatColorDistanceMode.ReflectedLight, 1, token).ConfigureAwait(false);
        return (int)values[0];
    }

    public async Task<int> ReadAmbientLightAsync(CancellationToken token = default)
    {
        var values = await ReadAsync(BuildHatColorDistanceMode.AmbientLight, 1, token).ConfigureAwait(false);
        return (int)values[0];
    }

    public async Task<BuildHatRgb> ReadRgbAsync(CancellationToken token = default)
    {
        var values = await ReadAsync(BuildHatColorDistanceMode.Rgb, 3, token).ConfigureAwait(false);
        return new BuildHatRgb((int)values[0], (int)values[1], (int)values[2]);
    }

    public void Select(BuildHatColorDistanceMode mode)
    {
        controller.PrepareColorDistanceSensor(Port.Index);
        Port.SelectMode(ToFirmwareMode(mode));
    }

    public void Deselect() => Port.Deselect();

    private async Task<IReadOnlyList<double>> ReadAsync(BuildHatColorDistanceMode mode, int count, CancellationToken token)
    {
        controller.PrepareColorDistanceSensor(Port.Index);
        var values = await Port.ReadOnceAsync(ToFirmwareMode(mode), token).ConfigureAwait(false);
        if (values.Count < count)
        {
            throw new IOException(String.Create(CultureInfo.InvariantCulture, $"Unexpected data. port=[{Port.Name}], mode=[{mode}], count=[{values.Count}]"));
        }

        return values;
    }

    private static int ToFirmwareMode(BuildHatColorDistanceMode mode) => mode switch
    {
        BuildHatColorDistanceMode.Color => 0,
        BuildHatColorDistanceMode.Distance => 1,
        BuildHatColorDistanceMode.Count => 2,
        BuildHatColorDistanceMode.ReflectedLight => 3,
        BuildHatColorDistanceMode.AmbientLight => 4,
        BuildHatColorDistanceMode.Rgb => 6,
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };
}
