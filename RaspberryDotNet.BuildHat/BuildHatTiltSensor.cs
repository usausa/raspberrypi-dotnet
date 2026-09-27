namespace RaspberryDotNet.BuildHat;

using System.Globalization;

public sealed class BuildHatTiltSensor
{
    private const int TiltMode = 0;

    private readonly BuildHatController controller;

    public BuildHatPort Port { get; }

    internal BuildHatTiltSensor(BuildHatController controller, BuildHatPort port)
    {
        this.controller = controller;
        Port = port;
    }

    public async Task<BuildHatTilt> ReadTiltAsync(CancellationToken token = default)
    {
        controller.PrepareTiltSensor(Port.Index);
        var values = await Port.ReadOnceAsync(TiltMode, token).ConfigureAwait(false);
        if (values.Count < 2)
        {
            throw new IOException(String.Create(CultureInfo.InvariantCulture, $"Unexpected data. port=[{Port.Name}], mode=[{TiltMode}], count=[{values.Count}]"));
        }

        return new BuildHatTilt((int)values[0], (int)values[1]);
    }

    public void Select()
    {
        controller.PrepareTiltSensor(Port.Index);
        Port.SelectMode(TiltMode);
    }

    public void Deselect() => Port.Deselect();
}
