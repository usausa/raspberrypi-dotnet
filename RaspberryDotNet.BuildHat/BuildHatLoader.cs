namespace RaspberryDotNet.BuildHat;

using System.Device.Gpio;
using System.Diagnostics;
using System.Globalization;

internal static class BuildHatLoader
{
    private const string FirmwarePrefix = "Firmware version: ";

    private const string BootloaderPrefix = "BuildHAT bootloader version";

    private const string InitializedMessage = "Done initialising ports";

    private const int VersionRetry = 5;

    private const int BlockSize = 1024;

    private static readonly TimeSpan VersionTimeout = TimeSpan.FromSeconds(1);

    private static readonly TimeSpan PromptTimeout = TimeSpan.FromSeconds(5);

    private static readonly TimeSpan InitializeTimeout = TimeSpan.FromSeconds(10);

    private static readonly TimeSpan LoadWait = TimeSpan.FromMilliseconds(100);

    private static readonly TimeSpan ResetPulse = TimeSpan.FromMilliseconds(10);

    private static readonly TimeSpan ResetWait = TimeSpan.FromMilliseconds(500);

    private static readonly TimeSpan ReadTimeout = TimeSpan.FromMilliseconds(500);

    public static (string Version, bool Loaded) Initialize(SerialDevice port, BuildHatOptions options, CancellationToken token)
    {
        var image = options.Firmware ?? BuildHatFirmware.Default;
        var line = ReadVersion(port, token);
        var loaded = false;
        if (!line.StartsWith(FirmwarePrefix, StringComparison.Ordinal))
        {
            LoadFirmware(port, image, token);
            loaded = true;
        }
        else if (ParseVersion(line) != image.Version)
        {
            Reset(options);
            port.DiscardInput();
            LoadFirmware(port, image, token);
            loaded = true;
        }

        if (loaded)
        {
            line = ReadVersion(port, token);
            if (!line.StartsWith(FirmwarePrefix, StringComparison.Ordinal))
            {
                throw new IOException("Build HAT firmware is not running.");
            }
        }

        return (line[FirmwarePrefix.Length..], loaded);
    }

    private static void Reset(BuildHatOptions options)
    {
        using var controller = new GpioController();
        controller.OpenPin(options.Boot0Pin, PinMode.Output, PinValue.Low);
        controller.OpenPin(options.ResetPin, PinMode.Output, PinValue.Low);
        Thread.Sleep(ResetPulse);
        controller.Write(options.ResetPin, PinValue.High);
        Thread.Sleep(ResetPulse);
        controller.SetPinMode(options.ResetPin, PinMode.InputPullUp);
        controller.SetPinMode(options.Boot0Pin, PinMode.InputPullDown);
        controller.ClosePin(options.ResetPin);
        controller.ClosePin(options.Boot0Pin);
        Thread.Sleep(ResetWait);
    }

    private static string ReadVersion(SerialDevice port, CancellationToken token)
    {
        for (var i = 0; i < VersionRetry; i++)
        {
            port.Write("version\r");
            if (WaitForLine(port, static x => x.StartsWith(FirmwarePrefix, StringComparison.Ordinal) || x.Contains(BootloaderPrefix, StringComparison.Ordinal), VersionTimeout, token) is { } line)
            {
                return line;
            }
        }

        throw new IOException("Build HAT does not respond.");
    }

    private static long ParseVersion(string line)
    {
        var text = line.AsSpan(FirmwarePrefix.Length).Trim();
        var index = text.IndexOf(' ');
        if (index >= 0)
        {
            text = text[..index];
        }

        return Int64.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var version) ? version : -1;
    }

    private static void LoadFirmware(SerialDevice port, BuildHatFirmware image, CancellationToken token)
    {
        port.Write("clear\r");
        WaitForPrompt(port, token);
        port.Write(String.Create(CultureInfo.InvariantCulture, $"load {image.Data.Length} {Checksum(image.Data.Span)}\r"));
        token.WaitHandle.WaitOne(LoadWait);
        WriteBlock(port, image.Data.Span);
        WaitForPrompt(port, token);
        port.Write(String.Create(CultureInfo.InvariantCulture, $"signature {image.Signature.Length}\r"));
        token.WaitHandle.WaitOne(LoadWait);
        WriteBlock(port, image.Signature.Span);
        WaitForPrompt(port, token);
        port.Write("reboot\r");
        if (WaitForLine(port, static x => x.Contains(InitializedMessage, StringComparison.Ordinal), InitializeTimeout, token) is null)
        {
            throw new IOException("Build HAT does not finish initializing.");
        }
    }

    private static string? WaitForLine(SerialDevice port, Func<string, bool> predicate, TimeSpan timeout, CancellationToken token)
    {
        var timestamp = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(timestamp) < timeout)
        {
            token.ThrowIfCancellationRequested();
            if ((port.ReadLine(ReadTimeout) is { } line) && predicate(line))
            {
                return line;
            }
        }

        return null;
    }

    private static void WaitForPrompt(SerialDevice port, CancellationToken token)
    {
        var timestamp = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(timestamp) < PromptTimeout)
        {
            token.ThrowIfCancellationRequested();
            if (port.ReadByte(ReadTimeout) == '>')
            {
                return;
            }
        }

        throw new IOException("Build HAT bootloader does not respond.");
    }

    private static void WriteBlock(SerialDevice port, ReadOnlySpan<byte> data)
    {
        port.Write([0x02]);
        for (var offset = 0; offset < data.Length; offset += BlockSize)
        {
            port.Write(data.Slice(offset, Math.Min(BlockSize, data.Length - offset)));
        }

        port.Write([0x03, 0x0D]);
    }

    private static uint Checksum(ReadOnlySpan<byte> data)
    {
        var sum = 1u;
        foreach (var value in data)
        {
            sum = (sum & 0x80000000u) != 0 ? (sum << 1) ^ 0x1D872B41u : sum << 1;
            sum ^= value;
        }

        return sum;
    }
}
