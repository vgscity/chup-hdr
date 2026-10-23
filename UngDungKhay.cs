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
    HookBanPhim? hook;
    ToolStripMenuItem mucTuLuu = null!, mucTuChay = null!, mucSuaSnip = null!;
    ToolStripMenuItem mucChon = null!, mucToan = null!, mucCuaSo = null!;
    CuaSoChinh? cuaSoChinh;

    public UngDungKhay()
    {
        cuaSo = new CuaSoHotkey(XuLyHotkey);
        khay = new NotifyIcon
        {
            Icon = TaoIcon(),
            Text = Chu.TenApp,
            Visible = true,
            ContextMenuStrip = TaoMenu(),
        };
        khay.DoubleClick += (_, _) => MoCuaSoChinh();
        khay.BalloonTipClicked += (_, _) => MoThuMuc();
        NgonNgu.Doi += () =>
        {
            var cu = khay.ContextMenuStrip;
            khay.ContextMenuStrip = TaoMenu();
            cu?.Dispose();
            CapNhatNhanPhim();
            // Cửa sổ đang mở dựng lại bằng ngôn ngữ mới, giữ nguyên chỗ
            if (cuaSoChinh is { IsDisposed: false } cs)
            {
                var viTri = cs.Location;
                cs.Close();
                MoCuaSoChinh();
                cuaSoChinh!.StartPosition = FormStartPosition.Manual;
                cuaSoChinh.Location = viTri;
            }
            HopGioiThieu.DungLaiNeuDangMo();
        };

        DangKyHotkey();
        NhatKy.Ghi("Khởi động VGScreenshotHDR " + Application.ProductVersion);
    }

    // ───── hotkey ─────
    public enum TrangThaiPhim { ChuaDat, DangKy, HookDuPhong, Loi }
    public readonly Dictionary<int, TrangThaiPhim> TrangThai = new();
    public static readonly int[] CacHotkey = { HK_CHON, HK_TOAN, HK_CUA_SO };

    public static Keys PhimCua(int id) => id switch
    {
        HK_CHON => CaiDat.HienTai.PhimChon,
        HK_TOAN => CaiDat.HienTai.PhimToan,
        _ => CaiDat.HienTai.PhimCuaSo,
    };

    static uint ModCua(Keys k) =>
        (k.HasFlag(Keys.Alt) ? Win32.MOD_ALT : 0) | (k.HasFlag(Keys.Shift) ? Win32.MOD_SHIFT : 0) | (k.HasFlag(Keys.Control) ? Win32.MOD_CONTROL : 0);

    /// Đăng ký (lại) cả 3 phím theo cài đặt. Phím nào app khác đang giữ thì bắt bằng hook bàn phím dự phòng.
    public void DangKyHotkey()
    {
        HuyHotkey();
        var thatBai = new Dictionary<(uint vk, uint mod), int>();
        foreach (int id in CacHotkey)
        {
            var k = PhimCua(id);
            if ((k & Keys.KeyCode) == Keys.None) { TrangThai[id] = TrangThaiPhim.ChuaDat; continue; }
            uint vk = (uint)(k & Keys.KeyCode), mod = ModCua(k);
            if (Win32.RegisterHotKey(cuaSo.Handle, id, mod | Win32.MOD_NOREPEAT, vk)) TrangThai[id] = TrangThaiPhim.DangKy;
            else { thatBai[(vk, mod)] = id; TrangThai[id] = TrangThaiPhim.Loi; }
        }
        if (thatBai.Count > 0)
        {
            // App khác (Snipping Tool, phần mềm card đồ hoạ, Lightshot…) đang giữ tổ hợp → hook chạy trước nên vẫn bắt được
            hook = new HookBanPhim((vk, mod) => thatBai.TryGetValue((vk, mod), out int id) ? id : null, XuLyHotkey);
            if (hook.DaCai) foreach (int id in thatBai.Values) TrangThai[id] = TrangThaiPhim.HookDuPhong;
        }
        NhatKy.Ghi("Phím tắt: " + string.Join(", ", CacHotkey.Select(id => $"{CaiDat.TenPhim(PhimCua(id))}={TrangThai[id]}")));

        if (TrangThai[HK_CHON] == TrangThaiPhim.Loi)
        {
            string loi = SnipDangGiuPrtScn() ? Chu.SnipGiuPrtScn : Chu.AppKhacGiuPhim;
            khay.ShowBalloonTip(8000, Chu.ChuaBatDuocPhim(CaiDat.TenPhim(CaiDat.HienTai.PhimChon)), loi, ToolTipIcon.Warning);
        }
        CapNhatNhanPhim();
    }

    /// Gỡ toàn bộ phím (cũng dùng khi đang ghi phím mới trong cửa sổ chính, để bấm PrtScn không chụp ngay)
    public void HuyHotkey()
    {
        foreach (int id in CacHotkey) Win32.UnregisterHotKey(cuaSo.Handle, id);
        hook?.Dispose();
        hook = null;
    }

    void CapNhatNhanPhim()
    {
        mucChon.ShortcutKeyDisplayString = CaiDat.TenPhim(CaiDat.HienTai.PhimChon);
        mucToan.ShortcutKeyDisplayString = CaiDat.TenPhim(CaiDat.HienTai.PhimToan);
        mucCuaSo.ShortcutKeyDisplayString = CaiDat.TenPhim(CaiDat.HienTai.PhimCuaSo);
        var chu = Chu.GoiYKhay(CaiDat.TenPhim(CaiDat.HienTai.PhimChon));
        khay.Text = chu.Length > 63 ? chu[..63] : chu;   // NotifyIcon.Text tối đa 63 ký tự
    }

    public void XuLyHotkey(int id)
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
            khay.ShowBalloonTip(5000, Chu.ChupLoi, e.Message, ToolTipIcon.Error);
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
        khungDangMo.FormClosed += (_, _) => { khungDangMo = null; KhungChonDong?.Invoke(); };
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
        string loi = Chu.DaChep;
        if (CaiDat.HienTai.TuLuuKhiChep) loi += Chu.VaLuu(Path.GetFileName(XuatAnh.Luu(a)));
        ThongBao(loi);
    }

    void ThongBao(string loi) => khay.ShowBalloonTip(2500, Chu.TenApp, loi, ToolTipIcon.Info);

    static string MoTa(ThongTinManHinh m) =>
        $"{m.Ten} {m.Vung.Width}×{m.Vung.Height} {(m.Hdr ? "HDR" : "SDR")} trắngSDR={m.TrangSdrNits:0}nits đỉnh={m.DinhNits:0}nits fmt={m.DinhDang}{(m.CoNoiDungHdr ? " (có nội dung HDR)" : "")}{(m.GhiChu != "" ? " " + m.GhiChu : "")}";

    // ───── menu ─────
    ContextMenuStrip TaoMenu()
    {
        var m = new ContextMenuStrip { Renderer = new VeMenuToi(), ShowImageMargin = true };
        m.Items.Add(new DauMenu(MoCuaSoChinh));
        m.Items.Add(new ToolStripSeparator());
        // "\t" không tạo cột phím tắt trong ContextMenuStrip (chữ bị dính "Chụp chọn vùngPrtScn") → dùng ShortcutKeyDisplayString
        mucChon = new ToolStripMenuItem(Chu.ChupChonVung, null, (_, _) => XuLyHotkey(HK_CHON)) { Font = new Font(m.Font, FontStyle.Bold) };
        mucToan = new ToolStripMenuItem(Chu.ChupToanMan, null, (_, _) => XuLyHotkey(HK_TOAN));
        mucCuaSo = new ToolStripMenuItem(Chu.ChupCuaSo, null, (_, _) => XuLyHotkey(HK_CUA_SO));
        m.Items.AddRange(new ToolStripItem[] { mucChon, mucToan, mucCuaSo });
        m.Items.Add(new ToolStripSeparator());
        m.Items.Add(new ToolStripMenuItem(Chu.MoCaiDat, null, (_, _) => MoCuaSoChinh()) { ShortcutKeyDisplayString = Chu.NhapDupIcon });
        m.Items.Add(Chu.MoThuMucAnh, null, (_, _) => MoThuMuc());
        m.Items.Add(Chu.DoiThuMucLuu, null, (_, _) => DoiThuMuc());
        // Dựng lại menu khi đổi ngôn ngữ: giữ trạng thái tự chạy đang có (bản Store hỏi StartupTask bất đồng bộ)
        bool tuChayCu = mucTuChay != null ? mucTuChay.Checked : !GoiStore.DangTrongGoi && DangTuChay();
        mucTuLuu = new ToolStripMenuItem(Chu.TuLuuKhiChep) { Checked = CaiDat.HienTai.TuLuuKhiChep, CheckOnClick = true };
        mucTuLuu.CheckedChanged += (_, _) => { CaiDat.HienTai.TuLuuKhiChep = mucTuLuu.Checked; CaiDat.HienTai.Luu(); CaiDatDoi?.Invoke(); };
        m.Items.Add(mucTuLuu);
        mucTuChay = new ToolStripMenuItem(Chu.TuChayKhiDangNhap) { Checked = tuChayCu, CheckOnClick = true };
        mucTuChay.CheckedChanged += (_, _) => { if (!dangDatTuChay) _ = DatTuChayAsync(mucTuChay.Checked); CaiDatDoi?.Invoke(); };
        m.Items.Add(mucTuChay);
        if (GoiStore.DangTrongGoi && !daHoiTuChay) { daHoiTuChay = true; _ = CapNhatTuChayTuGoiAsync(); }
        m.Items.Add(new ToolStripSeparator());
        mucSuaSnip = new ToolStripMenuItem(GoiStore.DangTrongGoi ? Chu.TraPhimSettings : Chu.TraPhimTatSnip);
        mucSuaSnip.Click += (_, _) => TraPhimPrtScn();
        m.Items.Add(mucSuaSnip);
        m.Items.Add(Chu.ThongTinManHdr, null, (_, _) => HienThongTinManHinh());
        m.Items.Add(Chu.XemNhatKy, null, (_, _) => { NhatKy.Ghi("Mở nhật ký"); Process.Start(new ProcessStartInfo(NhatKy.DuongDan) { UseShellExecute = true }); });
        m.Items.Add(new ToolStripSeparator());
        m.Items.Add(Chu.GioiThieuMenu, null, (_, _) => HopGioiThieu.Hien());
        m.Items.Add(new ToolStripMenuItem(Chu.ThemTienIch, null, (_, _) => GiaoDien.MoTrangVgs()) { ForeColor = GiaoDien.Hong });
        m.Items.Add(new ToolStripSeparator());
        m.Items.Add(Chu.Thoat, null, (_, _) => Thoat());
        m.Opening += (_, _) => mucSuaSnip.Visible = SnipDangGiuPrtScn();
        return m;
    }

    public void MoThuMuc()
    {
        Directory.CreateDirectory(CaiDat.HienTai.ThuMuc);
        Process.Start(new ProcessStartInfo(CaiDat.HienTai.ThuMuc) { UseShellExecute = true });
    }

    public void DoiThuMuc(IWin32Window? chu = null)
    {
        using var hop = new FolderBrowserDialog { SelectedPath = CaiDat.HienTai.ThuMuc, Description = Chu.ChonThuMuc, UseDescriptionForTitle = true };
        if (hop.ShowDialog(chu) == DialogResult.OK) { CaiDat.HienTai.ThuMuc = hop.SelectedPath; CaiDat.HienTai.Luu(); CaiDatDoi?.Invoke(); }
    }

    // ───── cửa sổ chính (nhấp đúp icon khay) ─────
    /// Báo cửa sổ chính vẽ lại khi cài đặt đổi từ menu khay (hoặc StartupTask trả kết quả muộn)
    public event Action? CaiDatDoi;
    public event Action? KhungChonDong;

    public bool TuLuu { get => mucTuLuu.Checked; set => mucTuLuu.Checked = value; }
    public bool TuChay { get => mucTuChay.Checked; set => mucTuChay.Checked = value; }

    void MoCuaSoChinh()
    {
        if (cuaSoChinh == null || cuaSoChinh.IsDisposed)
        {
            cuaSoChinh = new CuaSoChinh(this);
            cuaSoChinh.FormClosed += (_, _) => cuaSoChinh = null;
            cuaSoChinh.Show();
        }
        if (cuaSoChinh.WindowState == FormWindowState.Minimized) cuaSoChinh.WindowState = FormWindowState.Normal;
        cuaSoChinh.Activate();
    }

    void TraPhimPrtScn()
    {
        if (GoiStore.DangTrongGoi)
        {
            Process.Start(new ProcessStartInfo("ms-settings:easeofaccess-keyboard") { UseShellExecute = true });
            MessageBox.Show(Chu.HuongDanSettingsSnip, Chu.TenApp, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(@"Control Panel\Keyboard", writable: true);
            k?.SetValue("PrintScreenKeyForSnippingEnabled", 0, RegistryValueKind.DWord);
            MessageBox.Show(Chu.DaTatSnip, Chu.TenApp, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception e) { NhatKy.Ghi("Sửa registry PrtScn lỗi", e); MessageBox.Show(Chu.KhongSuaDuoc(e.Message)); }
    }

    void HienThongTinManHinh()
    {
        try
        {
            using var kq = Chup.ChupTatCa();
            var dong = kq.ManHinh.Select(m =>
                Chu.ManTai(m.Ten, m.Vung.Width, m.Vung.Height, m.Vung.X, m.Vung.Y) + "\n" +
                $"   {(m.Hdr ? Chu.HdrDangBat : "SDR")} · {Chu.DinhDangKhung(m.DinhDang)}\n" +
                "   " + Chu.MucTrang(m.TrangSdrNits, m.DinhNits) +
                (m.GhiChu != "" ? "\n   " + m.GhiChu : ""));
            MessageBox.Show(string.Join("\n\n", dong) + "\n\n" + Chu.NhatKyO(NhatKy.DuongDan), Chu.ThongTinMan, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception e) { MessageBox.Show(Chu.LoiXemNhatKy(e.Message, NhatKy.DuongDan), Chu.ThongTinMan); }
    }

    // ───── tự chạy ─────
    bool dangDatTuChay, daHoiTuChay;
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
        if (lyDo != null) khay.ShowBalloonTip(6000, Chu.TuChayKhiDangNhap, lyDo, ToolTipIcon.Warning);
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
        hook?.Dispose();
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
