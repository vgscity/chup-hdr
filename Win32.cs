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
    [DllImport("dwmapi.dll")] static extern int DwmGetWindowAttribute(IntPtr hwnd, int attr, out int giaTri, int size);
    [DllImport("user32.dll")] static extern IntPtr GetAncestor(IntPtr hWnd, uint flags);
    [DllImport("user32.dll")] static extern IntPtr GetTopWindow(IntPtr hWnd);
    [DllImport("user32.dll")] static extern IntPtr GetWindow(IntPtr hWnd, uint cmd);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] static extern bool IsIconic(IntPtr hWnd);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int index);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder ten, int max);

    const uint GA_ROOT = 2, GW_HWNDNEXT = 2;
    const int GWL_EXSTYLE = -20, WS_EX_TOOLWINDOW = 0x80, DWMWA_CLOAKED = 14;
    static readonly string[] lopBoQua = { "Shell_TrayWnd", "Shell_SecondaryTrayWnd", "Progman", "WorkerW", "NotifyIconOverflowWindow" };

    /// Cửa sổ người dùng đang làm việc. Gọi từ menu khay thì cửa sổ nổi lên là của chính app
    /// (từng chụp ra dải 180×47 ở góc trên trái) → bỏ qua, lấy cửa sổ thật trên cùng theo thứ tự Z.
    public static System.Drawing.Rectangle VungCuaSoDangMo()
    {
        var h = GetAncestor(GetForegroundWindow(), GA_ROOT);
        if (!LaCuaSoThat(h))
            for (h = GetTopWindow(IntPtr.Zero); h != IntPtr.Zero && !LaCuaSoThat(h); h = GetWindow(h, GW_HWNDNEXT)) { }
        if (h == IntPtr.Zero) return System.Drawing.Rectangle.Empty;
        return VungCuaSo(h);
    }

    static System.Drawing.Rectangle VungCuaSo(IntPtr h)
    {
        if (DwmGetWindowAttribute(h, DWMWA_EXTENDED_FRAME_BOUNDS, out RECT r, Marshal.SizeOf<RECT>()) != 0)
            if (!GetWindowRect(h, out r)) return System.Drawing.Rectangle.Empty;
        return System.Drawing.Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom);
    }

    static bool LaCuaSoThat(IntPtr h)
    {
        if (h == IntPtr.Zero || !IsWindowVisible(h) || IsIconic(h)) return false;
        GetWindowThreadProcessId(h, out uint pid);
        if (pid == (uint)Environment.ProcessId) return false;
        if (((long)GetWindowLongPtr(h, GWL_EXSTYLE) & WS_EX_TOOLWINDOW) != 0) return false;
        if (DwmGetWindowAttribute(h, DWMWA_CLOAKED, out int an, sizeof(int)) == 0 && an != 0) return false;
        var lop = new System.Text.StringBuilder(64);
        GetClassName(h, lop, lop.Capacity);
        if (lopBoQua.Contains(lop.ToString())) return false;
        var v = VungCuaSo(h);
        return v.Width > 1 && v.Height > 1;
    }
}

/// Bắt phím bằng hook bàn phím mức thấp, dùng khi RegisterHotKey thất bại vì app khác đang giữ tổ hợp phím.
/// Hook chạy trước hotkey hệ thống nên VGS vẫn nhận được phím.
sealed class HookBanPhim : IDisposable
{
    const int WH_KEYBOARD_LL = 13, WM_KEYDOWN = 0x100, WM_KEYUP = 0x101, WM_SYSKEYDOWN = 0x104, WM_SYSKEYUP = 0x105;
    const int VK_SHIFT = 0x10, VK_CONTROL = 0x11, VK_MENU = 0x12;

    [StructLayout(LayoutKind.Sequential)]
    struct KBDLLHOOKSTRUCT { public uint vkCode, scanCode, flags, time; public IntPtr extra; }
    delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)] static extern IntPtr SetWindowsHookEx(int id, HookProc proc, IntPtr hMod, uint threadId);
    [DllImport("user32.dll")] static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")] static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] static extern short GetAsyncKeyState(int vk);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern IntPtr GetModuleHandle(string? ten);

    readonly HookProc proc;   // giữ tham chiếu, không để GC thu delegate
    readonly IntPtr hook;
    readonly Func<uint, uint, int?> timHotkey;
    readonly Action<int> khiBam;
    readonly SynchronizationContext ngu;
    uint phimDangGiu;   // chặn lặp khi giữ phím

    /// timHotkey: nhận (mã phím, tổ hợp phím bổ trợ MOD_*) → id hotkey cần xử lý, hoặc null nếu bỏ qua.
    public HookBanPhim(Func<uint, uint, int?> timHotkey, Action<int> khiBam)
    {
        this.timHotkey = timHotkey;
        this.khiBam = khiBam;
        ngu = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        proc = XuLy;
        hook = SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(null), 0);
    }

    public bool DaCai => hook != IntPtr.Zero;

    static bool Giu(int vk) => (GetAsyncKeyState(vk) & 0x8000) != 0;

    IntPtr XuLy(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code >= 0)
        {
            var k = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            int msg = (int)wParam;
            if (msg is WM_KEYUP or WM_SYSKEYUP) { if (k.vkCode == phimDangGiu) phimDangGiu = 0; }
            else if (msg is WM_KEYDOWN or WM_SYSKEYDOWN)
            {
                uint mod = (Giu(VK_MENU) ? Win32.MOD_ALT : 0) | (Giu(VK_SHIFT) ? Win32.MOD_SHIFT : 0) | (Giu(VK_CONTROL) ? Win32.MOD_CONTROL : 0);
                if (timHotkey(k.vkCode, mod) is int id)
                {
                    if (phimDangGiu != k.vkCode) ngu.Post(_ => khiBam(id), null);   // không làm việc nặng trong hook
                    phimDangGiu = k.vkCode;
                    return 1;   // nuốt phím để app đang giữ tổ hợp không chụp chồng
                }
            }
        }
        return CallNextHookEx(hook, code, wParam, lParam);
    }

    public void Dispose() { if (hook != IntPtr.Zero) UnhookWindowsHookEx(hook); }
}
