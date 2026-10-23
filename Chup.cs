using System.Drawing;
using System.Drawing.Imaging;
using SharpGen.Runtime;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace VgsChupHdr;

public sealed class ThongTinManHinh
{
    public string Ten = "";
    public Rectangle Vung;
    public bool Hdr;
    public float TrangSdrNits;
    public float DinhNits;
    public string DinhDang = "";
    public string GhiChu = "";
    public bool CoNoiDungHdr;
}

public sealed class KetQuaChup : IDisposable
{
    public required Bitmap Anh;
    public Rectangle VungAo;          // toạ độ màn hình ảo (có thể âm)
    public List<ThongTinManHinh> ManHinh = new();
    public void Dispose() => Anh.Dispose();
}

/// Chụp toàn bộ các màn hình bằng DXGI Desktop Duplication.
/// Màn HDR trả về FP16 scRGB → tự tone-map xuống SDR theo mức trắng SDR người dùng đặt.
public static class Chup
{
    // Bảng tra gamma sRGB: tuyến tính [0,1] → byte
    static readonly byte[] bangSrgb = TaoBangSrgb();
    const int KichBang = 8192;

    static byte[] TaoBangSrgb()
    {
        var b = new byte[KichBang + 1];
        for (int i = 0; i <= KichBang; i++)
        {
            double c = (double)i / KichBang;
            double s = c <= 0.0031308 ? 12.92 * c : 1.055 * Math.Pow(c, 1 / 2.4) - 0.055;
            b[i] = (byte)Math.Clamp(Math.Round(s * 255), 0, 255);
        }
        return b;
    }

