using System.Runtime.InteropServices;

namespace VgsChupHdr;

/// Mọi chữ hiển thị cho người dùng, mỗi câu đủ tiếng Việt + tiếng Anh.
/// Thêm ngôn ngữ: thêm giá trị vào NgonNgu.Ma, sửa T(), rồi điền cột mới cho từng câu.
/// Nhật ký lỗi (NhatKy.Ghi) giữ tiếng Việt, chỉ để đọc khi gỡ lỗi.
static class NgonNgu
{
    public const string TuDong = "auto", TiengViet = "vi", TiengAnh = "en";

    [DllImport("kernel32.dll")] static extern ushort GetUserDefaultUILanguage();
    const int LANG_VIETNAMESE = 0x2A;

    // csproj bật InvariantGlobalization nên CultureInfo không cho biết ngôn ngữ Windows → hỏi thẳng Windows
    static readonly bool windowsTiengViet = DocWindows();
    static bool DocWindows()
    {
        try { return (GetUserDefaultUILanguage() & 0x3FF) == LANG_VIETNAMESE; }
        catch { return false; }
    }

    public static bool LaTiengViet => CaiDat.HienTai.NgonNgu switch
    {
        TiengViet => true,
        TiengAnh => false,
        _ => windowsTiengViet,
    };

    /// Báo các cửa sổ đang mở dựng lại chữ khi người dùng đổi ngôn ngữ
    public static event Action? Doi;

    public static void Dat(string ma)
    {
        if (CaiDat.HienTai.NgonNgu == ma) return;
        CaiDat.HienTai.NgonNgu = ma;
        CaiDat.HienTai.Luu();
        Doi?.Invoke();
    }

    public static string T(string vi, string en) => LaTiengViet ? vi : en;
}

static class Chu
{
    static string T(string vi, string en) => NgonNgu.T(vi, en);
    public const string TenApp = "VGScreenshotHDR";

    // ── chung
    public static string MoTaNgan => T("Chụp màn hình HDR đúng màu, không cháy sáng", "True-color HDR screenshots, no blown-out highlights");
    public static string PhienBan(string v) => T($"Phiên bản {v} · by VGS", $"Version {v} · by VGS");
    public static string ChuaDatPhim => T("(chưa đặt)", "(not set)");
    public static string MoVgs => T("Mở vgs.zone", "Open vgs.zone");
    public static string GioiThieu => T("Giới thiệu", "About");
    public static string Dong => T("Đóng", "Close");
    public static string DaChep => T("Đã chép vào clipboard", "Copied to clipboard");
    public static string VaLuu(string file) => T(" · lưu ", " · saved ") + file;
    public static string DaLuu(string file) => T("Đã lưu ", "Saved ") + file;
    public static string LoiChep(string loi) => T("Lỗi khi chép: ", "Copy failed: ") + loi;
    public static string LoiLuu(string loi) => T("Lỗi khi lưu: ", "Save failed: ") + loi;
    public static string ChupLoi => T("Chụp lỗi", "Capture failed");
    public static string DangChayRoi => T("VGScreenshotHDR đang chạy rồi, xem ở khay hệ thống (góc dưới phải).",
                                          "VGScreenshotHDR is already running. Look for it in the system tray (bottom right).");

    // ── quay màn hình
    public static string QuayManHinh => T("Quay màn hình", "Record screen");
    public static string DungQuay => T("Dừng", "Stop");
    public static string DangQuay => T("Đang quay…", "Recording…");
    public static string QuayXong(string file) => T("Đã lưu video ", "Video saved ") + file;
    public static string QuayLoi => T("Quay lỗi", "Recording failed");
    public static string QuayBatDau(string phim) => T($"Bắt đầu quay — bấm {phim} hoặc nút Dừng để kết thúc", $"Recording started — press {phim} or Stop to finish");

    // ── 3 kiểu chụp
    public static string ChupChonVung => T("Chụp chọn vùng", "Capture region");
    public static string ChupToanMan => T("Chụp toàn màn hình", "Capture full screen");
    public static string ChupCuaSo => T("Chụp cửa sổ đang mở", "Capture active window");
    public static string NutChonVung => T("Chọn vùng", "Region");
    public static string NutToanMan => T("Toàn màn hình", "Full screen");
    public static string NutCuaSo => T("Cửa sổ đang mở", "Active window");
    public static string MoTaChonVung => T("Màn hình đứng lại, kéo chọn, vẽ chú thích", "Freeze the screen, drag to select, annotate");
    public static string MoTaToanMan => T("Chép thẳng vào clipboard + lưu file", "Straight to clipboard + saved file");
    public static string MoTaCuaSo => T("Chỉ cửa sổ bạn đang làm việc", "Just the window you're working in");

