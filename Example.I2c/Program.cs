using System.Device.I2c;

// Grove Base Hat /dev/i2c-1
const int busId = 1;
const int deviceAddress = 0x44;

var settings = new I2cConnectionSettings(busId, deviceAddress);
using var device = I2cDevice.Create(settings);

var data = new byte[6];
while (true)
{
    // Write: [0x2C, 0x06]
    device.Write([0x2C, 0x06]);

    Thread.Sleep(20);

    // Read: T(2) + CRC + RH(2) + CRC
    device.Read(data);

    var rawTemperature = (ushort)((data[0] << 8) | data[1]);
    var temperature = (float)(-45.0 + (175.0 * rawTemperature / 65535.0));

    var rawHumidity = (ushort)((data[3] << 8) | data[4]);
    var humidity = (float)(100.0 * rawHumidity / 65535.0);

    Console.WriteLine($"T={temperature:F2}C, H={humidity:F2}%");

    Thread.Sleep(1000);
}
