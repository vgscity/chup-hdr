using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Microsoft.Win32;

namespace VgsChupHdr;

/// Ứng dụng chạy ở khay hệ thống: nhận hotkey, mở khung chọn, menu cài đặt.
sealed class UngDungKhay : ApplicationContext
{
    const int HK_CHON = 1, HK_TOAN = 2, HK_CUA_SO = 3;
    readonly NotifyIcon khay;
    readonly CuaSoHotkey cuaSo;
    KhungChon? khungDangMo;
    ToolStripMenuItem mucTuLuu = null!, mucTuChay = null!, mucSuaSnip = null!;

    public UngDungKhay()
    {
        cuaSo = new CuaSoHotkey(XuLyHotkey);
        khay = new NotifyIcon
        {
            Icon = TaoIcon(),
            Text = "VGS Chụp HDR · PrtScn để chụp",
            Visible = true,
            ContextMenuStrip = TaoMenu(),
        };
        khay.DoubleClick += (_, _) => MoKhungChon();
        khay.BalloonTipClicked += (_, _) => MoThuMuc();

        DangKyHotkey();
        NhatKy.Ghi("Khởi động VGS Chụp HDR " + Application.ProductVersion);
    }

    // ───── hotkey ─────
    void DangKyHotkey()
    {
        bool okChon = Win32.RegisterHotKey(cuaSo.Handle, HK_CHON, Win32.MOD_NOREPEAT, Win32.VK_SNAPSHOT);
        bool okToan = Win32.RegisterHotKey(cuaSo.Handle, HK_TOAN, Win32.MOD_SHIFT | Win32.MOD_NOREPEAT, Win32.VK_SNAPSHOT);
        bool okCuaSo = Win32.RegisterHotKey(cuaSo.Handle, HK_CUA_SO, Win32.MOD_ALT | Win32.MOD_NOREPEAT, Win32.VK_SNAPSHOT);
        if (!okChon)
        {
            NhatKy.Ghi("Không đăng ký được phím PrtScn (app khác đang giữ, hoặc Windows đang gán PrtScn cho Snipping Tool)");
            string loi = SnipDangGiuPrtScn()
                ? "Windows đang gán PrtScn cho Snipping Tool. Bấm phải vào biểu tượng → 'Trả phím PrtScn cho VGS' rồi làm theo hướng dẫn."
                : "App khác (Lightshot, ShareX, Greenshot…) đang giữ phím PrtScn. Tắt app đó rồi mở lại VGS Chụp HDR.";
            khay.ShowBalloonTip(8000, "Chưa bắt được phím PrtScn", loi, ToolTipIcon.Warning);
        }
        if (!okToan || !okCuaSo) NhatKy.Ghi($"Hotkey phụ: Shift+PrtScn={okToan}, Alt+PrtScn={okCuaSo}");
    }

    void XuLyHotkey(int id)
    {
        try
        {
            switch (id)
            {
                case HK_CHON: MoKhungChon(); break;
                case HK_TOAN: ChupNhanh(toanBo: true); break;
                case HK_CUA_SO: ChupNhanh(toanBo: false); break;
            }
        }
        catch (Exception e)
        {
            NhatKy.Ghi("Xử lý hotkey lỗi", e);
            khay.ShowBalloonTip(5000, "Chụp lỗi", e.Message, ToolTipIcon.Error);
        }
    }

