namespace RaspberryDotNet.BuildHat;

using System.Globalization;
using System.Text;

public sealed class BuildHatFirmware
{
    private static readonly Lazy<BuildHatFirmware> DefaultFirmware = new(LoadDefault);

    public static BuildHatFirmware Default => DefaultFirmware.Value;

    public long Version { get; }

    public ReadOnlyMemory<byte> Data { get; }

    public ReadOnlyMemory<byte> Signature { get; }

    public BuildHatFirmware(long version, ReadOnlyMemory<byte> data, ReadOnlyMemory<byte> signature)
    {
        Version = version;
        Data = data;
        Signature = signature;
    }

    private static BuildHatFirmware LoadDefault()
    {
        var version = Int64.Parse(Encoding.ASCII.GetString(ReadResource("version")).Trim(), NumberStyles.None, CultureInfo.InvariantCulture);
        return new BuildHatFirmware(version, ReadResource("firmware.bin"), ReadResource("signature.bin"));
    }

    private static byte[] ReadResource(string name)
    {
        using var stream = typeof(BuildHatFirmware).Assembly.GetManifestResourceStream("RaspberryDotNet.BuildHat." + name) ??
                           throw new InvalidOperationException($"Build HAT resource is not found. name=[{name}]");
        var buffer = new byte[stream.Length];
        stream.ReadExactly(buffer);
        return buffer;
    }
}
