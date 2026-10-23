# VGS Chụp HDR

Tool chụp màn hình Windows cho máy bật HDR: ảnh chụp ra đúng màu như mắt thấy, không cháy sáng.
Giao diện kiểu Lightshot: bấm PrtScn → màn hình đứng lại → kéo chọn vùng → vẽ chú thích → chép / lưu.

## Vì sao ảnh chụp bị cháy khi bật HDR
Nút PrtScn của Windows đọc bề mặt HDR (FP16 scRGB) rồi ép thẳng xuống 8-bit không tone-map.
Tool này chụp qua **DXGI Desktop Duplication**, lấy dữ liệu FP16 gốc, chia theo **mức trắng SDR**
người dùng đặt (thanh "SDR content brightness", đọc qua `DisplayConfigGetDeviceInfo`), rồi mã hoá sRGB.
Nội dung SDR ra chính xác; nội dung HDR thật (game, video) được nén dải sáng mềm thay vì cắt cụt.

## Phím
| Phím | Việc |
|---|---|
| `PrtScn` | Chọn vùng (kiểu Lightshot) |
| `Shift+PrtScn` | Chụp toàn màn hình → clipboard + file |
| `Alt+PrtScn` | Chụp cửa sổ đang mở → clipboard + file |
| Trong khung chọn: `Enter`/`Ctrl+C` chép · `Ctrl+S` lưu · `Ctrl+Z` hoàn tác · `Ctrl+A` cả màn · `Esc` thoát |
| Công cụ: `P` bút · `L` đường · `A` mũi tên · `R` khung · `M` đánh dấu · `T` chữ |

## Build (từ Mac hoặc Windows)
```bash
dotnet publish -c Release -o ban-phat-hanh
```
Ra `ban-phat-hanh/VgsChupHdr.exe` (self-contained, một file, không cần cài .NET).
Build chéo trên macOS cần `EnableWindowsTargeting=true` (đã có trong csproj).

## File
- `Chup.cs` – DXGI Desktop Duplication + tone-map scRGB → sRGB
- `ManHinhSdr.cs` – đọc mức trắng SDR từng màn
- `KhungChon.cs` – lớp phủ chọn vùng + thanh công cụ vẽ (`HinhVe.cs`)
- `UngDungKhay.cs` – tray, hotkey, menu, tự chạy
- `XuatAnh.cs` / `CaiDat.cs` / `NhatKy.cs` – lưu, clipboard, cài đặt, nhật ký lỗi

Nhật ký lỗi: `%AppData%\VgsChupHdr\nhat-ky.txt`. Cài đặt: `%AppData%\VgsChupHdr\cai-dat.json`.

## Chưa làm
- Tải ảnh lên lấy link chia sẻ (như prnt.sc) – dự định gắn với vgs.zone
- Lưu thêm bản .jxr/.avif giữ nguyên HDR
- Con trỏ chuột trong ảnh, màn hình xoay

## Microsoft Store
Gói MSIX dựng bằng GitHub Actions (`.github/workflows/msix.yml` → `goi/dung-goi.ps1`, manifest `goi/AppxManifest.xml`,
logo sinh bằng `goi/logo-tao.cjs`). Các bước đăng ký, lấy identity, nộp và nội dung listing mẫu: [goi/STORE.md](goi/STORE.md).
Trong gói, app nhận biết qua `GoiStore.DangTrongGoi`: tự chạy dùng StartupTask, việc trả phím PrtScn mở trang Settings thay vì ghi registry.
