using System.Globalization;

using Helper;

using Iot.Device.BuildHat;
using Iot.Device.Common;

LogDispatcher.LoggerFactory = new DebugLoggerFactory();

using var brick = new Brick("/dev/serial0");

// the sensor port is the port where the motor is connected, in this case, we use Port A
var sensorPort = "0";

// now, we need to send commands to the motor

// first, we send a power limit: this means we want to define the limit of the motor's power
brick.SendRawCommand($"port {sensorPort} ; plimit 1\r");

// second, we send the bias: Motor commands are amplified by 0.1 upwards or, in the case of negative commands, attenuated by 0.1. This can be helpful, for example, if the motor tends to “stick” in one direction or reacts unevenly.
brick.SendRawCommand($"port {sensorPort} ; bias 0\r");

// now we can start the engine:
var speed = 0.70; // this means. 70%

brick.SendRawCommand($"port {sensorPort} ; pwm ; set {speed.ToString(CultureInfo.InvariantCulture)}\r");

Console.ReadLine();
