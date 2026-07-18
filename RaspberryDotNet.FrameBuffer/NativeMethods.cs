namespace RaspberryDotNet.FrameBuffer;

using System.Runtime.InteropServices;

// ReSharper disable CommentTypo
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
#pragma warning disable CA5392
internal static partial class NativeMethods
{
    //------------------------------------------------------------------------
    // Const
    //------------------------------------------------------------------------

    public const int O_RDWR = 0x0002;

    // mmap

    public const int PROT_READ = 0x1;
    public const int PROT_WRITE = 0x2;
    public const int MAP_SHARED = 0x01;

    public static readonly IntPtr MAP_FAILED = new(-1);

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
}
