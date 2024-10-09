// Create screen
using SkiaSharp;

using TuringSmartScreenLib;
using TuringSmartScreenLib.Helpers.SkiaSharp;

using var screen = ScreenFactory.Create(ScreenType.RevisionC, "/dev/ttyACM0");

screen.SetBrightness(100);

screen.Clear();

using var bitmap1 = SKBitmap.Decode("test1.png");
using var buffer1 = screen.CreateBufferFrom(bitmap1);
screen.DisplayBuffer(0, 0, buffer1);

for (var i = 0; i <= 100; i++)
{
    screen.SetBrightness((byte)i);
    Thread.Sleep(10);
}