    public static KetQuaChup ChupTatCa()
    {
        var mucTrang = ManHinhSdr.DocTatCa();
        var danhSach = new List<(ThongTinManHinh info, IDXGIAdapter1 adapter, IDXGIOutput output)>();

        using var factory = DXGI.CreateDXGIFactory1<IDXGIFactory1>();
        for (uint i = 0; factory.EnumAdapters1(i, out IDXGIAdapter1? adapter).Success && adapter != null; i++)
        {
            for (uint j = 0; adapter.EnumOutputs(j, out IDXGIOutput? output).Success && output != null; j++)
            {
                using var o6 = output.QueryInterface<IDXGIOutput6>();
                var d = o6.Description1;
                if (!d.AttachedToDesktop) { output.Dispose(); continue; }
                var vung = Rectangle.FromLTRB(d.DesktopCoordinates.Left, d.DesktopCoordinates.Top,
                                              d.DesktopCoordinates.Right, d.DesktopCoordinates.Bottom);
                bool hdr = d.ColorSpace == ColorSpaceType.RgbFullG2084NoneP2020;
                mucTrang.TryGetValue(d.DeviceName, out float heSo);
                if (heSo <= 0) heSo = 1f;
                danhSach.Add((new ThongTinManHinh
                {
                    Ten = d.DeviceName,
                    Vung = vung,
                    Hdr = hdr,
                    TrangSdrNits = heSo * 80f,
                    DinhNits = d.MaxLuminance,
                }, adapter, output));
            }
        }

        if (danhSach.Count == 0) throw new InvalidOperationException("Không tìm thấy màn hình nào qua DXGI.");

        var vungAo = danhSach[0].info.Vung;
        foreach (var m in danhSach) vungAo = Rectangle.Union(vungAo, m.info.Vung);

        var anh = new Bitmap(vungAo.Width, vungAo.Height, PixelFormat.Format32bppArgb);
        var kq = new KetQuaChup { Anh = anh, VungAo = vungAo };

        var bits = anh.LockBits(new Rectangle(0, 0, anh.Width, anh.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        try
        {
            foreach (var (info, adapter, output) in danhSach)
            {
                try
                {
                    ChupMotMan(info, adapter, output, bits, vungAo);
                }
                catch (Exception e)
                {
                    info.GhiChu = "DXGI lỗi, dùng GDI (màu có thể sai): " + e.Message;
                    NhatKy.Ghi($"Chụp {info.Ten} qua DXGI lỗi, chuyển GDI", e);
                    ChupBangGdi(info, bits, vungAo);
                }
                kq.ManHinh.Add(info);
            }
        }
        finally
        {
            anh.UnlockBits(bits);
            foreach (var (_, adapter, output) in danhSach) { output.Dispose(); }
            foreach (var adapter in danhSach.Select(x => x.adapter).Distinct()) adapter.Dispose();
        }
        return kq;
    }

    static void ChupMotMan(ThongTinManHinh info, IDXGIAdapter1 adapter, IDXGIOutput output, BitmapData bits, Rectangle vungAo)
    {
        var r = D3D11.D3D11CreateDevice(adapter, DriverType.Unknown, DeviceCreationFlags.BgraSupport,
            null!, out ID3D11Device? device, out ID3D11DeviceContext? ctx);
        r.CheckError();
        using var _d = device!;
        using var _c = ctx!;

        using var o5 = output.QueryInterface<IDXGIOutput5>();
        // Tham số thứ 2 của Vortice là số format (không phải flags): truyền 0 → E_INVALIDARG
        var dinhDangNhan = new[] { Format.R16G16B16A16_Float, Format.B8G8R8A8_UNorm };
        using var dup = o5.DuplicateOutput1(device, (uint)dinhDangNhan.Length, dinhDangNhan);

        IDXGIResource? tai = null;
        OutduplFrameInfo frame = default;
        bool co = false;
        for (int lan = 0; lan < 40 && !co; lan++)
        {
            var rr = dup.AcquireNextFrame(100, out frame, out tai);
            if (rr.Success && tai != null)
            {
                // Khung đầu thường chỉ có con trỏ chuột (LastPresentTime = 0), texture còn đen → bỏ, lấy khung sau
                if (frame.LastPresentTime != 0) { co = true; break; }
                tai.Dispose(); tai = null;
                dup.ReleaseFrame();
            }
            else if (rr.Code != Vortice.DXGI.ResultCode.WaitTimeout.Code) rr.CheckError();
        }
        if (!co) throw new TimeoutException("Desktop Duplication không trả khung hình nào trong 4 giây.");

        try
        {
            using var tex = tai!.QueryInterface<ID3D11Texture2D>();
            var mo = tex.Description;
            info.DinhDang = mo.Format.ToString();

            var moStaging = new Texture2DDescription
            {
                Width = mo.Width, Height = mo.Height, MipLevels = 1, ArraySize = 1,
                Format = mo.Format, SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Staging, BindFlags = BindFlags.None,
                CPUAccessFlags = CpuAccessFlags.Read, MiscFlags = ResourceOptionFlags.None,
            };
            using var staging = device.CreateTexture2D(moStaging);
            ctx.CopyResource(staging, tex);
            var map = ctx.Map(staging, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None);
            try
            {
                int w = Math.Min((int)mo.Width, info.Vung.Width);
                int h = Math.Min((int)mo.Height, info.Vung.Height);
                int dx = info.Vung.Left - vungAo.Left, dy = info.Vung.Top - vungAo.Top;
                if (mo.Format == Format.R16G16B16A16_Float)
                    DoiHdrSangSdr(info, map.DataPointer, (int)map.RowPitch, w, h, bits, dx, dy);
                else
                    CheepBgra(map.DataPointer, (int)map.RowPitch, w, h, bits, dx, dy);
            }
            finally { ctx.Unmap(staging, 0); }
        }
        finally
        {
            tai?.Dispose();
            dup.ReleaseFrame();
        }
    }

    static unsafe void CheepBgra(IntPtr nguon, int pitch, int w, int h, BitmapData bits, int dx, int dy)
    {
        for (int y = 0; y < h; y++)
        {
            byte* src = (byte*)nguon + (long)y * pitch;
            byte* dst = (byte*)bits.Scan0 + (long)(y + dy) * bits.Stride + dx * 4;
            Buffer.MemoryCopy(src, dst, w * 4, w * 4);
            for (int x = 0; x < w; x++) dst[x * 4 + 3] = 255;
        }
    }

    /// scRGB FP16 (1.0 = 80 nits) → sRGB 8-bit, chia theo mức trắng SDR để nội dung SDR ra đúng như mắt thấy.
    /// Nếu khung có nhiều điểm sáng hơn trắng SDR (game/video HDR) thì nén dải sáng mềm thay vì cắt cụt.
    static unsafe void DoiHdrSangSdr(ThongTinManHinh info, IntPtr nguon, int pitch, int w, int h, BitmapData bits, int dx, int dy)
    {
        float heSo = 80f / info.TrangSdrNits;   // chia cho mức trắng SDR

        // Lượt 1: đếm điểm vượt trắng SDR
        long soVuot = 0;
        Parallel.For(0, h, y =>
        {
            Half* p = (Half*)((byte*)nguon + (long)y * pitch);
            long dem = 0;
            for (int x = 0; x < w; x++)
            {
                float r = (float)p[x * 4] * heSo, g = (float)p[x * 4 + 1] * heSo, b = (float)p[x * 4 + 2] * heSo;
                float Y = 0.2126f * r + 0.7152f * g + 0.0722f * b;
                if (Y > 1.02f) dem++;
            }
            if (dem > 0) Interlocked.Add(ref soVuot, dem);
        });
        bool coHdr = soVuot > (long)w * h / 1000; // > 0,1% điểm ảnh
        info.CoNoiDungHdr = coHdr;
        const float goi = 0.80f; // từ 80% trắng trở lên bắt đầu nén

        // Lượt 2: đổi màu
        Parallel.For(0, h, y =>
        {
            Half* p = (Half*)((byte*)nguon + (long)y * pitch);
            byte* dst = (byte*)bits.Scan0 + (long)(y + dy) * bits.Stride + dx * 4;
            for (int x = 0; x < w; x++)
            {
                float r = (float)p[x * 4] * heSo, g = (float)p[x * 4 + 1] * heSo, b = (float)p[x * 4 + 2] * heSo;
                if (coHdr)
                {
                    float Y = 0.2126f * r + 0.7152f * g + 0.0722f * b;
                    if (Y > goi)
                    {
                        float Ym = goi + (1 - goi) * (1 - MathF.Exp(-(Y - goi) / (1 - goi)));
                        float k = Ym / Y;
                        r *= k; g *= k; b *= k;
                    }
                }
                dst[x * 4 + 0] = Srgb(b);
                dst[x * 4 + 1] = Srgb(g);
                dst[x * 4 + 2] = Srgb(r);
                dst[x * 4 + 3] = 255;
            }
        });
    }

    static byte Srgb(float c)
    {
        if (c <= 0) return 0;
        if (c >= 1) return 255;
        return bangSrgb[(int)(c * KichBang + 0.5f)];
    }

    static void ChupBangGdi(ThongTinManHinh info, BitmapData bits, Rectangle vungAo)
    {
        using var tam = new Bitmap(info.Vung.Width, info.Vung.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(tam))
            g.CopyFromScreen(info.Vung.Location, Point.Empty, info.Vung.Size);
        var tb = tam.LockBits(new Rectangle(0, 0, tam.Width, tam.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            CheepBgra(tb.Scan0, tb.Stride, tam.Width, tam.Height, bits, info.Vung.Left - vungAo.Left, info.Vung.Top - vungAo.Top);
        }
        finally { tam.UnlockBits(tb); }
    }
}
