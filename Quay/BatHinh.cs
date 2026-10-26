using Vortice.Direct3D11;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;

namespace VgsChupHdr.Quay;

/// Bắt khung hình bằng Windows.Graphics.Capture (free-threaded, không cần DispatcherQueue).
/// Màn HDR → khung R16G16B16A16Float (scRGB); màn SDR → B8G8R8A8.
sealed class BatHinh : IDisposable
{
    readonly IDirect3DDevice winrtDevice;
    readonly GraphicsCaptureItem item;
    readonly Direct3D11CaptureFramePool pool;
    readonly GraphicsCaptureSession session;
    readonly SizeInt32 kich;
    bool dung;

    /// Texture chỉ dùng được trong lúc gọi (bị Dispose ngay sau); mốc thời gian là SystemRelativeTime của khung.
    public event Action<ID3D11Texture2D, TimeSpan>? KhungMoi;
    /// Nguồn đổi kích thước (đổi độ phân giải / cửa sổ co giãn) hoặc đóng → người gọi nên dừng quay.
    public event Action<string>? NguonDoi;

    public SizeInt32 KichThuoc => kich;

    public BatHinh(ID3D11Device device, GraphicsCaptureItem item, bool hdr, bool conTro)
    {
        this.item = item;
        winrtDevice = D3DInterop.TaoWinRtDevice(device);
        kich = item.Size;
        var dinhDang = hdr ? DirectXPixelFormat.R16G16B16A16Float : DirectXPixelFormat.B8G8R8A8UIntNormalized;
        pool = Direct3D11CaptureFramePool.CreateFreeThreaded(winrtDevice, dinhDang, 2, kich);
        pool.FrameArrived += KhiCoKhung;
        session = pool.CreateCaptureSession(item);
        try { session.IsCursorCaptureEnabled = conTro; } catch (Exception e) { NhatKy.Ghi("IsCursorCaptureEnabled không hỗ trợ", e); }
        // Win11: tắt viền vàng (sau khi XinTatVienAsync được chấp thuận); Win10 không có thuộc tính này
        try
        {
            if (Windows.Foundation.Metadata.ApiInformation.IsPropertyPresent("Windows.Graphics.Capture.GraphicsCaptureSession", "IsBorderRequired"))
                session.IsBorderRequired = false;
        }
        catch (Exception e) { NhatKy.Ghi("IsBorderRequired không đặt được", e); }
        item.Closed += (_, _) => NguonDoi?.Invoke("Nguồn quay đã đóng");
    }

    /// Win11: xin phép tắt viền vàng (gọi trước khi tạo BatHinh; hộp thoại hệ thống hiện một lần).
    public static async Task XinTatVienAsync()
    {
        try
        {
            if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.Graphics.Capture.GraphicsCaptureAccess"))
                await GraphicsCaptureAccess.RequestAccessAsync(GraphicsCaptureAccessKind.Borderless);
        }
        catch (Exception e) { NhatKy.Ghi("RequestAccessAsync(Borderless) không hỗ trợ", e); }
    }

    public void BatDau() => session.StartCapture();

    void KhiCoKhung(Direct3D11CaptureFramePool sender, object args)
    {
        if (dung) return;
        using var frame = sender.TryGetNextFrame();
        if (frame == null) return;
        if (frame.ContentSize.Width != kich.Width || frame.ContentSize.Height != kich.Height)
        {
            NguonDoi?.Invoke("Nguồn quay đổi kích thước");
            return;
        }
        using var tex = D3DInterop.LayTexture(frame.Surface);
        KhungMoi?.Invoke(tex, frame.SystemRelativeTime);
    }

    public void Dispose()
    {
        dung = true;
        try { session.Dispose(); } catch { }
        try { pool.FrameArrived -= KhiCoKhung; pool.Dispose(); } catch { }
        try { winrtDevice.Dispose(); } catch { }
    }
}
