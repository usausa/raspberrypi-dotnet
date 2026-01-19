using System.Device.Gpio;

const int pin = 5;
using var controller = new GpioController();

controller.OpenPin(pin, PinMode.Output);
controller.Write(pin, PinValue.Low);

while (true)
{
    controller.Write(pin, PinValue.High);
    Thread.Sleep(1000);

    controller.Write(pin, PinValue.Low);
    Thread.Sleep(1000);
}
