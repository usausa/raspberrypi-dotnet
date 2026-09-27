namespace RaspberryDotNet.BuildHat;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006
#pragma warning disable CA5392
#pragma warning disable CS8981
internal static partial class NativeMethods
{
    //------------------------------------------------------------------------
    // Const
    //------------------------------------------------------------------------

    public const int O_RDWR = 0x0002;
    public const int O_NOCTTY = 0x0100;
    public const int O_CLOEXEC = 0x80000;

    // termios

    public const uint B115200 = 0x1002;
    public const uint CREAD = 0x0080;
    public const uint CLOCAL = 0x0800;

    public const int VTIME = 5;
    public const int VMIN = 6;

    public const int TCSANOW = 0;

    public const int TCIFLUSH = 0;
    public const int TCIOFLUSH = 2;

    // poll

    public const short POLLIN = 0x0001;
    public const short POLLERR = 0x0008;
    public const short POLLHUP = 0x0010;
    public const short POLLNVAL = 0x0020;

    // errno

    public const int EINTR = 4;
    public const int EAGAIN = 11;

    //------------------------------------------------------------------------
    // Struct
    //------------------------------------------------------------------------

    [InlineArray(32)]
    public struct cc_array
    {
        private byte element;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct termios
    {
        public uint c_iflag;
        public uint c_oflag;
        public uint c_cflag;
        public uint c_lflag;
        public byte c_line;
        public cc_array c_cc;
        public uint c_ispeed;
        public uint c_ospeed;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct pollfd
    {
        public int fd;
        public short events;
        public short revents;
    }

    //------------------------------------------------------------------------
    // Method
    //------------------------------------------------------------------------

    [LibraryImport("libc", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    public static partial int open(string pathname, int flags);

    [LibraryImport("libc", SetLastError = true)]
    public static partial int close(int fd);

    [LibraryImport("libc", SetLastError = true)]
    public static partial nint read(int fd, ref byte buf, nint count);

    [LibraryImport("libc", SetLastError = true)]
    public static partial nint write(int fd, ref byte buf, nint count);

    [LibraryImport("libc", SetLastError = true)]
    public static partial int poll(ref pollfd fds, uint nfds, int timeout);

    [LibraryImport("libc", SetLastError = true)]
    public static partial int tcgetattr(int fd, out termios termios_p);

    [LibraryImport("libc", SetLastError = true)]
    public static partial int tcsetattr(int fd, int optional_actions, ref termios termios_p);

    [LibraryImport("libc")]
    public static partial void cfmakeraw(ref termios termios_p);

    [LibraryImport("libc", SetLastError = true)]
    public static partial int cfsetspeed(ref termios termios_p, uint speed);

    [LibraryImport("libc", SetLastError = true)]
    public static partial int tcflush(int fd, int queue_selector);

    [LibraryImport("libc", SetLastError = true)]
    public static partial int tcdrain(int fd);
}
