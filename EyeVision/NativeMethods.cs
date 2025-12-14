using System.Runtime.InteropServices;

namespace EyeVision;

public static class NativeMethods
{
    [DllImport("libc", SetLastError = true)]
    public static extern int fsync(int fd);

    [DllImport("libc", SetLastError = true)]
    public static extern int open(string pathname, int flags);

    [DllImport("libc", SetLastError = true)]
    public static extern int close(int fd);

    public const int O_RDONLY = 0;
}