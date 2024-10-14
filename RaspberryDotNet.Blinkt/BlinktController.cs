namespace RaspberryDotNet.Blinkt;

using System.Device.Gpio;

public sealed class BlinktController : IDisposable
{
    public const int LedCount = 8;

    private const byte DefaultBrightness = 0b00000111;

    private const int MasterOutSlaveIn = 23;
    private const int SerialClock = 24;

    private readonly GpioController controller = new();

    private readonly uint[] buffer = new uint[LedCount];

    private bool opened;

    public bool IsOpen => opened;

    public void Dispose()
    {
        if (IsOpen)
        {
            Close();
        }

        controller.Dispose();
    }

    public void Open()
    {
        if (IsOpen)
        {
            return;
        }

        controller.OpenPin(MasterOutSlaveIn, PinMode.Output);
        controller.Write(MasterOutSlaveIn, PinValue.Low);
        controller.OpenPin(SerialClock, PinMode.Output);
        controller.Write(SerialClock, PinValue.Low);

        Clear();

        opened = true;
    }

    public void Close()
    {
        controller.ClosePin(MasterOutSlaveIn);
        controller.ClosePin(SerialClock);

        opened = false;
    }

    public void Clear()
    {
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = DefaultBrightness;
        }
    }

    public void SetPixel(int led, byte red, byte green, byte blue, byte brightness = DefaultBrightness)
    {
        buffer[led] = ((uint)red << 24) +
                      ((uint)green << 16) +
                      ((uint)blue << 8) +
                      (uint)(brightness & 0b11111);
    }

    public void Show()
    {
        if (!IsOpen)
        {
            return;
        }

        WriteByte(0);
        WriteByte(0);
        WriteByte(0);
        WriteByte(0);

        foreach (var value in buffer)
        {
            WriteByte((byte)(0b11100000 | (value & 0b11111)));
            WriteByte((byte)((value >> 8) & 0xFF));
            WriteByte((byte)((value >> 16) & 0xFF));
            WriteByte((byte)((value >> 24) & 0xFF));
        }

        WriteByte(0);
    }

    private void WriteByte(byte b)
    {
        for (var i = 0; i < 8; i++)
        {
            controller.Write(MasterOutSlaveIn, (b & (1 << (7 - i))) != 0 ? PinValue.High : PinValue.Low);
            controller.Write(SerialClock, PinValue.High);
            controller.Write(SerialClock, PinValue.Low);
        }
    }
}
