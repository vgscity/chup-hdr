# VGScreenshotHDR

**Tiếng Việt** · [English below](#english)

Tool chụp màn hình Windows cho máy **bật HDR**: ảnh ra đúng màu như mắt thấy, không cháy sáng, không bạc màu.
Giao diện kiểu Lightshot: `PrtScn` → màn hình đứng lại → kéo chọn vùng → vẽ chú thích → chép / lưu.

- 🛍️ **Microsoft Store**: https://apps.microsoft.com/detail/9NBBNZMXZZZ6 (`winget install --id 9NBBNZMXZZZ6 --source msstore`)
- 🌐 Giới thiệu + bản exe cho máy không có Store: https://vgs.zone/chup-hdr
- 🐞 Báo lỗi, góp ý: https://vgs.zone/gop-y hoặc Issues của repo này

## Vì sao ảnh chụp bị cháy khi bật HDR
Nút PrtScn của Windows đọc bề mặt HDR (FP16 scRGB) rồi ép thẳng xuống 8-bit mà không tone-map.
Tool này chụp qua **DXGI Desktop Duplication**, lấy dữ liệu FP16 gốc, chia theo **mức trắng SDR** người dùng
đặt trong Windows (thanh "SDR content brightness", đọc qua `DisplayConfigGetDeviceInfo`), rồi mã hoá sRGB.
Nội dung SDR ra chính xác; nội dung HDR thật (game, video) được nén dải sáng mềm thay vì cắt cụt.
Máy không bật HDR vẫn chụp bình thường.

## Phím
| Phím | Việc |
|---|---|
| `PrtScn` | Chọn vùng (kiểu Lightshot) |
| `Shift+PrtScn` | Chụp toàn màn hình → clipboard + file |
| `Alt+PrtScn` | Chụp cửa sổ đang mở → clipboard + file |
| Trong khung chọn | `Enter`/`Ctrl+C` chép · `Ctrl+S` lưu · `Ctrl+Z` hoàn tác · `Ctrl+A` cả màn · `Esc` thoát |
| Công cụ | `P` bút · `L` đường · `A` mũi tên · `R` khung · `M` đánh dấu · `T` chữ |

Đổi phím, thư mục lưu, ngôn ngữ (Việt / English) trong cửa sổ chính (nhấp đúp icon ở khay).
Không bắt được `PrtScn`? Windows đang gán phím đó cho Snipping Tool (Settings › Accessibility › Keyboard) hoặc Lightshot / ShareX đang chạy.

## Build
Cần .NET 8 SDK. Build được cả trên Windows lẫn macOS/Linux (cross-build, `EnableWindowsTargeting`), chạy chỉ trên Windows 10 1809+ / 11, x64.
```bash
dotnet publish -c Release -o ban-phat-hanh      # exe một file, self-contained, không cần cài .NET
```
Gói MSIX cho Store: workflow `.github/workflows/msix.yml` (runner Windows) → `goi/dung-goi.ps1`. Xem `goi/README.md`.

## Cấu trúc
- `Chup.cs` – DXGI Desktop Duplication + tone-map scRGB → sRGB · `ManHinhSdr.cs` – mức trắng SDR từng màn
- `KhungChon.cs` – lớp phủ chọn vùng · `ThanhVe.cs` – thanh công cụ vẽ · `HinhVe.cs` – các hình chú thích
- `CuaSoChinh.cs` + `GiaoDienChung.cs` – cửa sổ chính · `UngDungKhay.cs` + `GiaoDienMenu.cs` – khay, hotkey, menu
- `Chu.cs` – toàn bộ chữ giao diện (vi + en) · `GoiStore.cs` – nhận biết chạy trong gói MSIX
- `dem/` – Worker Cloudflare đếm lượt tải exe (ẩn danh) và phát file từ R2

Nhật ký lỗi: `%AppData%\VgsChupHdr\nhat-ky.txt`. App không lên mạng, không thu thập dữ liệu.

## Giấy phép
MIT — xem [LICENSE](LICENSE). Tên VGScreenshotHDR và logo VGS không nằm trong giấy phép.

---

## English

Windows screenshot tool for **HDR displays**: captures look exactly like what you see, no blown-out highlights, no washed-out colors.
Lightshot-style flow: `PrtScn` → screen freezes → drag a region → annotate → copy / save.

- 🛍️ **Microsoft Store**: https://apps.microsoft.com/detail/9NBBNZMXZZZ6 (`winget install --id 9NBBNZMXZZZ6 --source msstore`)
- 🌐 Landing page + standalone exe for machines without the Store: https://vgs.zone/chup-hdr
- 🐞 Bugs & feedback: Issues on this repo

**Why PrtScn burns HDR screenshots.** Windows grabs the HDR surface (FP16 scRGB) and dumps it to 8-bit without tone-mapping.
This tool captures through **DXGI Desktop Duplication**, keeps the FP16 data, divides by the user's **SDR white level**
(the "SDR content brightness" slider, read via `DisplayConfigGetDeviceInfo`), then encodes sRGB. SDR content is exact;
real HDR content (games, video) gets a soft highlight roll-off instead of hard clipping. SDR displays work as usual.

**Keys.** `PrtScn` region · `Shift+PrtScn` full screen · `Alt+PrtScn` active window · in the overlay: `Enter`/`Ctrl+C` copy, `Ctrl+S` save,
`Ctrl+Z` undo, `Ctrl+A` whole screen, `Esc` close; tools `P` pen, `L` line, `A` arrow, `R` rectangle, `M` highlighter, `T` text.
Hotkeys, save folder and language (Vietnamese / English) live in the main window (double-click the tray icon).

**Build.** .NET 8 SDK; cross-builds from macOS/Linux (`EnableWindowsTargeting`), runs on Windows 10 1809+ / 11 x64.
`dotnet publish -c Release -o ban-phat-hanh` produces a single self-contained exe. MSIX for the Store: `.github/workflows/msix.yml` → `goi/dung-goi.ps1` (see `goi/README.md`).

Identifiers in the code are Vietnamese (the project is maintained by the VGS community); comments explain intent.
Fully offline, no telemetry. MIT licensed — the VGScreenshotHDR name and VGS logo are excluded, see [LICENSE](LICENSE).
