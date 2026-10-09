# Raspberry Pi platform library for .NET

|Library|NuGet|
|:----|:----|
|RaspberryDotNet.SystemInfo|[![NuGet](https://img.shields.io/nuget/v/RaspberryDotNet.SystemInfo.svg)](https://www.nuget.org/packages/RaspberryDotNet.SystemInfo)|
|RaspberryDotNet.BuildHat|[![NuGet](https://img.shields.io/nuget/v/RaspberryDotNet.BuildHat.svg)](https://www.nuget.org/packages/RaspberryDotNet.BuildHat)|

# 🖥RaspberryDotNet.SystemInfo

## VCIO

```csharp
using var vcio = PlatformProvider.GetVcioMonitor();

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
```

### Link

* [Prometheus Exporter alternative](https://github.com/usausa/prometheus-exporter-alternative)

## GPIO

```csharp
using var gpio = PlatformProvider.GetGpioMonitor();

Console.WriteLine("PHYS  SOC  FUNC  LEVEL");
Console.WriteLine("----  ---  ----  -----");
foreach (var p in gpio.Pins)
{
    Console.WriteLine($"{p.PhysicalPin,4}  {p.SocPin,3}  {p.Function,-4}  {p.Level,5}");
}
```

# 🧱RaspberryDotNet.BuildHat

```csharp
using var hat = new BuildHatController();
hat.Open();

await hat.WaitForDeviceAsync(0, TimeSpan.FromSeconds(10));

// Motor
var motor = hat.GetMotor(0);
await motor.RunForDegreesAsync(360, 30);
await motor.RunToAbsolutePositionAsync(0, 50, BuildHatDirection.Shortest, BuildHatStopMode.Hold);

// Motor pair
var pair = hat.GetMotorPair(0, 2);
await pair.RunForDegreesAsync(360, 30, -30);

// Color & Distance Sensor
var sensor = hat.GetColorDistanceSensor(3);
Console.WriteLine($"color={await sensor.ReadColorAsync()}, distance={await sensor.ReadDistanceAsync()}");
```

|Support|Devices|Tested|
|:----|:----|:----|
|Motor|Technic Large / XL Motor, Technic Medium / Large Angular Motor, Small Angular Motor, Medium Linear Motor|✓|
|PassiveMotor|System Medium / Train / Turntable Motor, Technic Large / XL Motor (passive)|-|
|Light|Light|-|
|ColorDistanceSensor|Color & Distance Sensor|✓|
|ColorSensor|Color Sensor|-|
|DistanceSensor|Distance Sensor|-|
|ForceSensor|Force Sensor|-|
|LightMatrix|3x3 Color Light Matrix|-|
|TiltSensor|WeDo 2.0 Tilt Sensor|-|
|MotionSensor|WeDo 2.0 Motion Sensor|-|
|Generic (`BuildHatPort`)|Other active devices|-|
|Unsupported|Button and other passive devices|-|

- `Select` / `SelectMode` / `SelectCombi` stream the data into `Port.State.Values`, and reads of the selected mode return the latest value immediately
- Enable the serial port and disable the serial login shell with `raspi-config`
- Events are raised on the receive thread
- The firmware of [python-build-hat](https://github.com/RaspberryPiFoundation/python-build-hat) (MIT, `Firmware/LICENSE.txt`) is embedded and uploaded when needed

# 🌐Link

- [MacDotNet](https://github.com/usausa/mac-dotnet)
- [LinuxDotNet](https://github.com/usausa/linux-dotnet)
- [RaspberryDotNet](https://github.com/usausa/raspberrypi-dotnet)
- [Disk information library](https://github.com/usausa/hardwareinfo-disk)

# 🔢PIN memo

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
