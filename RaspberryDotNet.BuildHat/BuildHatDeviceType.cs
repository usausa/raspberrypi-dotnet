namespace RaspberryDotNet.BuildHat;

using System.Globalization;

public sealed record BuildHatDeviceType(int Id, string Name, bool IsActive, bool IsMotor, bool HasAbsolutePosition)
{
    private const int LightId = 0x08;

    private const int TiltSensorId = 0x22;

    private const int MotionSensorId = 0x23;

    private const int ColorDistanceSensorId = 0x25;

    private const int ColorSensorId = 0x3D;

    private const int DistanceSensorId = 0x3E;

    private const int ForceSensorId = 0x3F;

    private const int LightMatrixId = 0x40;

    private static readonly Dictionary<int, BuildHatDeviceType> PassiveTypes = new()
    {
        [0x01] = new(0x01, "System Medium Motor", false, true, false),
        [0x02] = new(0x02, "System Train Motor", false, true, false),
        [0x03] = new(0x03, "System Turntable Motor", false, true, false),
        [0x05] = new(0x05, "Button", false, false, false),
        [0x06] = new(0x06, "Technic Large Motor", false, true, false),
        [0x07] = new(0x07, "Technic XL Motor", false, true, false),
        [0x08] = new(0x08, "Light", false, false, false)
    };

    private static readonly Dictionary<int, BuildHatDeviceType> ActiveTypes = new()
    {
        [0x22] = new(0x22, "WeDo 2.0 Tilt Sensor", true, false, false),
        [0x23] = new(0x23, "WeDo 2.0 Motion Sensor", true, false, false),
        [0x25] = new(0x25, "Color & Distance Sensor", true, false, false),
        [0x26] = new(0x26, "Medium Linear Motor", true, true, false),
        [0x2E] = new(0x2E, "Technic Large Motor", true, true, true),
        [0x2F] = new(0x2F, "Technic XL Motor", true, true, true),
        [0x30] = new(0x30, "Technic Medium Angular Motor (Cyan)", true, true, true),
        [0x31] = new(0x31, "Technic Large Angular Motor (Cyan)", true, true, true),
        [0x3D] = new(0x3D, "Color Sensor", true, false, false),
        [0x3E] = new(0x3E, "Distance Sensor", true, false, false),
        [0x3F] = new(0x3F, "Force Sensor", true, false, false),
        [0x40] = new(0x40, "3x3 Color Light Matrix", true, false, false),
        [0x41] = new(0x41, "Small Angular Motor", true, true, true),
        [0x4B] = new(0x4B, "Technic Medium Angular Motor (Grey)", true, true, true),
        [0x4C] = new(0x4C, "Technic Large Angular Motor (Grey)", true, true, true)
    };

    public BuildHatDeviceSupport Support => this switch
    {
        { IsActive: false, IsMotor: true } => BuildHatDeviceSupport.PassiveMotor,
        { IsActive: false, Id: LightId } => BuildHatDeviceSupport.Light,
        { IsActive: false } => BuildHatDeviceSupport.Unsupported,
        { IsMotor: true } => BuildHatDeviceSupport.Motor,
        { Id: TiltSensorId } => BuildHatDeviceSupport.TiltSensor,
        { Id: MotionSensorId } => BuildHatDeviceSupport.MotionSensor,
        { Id: ColorDistanceSensorId } => BuildHatDeviceSupport.ColorDistanceSensor,
        { Id: ColorSensorId } => BuildHatDeviceSupport.ColorSensor,
        { Id: DistanceSensorId } => BuildHatDeviceSupport.DistanceSensor,
        { Id: ForceSensorId } => BuildHatDeviceSupport.ForceSensor,
        { Id: LightMatrixId } => BuildHatDeviceSupport.LightMatrix,
        _ => BuildHatDeviceSupport.Generic
    };

    public static BuildHatDeviceType Find(int id, bool active)
    {
        var types = active ? ActiveTypes : PassiveTypes;
        return types.TryGetValue(id, out var type)
            ? type
            : new BuildHatDeviceType(id, String.Create(CultureInfo.InvariantCulture, $"{(active ? "Active" : "Passive")} device 0x{id:X2}"), active, false, false);
    }
}
