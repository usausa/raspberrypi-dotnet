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
        using var vcio = new Vcio();
        vcio.Open();

        // Temperature
        var temp = vcio.ReadTemperature();
        if (!Double.IsNaN(temp))
        {
            Console.WriteLine($"temp={temp:0.0}'C");
        }

        // Clock frequency
        foreach (var clock in Enum.GetValues<ClockType>())
        {
            var frequency = vcio.ReadFrequency(clock, measured: true);
            if (Double.IsNaN(frequency))
            {
                frequency = vcio.ReadFrequency(clock, measured: false);
            }

            if (!Double.IsNaN(frequency))
            {
                Console.WriteLine($"frequency[{clock.ToString().ToLowerInvariant()}]={frequency:0}");
            }
        }

        // Voltage
        foreach (var voltage in Enum.GetValues<VoltageType>())
        {
            var volt = vcio.ReadVoltage(voltage);
            if (!Double.IsNaN(volt))
            {
                Console.WriteLine($"volt[{voltage.ToString().ToLowerInvariant()}]={volt:0.0000}V");
            }
        }

        // Throttled
        var throttled = vcio.ReadThrottled();
        if (throttled != ThrottledFlags.Unknown)
        {
            Console.WriteLine($"throttled={throttled}");
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
        using var gpio = new GpioMap();
        gpio.Open();

        Console.WriteLine("PHYS  SOC  FUNC  LEVEL");
        Console.WriteLine("----  ---  ----  -----");
        foreach (var p in gpio.ReadHeaderGpioPins().OrderBy(x => x.PhysicalPin))
        {
            Console.WriteLine($"{p.PhysicalPin,4}  {p.SocPin,3}  {p.Function,-4}  {p.Level,5}");
        }

        return ValueTask.CompletedTask;
    }
}
