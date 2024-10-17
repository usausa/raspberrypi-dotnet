using Iot.Device.BuildHat;

using Brick brick = new("/dev/serial0");
var info = brick.BuildHatInformation;

// Information
Console.WriteLine($"Version: {info.Version}, Firmware date: {info.FirmwareDate}, Signature:");
Console.WriteLine($"{BitConverter.ToString(info.Signature)}");
Console.WriteLine($"VIn = {brick.InputVoltage.Volts} V");

// TODO Devices

// TODO Motor