    // ── khay + menu
    public static string GoiYKhay(string phim) => T($"{TenApp} · {phim} để chụp", $"{TenApp} · {phim} to capture");
    public static string MoCaiDat => T("Mở cửa sổ cài đặt…", "Open settings…");
    public static string NhapDupIcon => T("Nhấp đúp icon", "Double-click icon");
    public static string MoThuMucAnh => T("Mở thư mục ảnh", "Open screenshots folder");
    public static string DoiThuMucLuu => T("Đổi thư mục lưu…", "Change save folder…");
    public static string TuLuuKhiChep => T("Tự lưu file khi chép", "Auto-save when copying");
    public static string TuChayKhiDangNhap => T("Tự chạy khi đăng nhập", "Start with Windows");
    public static string TraPhimSettings => T("Trả phím PrtScn cho VGS (mở Settings)…", "Give PrtScn back to VGS (open Settings)…");
    public static string TraPhimTatSnip => T("Trả phím PrtScn cho VGS (tắt Snipping Tool)", "Give PrtScn back to VGS (turn off Snipping Tool)");
    public static string ThongTinManHdr => T("Thông tin màn hình HDR…", "HDR display info…");
    public static string XemNhatKy => T("Xem nhật ký lỗi", "View error log");
    public static string GioiThieuMenu => T("Giới thiệu…", "About…");
    public static string ThemTienIch => T("Thêm tiện ích VGS tại vgs.zone", "More VGS tools at vgs.zone");
    public static string Thoat => T("Thoát", "Exit");
    public static string ChonThuMuc => T("Chọn thư mục lưu ảnh chụp", "Choose where to save screenshots");

    // ── phím bị giữ / Snipping Tool
    public static string ChuaBatDuocPhim(string phim) => T("Chưa bắt được phím ", "Can't use key ") + phim;
    public static string SnipGiuPrtScn => T("Windows đang gán PrtScn cho Snipping Tool. Bấm phải vào biểu tượng → 'Trả phím PrtScn cho VGS' rồi làm theo hướng dẫn.",
                                            "Windows has assigned PrtScn to Snipping Tool. Right-click the tray icon → 'Give PrtScn back to VGS' and follow the steps.");
    public static string AppKhacGiuPhim => T("App khác (Lightshot, ShareX, Greenshot…) đang giữ phím này. Tắt app đó, hoặc đổi phím trong cửa sổ VGScreenshotHDR.",
                                             "Another app (Lightshot, ShareX, Greenshot…) is using this key. Close that app, or pick another key in the VGScreenshotHDR window.");
    public static string HuongDanSettingsSnip => T(
        "Windows đang gán phím PrtScn cho Snipping Tool.\n\nTrong trang Settings vừa mở, tắt mục \"Use the Print screen key to open screen capture\" "
        + "(Dùng phím Print screen để mở chụp màn hình), rồi đăng xuất và đăng nhập lại Windows để phím PrtScn về VGScreenshotHDR.",
        "Windows has assigned PrtScn to Snipping Tool.\n\nIn the Settings page that just opened, turn off \"Use the Print screen key to open screen capture\", "
        + "then sign out and back in so PrtScn goes to VGScreenshotHDR.");
    public static string DaTatSnip => T("Đã tắt 'PrtScn mở Snipping Tool'. Đăng xuất rồi đăng nhập lại Windows (hoặc khởi động lại) để phím PrtScn về VGScreenshotHDR.",
                                        "Turned off 'PrtScn opens Snipping Tool'. Sign out and back in (or restart) so PrtScn goes to VGScreenshotHDR.");
    public static string KhongSuaDuoc(string loi) => T("Không sửa được: ", "Couldn't change it: ") + loi;

    // ── thông tin màn hình
    public static string ThongTinMan => T("Thông tin màn hình", "Display info");
    public static string ManTai(string ten, int w, int h, int x, int y) => T($"{ten}: {w}×{h} tại ({x},{y})", $"{ten}: {w}×{h} at ({x},{y})");
    public static string HdrDangBat => T("HDR đang bật", "HDR on");
    public static string DinhDangKhung(string f) => T("định dạng khung ", "frame format ") + f;
    public static string MucTrang(float trang, float dinh) => T($"Mức trắng SDR: {trang:0} nits · đỉnh sáng: {dinh:0} nits", $"SDR white level: {trang:0} nits · peak: {dinh:0} nits");
    public static string NhatKyO(string duong) => T("Nhật ký: ", "Log: ") + duong;
    public static string LoiXemNhatKy(string loi, string duong) => T($"Lỗi: {loi}\n\nXem nhật ký: {duong}", $"Error: {loi}\n\nSee log: {duong}");

