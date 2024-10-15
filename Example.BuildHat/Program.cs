using System.Diagnostics;

using Gamepad;

using var pad = new GamepadController();

var fps = 0;
var watch = Stopwatch.StartNew();
using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(1000d / 60));
while (await timer.WaitForNextTickAsync().ConfigureAwait(false))
{
    fps++;

    if (watch.ElapsedMilliseconds > 1000)
    {
        Console.WriteLine(fps);
        fps = 0;
        watch.Restart();
    }
}
