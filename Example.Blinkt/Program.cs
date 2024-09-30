namespace Example.Blinkt;

using RaspberryDotNet.Blinkt;

public static class Program
{
    public static void Main()
    {
        using var controller = new BlinktController();

        controller.Open();
        controller.Show();

        controller.SetPixel(0, 0xFF, 0x00, 0x00);
        controller.SetPixel(1, 0x00, 0xFF, 0x00);
        controller.SetPixel(2, 0xFF, 0xFF, 0x00);
        controller.SetPixel(3, 0x00, 0x00, 0xFF);
        controller.SetPixel(4, 0xFF, 0x00, 0xFF);
        controller.SetPixel(5, 0x00, 0xFF, 0xFF);
        controller.SetPixel(6, 0xFF, 0xFF, 0xFF);
        controller.SetPixel(7, 0x00, 0x00, 0x00);
        controller.Show();

        Thread.Sleep(5000);

        controller.Clear();
        controller.Show();

        controller.Close();
    }
}
