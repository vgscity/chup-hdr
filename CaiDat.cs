using System.Text.Json;

namespace VgsChupHdr;

public sealed class CaiDat
{
    public bool TuLuuKhiChep { get; set; } = true;
    public string ThuMuc { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Screenshots");
    public bool MoFileSauKhiLuu { get; set; } = false;

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