    static bool SnipDangGiuPrtScn()
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(@"Control Panel\Keyboard");
            return (k?.GetValue("PrintScreenKeyForSnippingEnabled") as int?) == 1;
        }
        catch { return false; }
    }

    // ───── chụp ─────
    void MoKhungChon()
    {
        if (khungDangMo != null && !khungDangMo.IsDisposed) { khungDangMo.Activate(); return; }
        var kq = Chup.ChupTatCa();
        NhatKy.Ghi("Chụp: " + string.Join(" | ", kq.ManHinh.Select(MoTa)));
        khungDangMo = new KhungChon(kq, ThongBao);
        khungDangMo.FormClosed += (_, _) => khungDangMo = null;
        khungDangMo.Show();
    }

    void ChupNhanh(bool toanBo)
    {
        using var kq = Chup.ChupTatCa();
        Rectangle vung = kq.VungAo;
        if (!toanBo)
        {
            var cs = Win32.VungCuaSoDangMo();
            cs.Intersect(kq.VungAo);
            if (cs.Width > 0 && cs.Height > 0) vung = cs;
        }
        vung.Offset(-kq.VungAo.X, -kq.VungAo.Y);
        using var a = kq.Anh.Clone(vung, kq.Anh.PixelFormat);
        XuatAnh.Chep(a);
        string loi = "Đã chép vào clipboard";
        if (CaiDat.HienTai.TuLuuKhiChep) loi += " · lưu " + Path.GetFileName(XuatAnh.Luu(a));
        ThongBao(loi);
    }

    void ThongBao(string loi) => khay.ShowBalloonTip(2500, "VGS Chụp HDR", loi, ToolTipIcon.Info);

    static string MoTa(ThongTinManHinh m) =>
        $"{m.Ten} {m.Vung.Width}×{m.Vung.Height} {(m.Hdr ? "HDR" : "SDR")} trắngSDR={m.TrangSdrNits:0}nits đỉnh={m.DinhNits:0}nits fmt={m.DinhDang}{(m.CoNoiDungHdr ? " (có nội dung HDR)" : "")}{(m.GhiChu != "" ? " " + m.GhiChu : "")}";

    // ───── menu ─────
    ContextMenuStrip TaoMenu()
    {
        var m = new ContextMenuStrip();
        m.Items.Add("Chụp chọn vùng\tPrtScn", null, (_, _) => MoKhungChon());
        m.Items.Add("Chụp toàn màn hình\tShift+PrtScn", null, (_, _) => XuLyHotkey(HK_TOAN));
        m.Items.Add("Chụp cửa sổ đang mở\tAlt+PrtScn", null, (_, _) => XuLyHotkey(HK_CUA_SO));
        m.Items.Add(new ToolStripSeparator());
        m.Items.Add("Mở thư mục ảnh", null, (_, _) => MoThuMuc());
        m.Items.Add("Đổi thư mục lưu…", null, (_, _) => DoiThuMuc());
        mucTuLuu = new ToolStripMenuItem("Tự lưu file khi chép") { Checked = CaiDat.HienTai.TuLuuKhiChep, CheckOnClick = true };
        mucTuLuu.CheckedChanged += (_, _) => { CaiDat.HienTai.TuLuuKhiChep = mucTuLuu.Checked; CaiDat.HienTai.Luu(); };
        m.Items.Add(mucTuLuu);
        mucTuChay = new ToolStripMenuItem("Tự chạy khi đăng nhập") { Checked = !GoiStore.DangTrongGoi && DangTuChay(), CheckOnClick = true };
        mucTuChay.CheckedChanged += (_, _) => { if (!dangDatTuChay) _ = DatTuChayAsync(mucTuChay.Checked); };
        m.Items.Add(mucTuChay);
        if (GoiStore.DangTrongGoi) _ = CapNhatTuChayTuGoiAsync();
        m.Items.Add(new ToolStripSeparator());
        mucSuaSnip = new ToolStripMenuItem(GoiStore.DangTrongGoi ? "Trả phím PrtScn cho VGS (mở Settings)…" : "Trả phím PrtScn cho VGS (tắt Snipping Tool)");
        mucSuaSnip.Click += (_, _) => TraPhimPrtScn();
        m.Items.Add(mucSuaSnip);
        m.Items.Add("Thông tin màn hình HDR…", null, (_, _) => HienThongTinManHinh());
        m.Items.Add("Xem nhật ký lỗi", null, (_, _) => { NhatKy.Ghi("Mở nhật ký"); Process.Start(new ProcessStartInfo(NhatKy.DuongDan) { UseShellExecute = true }); });
        m.Items.Add(new ToolStripSeparator());
        m.Items.Add("Thoát", null, (_, _) => Thoat());
        m.Opening += (_, _) => mucSuaSnip.Visible = SnipDangGiuPrtScn();
        return m;
    }

    void MoThuMuc()
    {
        Directory.CreateDirectory(CaiDat.HienTai.ThuMuc);
        Process.Start(new ProcessStartInfo(CaiDat.HienTai.ThuMuc) { UseShellExecute = true });
    }

    void DoiThuMuc()
    {
        using var hop = new FolderBrowserDialog { SelectedPath = CaiDat.HienTai.ThuMuc, Description = "Chọn thư mục lưu ảnh chụp", UseDescriptionForTitle = true };
        if (hop.ShowDialog() == DialogResult.OK) { CaiDat.HienTai.ThuMuc = hop.SelectedPath; CaiDat.HienTai.Luu(); }
    }

    void TraPhimPrtScn()
    {
        if (GoiStore.DangTrongGoi)
        {
            Process.Start(new ProcessStartInfo("ms-settings:easeofaccess-keyboard") { UseShellExecute = true });
            MessageBox.Show("Windows đang gán phím PrtScn cho Snipping Tool.\n\nTrong trang Settings vừa mở, tắt mục "
                + "\"Use the Print screen key to open screen capture\" (Dùng phím Print screen để mở chụp màn hình), "
                + "rồi đăng xuất và đăng nhập lại Windows để phím PrtScn về VGS Chụp HDR.",
                "VGS Chụp HDR", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(@"Control Panel\Keyboard", writable: true);
            k?.SetValue("PrintScreenKeyForSnippingEnabled", 0, RegistryValueKind.DWord);
            MessageBox.Show("Đã tắt 'PrtScn mở Snipping Tool'. Đăng xuất rồi đăng nhập lại Windows (hoặc khởi động lại) để phím PrtScn về VGS Chụp HDR.",
                "VGS Chụp HDR", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception e) { NhatKy.Ghi("Sửa registry PrtScn lỗi", e); MessageBox.Show("Không sửa được: " + e.Message); }
    }

    void HienThongTinManHinh()
    {
        try
        {
            using var kq = Chup.ChupTatCa();
            var dong = kq.ManHinh.Select(m =>
                $"{m.Ten}: {m.Vung.Width}×{m.Vung.Height} tại ({m.Vung.X},{m.Vung.Y})\n" +
                $"   {(m.Hdr ? "HDR đang bật" : "SDR")} · định dạng khung {m.DinhDang}\n" +
                $"   Mức trắng SDR: {m.TrangSdrNits:0} nits · đỉnh sáng: {m.DinhNits:0} nits" +
                (m.GhiChu != "" ? "\n   " + m.GhiChu : ""));
            MessageBox.Show(string.Join("\n\n", dong) + $"\n\nNhật ký: {NhatKy.DuongDan}", "Thông tin màn hình", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception e) { MessageBox.Show("Lỗi: " + e.Message + "\n\nXem nhật ký: " + NhatKy.DuongDan, "Thông tin màn hình"); }
    }

    // ───── tự chạy ─────
    bool dangDatTuChay;
    async Task CapNhatTuChayTuGoiAsync()
    {
        bool bat = await GoiStore.DangTuChayAsync();
        dangDatTuChay = true; mucTuChay.Checked = bat; dangDatTuChay = false;
    }
    async Task DatTuChayAsync(bool bat)
    {
        if (!GoiStore.DangTrongGoi) { DatTuChay(bat); return; }
        var (ketQua, lyDo) = await GoiStore.DatTuChayAsync(bat);
        dangDatTuChay = true; mucTuChay.Checked = ketQua; dangDatTuChay = false;
        if (lyDo != null) khay.ShowBalloonTip(6000, "Tự chạy khi đăng nhập", lyDo, ToolTipIcon.Warning);
    }
    const string KhoaRun = @"Software\Microsoft\Windows\CurrentVersion\Run";
    static bool DangTuChay()
    {
        using var k = Registry.CurrentUser.OpenSubKey(KhoaRun);
        return k?.GetValue("VgsChupHdr") != null;
    }
    static void DatTuChay(bool bat)
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(KhoaRun, writable: true);
            if (bat) k?.SetValue("VgsChupHdr", $"\"{Environment.ProcessPath}\"");
            else k?.DeleteValue("VgsChupHdr", false);
        }
        catch (Exception e) { NhatKy.Ghi("Đặt tự chạy lỗi", e); }
    }

    // ───── icon: ô vuông gradient tím–hồng, chữ V trắng ─────
    static Icon TaoIcon()
    {
        try
        {
            using var luong = typeof(UngDungKhay).Assembly.GetManifestResourceStream("tray.png");
            if (luong != null) { using var b = new Bitmap(luong); return Icon.FromHandle(b.GetHicon()); }
        }
        catch (Exception e) { NhatKy.Ghi("Đọc icon nhúng lỗi, vẽ tay", e); }
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            using var nen = new LinearGradientBrush(new Rectangle(0, 0, 32, 32), Color.FromArgb(124, 58, 237), Color.FromArgb(236, 72, 153), 45f);
            using var duong = new GraphicsPath();
            duong.AddArc(0, 0, 12, 12, 180, 90); duong.AddArc(20, 0, 12, 12, 270, 90);
            duong.AddArc(20, 20, 12, 12, 0, 90); duong.AddArc(0, 20, 12, 12, 90, 90); duong.CloseFigure();
            g.FillPath(nen, duong);
            using var phong = new Font("Segoe UI", 20f, FontStyle.Bold, GraphicsUnit.Pixel);
            var k = g.MeasureString("V", phong);
            g.DrawString("V", phong, Brushes.White, (32 - k.Width) / 2, (32 - k.Height) / 2 + 1);
        }
        return Icon.FromHandle(bmp.GetHicon());
    }

    void Thoat()
    {
        khay.Visible = false;
        for (int id = 1; id <= 3; id++) Win32.UnregisterHotKey(cuaSo.Handle, id);
        cuaSo.DestroyHandle();
        ExitThread();
    }

    /// Cửa sổ ẩn chỉ để nhận WM_HOTKEY
    sealed class CuaSoHotkey : NativeWindow
    {
        readonly Action<int> khiBam;
        public CuaSoHotkey(Action<int> khiBam) { this.khiBam = khiBam; CreateHandle(new CreateParams()); }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Win32.WM_HOTKEY) khiBam((int)m.WParam);
            base.WndProc(ref m);
        }
    }
}
