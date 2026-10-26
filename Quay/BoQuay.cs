using System.Diagnostics;
using System.Runtime.InteropServices;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace VgsChupHdr.Quay;

/// Điều phối một lần quay: bắt hình → tone-map GPU → mã hoá, kèm âm thanh. Một lần quay tại một thời điểm.
sealed class BoQuay : IDisposable
{
    public static BoQuay? DangChay { get; private set; }

    ID3D11Device device = null!;
    ID3D11DeviceContext ctx = null!;
    BatHinh bat = null!;
    ToneMapGpu toneMap = null!;
    MaHoaVideo maHoa = null!;
    AmThanh? am;
    Task chayMaHoa = Task.CompletedTask;
    readonly Stopwatch dongHo = new();
    readonly object khoa = new();
    TimeSpan khungCuoi = TimeSpan.MinValue, buocKhung;
    bool dangDung;

    public string DuongDan { get; private set; } = "";
    public TimeSpan ThoiLuong => dongHo.Elapsed;
    public bool Hdr { get; private set; }
    /// Báo khi đã dừng (tự dừng do nguồn đổi hoặc do người dùng): đường dẫn file, hoặc lỗi
    public event Action<string?, Exception?>? DaDung;

    [StructLayout(LayoutKind.Sequential)]
    struct MONITORINFO { public int cbSize; public Win32.RECT rcMonitor, rcWork; public uint dwFlags; }
    [DllImport("user32.dll")] static extern bool GetMonitorInfoW(IntPtr hmon, ref MONITORINFO mi);

    /// Vùng ảo của màn hình chứa điểm (mặc định: con trỏ)
    public static Rectangle VungManHinh(IntPtr hmon)
    {
        var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        GetMonitorInfoW(hmon, ref mi);
        return Rectangle.FromLTRB(mi.rcMonitor.Left, mi.rcMonitor.Top, mi.rcMonitor.Right, mi.rcMonitor.Bottom);
    }

    /// Bắt đầu quay một vùng (toạ độ màn hình ảo). Vùng phải nằm trong một màn hình.
    public static async Task<BoQuay> BatDauAsync(Rectangle vung, int fps, int chatLuong, bool tiengHeThong, bool conTro)
    {
        if (DangChay != null) throw new InvalidOperationException("Đang quay rồi");
        var rc = new Win32.RECT { Left = vung.Left, Top = vung.Top, Right = vung.Right, Bottom = vung.Bottom };
        var hmon = D3DInterop.MonitorFromRect(ref rc, D3DInterop.MONITOR_DEFAULTTONEAREST);
        var vungMan = VungManHinh(hmon);
        vung.Intersect(vungMan);
        if (vung.Width < 16 || vung.Height < 16) throw new InvalidOperationException("Vùng quay quá nhỏ");

        var q = new BoQuay();
        try
        {
            // Màn hình: HDR? mức trắng SDR? adapter nào?
            var (adapter, hdr, trangNits) = TimManHinh(hmon);
            using (adapter)
            {
                D3D11.D3D11CreateDevice(adapter, DriverType.Unknown, DeviceCreationFlags.BgraSupport | DeviceCreationFlags.VideoSupport,
                    null!, out ID3D11Device? dev, out ID3D11DeviceContext? dc).CheckError();
                q.device = dev!; q.ctx = dc!;
            }
            using (var mt = q.device.QueryInterface<ID3D11Multithread>()) mt.SetMultithreadProtected(true);
            q.Hdr = hdr;

            await BatHinh.XinTatVienAsync();
            var item = D3DInterop.ItemManHinh(hmon);
            q.bat = new BatHinh(q.device, item, hdr, conTro);
            var kichNguon = new Size(q.bat.KichThuoc.Width, q.bat.KichThuoc.Height);
            var vungTrongMan = new Rectangle(vung.X - vungMan.X, vung.Y - vungMan.Y, vung.Width, vung.Height);
            q.toneMap = new ToneMapGpu(q.device, q.ctx, kichNguon, vungTrongMan, hdr, 80f / trangNits);

            if (tiengHeThong)
            {
                try { q.am = new AmThanh(q.dongHo); }
                catch (Exception e) { NhatKy.Ghi("Không mở được loopback âm thanh, quay không tiếng", e); q.am = null; }
            }
            q.buocKhung = TimeSpan.FromSeconds(1.0 / fps);
            q.maHoa = new MaHoaVideo(q.toneMap.Rong, q.toneMap.Cao, fps, Bitrate(chatLuong, q.toneMap.Rong, q.toneMap.Cao, fps), q.am?.DinhDang);

            var thuMuc = CaiDat.HienTai.ThuMucQuay;
            Directory.CreateDirectory(thuMuc);
            q.DuongDan = Path.Combine(thuMuc, $"VGS {DateTime.Now:yyyy-MM-dd HHmmss}.mp4");

            q.chayMaHoa = q.maHoa.ChayAsync(q.DuongDan);
            q.bat.KhungMoi += q.XuLyKhung;
            q.bat.NguonDoi += lyDo => { NhatKy.Ghi("Dừng quay: " + lyDo); _ = q.DungAsync(); };
            if (q.am != null) q.am.DuLieu += q.maHoa.ThemAmThanh;
            q.dongHo.Start();
            q.am?.BatDau();
            q.bat.BatDau();
            DangChay = q;
            NhatKy.Ghi($"Bắt đầu quay {q.toneMap.Rong}×{q.toneMap.Cao} {fps}fps {(hdr ? "HDR trắng " + trangNits + "nit" : "SDR")} tiếng={(q.am != null)} → {q.DuongDan}");
            return q;
        }
        catch
        {
            q.Dispose();
            throw;
        }
    }

