namespace RaspberryDotNet.SystemInfo;

using static RaspberryDotNet.SystemInfo.NativeMethods;

public sealed unsafe class Vcio : IDisposable
{
    private const string DevicePath = "/dev/vcio";

    private int fd = -1;

    public bool IsOpen => fd >= 0;

    public static bool IsSupported() => File.Exists(DevicePath);

    //------------------------------------------------------------------------
    // Open/Close
    //------------------------------------------------------------------------

    public void Dispose()
    {
        Close();
    }

    public void Open()
    {
        if (IsOpen)
        {
            return;
        }

        fd = open(DevicePath, O_RDWR);
    }

    public void Close()
    {
        if (fd >= 0)
        {
            _ = close(fd);
            fd = -1;
        }
    }

    //------------------------------------------------------------------------
    // Helper
    //------------------------------------------------------------------------

    private bool MailboxProperty(void* buffer)
    {
        if (!IsOpen)
        {
            throw new InvalidOperationException("VCIO is not open.");
        }

        var ret = ioctl(fd, IOCTL_MBOX_PROPERTY, (IntPtr)buffer);
        if (ret < 0)
        {
            return false;
        }

        // buf[1] bit31 set -> response success
        var buf = (uint*)buffer;
        return (buf[1] & 0x8000_0000u) != 0;
    }

    //------------------------------------------------------------------------
    // Readers
    //------------------------------------------------------------------------

    public double ReadTemperature()
    {
        var buf = stackalloc uint[8];
        buf[0] = 8u * 4u;          // total size bytes
        buf[1] = 0;                // request
        buf[2] = TAG_GET_TEMPERATURE;
        buf[3] = 8;                // value buffer size
        buf[4] = 4;                // request size
        buf[5] = TEMP_ID_SOC;      // temperature id
        buf[6] = 0;                // response: milli-C
        buf[7] = 0;                // end tag

        if (!MailboxProperty(buf))
        {
            return double.NaN;
        }

        var milliC = unchecked((int)buf[6]);
        return milliC / 1000.0;
    }

    public double ReadFrequency(ClockType clock, bool measured = true)
    {
        var buf = stackalloc uint[8];
        buf[0] = 8u * 4u;
        buf[1] = 0;
        buf[2] = measured ? TAG_GET_CLOCK_RATE_MEASURED : TAG_GET_CLOCK_RATE;
        buf[3] = 8;
        buf[4] = 4;
        buf[5] = (uint)clock;
        buf[6] = 0;
        buf[7] = 0;

        if (!MailboxProperty(buf))
        {
            return double.NaN;
        }

        return buf[6];
    }

    public double ReadVoltage(VoltageType domain)
    {
        var buf = stackalloc uint[8];
        buf[0] = 8u * 4u;
        buf[1] = 0;
        buf[2] = TAG_GET_VOLTAGE;
        buf[3] = 8;
        buf[4] = 4;
        buf[5] = (uint)domain;
        buf[6] = 0;        // response: microvolts
        buf[7] = 0;

        if (!MailboxProperty(buf))
        {
            return double.NaN;
        }

        var microVolts = buf[6];
        return microVolts / 1_000_000.0;
    }

    public ThrottledFlags ReadThrottled()
    {
        var buf = stackalloc uint[7];
        buf[0] = 7u * 4u;
        buf[1] = 0;
        buf[2] = TAG_GET_THROTTLED;
        buf[3] = 4;   // value buffer size
        buf[4] = 0;   // request size
        buf[5] = 0;   // response: flags
        buf[6] = 0;

        if (!MailboxProperty(buf))
        {
            return ThrottledFlags.Unknown;
        }

        return (ThrottledFlags)buf[5];
    }
}
