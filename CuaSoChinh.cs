using System.Drawing;
using System.Windows.Forms;

namespace VgsChupHdr;

/// Cửa sổ chính (nhấp đúp icon khay): chụp nhanh, đổi phím tắt, thư mục lưu, tự chạy khi khởi động.
sealed class CuaSoChinh : Form
{
    readonly UngDungKhay app;
    readonly float k;
    readonly Dictionary<int, (O_Phim o, Label trangThai)> hangPhim = new();
    readonly Label nhanThuMuc;
    readonly CheckBox hopTuLuu, hopTuChay;
    readonly ToolTip goiY = new();
    bool dangNapLai;

    readonly (string ten, string moTa)[] thongTinPhim =
    {
        (Chu.ChupChonVung, Chu.MoTaChonVung),
        (Chu.ChupToanMan, Chu.MoTaToanMan),
        (Chu.ChupCuaSo, Chu.MoTaCuaSo),
    };

    public CuaSoChinh(UngDungKhay app)
    {
        this.app = app;
        Text = Chu.TenApp;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = GiaoDien.Nen;
        ForeColor = GiaoDien.Chu;
        Font = new Font("Segoe UI", 10f);
        AutoScaleMode = AutoScaleMode.None;   // bố cục tự co theo chữ, chỉ bề rộng đặt theo DPI
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        KeyPreview = true;
        try { Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath!); } catch { }

        k = DeviceDpi / 96f;
        int rong = Px(560), rongTrong = rong - Px(48);

