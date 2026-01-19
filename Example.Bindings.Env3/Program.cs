using System.Device.I2c;

using Iot.Device.Sht3x;

const int busId = 1;
const int sht30Address = 0x44;

using var shtI2C = I2cDevice.Create(new I2cConnectionSettings(busId, sht30Address));
using var sht = new Sht3x(shtI2C);

while (true)
{
    var t = sht.Temperature.DegreesCelsius;
    var h = sht.Humidity.Percent;

    Console.WriteLine($"T={t:F2}C, H={h:F2}%");
    Thread.Sleep(1000);
}
