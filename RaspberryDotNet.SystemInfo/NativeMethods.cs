namespace RaspberryDotNet.SystemInfo;

using System.Runtime.InteropServices;

// ReSharper disable CollectionNeverQueried.Global
// ReSharper disable CommentTypo
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006
#pragma warning disable SA1005
#pragma warning disable CA5392
#pragma warning disable CS8981
internal static partial class NativeMethods
{
    //------------------------------------------------------------------------
    // Const
    //------------------------------------------------------------------------

    public const int O_RDONLY = 0;
    public const int O_RDWR = 0x0002;

    // mmap

    public const int PROT_READ = 0x1;
    public const int MAP_SHARED = 0x01;

    public static readonly IntPtr MAP_FAILED = new(-1);

    // Ioctl

    // We need: _IOWR(type, nr, size)
    // On Linux: IOC(dir,type,nr,size)
    // dir bits: IOC_READ|IOC_WRITE

    public const int IOC_NRBITS = 8;
    public const int IOC_TYPEBITS = 8;
    public const int IOC_SIZEBITS = 14;
    //public const int IOC_DIRBITS = 2;

    public const int IOC_NRSHIFT = 0;
    public const int IOC_TYPESHIFT = IOC_NRSHIFT + IOC_NRBITS;          // 8
    public const int IOC_SIZESHIFT = IOC_TYPESHIFT + IOC_TYPEBITS;      // 16
    public const int IOC_DIRSHIFT = IOC_SIZESHIFT + IOC_SIZEBITS;       // 30

    //public const int IOC_NONE = 0;
    public const int IOC_WRITE = 1;
    public const int IOC_READ = 2;

    public static ulong IOC(int dir, int type, int nr, int size)
    {
        // request is unsigned long; on 64-bit it's 64-bit
        // Shift as unsigned so that dir << 30 is not sign-extended into the upper 32 bits when the uint result is widened (zero-extended) to ulong
        return ((uint)dir << IOC_DIRSHIFT) |
               ((uint)type << IOC_TYPESHIFT) |
               ((uint)nr << IOC_NRSHIFT) |
               ((uint)size << IOC_SIZESHIFT);
    }

    public static ulong IOWR(int type, int nr, int size) =>
        IOC(IOC_READ | IOC_WRITE, type, nr, size);

    // Raspberry Pi mailbox property ioctl:

    public static readonly ulong IOCTL_MBOX_PROPERTY = IOWR(100, 0, IntPtr.Size);

    // Property interface constants
    public const uint TAG_GET_TEMPERATURE = 0x00030006;
    public const uint TAG_GET_CLOCK_RATE = 0x00030002;
    public const uint TAG_GET_CLOCK_RATE_MEASURED = 0x00030047;
    public const uint TAG_GET_VOLTAGE = 0x00030003;
    public const uint TAG_GET_THROTTLED = 0x00030046;

    public const uint TEMP_ID_SOC = 0;

    //------------------------------------------------------------------------
    // Struct
    //------------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential)]
    public struct pollFd
    {
        public int fd;
        public short events;
        public short revents;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct input_event
    {
        public long tv_sec;      // seconds
        public long tv_usec;     // microseconds
        public ushort type;
        public ushort code;
        public int value;
    }

    //------------------------------------------------------------------------
    // Method
    //------------------------------------------------------------------------
    [LibraryImport("libc", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    public static partial int open(string pathname, int flags);

    [LibraryImport("libc", SetLastError = true)]
    public static partial int close(int fd);

    [LibraryImport("libc", SetLastError = true)]
    internal static partial IntPtr mmap(IntPtr addr, UIntPtr length, int prot, int flags, int fd, IntPtr offset);

    [LibraryImport("libc", SetLastError = true)]
    internal static partial int munmap(IntPtr addr, UIntPtr length);

    [LibraryImport("libc", SetLastError = true)]
    public static partial int ioctl(int fd, ulong request, IntPtr argp);
}
