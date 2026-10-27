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
Lõi đã chạy thử trên Windows (27/10/2026, màn 3840×2160): `Quay/BatHinh.cs` (Graphics.Capture), `Quay/ToneMapGpu.cs`
(shader HDR→SDR + cắt vùng + bể texture), `Quay/MaHoaVideo.cs` (MediaStreamSource → MediaTranscoder, MP4 H.264/AAC),
`Quay/AmThanh.cs` (WASAPI loopback, NAudio), `Quay/BoQuay.cs` (điều phối, lọc fps, bitrate), `Quay/ThanhQuay.cs` (thanh nổi).
Phím `Ctrl+PrtScn` (CaiDat.PhimQuay) bật/tắt quay **toàn màn hình dưới con trỏ**; mục "Quay màn hình" trong menu khay.
Việc còn lại cho phiên Windows, theo thứ tự:
1. XONG chạy thử (27/10/2026): interop, CreateFromDirect3D11Surface, H.264 4K + AAC 48 kHz đều chạy; màu HDR đúng (xem khung trích).
   Đã sửa: (a) tiếng im lặng chèn nửa tốc độ thật → encoder kéo video xuống ~36–42 fps, giờ đủ ~58–60 fps;
   (b) thanh quay lọt vào video khi quay toàn màn → `SetWindowDisplayAffinity(WDA_EXCLUDEFROMCAPTURE)`;
   (c) nhật ký "Quay xong …" luôn ghi thống kê từng khâu (nhận / lọc fps / rớt do bể / rớt do hàng đợi / gửi encoder).
   (d) đo HDR 4K60 có tiếng: 55–58 fps → nới ngưỡng lọc fps 0,9→0,75 chu kỳ + bể texture 6→8: 59,97 / 59,05 / 58,31 fps.
   Còn: kích thước lẻ (chỉ có khi quay theo vùng chọn, việc 2) chưa thử.
2. Nối chọn vùng: KhungChon thêm chế độ quay (Ctrl+PrtScn mở khung, Enter = bắt đầu quay vùng đã chọn thay vì chép);
   cửa sổ đang mở = `D3DInterop.ItemCuaSo(hwnd)` (BoQuay hiện chỉ nhận vùng trên một màn).
3. Trang **Quay** trong cửa sổ chính (CuaSoChinh): fps 30/60, chất lượng 0/1/2, tiếng hệ thống, con trỏ, thư mục, phím (CaiDat.Quay*).
   Chữ mới vào `Chu.cs` đủ vi + en.
4. Xong thì tăng `<Version>` lên 1.1.0, cập nhật README (mục Quay) và `docs/QUAY-MAN-HINH.md`, chạy workflow MSIX, nộp bản mới lên Store.
