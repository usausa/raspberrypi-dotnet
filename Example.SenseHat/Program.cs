using System.Drawing;

using Iot.Device.SenseHat;

using var hat = new SenseHat();

var n = 0;
using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
while (await timer.WaitForNextTickAsync().ConfigureAwait(false))
{
    hat.Fill((n / 10) % 2 == 0 ? Color.DarkBlue : Color.DarkRed);

    hat.ReadJoystickState();
    var u = hat.HoldingUp ? "U" : "-";
    var d = hat.HoldingDown ? "D" : "-";
    var l = hat.HoldingLeft ? "L" : "-";
    var r = hat.HoldingRight ? "R" : "-";
    var b = hat.HoldingButton ? "B" : "-";

    Console.WriteLine(
        $"Stick=[{u}{d}{l}{r}{b}], " +
        $"Temperature=[{hat.Temperature.Value:F2}], " +
        $"Temperature2=[{hat.Temperature2.Value:F2}], " +
        $"Humidity=[{hat.Humidity.Percent:F2}], " +
        $"Pressure=[{hat.Pressure.Hectopascals:F2}], " +
        $"Acceleration=[{hat.Acceleration.X:F2}, {hat.Acceleration.Y:F2}, {hat.Acceleration.Z:F2}], " +
        $"Angular=[{hat.AngularRate.X:F2}, {hat.AngularRate.Y:F2}, {hat.AngularRate.Z:F2}], " +
        $"Magnetic=[{hat.MagneticInduction.X:F2}, {hat.MagneticInduction.Y:F2}, {hat.MagneticInduction.Z:F2}]");

    n++;
}
