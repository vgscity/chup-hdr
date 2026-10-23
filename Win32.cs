using System.Runtime.InteropServices;

namespace VgsChupHdr;

static class Win32
{
    public const int WM_HOTKEY = 0x0312;
    public const uint MOD_ALT = 0x1, MOD_CONTROL = 0x2, MOD_SHIFT = 0x4, MOD_NOREPEAT = 0x4000;
    public const uint VK_SNAPSHOT = 0x2C;
    public const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }

    [DllImport("user32.dll", SetLastError = true)] public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr hwnd, int attr, out RECT rect, int size);

    public static System.Drawing.Rectangle VungCuaSoDangMo()
    {
        var h = GetForegroundWindow();
        if (h == IntPtr.Zero) return System.Drawing.Rectangle.Empty;
        if (DwmGetWindowAttribute(h, DWMWA_EXTENDED_FRAME_BOUNDS, out var r, Marshal.SizeOf<RECT>()) != 0)
            if (!GetWindowRect(h, out r)) return System.Drawing.Rectangle.Empty;
        return System.Drawing.Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom);
    }
}
