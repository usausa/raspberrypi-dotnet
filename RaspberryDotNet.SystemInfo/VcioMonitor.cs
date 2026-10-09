namespace RaspberryDotNet.SystemInfo;

using static RaspberryDotNet.SystemInfo.NativeMethods;

public sealed class VcioClock
{
    public ClockType Type { get; }

    public bool Measured { get; }

    public double Frequency { get; internal set; }

    internal VcioClock(ClockType type, bool measured)
    {
        Type = type;
        Measured = measured;
    }
}

public sealed class VcioVoltage
{
    public VoltageType Type { get; }

    public double Voltage { get; internal set; }

    internal VcioVoltage(VoltageType type)
    {
        Type = type;
    }
}

public sealed class VcioMonitor : IDisposable
{
    private const string DevicePath = "/dev/vcio";

    private readonly VcioClock[] clocks;

    private readonly VcioVoltage[] voltages;

    private int fd;

    private bool disposed;

    public bool Supported { get; }

    public DateTime UpdateAt { get; private set; }

    public double Temperature { get; private set; } = double.NaN;

    public ThrottledFlags Throttled { get; private set; } = ThrottledFlags.Unknown;

    public IReadOnlyList<VcioClock> Clocks => clocks;

    public IReadOnlyList<VcioVoltage> Voltages => voltages;

    //------------------------------------------------------------------------
    // Constructor
    //------------------------------------------------------------------------

    private VcioMonitor(int fd, VcioClock[] clocks, VcioVoltage[] voltages)
    {
        this.fd = fd;
        this.clocks = clocks;
        this.voltages = voltages;
        Supported = fd >= 0;
    }

    internal static VcioMonitor Create()
    {
        var fd = open(DevicePath, O_RDWR);

        var clocks = new List<VcioClock>();
        var voltages = new List<VcioVoltage>();
        if (fd >= 0)
        {
            foreach (var type in Enum.GetValues<ClockType>())
            {
                if (ReadClock(fd, type, true, out _))
                {
                    clocks.Add(new VcioClock(type, true));
                }
                else if (ReadClock(fd, type, false, out _))
                {
                    clocks.Add(new VcioClock(type, false));
                }
            }

            foreach (var type in Enum.GetValues<VoltageType>())
            {
                if (ReadVoltage(fd, type, out _))
                {
                    voltages.Add(new VcioVoltage(type));
                }
            }
        }

        var instance = new VcioMonitor(fd, [.. clocks], [.. voltages]);
        instance.Update();
        return instance;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        if (fd >= 0)
        {
            _ = close(fd);
            fd = -1;
        }
    }

    //------------------------------------------------------------------------
    // Update
    //------------------------------------------------------------------------

    public bool Update()
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (fd < 0)
        {
            return false;
        }

        var success = ReadTemperature(fd, out var temperature);
        Temperature = temperature;

        success &= ReadThrottled(fd, out var throttled);
        Throttled = throttled;

        foreach (var clock in clocks)
        {
            success &= ReadClock(fd, clock.Type, clock.Measured, out var frequency);
            clock.Frequency = frequency;
        }

        foreach (var voltage in voltages)
        {
            success &= ReadVoltage(fd, voltage.Type, out var value);
            voltage.Voltage = value;
        }

        if (success)
        {
            UpdateAt = DateTime.Now;
        }

        return success;
    }

    //------------------------------------------------------------------------
    // Helper
    //------------------------------------------------------------------------

    private static unsafe bool MailboxProperty(int fd, uint* buffer)
    {
        var ret = ioctl(fd, IOCTL_MBOX_PROPERTY, (IntPtr)buffer);
        if (ret < 0)
        {
            return false;
        }

        // buf[1] bit31 set -> response success
        return (buffer[1] & 0x8000_0000u) != 0;
    }

    //------------------------------------------------------------------------
    // Readers
    //------------------------------------------------------------------------

    private static unsafe bool ReadTemperature(int fd, out double value)
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

        if (!MailboxProperty(fd, buf))
        {
            value = double.NaN;
            return false;
        }

        var milliC = unchecked((int)buf[6]);
        value = milliC / 1000.0;
        return true;
    }

    private static unsafe bool ReadClock(int fd, ClockType clock, bool measured, out double value)
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

        if (!MailboxProperty(fd, buf))
        {
            value = double.NaN;
            return false;
        }

        value = buf[6];
        return true;
    }

    private static unsafe bool ReadVoltage(int fd, VoltageType voltage, out double value)
    {
        var buf = stackalloc uint[8];
        buf[0] = 8u * 4u;
        buf[1] = 0;
        buf[2] = TAG_GET_VOLTAGE;
        buf[3] = 8;
        buf[4] = 4;
        buf[5] = (uint)voltage;
        buf[6] = 0;        // response: microvolts
        buf[7] = 0;

        if (!MailboxProperty(fd, buf))
        {
            value = double.NaN;
            return false;
        }

        var microVolts = buf[6];
        value = microVolts / 1_000_000.0;
        return true;
    }

    private static unsafe bool ReadThrottled(int fd, out ThrottledFlags value)
    {
        var buf = stackalloc uint[7];
        buf[0] = 7u * 4u;
        buf[1] = 0;
        buf[2] = TAG_GET_THROTTLED;
        buf[3] = 4;   // value buffer size
        buf[4] = 0;   // request size
        buf[5] = 0;   // response: flags
        buf[6] = 0;

        if (!MailboxProperty(fd, buf))
        {
            value = ThrottledFlags.Unknown;
            return false;
        }

        value = (ThrottledFlags)buf[5];
        return true;
    }
}
