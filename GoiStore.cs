using System.Runtime.InteropServices;

namespace VgsChupHdr;

/// Nhận biết app đang chạy trong gói MSIX (cài từ Microsoft Store) hay exe rời.
static class GoiStore
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    static extern int GetCurrentPackageFullName(ref int length, System.Text.StringBuilder? fullName);
    const int APPMODEL_ERROR_NO_PACKAGE = 15700;

    public static readonly bool DangTrongGoi = KiemTra();

    static bool KiemTra()
    {
        try
        {
            int dai = 0;
            int kq = GetCurrentPackageFullName(ref dai, null);
            return kq != APPMODEL_ERROR_NO_PACKAGE;
        }
        catch { return false; }
    }

    /// Trong gói: hỏi/đặt StartupTask khai trong AppxManifest (người dùng bật tắt được ở Settings > Apps > Startup)
    public static async Task<bool> DangTuChayAsync()
    {
        try
        {
            var t = await Windows.ApplicationModel.StartupTask.GetAsync("VgsChupHdrStartup");
            return t.State is Windows.ApplicationModel.StartupTaskState.Enabled
                or Windows.ApplicationModel.StartupTaskState.EnabledByPolicy;
        }
        catch (Exception e) { NhatKy.Ghi("StartupTask.GetAsync lỗi", e); return false; }
    }

    /// Trả về trạng thái sau khi đổi; false kèm lý do nếu Windows không cho bật (người dùng đã tắt trong Settings).
    public static async Task<(bool bat, string? lyDo)> DatTuChayAsync(bool bat)
    {
        try
        {
            var t = await Windows.ApplicationModel.StartupTask.GetAsync("VgsChupHdrStartup");
            if (!bat) { t.Disable(); return (false, null); }
            var s = await t.RequestEnableAsync();
            return s switch
            {
                Windows.ApplicationModel.StartupTaskState.Enabled or Windows.ApplicationModel.StartupTaskState.EnabledByPolicy => (true, null),
                Windows.ApplicationModel.StartupTaskState.DisabledByUser => (false, "Anh/chị đã tắt mục này trong Settings > Apps > Startup, bật lại ở đó."),
                Windows.ApplicationModel.StartupTaskState.DisabledByPolicy => (false, "Chính sách máy không cho app tự chạy."),
                _ => (false, "Windows không cho bật tự chạy lúc này."),
            };
        }
        catch (Exception e) { NhatKy.Ghi("StartupTask đặt lỗi", e); return (false, e.Message); }
    }
}
