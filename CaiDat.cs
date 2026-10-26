using System.Text.Json;

namespace VgsChupHdr;

public sealed class CaiDat
{
    public bool TuLuuKhiChep { get; set; } = true;
    public string ThuMuc { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Screenshots");
    public bool MoFileSauKhiLuu { get; set; } = false;
    public string NgonNgu { get; set; } = VgsChupHdr.NgonNgu.TuDong;   // "auto" | "vi" | "en"

    // Phím tắt (Keys của WinForms: mã phím | Shift/Control/Alt), đổi được trong cửa sổ chính
    public const Keys MacDinhChon = Keys.PrintScreen;
    public const Keys MacDinhToan = Keys.Shift | Keys.PrintScreen;
    public const Keys MacDinhCuaSo = Keys.Alt | Keys.PrintScreen;
    public Keys PhimChon { get; set; } = MacDinhChon;
    public Keys PhimToan { get; set; } = MacDinhToan;
    public Keys PhimCuaSo { get; set; } = MacDinhCuaSo;

    // Quay màn hình (v1, 26/10/2026): Ctrl+PrtScn bật/tắt; MP4 H.264 vào Videos\VGScreenshotHDR
    public const Keys MacDinhQuay = Keys.Control | Keys.PrintScreen;
    public Keys PhimQuay { get; set; } = MacDinhQuay;
    public int QuayFps { get; set; } = 60;            // 30 | 60
    public int QuayChatLuong { get; set; } = 1;       // 0 thấp · 1 vừa · 2 cao
    public bool QuayTiengHeThong { get; set; } = true;
    public bool QuayConTro { get; set; } = true;
    public string ThuMucQuay { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "VGScreenshotHDR");

    /// "Ctrl+Shift+A", "PrtScn"…
    public static string TenPhim(Keys k)
    {
        if (k == Keys.None) return Chu.ChuaDatPhim;
        var phan = new List<string>();
        if (k.HasFlag(Keys.Control)) phan.Add("Ctrl");
        if (k.HasFlag(Keys.Shift)) phan.Add("Shift");
        if (k.HasFlag(Keys.Alt)) phan.Add("Alt");
        var ma = k & Keys.KeyCode;
        phan.Add(ma switch
        {
            Keys.PrintScreen => "PrtScn",
            >= Keys.D0 and <= Keys.D9 => ((char)('0' + (ma - Keys.D0))).ToString(),
            Keys.Oemtilde => "`",
            Keys.OemMinus => "-",
            Keys.Oemplus => "=",
            Keys.Next => "PageDown",
            Keys.Prior => "PageUp",
            _ => ma.ToString(),
        });
        return string.Join("+", phan);
    }

    static readonly string duongDan = Path.Combine(NhatKy.ThuMuc, "cai-dat.json");
    public static CaiDat HienTai { get; private set; } = Doc();

    static CaiDat Doc()
    {
        try
        {
            if (File.Exists(duongDan))
                return JsonSerializer.Deserialize<CaiDat>(File.ReadAllText(duongDan)) ?? new CaiDat();
        }
        catch (Exception e) { NhatKy.Ghi("Đọc cài đặt lỗi", e); }
        return new CaiDat();
    }

    public void Luu()
    {
        try
        {
            Directory.CreateDirectory(NhatKy.ThuMuc);
            File.WriteAllText(duongDan, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception e) { NhatKy.Ghi("Lưu cài đặt lỗi", e); }
    }
}
