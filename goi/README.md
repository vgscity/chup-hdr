# goi/ — đóng gói MSIX cho Microsoft Store

- `AppxManifest.xml`: manifest gói. Ba chỗ `__IDENTITY_NAME__`, `__PUBLISHER__`, `__PUBLISHER_DISPLAY__` được `dung-goi.ps1` thay bằng
  biến repo `STORE_IDENTITY_NAME`, `STORE_PUBLISHER`, `STORE_PUBLISHER_DISPLAY` (lấy từ Partner Center › Product identity).
  Fork thì phải đặt tên app và identity riêng của mình.
- `dung-goi.ps1`: chạy trên Windows (cần Windows SDK: makeappx, signtool). Ra `VgsChupHdr-store.msix` (chưa ký, nộp Store — Store tự ký)
  và `VgsChupHdr-test.msix` + `.cer` (ký bằng chứng chỉ tự tạo để cài thử: cài .cer vào Trusted People trước).
- `logo-tao.cjs`, `logo-store-them.cjs`: sinh bộ logo/ảnh Store từ mask logo (cần `sharp`). Ảnh đã sinh sẵn trong `anh/`, `anh-store/`.
- `anh-landing/`: ảnh dựng từ giao diện thật cho trang giới thiệu vgs.zone/chup-hdr.

Workflow `.github/workflows/msix.yml` làm toàn bộ trên runner Windows và đưa artifact `VgsChupHdr-msix` + `VgsChupHdr-exe`.
Bản mới: tăng `<Version>` trong `VgsChupHdr.csproj` (Store cần 4 số, số cuối 0, script tự thêm), chạy workflow, nộp gói mới trên Partner Center.
