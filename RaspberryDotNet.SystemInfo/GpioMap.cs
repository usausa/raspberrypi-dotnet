namespace RaspberryDotNet.SystemInfo;

using static RaspberryDotNet.SystemInfo.NativeMethods;

#pragma warning disable CA2216
public sealed unsafe class GpioMap : IDisposable
{
    private const string DevicePath = "/dev/gpiomem";
    private const int GpioBlockSize = 4 * 1024;

    private IntPtr map = IntPtr.Zero;

    public bool IsOpen => map != IntPtr.Zero;

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

        var fd = open(DevicePath, O_RDONLY);
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

    //------------------------------------------------------------------------
    // Read states
    //------------------------------------------------------------------------

    public IReadOnlyList<GpioSocPinState> ReadSocBanks(int start = 0, int end = 27)
    {
        if (IsOpen)
        {
            throw new InvalidOperationException("GPIO map is not open.");
        }

        var list = new List<GpioSocPinState>(end - start + 1);

        var basePtr = (byte*)map.ToPointer();
        for (var soc = start; soc <= end; soc++)
        {
            var fsel = GetFsel(basePtr, (uint)soc);
            var lev = GetLevel(basePtr, (uint)soc);
            var func = (fsel <= 7) ? (GpioFunction)fsel : GpioFunction.Unknown;

            list.Add(new GpioSocPinState(soc, func, (int)lev));
        }

        return list;
    }

    public IReadOnlyList<GpioHeaderPinState> ReadHeaderGpioPins()
    {
        if (IsOpen)
        {
            throw new InvalidOperationException("GPIO map is not open.");
        }

        var list = new List<GpioHeaderPinState>(HeaderGpioPorts.Length);

        var basePtr = (byte*)map.ToPointer();
        foreach (var port in HeaderGpioPorts)
        {
            var fsel = GetFsel(basePtr, (uint)port.SocPin);
            var lev = GetLevel(basePtr, (uint)port.SocPin);
            var func = (fsel <= 7) ? (GpioFunction)fsel : GpioFunction.Unknown;

            list.Add(new GpioHeaderPinState(port.PhysicalPin, port.SocPin, func, (int)lev));
        }

        return list;
    }
    //------------------------------------------------------------------------
    // Low level register access
    //------------------------------------------------------------------------

    private static uint Read32(byte* basePtr, int byteOffset)
        => *(uint*)(basePtr + byteOffset);

    private static uint GetFsel(byte* basePtr, uint socPin)
    {
        var reg = socPin / 10;
        var shift = (socPin % 10) * 3;
        var off = (int)(reg * 4);
        var v = Read32(basePtr, off);
        return (v >> (int)shift) & 0x7u;
    }

    private static uint GetLevel(byte* basePtr, uint socPin)
    {
        var off = (socPin < 32) ? 0x34 : 0x38;
        var shift = socPin % 32;
        var v = Read32(basePtr, off);
        return (v >> (int)shift) & 1u;
    }

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
