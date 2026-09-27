namespace RaspberryDotNet.BuildHat;

using System.Globalization;

public sealed class BuildHatDistanceSensor
{
    private const int DistanceMode = 0;

    private const byte EyesHeader = 0xC5;

    private const int MaxBrightness = 100;

    private readonly BuildHatController controller;

    public BuildHatPort Port { get; }

    internal BuildHatDistanceSensor(BuildHatController controller, BuildHatPort port)
    {
        this.controller = controller;
        Port = port;
    }

    public async Task<int> ReadDistanceAsync(CancellationToken token = default)
    {
        controller.PrepareDistanceSensor(Port.Index);
        var values = await Port.ReadOnceAsync(DistanceMode, token).ConfigureAwait(false);
        if (values.Count < 1)
        {
            throw new IOException(String.Create(CultureInfo.InvariantCulture, $"Unexpected data. port=[{Port.Name}], mode=[{DistanceMode}], count=[{values.Count}]"));
        }

        return (int)values[0];
    }

    public void SetEyes(int rightUpper, int leftUpper, int rightLower, int leftLower)
    {
        ValidateBrightness(rightUpper, nameof(rightUpper));
        ValidateBrightness(leftUpper, nameof(leftUpper));
        ValidateBrightness(rightLower, nameof(rightLower));
        ValidateBrightness(leftLower, nameof(leftLower));

        controller.PrepareDistanceSensor(Port.Index);
        Port.Write([EyesHeader, (byte)rightUpper, (byte)leftUpper, (byte)rightLower, (byte)leftLower]);
    }

    public void Select()
    {
        controller.PrepareDistanceSensor(Port.Index);
        Port.SelectMode(DistanceMode);
    }

    public void Deselect() => Port.Deselect();

    private static void ValidateBrightness(int value, string name)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value, name);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxBrightness, name);
    }
}
