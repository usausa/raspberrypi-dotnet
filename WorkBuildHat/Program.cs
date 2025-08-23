using Helper;

using Iot.Device.BuildHat;
using Iot.Device.BuildHat.Models;
using Iot.Device.BuildHat.Motors;
using Iot.Device.Common;

LogDispatcher.LoggerFactory = new DebugLoggerFactory();

using var brick = new Brick("/dev/serial0");
Thread.Sleep(2000);

Console.WriteLine();
Console.WriteLine("----------------------------------------");
Console.WriteLine("Connect");
Console.WriteLine("----------------------------------------");
Console.WriteLine();

brick.WaitForSensorToConnect(SensorPort.PortA);

Console.WriteLine();
Console.WriteLine("----------------------------------------");
Console.WriteLine("Connected");
Console.WriteLine("----------------------------------------");
Console.WriteLine();

var motor = (ActiveMotor)brick.GetMotor(SensorPort.PortA);

Console.WriteLine();
Console.WriteLine("----------------------------------------");
Console.WriteLine("Set speed");
Console.WriteLine("----------------------------------------");
Console.WriteLine();

motor.SetPowerLimit(0.7);
motor.SetBias(0.3);
motor.TargetSpeed = 50;

Console.WriteLine();
Console.WriteLine("----------------------------------------");
Console.WriteLine("MoveForSeconds");
Console.WriteLine("----------------------------------------");
Console.WriteLine();

motor.MoveForSeconds(5);

Console.ReadLine();
