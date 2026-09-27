namespace RaspberryDotNet.BuildHat;

using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

using static RaspberryDotNet.BuildHat.NativeMethods;

internal sealed class SerialDevice : IDisposable
{
    private static readonly TimeSpan BatchWait = TimeSpan.FromMilliseconds(30);

    private readonly int fd;

    private readonly byte[] buffer = new byte[4096];

    private int start;

    private int end;

    private SerialDevice(int fd)
    {
        this.fd = fd;
    }

    public static SerialDevice Open(string path)
    {
        var fd = open(path, O_RDWR | O_NOCTTY | O_CLOEXEC);
        if (fd < 0)
        {
            throw new IOException(String.Create(CultureInfo.InvariantCulture, $"Serial device open failed. path=[{path}], error=[{Marshal.GetLastPInvokeError()}]"));
        }

        try
        {
            if (tcgetattr(fd, out var attributes) != 0)
            {
                throw CreateException(nameof(tcgetattr));
            }

            cfmakeraw(ref attributes);
            if (cfsetspeed(ref attributes, B115200) != 0)
            {
                throw CreateException(nameof(cfsetspeed));
            }

            attributes.c_cflag |= CLOCAL | CREAD;
            attributes.c_cc[VMIN] = 0;
            attributes.c_cc[VTIME] = 0;
            if (tcsetattr(fd, TCSANOW, ref attributes) != 0)
            {
                throw CreateException(nameof(tcsetattr));
            }

            _ = tcflush(fd, TCIOFLUSH);
            return new SerialDevice(fd);
        }
        catch
        {
            _ = close(fd);
            throw;
        }
    }

    public void Dispose()
    {
        _ = tcdrain(fd);
        _ = close(fd);
    }

    public void DiscardInput()
    {
        _ = tcflush(fd, TCIFLUSH);
        start = 0;
        end = 0;
    }

    public void Write(string text) => Write(Encoding.ASCII.GetBytes(text));

    public void Write(ReadOnlySpan<byte> data)
    {
        while (!data.IsEmpty)
        {
            var written = write(fd, ref MemoryMarshal.GetReference(data), data.Length);
            if (written < 0)
            {
                var error = Marshal.GetLastPInvokeError();
                if ((error == EINTR) || (error == EAGAIN))
                {
                    continue;
                }

                throw CreateException(nameof(write), error);
            }

            data = data[(int)written..];
        }
    }

    public string? ReadLine(TimeSpan timeout)
    {
        var timestamp = Stopwatch.GetTimestamp();
        while (true)
        {
            var index = Array.IndexOf(buffer, (byte)'\n', start, end - start);
            if (index >= 0)
            {
                var length = (index > start) && (buffer[index - 1] == (byte)'\r') ? index - 1 - start : index - start;
                var line = Encoding.ASCII.GetString(buffer, start, length);
                start = index + 1;
                return line;
            }

            var remain = timeout - Stopwatch.GetElapsedTime(timestamp);
            if ((remain <= TimeSpan.Zero) || !Fill(remain))
            {
                return null;
            }
        }
    }

    public int ReadByte(TimeSpan timeout)
    {
        if ((start == end) && !Fill(timeout))
        {
            return -1;
        }

        return buffer[start++];
    }

    private bool Fill(TimeSpan timeout)
    {
        if (start == end)
        {
            start = 0;
            end = 0;
        }
        else if ((end == buffer.Length) && (start == 0))
        {
            end = 0;
        }
        else if (end == buffer.Length)
        {
            Buffer.BlockCopy(buffer, start, buffer, 0, end - start);
            end -= start;
            start = 0;
        }

        var fds = new pollfd { fd = fd, events = POLLIN };
        var result = poll(ref fds, 1, (int)Math.Ceiling(timeout.TotalMilliseconds));
        if (result < 0)
        {
            var error = Marshal.GetLastPInvokeError();
            if (error == EINTR)
            {
                return false;
            }

            throw CreateException(nameof(poll), error);
        }

        if (result == 0)
        {
            return false;
        }

        if ((fds.revents & (POLLERR | POLLHUP | POLLNVAL)) != 0)
        {
            throw new IOException(String.Create(CultureInfo.InvariantCulture, $"Serial device error. events=[0x{fds.revents:X}]"));
        }

        Thread.Sleep(BatchWait);
        var count = read(fd, ref buffer[end], buffer.Length - end);
        if (count < 0)
        {
            var error = Marshal.GetLastPInvokeError();
            if ((error == EINTR) || (error == EAGAIN))
            {
                return false;
            }

            throw CreateException(nameof(read), error);
        }

        end += (int)count;
        return count > 0;
    }

    private static IOException CreateException(string function) => CreateException(function, Marshal.GetLastPInvokeError());

    private static IOException CreateException(string function, int error) =>
        new(String.Create(CultureInfo.InvariantCulture, $"Serial device {function} failed. error=[{error}]"));
}