        var khung = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, Margin = Padding.Empty };
        Controls.Add(khung);

        // ── dải đầu
        var dai = new HopGioiThieu.DaiGradient { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = Padding.Empty, Padding = new Padding(Px(24), Px(20), Px(24), Px(20)), MinimumSize = new Size(rong, 0), WrapContents = false };
        dai.Controls.Add(new PictureBox { Image = GiaoDien.Logo(), SizeMode = PictureBoxSizeMode.Zoom, Size = new Size(Px(52), Px(52)), Margin = new Padding(0, 0, Px(16), 0), BackColor = Color.Transparent });
        var cotTen = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, BackColor = Color.Transparent, Margin = Padding.Empty, WrapContents = false };
        cotTen.Controls.Add(Nhan(Chu.TenApp, new Font("Segoe UI Semibold", 16f), Color.White));
        cotTen.Controls.Add(Nhan($"{Chu.MoTaNgan} · v{GiaoDien.PhienBan}", new Font("Segoe UI", 9.5f), Color.FromArgb(240, 240, 240)));
        dai.Controls.Add(cotTen);
        khung.Controls.Add(dai);

        var than = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, Padding = new Padding(Px(24), Px(8), Px(24), Px(20)), Margin = Padding.Empty };
        khung.Controls.Add(than);

        // ── chụp ngay
        than.Controls.Add(TieuDe(Chu.ChupNgay));
        var hangChup = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 3, Margin = Padding.Empty, MinimumSize = new Size(rongTrong, 0) };
        for (int i = 0; i < 3; i++) hangChup.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        string[] tenNut = { Chu.NutChonVung, Chu.NutToanMan, Chu.NutCuaSo };
        for (int i = 0; i < 3; i++)
        {
            int id = UngDungKhay.CacHotkey[i];
            var b = Nut(tenNut[i], i == 0);
            b.Dock = DockStyle.Fill;
            b.Margin = new Padding(i == 0 ? 0 : Px(4), 0, i == 2 ? 0 : Px(4), 0);
            b.Click += async (_, _) => await ChupTuCuaSo(id);
            hangChup.Controls.Add(b, i, 0);
        }
        than.Controls.Add(hangChup);

        // ── phím tắt
        than.Controls.Add(TieuDe(Chu.PhimTat));
        than.Controls.Add(Nhan(Chu.HuongDanGhiPhim, new Font("Segoe UI", 9f), GiaoDien.ChuMo, rongTrong, new Padding(0, 0, 0, Px(6))));
        var bangPhim = new TableLayoutPanel { AutoSize = true, ColumnCount = 3, Margin = Padding.Empty, MinimumSize = new Size(rongTrong, 0) };
        bangPhim.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bangPhim.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bangPhim.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        for (int i = 0; i < 3; i++)
        {
            int id = UngDungKhay.CacHotkey[i];
            var cot = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0, Px(4), 0, Px(4)) };
            cot.Controls.Add(Nhan(thongTinPhim[i].ten, Font, GiaoDien.Chu));
            cot.Controls.Add(Nhan(thongTinPhim[i].moTa, new Font("Segoe UI", 8.5f), GiaoDien.ChuMo));
            var o = new O_Phim(UngDungKhay.PhimCua(id), k) { Margin = new Padding(Px(8), Px(6), Px(8), Px(6)), Anchor = AnchorStyles.Left };
            o.BatDauGhi += () => app.HuyHotkey();               // gỡ phím để bấm PrtScn không chụp ngay
            o.KetThucGhi += moi => DatPhim(id, moi);
            var tt = Nhan("", new Font("Segoe UI", 8.5f), GiaoDien.ChuMo);
            tt.Anchor = AnchorStyles.Left;
            tt.MinimumSize = new Size(Px(120), 0);
            bangPhim.Controls.Add(cot, 0, i);
            bangPhim.Controls.Add(o, 1, i);
            bangPhim.Controls.Add(tt, 2, i);
            hangPhim[id] = (o, tt);
        }
        than.Controls.Add(bangPhim);
        var nutMacDinh = Nut(Chu.KhoiPhucPhim, false);
        nutMacDinh.Margin = new Padding(0, Px(6), 0, 0);
        nutMacDinh.Click += (_, _) =>
        {
            CaiDat.HienTai.PhimChon = CaiDat.MacDinhChon;
            CaiDat.HienTai.PhimToan = CaiDat.MacDinhToan;
            CaiDat.HienTai.PhimCuaSo = CaiDat.MacDinhCuaSo;
            CaiDat.HienTai.Luu();
            app.DangKyHotkey();
            NapLai();
        };
        than.Controls.Add(nutMacDinh);

        // ── lưu ảnh
        than.Controls.Add(TieuDe(Chu.LuuAnh));
        var hangThuMuc = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 3, Margin = Padding.Empty, MinimumSize = new Size(rongTrong, 0) };
        hangThuMuc.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        hangThuMuc.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        hangThuMuc.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        nhanThuMuc = Nhan("", Font, GiaoDien.Chu, rongTrong - Px(200));
        nhanThuMuc.AutoEllipsis = true;
        nhanThuMuc.Anchor = AnchorStyles.Left;
        var nutDoi = Nut(Chu.Doi, false);
        nutDoi.Click += (_, _) => app.DoiThuMuc(this);
        var nutMo = Nut(Chu.MoThuMuc, false);
        nutMo.Margin = new Padding(Px(8), 0, 0, 0);
        nutMo.Click += (_, _) => app.MoThuMuc();
        hangThuMuc.Controls.Add(nhanThuMuc, 0, 0);
        hangThuMuc.Controls.Add(nutDoi, 1, 0);
        hangThuMuc.Controls.Add(nutMo, 2, 0);
        than.Controls.Add(hangThuMuc);
        hopTuLuu = Hop(Chu.TuLuuMoiLanChep);
        hopTuLuu.CheckedChanged += (_, _) => { if (!dangNapLai) app.TuLuu = hopTuLuu.Checked; };
        than.Controls.Add(hopTuLuu);

        // ── khởi động
        than.Controls.Add(TieuDe(Chu.KhoiDong));
        hopTuChay = Hop(Chu.TuChayCungWindows);
        hopTuChay.CheckedChanged += (_, _) => { if (!dangNapLai) app.TuChay = hopTuChay.Checked; };
        than.Controls.Add(hopTuChay);

        // ── ngôn ngữ (tên tiếng Việt / English giữ nguyên, để ai cũng nhận ra ngôn ngữ của mình)
        than.Controls.Add(TieuDe(Chu.NgonNguTieuDe));
        // 3 nút liền nhau thay ComboBox (ComboBox ra nền trắng, lệch giao diện tối)
        var hangNgonNgu = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = Padding.Empty };
        foreach (var (ma, ten) in new[] { (NgonNgu.TuDong, Chu.NgonNguTuDong), (NgonNgu.TiengViet, "Tiếng Việt"), (NgonNgu.TiengAnh, "English") })
        {
            bool dangChon = CaiDat.HienTai.NgonNgu == ma;
            var b = Nut(ten, dangChon);
            b.MinimumSize = new Size(Px(100), Px(36));
            b.Margin = new Padding(0, 0, Px(6), 0);
            // Đổi ngôn ngữ thì app dựng lại cửa sổ này → đổi sau khi xử lý xong cú bấm
            if (!dangChon) b.Click += (_, _) => BeginInvoke(() => NgonNgu.Dat(ma));
            hangNgonNgu.Controls.Add(b);
        }
        than.Controls.Add(hangNgonNgu);

        // ── chân: vgs.zone
        var chan = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 3, Margin = new Padding(0, Px(22), 0, 0), MinimumSize = new Size(rongTrong, 0) };
        chan.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        chan.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        chan.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var lienKet = new LinkLabel
        {
            AutoSize = true, UseMnemonic = false, Anchor = AnchorStyles.Left,
            Text = Chu.ThemTienIchTai,
            ForeColor = GiaoDien.ChuMo, LinkColor = GiaoDien.Hong, ActiveLinkColor = Color.White, VisitedLinkColor = GiaoDien.Hong,
        };
        lienKet.LinkArea = new LinkArea(lienKet.Text.IndexOf("vgs.zone", StringComparison.Ordinal), "vgs.zone".Length);
        lienKet.LinkClicked += (_, _) => GiaoDien.MoTrangVgs();
        var nutGioiThieu = Nut(Chu.GioiThieu, false);
        nutGioiThieu.Click += (_, _) => HopGioiThieu.Hien();
        var nutVgs = Nut(Chu.MoVgs, true);
        nutVgs.Margin = new Padding(Px(8), 0, 0, 0);
        nutVgs.Click += (_, _) => GiaoDien.MoTrangVgs();
        chan.Controls.Add(lienKet, 0, 0);
        chan.Controls.Add(nutGioiThieu, 1, 0);
        chan.Controls.Add(nutVgs, 2, 0);
        than.Controls.Add(chan);

        NapLai();
        app.CaiDatDoi += NapLai;
        FormClosed += (_, _) => app.CaiDatDoi -= NapLai;
    }

    int Px(int v) => (int)(v * k);

    /// Đọc lại cài đặt + trạng thái đăng ký phím lên giao diện
    void NapLai()
    {
        if (IsDisposed) return;
        dangNapLai = true;
        foreach (var (id, (o, tt)) in hangPhim)
        {
            o.Phim = UngDungKhay.PhimCua(id);
            (tt.Text, tt.ForeColor) = app.TrangThai.GetValueOrDefault(id) switch
            {
                UngDungKhay.TrangThaiPhim.DangKy => (Chu.PhimDangChay, Color.FromArgb(52, 199, 89)),
                UngDungKhay.TrangThaiPhim.HookDuPhong => (Chu.PhimDuPhong, Color.FromArgb(52, 199, 89)),
                UngDungKhay.TrangThaiPhim.ChuaDat => (Chu.PhimChuaDat, GiaoDien.ChuMo),
                _ => (Chu.PhimBiGiu, Color.FromArgb(255, 149, 0)),
            };
        }
        nhanThuMuc.Text = CaiDat.HienTai.ThuMuc;
        goiY.SetToolTip(nhanThuMuc, CaiDat.HienTai.ThuMuc);
        hopTuLuu.Checked = app.TuLuu;
        hopTuChay.Checked = app.TuChay;
        dangNapLai = false;
    }

    void DatPhim(int id, Keys? moi)
    {
        if (moi is Keys phim)
        {
            // Trùng phím khác → đổi chỗ cho nhau thay vì báo lỗi
            foreach (int khac in UngDungKhay.CacHotkey)
                if (khac != id && phim != Keys.None && UngDungKhay.PhimCua(khac) == phim) GanPhim(khac, UngDungKhay.PhimCua(id));
            GanPhim(id, phim);
            CaiDat.HienTai.Luu();
        }
        app.DangKyHotkey();
        NapLai();
    }

    static void GanPhim(int id, Keys phim)
    {
        if (id == UngDungKhay.CacHotkey[0]) CaiDat.HienTai.PhimChon = phim;
        else if (id == UngDungKhay.CacHotkey[1]) CaiDat.HienTai.PhimToan = phim;
        else CaiDat.HienTai.PhimCuaSo = phim;
    }

    /// Ẩn cửa sổ trước khi chụp để nó không lọt vào ảnh
    async Task ChupTuCuaSo(int id)
    {
        Hide();
        await Task.Delay(250);
        bool chonVung = id == UngDungKhay.CacHotkey[0];
        if (chonVung) app.KhungChonDong += HienLai;   // khung chọn vùng đóng rồi mới hiện lại
        try { app.XuLyHotkey(id); }
        finally { if (!chonVung) { await Task.Delay(400); HienLai(); } }
    }

    void HienLai()
    {
        app.KhungChonDong -= HienLai;
        if (IsDisposed) return;
        Show();
        Activate();
    }

    // ───── dựng control ─────
    Label TieuDe(string chu) => Nhan(chu.ToUpperInvariant(), new Font("Segoe UI Semibold", 9f), GiaoDien.Hong, 0, new Padding(0, Px(18), 0, Px(8)));

    static Label Nhan(string chu, Font font, Color mau, int rongToiDa = 0, Padding? le = null) => new()
    {
        Text = chu, Font = font, ForeColor = mau, BackColor = Color.Transparent,
        AutoSize = true, UseMnemonic = false, MaximumSize = new Size(rongToiDa, 0), Margin = le ?? Padding.Empty,
    };

    CheckBox Hop(string chu) => new()
    {
        Text = chu, AutoSize = true, ForeColor = GiaoDien.Chu, UseMnemonic = false,
        Margin = new Padding(0, Px(8), 0, 0), Cursor = Cursors.Hand,
    };

    Button Nut(string chu, bool chinh)
    {
        var b = new Button
        {
            Text = chu, AutoSize = true, UseMnemonic = false,
            MinimumSize = new Size(Px(chinh ? 130 : 90), Px(38)),
            Padding = new Padding(Px(10), 0, Px(10), 0), Margin = Padding.Empty,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, ForeColor = Color.White,
            BackColor = chinh ? GiaoDien.Tim : Color.FromArgb(45, 45, 45),
        };
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = chinh ? GiaoDien.Hong : Color.FromArgb(70, 70, 70);
        return b;
    }
}

