namespace RaspberryDotNet.SystemInfo;

public static class PlatformProvider
{
    //------------------------------------------------------------------------
    // VideoCore
    //------------------------------------------------------------------------

    public static VcioMonitor GetVcioMonitor() => VcioMonitor.Create();

    //------------------------------------------------------------------------
    // GPIO
    //------------------------------------------------------------------------

    public static GpioMonitor GetGpioMonitor() => GpioMonitor.Create();
}
