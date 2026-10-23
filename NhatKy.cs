namespace VgsChupHdr;

/// Ghi lỗi + thông tin chẩn đoán vào %AppData%\VgsChupHdr\nhat-ky.txt
static class NhatKy
{
    public static readonly string ThuMuc = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VgsChupHdr");
    public static readonly string DuongDan = Path.Combine(ThuMuc, "nhat-ky.txt");
    static readonly object khoa = new();

    public static void Ghi(string noiDung, Exception? loi = null)
    {
        try
        {
            Directory.CreateDirectory(ThuMuc);
            lock (khoa)
            {
                File.AppendAllText(DuongDan,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {noiDung}{(loi != null ? "\n" + loi : "")}\n");
            }
        }
        catch { /* không để lỗi ghi nhật ký làm sập app */ }
    }
}
