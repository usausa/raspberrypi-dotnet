// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo
#pragma warning disable CA1308
namespace Example.SystemInfo;

using RaspberryDotNet.SystemInfo;

using Smart.CommandLine.Hosting;

public static class CommandBuilderExtensions
{
    public static void AddCommands(this ICommandBuilder commands)
    {
        commands.AddCommand<VcioCommand>();
        commands.AddCommand<GpioCommand>();
    }
}

//--------------------------------------------------------------------------------
// VCIO
//--------------------------------------------------------------------------------
[Command("vcio", "VCIO information")]
public sealed class VcioCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        using var vcio = PlatformProvider.GetVcioMonitor();
        if (!vcio.Supported)
        {
            Console.WriteLine("Failed to open /dev/vcio.");
            return ValueTask.CompletedTask;
        }

        // Temperature
        if (!Double.IsNaN(vcio.Temperature))
        {
            Console.WriteLine($"temp={vcio.Temperature:0.0}'C");
        }

        // Clock frequency
        foreach (var clock in vcio.Clocks)
        {
            Console.WriteLine($"frequency[{clock.Type.ToString().ToLowerInvariant()}]={clock.Frequency:0}");
        }

        // Voltage
        foreach (var voltage in vcio.Voltages)
        {
            Console.WriteLine($"volt[{voltage.Type.ToString().ToLowerInvariant()}]={voltage.Voltage:0.0000}V");
        }

        // Throttled
        if (vcio.Throttled != ThrottledFlags.Unknown)
        {
            Console.WriteLine($"throttled={vcio.Throttled}");
        }

        return ValueTask.CompletedTask;
    }
}

//--------------------------------------------------------------------------------
// GPIO
//--------------------------------------------------------------------------------
[Command("gpio", "GPIO information")]
public sealed class GpioCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        using var gpio = PlatformProvider.GetGpioMonitor();
        if (!gpio.Supported)
        {
            Console.WriteLine("Failed to open /dev/gpiomem.");
            return ValueTask.CompletedTask;
        }

        Console.WriteLine("PHYS  SOC  FUNC  LEVEL");
        Console.WriteLine("----  ---  ----  -----");
        foreach (var p in gpio.Pins)
        {
            Console.WriteLine($"{p.PhysicalPin,4}  {p.SocPin,3}  {p.Function,-4}  {p.Level,5}");
        }

        return ValueTask.CompletedTask;
    }
}
