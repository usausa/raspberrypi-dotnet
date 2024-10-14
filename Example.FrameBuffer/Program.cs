using System.Drawing;

using RaspberryDotNet.FrameBuffer;

using var controller = new FrameBufferController("/dev/fb0", new PixelBufferBgr353(8, 8));

controller.Open();
controller.Buffer.Clear();
controller.Update();

controller.Buffer.Clear(Color.Red);
controller.Update();
Thread.Sleep(3000);

controller.Buffer.Clear(Color.Green);
controller.Update();
Thread.Sleep(3000);

controller.Buffer.Clear(Color.Blue);
controller.Update();
Thread.Sleep(3000);

for (var x = 0; x < 8; x++)
{
    var b = (byte)((16 * (x + 1)) - 1);
    controller.Buffer.SetPixel(x, 0, b, 0, 0);
    controller.Buffer.SetPixel(x, 1, 0, b, 0);
    controller.Buffer.SetPixel(x, 2, b, b, 0);
    controller.Buffer.SetPixel(x, 3, 0, 0, b);
    controller.Buffer.SetPixel(x, 4, b, 0, b);
    controller.Buffer.SetPixel(x, 5, 0, b, b);
    controller.Buffer.SetPixel(x, 6, b, b, b);
    controller.Buffer.SetPixel(x, 7, 0, 0, 0);
}
controller.Update();
Thread.Sleep(3000);