/// Ô ghi phím tắt: bấm vào → "Nhấn phím…", nhận tổ hợp đầu tiên có phím chính.
/// PrtScn không sinh KeyDown trong WinForms (chỉ KeyUp) nên bắt cả hai.
sealed class O_Phim : Control
{
    Keys phim;
    bool dangGhi;
    public event Action? BatDauGhi;
    public event Action<Keys?>? KetThucGhi;   // null = huỷ

    public O_Phim(Keys phim, float k)
    {
        this.phim = phim;
        SetStyle(ControlStyles.Selectable | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        TabStop = true;
        Cursor = Cursors.Hand;
        Size = new Size((int)(170 * k), (int)(36 * k));
        Font = new Font("Segoe UI Semibold", 10f);
    }

    public Keys Phim { get => phim; set { phim = value; Invalidate(); } }

    protected override void OnMouseDown(MouseEventArgs e) { Focus(); BatDau(); base.OnMouseDown(e); }
    protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { if (dangGhi) Xong(null); base.OnLostFocus(e); }

    void BatDau()
    {
        if (dangGhi) return;
        dangGhi = true;
        BatDauGhi?.Invoke();
        Invalidate();
    }

    void Xong(Keys? moi)
    {
        dangGhi = false;
        if (moi is Keys m) phim = m;
        Invalidate();
        KetThucGhi?.Invoke(moi);
    }

    protected override bool IsInputKey(Keys keyData) => dangGhi || base.IsInputKey(keyData);

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (!dangGhi) return base.ProcessCmdKey(ref msg, keyData);
        Nhan(keyData);
        return true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (dangGhi) { Nhan(e.KeyData); e.Handled = e.SuppressKeyPress = true; return; }
        if (e.KeyCode is Keys.Enter or Keys.Space) BatDau();
        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (dangGhi && e.KeyCode == Keys.PrintScreen) Nhan(e.KeyData);
        base.OnKeyUp(e);
    }

