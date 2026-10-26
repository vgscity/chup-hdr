# Quay màn hình (v1) — thiết kế và bàn giao

Mục tiêu: quay màn hình **đúng màu khi bật HDR** (lợi thế riêng của app; OBS quay màn HDR cũng cháy sáng nếu không cấu hình kỹ),
ra MP4 H.264 bằng encoder phần cứng, có tiếng hệ thống. Giao diện cùng kiểu với chụp: phím → chọn vùng → thanh quay nổi → dừng → file.

## Đường đi của một khung hình
1. **Bắt hình** `Quay/BatHinh.cs` — Windows.Graphics.Capture (`Direct3D11CaptureFramePool.CreateFreeThreaded`), item là màn hình
   (`CreateForMonitor`) hoặc cửa sổ (`CreateForWindow`). Màn HDR → khung `R16G16B16A16Float` (scRGB, 1.0 = 80 nit), màn SDR → `B8G8R8A8`.
   Win11: xin `GraphicsCaptureAccess.RequestAccessAsync(Borderless)` rồi `IsBorderRequired = false`; con trỏ bật qua `IsCursorCaptureEnabled`.
2. **Tone-map trên GPU** `Quay/ToneMapGpu.cs` — pixel shader D3D11 (HLSL trong file, biên dịch lúc chạy bằng Vortice.D3DCompiler), cùng công
   thức với `Chup.cs`: chia theo mức trắng SDR của màn (`ManHinhSdr`), có nội dung HDR thì nén mềm từ 80 % (`goi = 0.8`), rồi sRGB.
   Đồng thời cắt vùng (texcoord) và ép kích thước chẵn. Ra texture `B8G8R8A8_UNORM` lấy từ bể (pool) để không cấp phát mỗi khung.
3. **Mã hoá** `Quay/MaHoaVideo.cs` — `MediaStreamSource` (video BGRA8 uncompressed + audio PCM16) → `MediaTranscoder.PrepareMediaStreamSourceTranscodeAsync`
   với `MediaEncodingProfile.CreateMp4` (H.264 + AAC, `HardwareAccelerationEnabled = true`). Khung đưa vào bằng
   `MediaStreamSample.CreateFromDirect3D11Surface` (không copy về CPU); texture trả lại bể ở sự kiện `Processed`.
4. **Âm thanh** `Quay/AmThanh.cs` — NAudio `WasapiLoopbackCapture` (tiếng hệ thống), float → PCM16 giữ nguyên sample rate thiết bị.
   Khi im lặng WASAPI không trả dữ liệu → tự chèn khung im lặng để dòng thời gian không đứt (nếu không MSS sẽ treo).
5. **Điều phối** `Quay/BoQuay.cs` — bắt đầu (vùng, màn, fps, chất lượng, âm thanh) / dừng; lọc khung theo fps đích (màn 144 Hz chỉ lấy 60);
   mốc thời gian chung từ một `Stopwatch`; ghi vào `Videos\VGScreenshotHDR\VGS yyyy-MM-dd HHmmss.mp4`; sự kiện báo trạng thái.
6. **Giao diện** — phím `Ctrl+PrtScn` (đổi được) mở khung chọn vùng ở chế độ quay → bấm Quay (hoặc Enter) → `Quay/ThanhQuay.cs`
   thanh nhỏ nổi (đồng hồ, nút dừng, kéo được), phím lần nữa hoặc nút → dừng → thông báo ở khay. Trang **Quay** trong cửa sổ chính:
   fps (30/60), chất lượng (thấp/vừa/cao → bitrate), tiếng hệ thống, mic (v1.1), thư mục.

## Phạm vi v1 / để sau
- v1: vùng / toàn màn / cửa sổ, 30–60 fps, H.264 MP4, tiếng hệ thống, HDR tone-map, con trỏ chuột.
- v1.1: mic (trộn hai nguồn), tạm dừng, đếm ngược 3 s, giới hạn thời lượng.
- Sau: GIF/WebP, webcam góc, chú thích khi quay, HEVC/AV1, cắt nhanh sau khi quay.

## Thử trên máy Windows (phiên Windows làm)
- Màn HDR bật: quay desktop + video YouTube HDR + game → so với PrtScn/OBS: không cháy, màu như mắt thấy.
- 4K 60 fps: GPU tone-map + encoder phần cứng phải dưới 10 ms/khung; xem `nhat-ky.txt` có dòng "khung rớt".
- Im lặng 10 s giữa chừng: file vẫn chạy đúng thời lượng, không tua nhanh.
- Dừng bằng phím / nút / tắt app: file đóng đúng (moov atom), mở được trong Windows Media Player, VLC, Telegram.
- Màn SDR, nhiều màn, đổi fps giữa chừng (không cho), đóng cửa sổ đang quay (dừng và báo).

## Chỗ dễ vấp đã biết
- `Direct3D11CaptureFramePool` không free-threaded cần DispatcherQueue → dùng `CreateFreeThreaded`.
- Kích thước H.264 phải chẵn; vùng chọn lẻ thì bớt 1 px.
- Khung từ Capture có thể đổi kích thước (đổi độ phân giải) → `Recreate` pool, v1 chỉ dừng quay và báo.
- `MediaStreamSource` đòi mẫu liên tục; thiếu audio là treo — xem mục 4.
- Mức trắng SDR người dùng đổi giữa chừng → v1 đọc một lần lúc bắt đầu.
- Trong gói Store, Graphics.Capture không cần capability thêm; WASAPI loopback cũng không.