    static (IDXGIAdapter1 adapter, bool hdr, float trangNits) TimManHinh(IntPtr hmon)
    {
        var mucTrang = ManHinhSdr.DocTatCa();
        using var factory = DXGI.CreateDXGIFactory1<IDXGIFactory1>();
        for (uint i = 0; factory.EnumAdapters1(i, out IDXGIAdapter1? adapter).Success && adapter != null; i++)
        {
            for (uint j = 0; adapter.EnumOutputs(j, out IDXGIOutput? output).Success && output != null; j++)
            {
                using (output)
                using (var o6 = output.QueryInterface<IDXGIOutput6>())
                {
                    var d = o6.Description1;
                    if (d.Monitor != hmon) continue;
                    mucTrang.TryGetValue(d.DeviceName, out float heSo);
                    if (heSo <= 0) heSo = 1f;
                    return (adapter, d.ColorSpace == ColorSpaceType.RgbFullG2084NoneP2020, heSo * 80f);
                }
            }
            adapter.Dispose();
        }
        throw new InvalidOperationException("Không tìm thấy màn hình qua DXGI");
    }

    /// bit/giây theo chất lượng (0 thấp, 1 vừa, 2 cao): ~0,07 / 0,10 / 0,15 bit mỗi điểm ảnh mỗi khung
    static uint Bitrate(int chatLuong, int w, int h, int fps)
    {
        double bpp = chatLuong switch { 0 => 0.07, 2 => 0.15, _ => 0.10 };
        return (uint)Math.Clamp(w * (double)h * fps * bpp, 2_000_000, 80_000_000);
    }

    void XuLyKhung(ID3D11Texture2D tex, TimeSpan _)
    {
        lock (khoa)
        {
            if (dangDung) return;
            var luc = dongHo.Elapsed;
            if (khungCuoi != TimeSpan.MinValue && luc - khungCuoi < buocKhung * 0.9) return;   // màn 144 Hz chỉ lấy đủ fps đích
            khungCuoi = luc;
            var o = toneMap.Lay();
            if (o == null) return;   // encoder chậm, bể đầy → rớt khung
            toneMap.Ve(tex, o);
            ctx.Flush();
            maHoa.ThemKhung(o.Surface, luc, () => toneMap.Tra(o));
        }
    }

    public async Task DungAsync()
    {
        lock (khoa) { if (dangDung) return; dangDung = true; }
        Exception? loi = null;
        try
        {
            bat.Dispose();
            am?.Dung();
            maHoa.KetThuc();
            await chayMaHoa;
            if (maHoa.KhungRot > 0) NhatKy.Ghi($"Quay xong, rớt {maHoa.KhungRot} khung (encoder không kịp)");
        }
        catch (Exception e) { loi = e; NhatKy.Ghi("Dừng quay lỗi", e); }
        finally
        {
            dongHo.Stop();
            DangChay = null;
            Dispose();
        }
        DaDung?.Invoke(loi == null ? DuongDan : null, loi);
    }

    public void Dispose()
    {
        try { am?.Dispose(); } catch { }
        try { toneMap?.Dispose(); } catch { }
        try { ctx?.Dispose(); device?.Dispose(); } catch { }
    }
}
