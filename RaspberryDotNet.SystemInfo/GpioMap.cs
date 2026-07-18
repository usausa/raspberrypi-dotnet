// ReSharper disable CommentTypo
namespace RaspberryDotNet.SystemInfo;

using static RaspberryDotNet.SystemInfo.NativeMethods;

public sealed class GpioMap : IDisposable
{
    // BCM2835-2711 (Pi 4 and earlier)
    private const string DevicePathBcm = "/dev/gpiomem";

    // BCM2712 + RP1 (Pi 5 family)
    private const string DevicePathRp1 = "/dev/gpiomem0";

    private const int GpioBlockSize = 4 * 1024;

    // RP1 IO_BANK0 register layout (see ReadRp1Pin for sources / caveats).
    private const uint Rp1FuncSelMask = 0x1f;   // CTRL bits [4:0]
    private const uint Rp1FuncSelSysRio = 5;    // FUNCSEL value that selects SYS_RIO (software GPIO)
    private const int Rp1StatusInFromPadBit = 17; // STATUS.INFROMPAD: input level at the pad
    private const int Rp1StatusOeToPadBit = 13;   // STATUS.OETOPAD: output enable to the pad

    private IntPtr map = IntPtr.Zero;
    private bool isRp1;

    public bool IsOpen => map != IntPtr.Zero;

    public static bool IsSupported() => File.Exists(DevicePathRp1) || File.Exists(DevicePathBcm);

    //------------------------------------------------------------------------
    // Open/Close
    //------------------------------------------------------------------------

    public void Dispose()
    {
        Close();
    }

    public bool Open()
    {
        if (IsOpen)
        {
            return true;
        }

        isRp1 = IsRp1Model();
        var devicePath = isRp1 ? DevicePathRp1 : DevicePathBcm;

        var fd = open(devicePath, O_RDONLY);
        if (fd >= 0)
        {
            try
            {
                map = mmap(IntPtr.Zero, GpioBlockSize, PROT_READ, MAP_SHARED, fd, IntPtr.Zero);
                if (map == MAP_FAILED)
                {
                    map = IntPtr.Zero;
                }
            }
            finally
            {
                _ = close(fd);
            }
        }

        return IsOpen;
    }

    public void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        try
        {
            _ = munmap(map, GpioBlockSize);
        }
        finally
        {
            map = IntPtr.Zero;
        }
    }

    private static bool IsRp1Model()
    {
        try
        {
            if (File.Exists("/proc/device-tree/compatible") &&
                File.ReadAllText("/proc/device-tree/compatible").Contains("bcm2712", StringComparison.Ordinal))
            {
                return true;
            }

            if (File.Exists("/proc/device-tree/model") &&
                File.ReadAllText("/proc/device-tree/model").Contains("Raspberry Pi 5", StringComparison.Ordinal))
            {
                return true;
            }
        }
        catch (IOException)
        {
            // Fall back to the BCM
        }
        catch (UnauthorizedAccessException)
        {
            // Fall back to the BCM
        }

        return false;
    }

    //------------------------------------------------------------------------
    // Read states
    //------------------------------------------------------------------------

    public unsafe IReadOnlyList<GpioSocPinState> ReadSocBanks(int start = 0, int end = 27)
    {
        if (!IsOpen)
        {
            throw new InvalidOperationException("GPIO map is not open.");
        }

        var list = new List<GpioSocPinState>(end - start + 1);

        var basePtr = (byte*)map.ToPointer();
        for (var soc = start; soc <= end; soc++)
        {
            var (func, level) = ReadPin(basePtr, (uint)soc);

            list.Add(new GpioSocPinState(soc, func, level));
        }

        return list;
    }

    public unsafe IReadOnlyList<GpioHeaderPinState> ReadHeaderGpioPins()
    {
        if (!IsOpen)
        {
            throw new InvalidOperationException("GPIO map is not open.");
        }

        var list = new List<GpioHeaderPinState>(HeaderGpioPorts.Length);

        var basePtr = (byte*)map.ToPointer();
        foreach (var port in HeaderGpioPorts)
        {
            var (func, level) = ReadPin(basePtr, (uint)port.SocPin);

            list.Add(new GpioHeaderPinState(port.PhysicalPin, port.SocPin, func, level));
        }

        return list;
    }

    private unsafe (GpioFunction Function, int Level) ReadPin(byte* basePtr, uint socPin)
    {
        if (isRp1)
        {
            return ReadRp1Pin(basePtr, socPin);
        }

        var fsel = GetFsel(basePtr, socPin);
        var lev = GetLevel(basePtr, socPin);
        var func = (fsel <= 7) ? (GpioFunction)fsel : GpioFunction.Unknown;
        return (func, (int)lev);
    }

    //------------------------------------------------------------------------
    // Low level register access (BCM2835-2711)
    //------------------------------------------------------------------------

    private static unsafe uint GetFsel(byte* basePtr, uint socPin)
    {
        var reg = socPin / 10;
        var shift = (socPin % 10) * 3;
        var off = (int)(reg * 4);
        var v = Read32(basePtr, off);
        return (v >> (int)shift) & 0x7u;
    }

    private static unsafe uint GetLevel(byte* basePtr, uint socPin)
    {
        var off = (socPin < 32) ? 0x34 : 0x38;
        var shift = socPin % 32;
        var v = Read32(basePtr, off);
        return (v >> (int)shift) & 1u;
    }

    //------------------------------------------------------------------------
    // Low level register access (RP1 / Pi 5 IO_BANK0)
    //------------------------------------------------------------------------

    private static unsafe (GpioFunction Function, int Level) ReadRp1Pin(byte* basePtr, uint socPin)
    {
        var status = Read32(basePtr, (int)(socPin * 8));
        var ctrl = Read32(basePtr, (int)((socPin * 8) + 4));

        var level = (int)((status >> Rp1StatusInFromPadBit) & 1u);
        var funcSel = ctrl & Rp1FuncSelMask;

        if (funcSel != Rp1FuncSelSysRio)
        {
            return (GpioFunction.Unknown, level);
        }

        var func = ((status >> Rp1StatusOeToPadBit) & 1u) != 0 ? GpioFunction.Out : GpioFunction.In;
        return (func, level);
    }

    //------------------------------------------------------------------------
    // Helper
    //------------------------------------------------------------------------

    private static unsafe uint Read32(byte* basePtr, int byteOffset) => Volatile.Read(ref *(uint*)(basePtr + byteOffset));

    //------------------------------------------------------------------------
    // Header GPIO mapping
    //------------------------------------------------------------------------

    private sealed record HeaderGpioPort(int PhysicalPin, int SocPin);

    // Standard mapping for 40-pin header GPIO-only pins (28 ports).
    private static readonly HeaderGpioPort[] HeaderGpioPorts =
    [
        new(3,  2),
        new(5,  3),
        new(7,  4),
        new(8, 14),
        new(10, 15),
        new(11, 17),
        new(12, 18),
        new(13, 27),
        new(15, 22),
        new(16, 23),
        new(18, 24),
        new(19, 10),
        new(21,  9),
        new(22, 25),
        new(23, 11),
        new(24,  8),
        new(26,  7),
        new(27,  0), // ID_SD
        new(28,  1), // ID_SC
        new(29,  5),
        new(31,  6),
        new(32, 12),
        new(33, 13),
        new(35, 19),
        new(36, 16),
        new(37, 26),
        new(38, 20),
        new(40, 21)
    ];
}
