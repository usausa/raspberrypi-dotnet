# raspberrypi-dotnet

# RaspberryDotNet.SystemInfo

## VCIO

```csharp
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
```

## GPIO

```csharp
using var gpio = new GpioMap();
gpio.Open();

Console.WriteLine("PHYS  SOC  FUNC  LEVEL");
Console.WriteLine("----  ---  ----  -----");
foreach (var p in gpio.ReadHeaderGpioPins().OrderBy(x => x.PhysicalPin))
{
    Console.WriteLine($"{p.PhysicalPin,4}  {p.SocPin,3}  {p.Function,-4}  {p.Level,5}");
}
```

# PIN memo

|Pin|Signal|BCM(GPIO)|
|:----|:----|:----|
|1|3.3V|-|
|2|5V|-|
|3|SDA1 (I2C)|GPIO2|
|4|5V|-|
|5|SCL1 (I2C)|GPIO3|
|6|GND|-|
|7|GPIO|GPIO4|
|8|TXD0 (UART)|GPIO14|
|9|GND|-|
|10|RXD0 (UART)|GPIO15|
|11|GPIO|GPIO17|
|12|PWM0 / GPIO|GPIO18|
|13|GPIO|GPIO27|
|14|GND|-|
|15|GPIO|GPIO22|
|16|GPIO|GPIO23|
|17|3.3V|-|
|18|GPIO|GPIO24|
|19|MOSI (SPI0)|GPIO10|
|20|GND|-|
|21|MISO (SPI0)|GPIO9|
|22|GPIO|GPIO25|
|23|SCLK (SPI0)|GPIO11|
|24|CE0 (SPI0)|GPIO8|
|25|GND|-|
|26|CE1 (SPI0)|GPIO7|
|27|SDA0 (I2C0 / ID_SD)|GPIO0|
|28|SCL0 (I2C0 / ID_SC)|GPIO1|
|29|GPIO|GPIO5|
|30|GND|-|
|31|GPIO|GPIO6|
|32|PWM0 / GPIO|GPIO12|
|33|PWM1 / GPIO|GPIO13|
|34|GND|-|
|35|PWM1 / GPIO|GPIO19|
|36|GPIO|GPIO16|
|37|GPIO|GPIO26|
|38|GPIO|GPIO20|
|39|GND|-|
|40|GPIO|GPIO21|