    // ── tự chạy (gói Store)
    public static string TuChayBiTat => T("Bạn đã tắt mục này trong Settings > Apps > Startup, bật lại ở đó.", "You turned this off in Settings > Apps > Startup. Turn it back on there.");
    public static string TuChayBiChan => T("Chính sách máy không cho app tự chạy.", "A system policy blocks apps from starting automatically.");
    public static string TuChayKhongDuoc => T("Windows không cho bật tự chạy lúc này.", "Windows won't allow auto-start right now.");

    // ── cửa sổ chính
    public static string ChupNgay => T("Chụp ngay", "Capture now");
    public static string PhimTat => T("Phím tắt", "Shortcuts");
    public static string HuongDanGhiPhim => T("Bấm vào ô rồi nhấn tổ hợp phím mới. Esc để huỷ, Delete để bỏ trống.", "Click a box, then press the new key combo. Esc cancels, Delete clears.");
    public static string NhanPhim => T("Nhấn phím…", "Press keys…");
    public static string KhoiPhucPhim => T("Khôi phục phím mặc định", "Restore default keys");
    public static string PhimDangChay => T("✓ Đang hoạt động", "✓ Active");
    public static string PhimDuPhong => T("✓ Hoạt động (bắt dự phòng)", "✓ Active (fallback hook)");
    public static string PhimChuaDat => T("Chưa đặt phím", "No key set");
    public static string PhimBiGiu => T("✗ App khác đang giữ", "✗ Used by another app");
    public static string LuuAnh => T("Lưu ảnh", "Saving");
    public static string Doi => T("Đổi…", "Change…");
    public static string MoThuMuc => T("Mở thư mục", "Open folder");
    public static string TuLuuMoiLanChep => T("Tự lưu file mỗi khi chép ảnh vào clipboard", "Also save a file every time I copy to the clipboard");
    public static string KhoiDong => T("Khởi động", "Startup");
    public static string TuChayCungWindows => T("Tự chạy VGScreenshotHDR khi đăng nhập Windows", "Start VGScreenshotHDR when I sign in to Windows");
    public static string NgonNguTieuDe => T("Ngôn ngữ", "Language");
    public static string NgonNguTuDong => T("Tự động (theo Windows)", "Automatic (follow Windows)");
    public static string ThemTienIchTai => T("Thêm tiện ích miễn phí tại vgs.zone", "More free tools at vgs.zone");

    // ── cửa sổ chính kiểu thanh bên
    public static string TongQuan => T("Tổng quan", "Overview");
    public static string Chung => T("Chung", "General");
    public static string ThemTienIchNgan => T("Thêm tiện ích vgs.zone", "More tools at vgs.zone");
    public static string HdrBat(float nit) => T($"HDR đang bật · trắng SDR {nit:0} nit", $"HDR on · SDR white {nit:0} nits");
    public static string HdrTat => T("HDR đang tắt", "HDR is off");
    public static string MoTaHdrBat(string ten, int w, int h) => T($"{ten} · {w}×{h} · ảnh chụp sẽ đúng màu", $"{ten} · {w}×{h} · screenshots keep true colors");
    public static string MoTaHdrTat(string ten, int w, int h) => T($"{ten} · {w}×{h} · chụp như bình thường, bật HDR trong Settings > Display",
                                                                  $"{ten} · {w}×{h} · captures as usual, turn on HDR in Settings > Display");
    public static string KhongDocDuocMan => T("Không đọc được thông tin màn hình", "Couldn't read display info");
    public static string ThuMucLuu => T("Thư mục lưu", "Save folder");
    public static string MoTaTuLuu => T("Mỗi lần chép vào clipboard cũng lưu 1 file PNG", "Every copy to the clipboard also saves a PNG");
    public static string MoTaTuChay => T("Biểu tượng nằm sẵn ở khay, bấm phím tắt là chụp", "Sits in the tray, ready when you press the shortcut");
    public static string MoTaNgonNgu => T("Mặc định theo ngôn ngữ Windows", "Follows your Windows language by default");
    public static string TuDongNgan => T("Tự động", "Automatic");
    public static string SuaPhim => T("Đổi phím", "Change key");
    public static string XoaPhim => T("Bỏ phím", "Clear key");
    public static string HuyGhi => T("Huỷ (Esc)", "Cancel (Esc)");
    public static string TrangThaiHoatDong => T("Hoạt động", "Active");
    public static string TrangThaiDuPhong => T("Dự phòng", "Fallback");
    public static string GoiYDuPhong => T("App khác đang giữ tổ hợp này, VGS bắt phím bằng hook dự phòng nên vẫn chạy.",
                                          "Another app holds this combo; VGS catches it with a fallback hook, so it still works.");
    public static string TrangThaiChuaDat => T("Chưa đặt", "Not set");
    public static string TrangThaiBiGiu => T("Bị giữ", "Taken");
    public static string GoiYBiGiu => T("App khác đang giữ tổ hợp này. Đổi phím khác hoặc tắt app kia.", "Another app holds this combo. Pick another key or close that app.");
    public static string Xong => T("Xong", "Done");

