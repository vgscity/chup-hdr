# VGScreenshotHDR – hướng dẫn cho người (và AI) đóng góp

Đọc `README.md` trước. Tên hiển thị: `VGScreenshotHDR`; định danh nội bộ giữ `VgsChupHdr` (exe, `%AppData%\VgsChupHdr`, mutex) để
không mất cài đặt của người dùng cũ.

## Luật chung
- Giao diện hai ngôn ngữ: mọi chữ hiển thị mới thêm vào `Chu.cs` đủ tiếng Việt + tiếng Anh, không viết thẳng trong form. Nhật ký lỗi giữ tiếng Việt.
- Trong gói Store (`GoiStore.DangTrongGoi`) không ghi registry (bị ảo hoá); tự chạy cùng Windows dùng `StartupTask` khai trong `goi/AppxManifest.xml`.
- Tên hiển thị trong manifest phải khớp tên đã đặt trên Store của người phát hành. Fork phải đổi tên, identity và logo (xem LICENSE).
- Đổi số bản ở `<Version>` trong `VgsChupHdr.csproj` trước mỗi bản phát hành; gói Store dựng bằng workflow `msix.yml`.
- Chạy thử trên máy Windows có màn HDR thật: PrtScn → chọn vùng → vẽ → Enter; Shift+PrtScn; Alt+PrtScn; so với ảnh PrtScn của Windows.
  Màn hình tự tắt (máy để lâu) thì DXGI không trả khung → app lùi GDI, không phải lỗi.
- `dem/`: Worker đếm lượt tải, muối băm IP là secret `MUOI`, không bao giờ đưa vào mã hay file cấu hình.

## Đang làm: quay màn hình v1 (từ 26/10/2026) — đọc `docs/QUAY-MAN-HINH.md`
Lõi đã viết và build chéo sạch nhưng CHƯA chạy thử trên Windows: `Quay/BatHinh.cs` (Graphics.Capture), `Quay/ToneMapGpu.cs`
(shader HDR→SDR + cắt vùng + bể texture), `Quay/MaHoaVideo.cs` (MediaStreamSource → MediaTranscoder, MP4 H.264/AAC),
`Quay/AmThanh.cs` (WASAPI loopback, NAudio), `Quay/BoQuay.cs` (điều phối, lọc fps, bitrate), `Quay/ThanhQuay.cs` (thanh nổi).
Phím `Ctrl+PrtScn` (CaiDat.PhimQuay) bật/tắt quay **toàn màn hình dưới con trỏ**; mục "Quay màn hình" trong menu khay.
Việc còn lại cho phiên Windows, theo thứ tự:
1. Chạy thử: `dotnet run -c Release`, Ctrl+PrtScn 10 giây rồi bấm lại; xem `nhat-ky.txt` (dòng "Bắt đầu quay…", "rớt N khung") và
   file trong `Videos\VGScreenshotHDR`. Chỗ dễ sai: `GraphicsCaptureItem.As<IGraphicsCaptureItemInterop>()` / `FromAbi`
   (D3DInterop.cs), `MediaStreamSample.CreateFromDirect3D11Surface` với texture của device Vortice (cần ID3D11Multithread — đã bật),
   audio im lặng (MaHoaVideo.LayAm tự chèn im lặng), kích thước lẻ.
2. Nối chọn vùng: KhungChon thêm chế độ quay (Ctrl+PrtScn mở khung, Enter = bắt đầu quay vùng đã chọn thay vì chép);
   cửa sổ đang mở = `D3DInterop.ItemCuaSo(hwnd)` (BoQuay hiện chỉ nhận vùng trên một màn).
3. Trang **Quay** trong cửa sổ chính (CuaSoChinh): fps 30/60, chất lượng 0/1/2, tiếng hệ thống, con trỏ, thư mục, phím (CaiDat.Quay*).
   Chữ mới vào `Chu.cs` đủ vi + en.
4. Xong thì tăng `<Version>` lên 1.1.0, cập nhật README (mục Quay) và `docs/QUAY-MAN-HINH.md`, chạy workflow MSIX, nộp bản mới lên Store.
