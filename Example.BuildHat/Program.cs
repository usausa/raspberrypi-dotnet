using RaspberryDotNet.BuildHat;

using var hat = new BuildHatController();
hat.PortChanged += static (_, e) => Console.WriteLine($"Port {e.Port.Name}: {e.DeviceType?.Name ?? "none"}, support=[{e.DeviceType?.Support}]");
hat.FaultDetected += static (_, e) => Console.WriteLine($"Fault: {e.Fault}");
hat.ConnectionLost += static (_, e) => Console.WriteLine($"Connection lost: {e.GetException().Message}");

hat.Open();
var status = hat.Status;
Console.WriteLine($"Firmware=[{status.Firmware}], loaded=[{status.FirmwareLoaded}]");
hat.SetLedMode(BuildHatLedMode.Green);

var drive = hat.GetMotor(0);
var steering = hat.GetMotor(1);
await hat.WaitForDeviceAsync(drive.Port.Index, TimeSpan.FromSeconds(15)).ConfigureAwait(false);
await hat.WaitForDeviceAsync(steering.Port.Index, TimeSpan.FromSeconds(15)).ConfigureAwait(false);
Console.WriteLine($"Voltage=[{hat.Status.Voltage}]");

await drive.RunForSecondsAsync(2, 20).ConfigureAwait(false);
await drive.RunForDegreesAsync(360, 30).ConfigureAwait(false);
Console.WriteLine($"Port {drive.Port.Name}: position=[{drive.State.Position}]");

int[] angles = [30, -30, 0];
foreach (var angle in angles)
{
    await steering.RunToAbsolutePositionAsync(angle, 50, stop: BuildHatStopMode.Hold).ConfigureAwait(false);
    Console.WriteLine($"Port {steering.Port.Name}: absolute=[{steering.State.AbsolutePosition}]");
}

steering.Coast();

if (hat.Ports[3].State.DeviceType is { Support: BuildHatDeviceSupport.ColorDistanceSensor })
{
    var sensor = hat.GetColorDistanceSensor(3);
    var color = await sensor.ReadColorAsync().ConfigureAwait(false);
    var distance = await sensor.ReadDistanceAsync().ConfigureAwait(false);
    var rgb = await sensor.ReadRgbAsync().ConfigureAwait(false);
    Console.WriteLine($"Port {sensor.Port.Name}: color=[{color}], distance=[{distance}], rgb=[{rgb}]");
}

hat.SetLedMode(BuildHatLedMode.VoltageDependent);
