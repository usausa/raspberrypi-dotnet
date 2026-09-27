namespace RaspberryDotNet.BuildHat;

public sealed class BuildHatPortEventArgs : EventArgs
{
    public BuildHatPort Port { get; }

    public BuildHatDeviceType? DeviceType { get; }

    public BuildHatPortEventArgs(BuildHatPort port, BuildHatDeviceType? deviceType)
    {
        Port = port;
        DeviceType = deviceType;
    }
}