    void Nhan(Keys keyData)
    {
        var ma = keyData & Keys.KeyCode;
        if (ma is Keys.ShiftKey or Keys.ControlKey or Keys.Menu or Keys.LWin or Keys.RWin or Keys.None) { Invalidate(); return; }
        if (ma == Keys.Escape && (keyData & Keys.Modifiers) == 0) { Xong(null); return; }
        if (ma is Keys.Delete or Keys.Back && (keyData & Keys.Modifiers) == 0) { Xong(Keys.None); return; }
        Xong(keyData & (Keys.KeyCode | Keys.Shift | Keys.Control | Keys.Alt));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Parent?.BackColor ?? GiaoDien.Nen);
        var r = new Rectangle(0, 0, Width - 1, Height - 1);
        using (var nen = new SolidBrush(dangGhi ? Color.FromArgb(58, 42, 82) : Color.FromArgb(45, 45, 45))) g.FillRectangle(nen, r);
        using (var vien = new Pen(dangGhi || Focused ? GiaoDien.Hong : GiaoDien.Vien, dangGhi ? 2 : 1)) g.DrawRectangle(vien, r);
        string chu = dangGhi ? Chu.NhanPhim : CaiDat.TenPhim(phim);
        TextRenderer.DrawText(g, chu, Font, r, dangGhi ? GiaoDien.Hong : GiaoDien.Chu,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}
