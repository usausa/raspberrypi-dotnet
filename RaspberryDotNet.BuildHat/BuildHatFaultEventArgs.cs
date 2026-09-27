namespace RaspberryDotNet.BuildHat;

public sealed class BuildHatFaultEventArgs : EventArgs
{
    public BuildHatFault Fault { get; }

    public BuildHatFaultEventArgs(BuildHatFault fault)
    {
        Fault = fault;
    }
}
