using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace VgsChupHdr;

static class XuatAnh
{
    public static string TenFileMoi() => $"VGS {DateTime.Now:yyyy-MM-dd HHmmss}.png";

    /// Lưu PNG vào thư mục cài đặt, trả về đường dẫn
    public static string Luu(Bitmap anh)
    {
        var thuMuc = CaiDat.HienTai.ThuMuc;
        Directory.CreateDirectory(thuMuc);
        var duong = Path.Combine(thuMuc, TenFileMoi());
        anh.Save(duong, ImageFormat.Png);
        return duong;
    }

    public static void LuuTheoDuongDan(Bitmap anh, string duong)
    {
        var dinhDang = Path.GetExtension(duong).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => ImageFormat.Jpeg,
            ".bmp" => ImageFormat.Bmp,
            _ => ImageFormat.Png,
        };
        anh.Save(duong, dinhDang);
    }

    /// Đưa vào clipboard cả dạng Bitmap (cho Paint, Word…) và PNG (cho Telegram, Discord, trình duyệt)
    public static void Chep(Bitmap anh)
    {
        var png = new MemoryStream();
        anh.Save(png, ImageFormat.Png);
        png.Position = 0;
        var d = new DataObject();
        d.SetData(DataFormats.Bitmap, true, anh);
        d.SetData("PNG", false, png);
        for (int lan = 0; lan < 5; lan++)
        {
            try { Clipboard.SetDataObject(d, true, 5, 100); return; }
            catch (Exception) when (lan < 4) { Thread.Sleep(100); }
        }
    }
}
