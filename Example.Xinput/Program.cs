using Gamepad;

using var pad = new GamepadController();

pad.ButtonChanged += (_, e) =>
{
    Console.WriteLine($"Button {e.Button} Changed: {e.Pressed}");
};
pad.AxisChanged += (_, e) =>
{
    Console.WriteLine($"Axis {e.Axis} Changed: {e.Value}");
};

Console.ReadLine();
