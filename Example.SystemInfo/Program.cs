#pragma warning disable CA1308
using RaspberryDotNet.SystemInfo;

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
