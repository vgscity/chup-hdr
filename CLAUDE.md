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