    // ── hộp giới thiệu
    public static string GioiThieuTieuDe => T("Giới thiệu VGScreenshotHDR", "About VGScreenshotHDR");
    public static string GioiThieuNoiDung => T(
        "Bật HDR thì nút PrtScn của Windows chụp ra ảnh bạc màu, cháy sáng. VGScreenshotHDR đọc thẳng dữ liệu HDR của màn hình rồi chuyển về đúng màu mắt bạn thấy.",
        "With HDR on, Windows' PrtScn gives you washed-out, blown-out screenshots. VGScreenshotHDR reads the display's HDR data directly and converts it to the colors you actually see.");
    public static string GoiYChonVung => T("Chọn vùng, vẽ chú thích, chép / lưu", "Select, annotate, copy / save");
    public static string GoiYToanMan => T("Chụp toàn màn hình", "Full screen");
    public static string GoiYCuaSo => T("Chụp cửa sổ đang mở", "Active window");
    public static string CongCuTrongKhung => T("Trong khung chọn: P bút · L đường · A mũi tên · R khung · M đánh dấu · T chữ · Ctrl+Z hoàn tác",
                                               "While selecting: P pen · L line · A arrow · R rectangle · M highlighter · T text · Ctrl+Z undo");
    public static string ThemTienIchDai => T("Thêm tiện ích miễn phí cho game thủ & dân văn phòng tại vgs.zone", "More free tools for gamers & office folks at vgs.zone");

    // ── khung chọn vùng
    public static string ButVe => T("Bút vẽ tự do", "Pen");
    public static string DuongThang => T("Đường thẳng", "Line");
    public static string MuiTen => T("Mũi tên", "Arrow");
    public static string KhungChuNhat => T("Khung chữ nhật", "Rectangle");
    public static string ButDanhDau => T("Bút đánh dấu", "Highlighter");
    public static string ChuGo => T("Chữ (bấm vào ảnh để gõ)", "Text (click the image to type)");
    public static string MauVe => T("Màu vẽ", "Color");
    public static string HoanTac => T("Hoàn tác (Ctrl+Z)", "Undo (Ctrl+Z)");
    public static string ChepClipboard => T("Chép vào clipboard (Enter / Ctrl+C)", "Copy to clipboard (Enter / Ctrl+C)");
    public static string LuuFile => T("Lưu file… (Ctrl+S)", "Save file… (Ctrl+S)");
    public static string DongEsc => T("Đóng (Esc)", "Close (Esc)");
    public static string MauKhac => T("Màu khác…", "More colors…");
    public static string HuongDanKeo => T("Kéo chuột để chọn vùng  ·  Ctrl+A cả màn hình  ·  Esc thoát", "Drag to select  ·  Ctrl+A full screen  ·  Esc to exit");
    public static string LocFile => T("Ảnh PNG|*.png|Ảnh JPG|*.jpg", "PNG image|*.png|JPG image|*.jpg");
    public static string LuuAnhChup => T("Lưu ảnh chụp", "Save screenshot");
    public static (string ten, Color mau)[] BangMau => new[]
    {
        (T("Đỏ", "Red"), Color.FromArgb(255, 59, 48)), (T("Cam", "Orange"), Color.FromArgb(255, 149, 0)), (T("Vàng", "Yellow"), Color.FromArgb(255, 204, 0)),
        (T("Xanh lá", "Green"), Color.FromArgb(52, 199, 89)), (T("Xanh dương", "Blue"), Color.FromArgb(0, 122, 255)), (T("Tím", "Purple"), Color.FromArgb(175, 82, 222)),
        (T("Hồng", "Pink"), Color.FromArgb(236, 72, 153)), (T("Trắng", "White"), Color.White), (T("Đen", "Black"), Color.Black),
    };
}
