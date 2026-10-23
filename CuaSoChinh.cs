using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace VgsChupHdr;

/// Cửa sổ chính (nhấp đúp icon khay): thanh bên trái + 5 trang. Mỗi lần chuyển trang / cài đặt đổi thì dựng lại trang.
sealed class CuaSoChinh : Form
{
    public enum Trang { TongQuan, PhimTat, LuuAnh, Chung, GioiThieu }

    // Ký tự Segoe Fluent Icons / MDL2
    const string IcCamera = "\uE722", IcBanPhim = "\uE765", IcThuMuc = "\uE8B7", IcCaiDat = "\uE713", IcThongTin = "\uE946",
                 IcQuaCau = "\uE774", IcSang = "\uE706", IcCat = "\uE7A8", IcManHinh = "\uE7F4", IcCuaSo = "\uE8A7",
                 IcSua = "\uE70F", IcXoa = "\uE74D", IcHuy = "\uE711";

    readonly UngDungKhay app;
    readonly float k;
    readonly Panel noiDung;
    readonly Dictionary<Trang, MucDieuHuong> mucTrang = new();
    readonly ToolTip goiY = new();
    bool dangGhiPhim;

    public Trang TrangHienTai { get; private set; }

    public CuaSoChinh(UngDungKhay app, Trang dau = Trang.TongQuan)
    {
        this.app = app;
        Text = Chu.TenApp;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = GiaoDien.NenCuaSo;
        ForeColor = GiaoDien.Chu;
        Font = new Font("Segoe UI", 10f);
        AutoScaleMode = AutoScaleMode.None;
        KeyPreview = true;
        try { Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath!); } catch { }

        k = DeviceDpi / 96f;
        ClientSize = new Size(Px(860), Px(600));

        // ── thanh bên
        var thanhBen = new Panel { Dock = DockStyle.Left, Width = Px(214), BackColor = GiaoDien.NenThanhBen, Padding = new Padding(Px(12), Px(16), Px(12), Px(14)) };
        thanhBen.Paint += (_, e) => { using var p = new Pen(GiaoDien.VienCuaSo); e.Graphics.DrawLine(p, thanhBen.Width - 1, 0, thanhBen.Width - 1, thanhBen.Height); };

        var dsMuc = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = GiaoDien.NenThanhBen };
        var dauThanh = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(Px(4), 0, 0, Px(16)), BackColor = GiaoDien.NenThanhBen };
        dauThanh.Controls.Add(new PictureBox { Image = GiaoDien.Logo(), SizeMode = PictureBoxSizeMode.Zoom, Size = new Size(Px(30), Px(30)), Margin = new Padding(0, 0, Px(10), 0) });
        var ten = Nhan(Chu.TenApp, new Font("Segoe UI Semibold", 10.5f), GiaoDien.Chu);
        ten.Margin = new Padding(0, Px(5), 0, 0);
        dauThanh.Controls.Add(ten);
        dsMuc.Controls.Add(dauThanh);
        foreach (var (t, ic, chu) in new[]
        {
            (Trang.TongQuan, IcCamera, Chu.TongQuan), (Trang.PhimTat, IcBanPhim, Chu.PhimTat), (Trang.LuuAnh, IcThuMuc, Chu.LuuAnh),
            (Trang.Chung, IcCaiDat, Chu.Chung), (Trang.GioiThieu, IcThongTin, Chu.GioiThieu),
        })
        {
            var m = new MucDieuHuong(ic, chu) { Size = new Size(Px(188), Px(38)), Margin = new Padding(0, 0, 0, Px(2)) };
            m.Click += (_, _) => HienTrang(t);
            mucTrang[t] = m;
            dsMuc.Controls.Add(m);
        }
        var mucVgs = new MucDieuHuong(IcQuaCau, Chu.ThemTienIchNgan) { Dock = DockStyle.Bottom, Height = Px(38), MauChu = GiaoDien.Hong };
        mucVgs.Click += (_, _) => GiaoDien.MoTrangVgs();
        thanhBen.Controls.Add(dsMuc);
        thanhBen.Controls.Add(mucVgs);

        // ── vùng nội dung
        noiDung = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = GiaoDien.NenCuaSo };
        Controls.Add(thanhBen);
        Controls.Add(noiDung);
        noiDung.BringToFront();

        HienTrang(dau);
        app.CaiDatDoi += NapLai;
        FormClosed += (_, _) => app.CaiDatDoi -= NapLai;
    }

    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); Ve.ThanhTieuDeToi(this); }

    int Px(int v) => (int)(v * k);
    int RongTrang => noiDung.ClientSize.Width - Px(56) - SystemInformation.VerticalScrollBarWidth;

    void NapLai() { if (!IsDisposed && !dangGhiPhim) HienTrang(TrangHienTai); }

    public void HienTrang(Trang t)
    {
        TrangHienTai = t;
        foreach (var (tt, m) in mucTrang) { m.DangChon = tt == t; m.Invalidate(); }

        noiDung.SuspendLayout();
        foreach (Control c in noiDung.Controls.Cast<Control>().ToList()) c.Dispose();
        var trang = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true,
            Location = new Point(Px(28), Px(22)), Margin = Padding.Empty, BackColor = GiaoDien.NenCuaSo, Padding = new Padding(0, 0, 0, Px(24)),
        };
        string tieuDe = t switch
        {
            Trang.TongQuan => Chu.TongQuan, Trang.PhimTat => Chu.PhimTat, Trang.LuuAnh => Chu.LuuAnh,
            Trang.Chung => Chu.Chung, _ => Chu.GioiThieu,
        };
        if (t != Trang.GioiThieu) trang.Controls.Add(Nhan(tieuDe, new Font("Segoe UI Semibold", 16f), GiaoDien.Chu, 0, new Padding(0, 0, 0, Px(14))));
        switch (t)
        {
            case Trang.TongQuan: DungTongQuan(trang); break;
            case Trang.PhimTat: DungPhimTat(trang); break;
            case Trang.LuuAnh: DungLuuAnh(trang); break;
            case Trang.Chung: DungChung(trang); break;
            default: DungGioiThieu(trang); break;
        }
        noiDung.Controls.Add(trang);
        noiDung.ResumeLayout();
        noiDung.AutoScrollPosition = Point.Empty;
    }

    // ───── các trang ─────
    void DungTongQuan(FlowLayoutPanel trang)
    {
        try
        {
            foreach (var m in Chup.DocManHinh())
            {
                var the = TheCaiDat(m.Hdr ? Chu.HdrBat(m.TrangSdrNits) : Chu.HdrTat,
                    m.Hdr ? Chu.MoTaHdrBat(TenMan(m.Ten), m.Vung.Width, m.Vung.Height) : Chu.MoTaHdrTat(TenMan(m.Ten), m.Vung.Width, m.Vung.Height),
                    null, IcSang, m.Hdr ? GiaoDien.TimNhat : GiaoDien.ChuNhat);
                if (m.Hdr) { the.MauNen = GiaoDien.NenHdr; the.MauVien = GiaoDien.VienHdr; }
                trang.Controls.Add(the);
            }
        }
        catch (Exception e)
        {
            NhatKy.Ghi("Đọc thông tin màn hình lỗi", e);
            trang.Controls.Add(TheCaiDat(Chu.KhongDocDuocMan, e.Message, null, IcSang, GiaoDien.CamCanhBao));
        }

        trang.Controls.Add(TieuDeNho(Chu.ChupNgay));
        var hang = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = Padding.Empty, BackColor = GiaoDien.NenCuaSo };
        int gap = Px(10), rong = (RongTrang - 2 * gap) / 3;
        var o = new[] { (IcCat, Chu.NutChonVung), (IcManHinh, Chu.NutToanMan), (IcCuaSo, Chu.NutCuaSo) };
        for (int i = 0; i < 3; i++)
        {
            int id = UngDungKhay.CacHotkey[i];
            var oc = new OChup(o[i].Item1, o[i].Item2, UngDungKhay.PhimCua(id), i == 0)
            {
                Size = new Size(rong, Px(118)), Margin = new Padding(0, 0, i < 2 ? gap : 0, 0),
            };
            oc.Click += async (_, _) => await ChupTuCuaSo(id);
            hang.Controls.Add(oc);
        }
        trang.Controls.Add(hang);
    }

    static string TenMan(string ten) => ten.Replace(@"\\.\", "");

    void DungPhimTat(FlowLayoutPanel trang)
    {
        trang.Controls.Add(Nhan(Chu.HuongDanGhiPhim, new Font("Segoe UI", 9.5f), GiaoDien.ChuNhat, RongTrang, new Padding(0, 0, 0, Px(12))));
        var the = new The { Width = RongTrang, Margin = Padding.Empty, Padding = new Padding(Px(16), Px(4), Px(10), Px(4)) };
        // Cố định bề rộng bảng = thẻ (không thì cột chữ đẩy nút xoá ra ngoài thẻ), cột chữ tự xuống dòng
        int rongBang = RongTrang - Px(26), rongChu = rongBang - Px(150 + 10 + 100 + 66 + 8);
        var bang = new TableLayoutPanel { AutoSize = true, ColumnCount = 5, BackColor = Color.Transparent, Location = new Point(Px(16), Px(4)), MinimumSize = new Size(rongBang, 0), MaximumSize = new Size(rongBang, 0) };
        bang.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int c = 0; c < 4; c++) bang.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        (string ten, string moTa)[] thongTin = { (Chu.ChupChonVung, Chu.MoTaChonVung), (Chu.ChupToanMan, Chu.MoTaToanMan), (Chu.ChupCuaSo, Chu.MoTaCuaSo) };
        int dong = 0;
        for (int i = 0; i < 3; i++)
        {
            int id = UngDungKhay.CacHotkey[i];
            if (i > 0)
            {
                var ke = new Panel { Height = 1, BackColor = GiaoDien.VienThe, Dock = DockStyle.Fill, Margin = Padding.Empty };
                bang.Controls.Add(ke, 0, dong);
                bang.SetColumnSpan(ke, 5);
                dong++;
            }
            var cot = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Color.Transparent, Margin = new Padding(0, Px(12), Px(8), Px(12)) };
            cot.Controls.Add(Nhan(thongTin[i].ten, Font, GiaoDien.Chu, rongChu));
            cot.Controls.Add(Nhan(thongTin[i].moTa, new Font("Segoe UI", 9f), GiaoDien.ChuNhat, rongChu));

            var o = new O_Phim(UngDungKhay.PhimCua(id), k) { Anchor = AnchorStyles.Right, Margin = new Padding(0, 0, Px(10), 0) };
            var (chuTT, mauTT, goiYTT) = app.TrangThai.GetValueOrDefault(id) switch
            {
                UngDungKhay.TrangThaiPhim.DangKy => ("✓ " + Chu.TrangThaiHoatDong, GiaoDien.XanhOk, ""),
                UngDungKhay.TrangThaiPhim.HookDuPhong => ("✓ " + Chu.TrangThaiDuPhong, GiaoDien.XanhOk, Chu.GoiYDuPhong),
                UngDungKhay.TrangThaiPhim.ChuaDat => (Chu.TrangThaiChuaDat, GiaoDien.ChuNhat, ""),
                _ => ("✗ " + Chu.TrangThaiBiGiu, GiaoDien.CamCanhBao, Chu.GoiYBiGiu),
            };
            var tt = Nhan(chuTT, new Font("Segoe UI", 9f), mauTT);
            tt.Anchor = AnchorStyles.Left;
            tt.MinimumSize = new Size(Px(100), 0);
            if (goiYTT != "") goiY.SetToolTip(tt, goiYTT);

            var nutSua = new NutPhang("") { BieuTuongTron = true, BieuTuong = IcSua, MauChu = GiaoDien.ChuPhu, Size = new Size(Px(32), Px(32)), Anchor = AnchorStyles.None, Margin = new Padding(0) };
            var nutXoa = new NutPhang("") { BieuTuongTron = true, BieuTuong = IcXoa, MauChu = GiaoDien.ChuPhu, Size = new Size(Px(32), Px(32)), Anchor = AnchorStyles.None, Margin = new Padding(Px(2), 0, 0, 0) };
            goiY.SetToolTip(nutSua, Chu.SuaPhim);
            goiY.SetToolTip(nutXoa, Chu.XoaPhim);
            o.BatDauGhi += () =>
            {
                dangGhiPhim = true;
                app.HuyHotkey();                       // gỡ phím để bấm PrtScn không chụp ngay
                tt.Text = Chu.NhanPhim; tt.ForeColor = GiaoDien.HongNhat;
                nutSua.BieuTuong = IcHuy; nutSua.Invalidate(); goiY.SetToolTip(nutSua, Chu.HuyGhi);
            };
            o.KetThucGhi += moi => { dangGhiPhim = false; BeginInvoke(() => DatPhim(id, moi)); };
            nutSua.Click += (_, _) => { if (o.DangGhi) o.Huy(); else o.BatDauTuNgoai(); };
            nutXoa.Click += (_, _) => DatPhim(id, Keys.None);

            bang.Controls.Add(cot, 0, dong);
            bang.Controls.Add(o, 1, dong);
            bang.Controls.Add(tt, 2, dong);
            bang.Controls.Add(nutSua, 3, dong);
            bang.Controls.Add(nutXoa, 4, dong);
            dong++;
        }
        the.Controls.Add(bang);
        bang.SizeChanged += (_, _) => the.Height = bang.Height + Px(8);
        the.Height = bang.PreferredSize.Height + Px(8);
        trang.Controls.Add(the);

        var nutMacDinh = new NutPhang(Chu.KhoiPhucPhim) { AutoSize = true, Margin = new Padding(0, Px(14), 0, 0) };
        nutMacDinh.Click += (_, _) =>
        {
            CaiDat.HienTai.PhimChon = CaiDat.MacDinhChon;
            CaiDat.HienTai.PhimToan = CaiDat.MacDinhToan;
            CaiDat.HienTai.PhimCuaSo = CaiDat.MacDinhCuaSo;
            CaiDat.HienTai.Luu();
            app.DangKyHotkey();
            HienTrang(Trang.PhimTat);
        };
        trang.Controls.Add(nutMacDinh);
    }

    void DungLuuAnh(FlowLayoutPanel trang)
    {
        var nutDoi = new NutPhang(Chu.Doi) { AutoSize = true, Margin = new Padding(0, 0, Px(8), 0) };
        nutDoi.Click += (_, _) => app.DoiThuMuc(this);
        var nutMo = new NutPhang(Chu.MoThuMuc) { AutoSize = true, Margin = Padding.Empty };
        nutMo.Click += (_, _) => app.MoThuMuc();
        var nut = new FlowLayoutPanel { AutoSize = true, WrapContents = false, BackColor = Color.Transparent, Margin = Padding.Empty };
        nut.Controls.Add(nutDoi);
        nut.Controls.Add(nutMo);
        trang.Controls.Add(TheCaiDat(Chu.ThuMucLuu, CaiDat.HienTai.ThuMuc, nut, IcThuMuc));

        var ct = new CongTac();
        ct.DatIm(app.TuLuu);
        ct.DoiTrangThai += (_, _) => app.TuLuu = ct.Bat;
        trang.Controls.Add(TheCaiDat(Chu.TuLuuKhiChep, Chu.MoTaTuLuu, ct));
    }

    void DungChung(FlowLayoutPanel trang)
    {
        var ct = new CongTac();
        ct.DatIm(app.TuChay);
        ct.DoiTrangThai += (_, _) => app.TuChay = ct.Bat;
        trang.Controls.Add(TheCaiDat(Chu.TuChayKhiDangNhap, Chu.MoTaTuChay, ct));

        // Tên "Tiếng Việt" / "English" giữ nguyên để ai cũng nhận ra ngôn ngữ của mình
        var ma = new[] { NgonNgu.TuDong, NgonNgu.TiengViet, NgonNgu.TiengAnh };
        var gat = new NutGat(new[] { Chu.TuDongNgan, "Tiếng Việt", "English" }, Math.Max(0, Array.IndexOf(ma, CaiDat.HienTai.NgonNgu)));
        gat.ChonMoi += i => BeginInvoke(() => NgonNgu.Dat(ma[i]));   // app dựng lại cửa sổ → đổi sau khi xử lý xong cú bấm
        trang.Controls.Add(TheCaiDat(Chu.NgonNguTieuDe, Chu.MoTaNgonNgu, gat));
    }

    void DungGioiThieu(FlowLayoutPanel trang)
    {
        var dau = new FlowLayoutPanel { AutoSize = true, WrapContents = false, BackColor = GiaoDien.NenCuaSo, Margin = new Padding(0, Px(6), 0, Px(18)) };
        dau.Controls.Add(new PictureBox { Image = GiaoDien.Logo(), SizeMode = PictureBoxSizeMode.Zoom, Size = new Size(Px(64), Px(64)), Margin = new Padding(0, 0, Px(16), 0) });
        var cot = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = GiaoDien.NenCuaSo, Margin = new Padding(0, Px(6), 0, 0) };
        cot.Controls.Add(Nhan(Chu.TenApp, new Font("Segoe UI Semibold", 18f), GiaoDien.Chu));
        cot.Controls.Add(Nhan(Chu.PhienBan(GiaoDien.PhienBan), new Font("Segoe UI", 9.5f), GiaoDien.ChuNhat));
        dau.Controls.Add(cot);
        trang.Controls.Add(dau);

        trang.Controls.Add(Nhan(Chu.GioiThieuNoiDung, Font, GiaoDien.ChuPhu, RongTrang, new Padding(0, 0, 0, Px(16))));

        var the = new The { Width = RongTrang, Margin = Padding.Empty };
        var bang = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, BackColor = Color.Transparent, Location = new Point(Px(16), Px(10)) };
        (int id, string viec)[] dong = { (UngDungKhay.CacHotkey[0], Chu.GoiYChonVung), (UngDungKhay.CacHotkey[1], Chu.GoiYToanMan), (UngDungKhay.CacHotkey[2], Chu.GoiYCuaSo) };
        for (int i = 0; i < dong.Length; i++)
        {
            var o = new O_Phim(UngDungKhay.PhimCua(dong[i].id), k) { ChiXem = true, Margin = new Padding(0, Px(3), Px(14), Px(3)) };
            var v = Nhan(dong[i].viec, Font, GiaoDien.Chu);
            v.Anchor = AnchorStyles.Left;
            bang.Controls.Add(o, 0, i);
            bang.Controls.Add(v, 1, i);
        }
        the.Controls.Add(bang);
        the.Height = bang.PreferredSize.Height + Px(20);
        trang.Controls.Add(the);

        trang.Controls.Add(Nhan(Chu.CongCuTrongKhung, new Font("Segoe UI", 9f), GiaoDien.ChuNhat, RongTrang, new Padding(0, Px(10), 0, Px(20))));

        var hang = new FlowLayoutPanel { AutoSize = true, WrapContents = false, BackColor = GiaoDien.NenCuaSo, Margin = Padding.Empty };
        var nutVgs = new NutPhang(Chu.MoVgs, true) { AutoSize = true, BieuTuong = IcQuaCau, Margin = new Padding(0, 0, Px(8), 0) };
        nutVgs.Click += (_, _) => GiaoDien.MoTrangVgs();
        var nutHdr = new NutPhang(Chu.ThongTinManHdr) { AutoSize = true, Margin = new Padding(0, 0, Px(8), 0) };
        nutHdr.Click += (_, _) => app.HienThongTinManHinh();
        var nutNk = new NutPhang(Chu.XemNhatKy) { AutoSize = true, Margin = Padding.Empty };
        nutNk.Click += (_, _) => app.XemNhatKy();
        hang.Controls.Add(nutVgs);
        hang.Controls.Add(nutHdr);
        hang.Controls.Add(nutNk);
        trang.Controls.Add(hang);
    }

    // ───── dựng khối ─────
    /// Thẻ cài đặt: [biểu tượng] tiêu đề + mô tả bên trái, control (công tắc, nút…) bên phải
    The TheCaiDat(string tieuDe, string moTa, Control? benPhai, string? bieuTuong = null, Color? mauBieuTuong = null)
    {
        int rong = RongTrang, le = Px(16);
        var the = new The { Width = rong, Margin = new Padding(0, 0, 0, Px(10)) };
        var bang = new TableLayoutPanel
        {
            AutoSize = true, ColumnCount = 3, BackColor = Color.Transparent, Location = new Point(le, Px(13)),
            MinimumSize = new Size(rong - 2 * le, 0), MaximumSize = new Size(rong - 2 * le, 0),
        };
        bang.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bang.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bang.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        if (bieuTuong != null)
        {
            var ic = Nhan(bieuTuong, new Font(Ve.FontBieuTuong, 14f), mauBieuTuong ?? GiaoDien.ChuPhu);
            ic.Margin = new Padding(0, Px(4), Px(12), 0);
            ic.Anchor = AnchorStyles.Left;
            bang.Controls.Add(ic, 0, 0);
        }
        int rongPhai = benPhai?.PreferredSize.Width ?? 0;
        var cot = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Color.Transparent, Margin = Padding.Empty, Anchor = AnchorStyles.Left };
        int rongChu = rong - 2 * le - rongPhai - Px(bieuTuong != null ? 56 : 24);
        cot.Controls.Add(Nhan(tieuDe, Font, GiaoDien.Chu, rongChu));
        if (moTa != "") cot.Controls.Add(Nhan(moTa, new Font("Segoe UI", 9f), GiaoDien.ChuNhat, rongChu, new Padding(0, Px(2), 0, 0)));
        bang.Controls.Add(cot, 1, 0);
        if (benPhai != null)
        {
            benPhai.Anchor = AnchorStyles.Right;
            benPhai.Margin = new Padding(Px(12), 0, 0, 0);
            bang.Controls.Add(benPhai, 2, 0);
        }
        the.Controls.Add(bang);
        bang.SizeChanged += (_, _) => the.Height = bang.Height + Px(26);
        the.Height = bang.PreferredSize.Height + Px(26);
        return the;
    }

    Label TieuDeNho(string chu) => Nhan(chu, new Font("Segoe UI Semibold", 9.5f), GiaoDien.ChuNhat, 0, new Padding(0, Px(14), 0, Px(10)));

    static Label Nhan(string chu, Font font, Color mau, int rongToiDa = 0, Padding? le = null) => new()
    {
        Text = chu, Font = font, ForeColor = mau, BackColor = Color.Transparent,
        AutoSize = true, UseMnemonic = false, MaximumSize = new Size(rongToiDa, 0), Margin = le ?? Padding.Empty,
    };

    // ───── hành động ─────
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
        if (!IsDisposed) HienTrang(TrangHienTai);
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
}

/// Ô "chụp ngay" ở trang Tổng quan: biểu tượng, tên, phím tắt dạng keycap
sealed class OChup : ControlVe
{
    readonly string bieuTuong, ten;
    readonly Keys phim;
    readonly bool chinh;

    public OChup(string bieuTuong, string ten, Keys phim, bool chinh)
    {
        this.bieuTuong = bieuTuong; this.ten = ten; this.phim = phim; this.chinh = chinh;
        Cursor = Cursors.Hand;
        Font = new Font("Segoe UI Semibold", 10.5f);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = ChuanBi(e);
        float k = K;
        var r = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
        Color nen = chinh ? (Hover ? GiaoDien.Hong : GiaoDien.Tim) : (Hover ? GiaoDien.NenHoverNut : GiaoDien.NenThe);
        using (var d = Ve.BoGoc(r, 12 * k))
        {
            using (var b = new SolidBrush(nen)) g.FillPath(b, d);
            if (!chinh) { using var v = new Pen(GiaoDien.VienThe); g.DrawPath(v, d); }
        }
        int x = (int)(16 * k);
        using var fIc = new Font(Ve.FontBieuTuong, 16f);
        TextRenderer.DrawText(g, bieuTuong, fIc, new Point(x, (int)(16 * k)), chinh ? Color.White : GiaoDien.TimNhat);
        TextRenderer.DrawText(g, ten, Font, new Point(x, (int)(52 * k)), chinh ? Color.White : GiaoDien.Chu);
        O_Phim.VeKeycap(g, O_Phim.TachPhim(phim), new Font("Segoe UI", 8.5f), x, (int)(80 * k), (int)(22 * k), k,
            chinh ? Color.FromArgb(60, 255, 255, 255) : GiaoDien.NenPhim, chinh ? Color.FromArgb(90, 255, 255, 255) : GiaoDien.VienPhim, Color.White);
    }
}

/// Ô phím tắt dạng keycap: bấm (hoặc nút sửa) → ghi tổ hợp mới. PrtScn không sinh KeyDown trong WinForms (chỉ KeyUp) nên bắt cả hai.
sealed class O_Phim : Control
{
    Keys phim;
    public bool DangGhi { get; private set; }
    public bool ChiXem;
    public event Action? BatDauGhi;
    public event Action<Keys?>? KetThucGhi;   // null = huỷ
    readonly float k;

    public O_Phim(Keys phim, float k)
    {
        this.phim = phim; this.k = k;
        SetStyle(ControlStyles.Selectable | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        TabStop = true;
        Font = new Font("Segoe UI", 9f);
        Size = new Size((int)(150 * k), (int)(32 * k));
    }

    protected override void OnCreateControl() { if (ChiXem) { Cursor = Cursors.Default; TabStop = false; } else Cursor = Cursors.Hand; base.OnCreateControl(); }

    public static string[] TachPhim(Keys p)
    {
        var ten = CaiDat.TenPhim(p);
        if ((p & Keys.KeyCode) == Keys.None) return new[] { ten };
        var phan = new List<string>();
        if (p.HasFlag(Keys.Control)) phan.Add("Ctrl");
        if (p.HasFlag(Keys.Shift)) phan.Add("Shift");
        if (p.HasFlag(Keys.Alt)) phan.Add("Alt");
        phan.Add(ten.Split('+').Last() is { Length: > 0 } cuoi ? cuoi : "+");
        return phan.ToArray();
    }

    /// Vẽ dãy nắp phím, trả về bề rộng đã vẽ
    public static int VeKeycap(Graphics g, string[] phan, Font f, int x, int y, int cao, float k, Color nen, Color vien, Color chu)
    {
        int bd = x;
        foreach (var p in phan)
        {
            int rong = Math.Max(cao, TextRenderer.MeasureText(p, f).Width + (int)(12 * k));
            var r = new RectangleF(x, y, rong, cao);
            using (var d = Ve.BoGoc(r, 5 * k))
            {
                using (var b = new SolidBrush(nen)) g.FillPath(b, d);
                using var v = new Pen(vien, 1f);
                g.DrawPath(v, d);
            }
            using (var day = new Pen(vien, 2f * k)) g.DrawLine(day, r.X + 4 * k, r.Bottom - 1, r.Right - 4 * k, r.Bottom - 1);   // gờ dưới như nắp phím thật
            TextRenderer.DrawText(g, p, f, Rectangle.Round(new RectangleF(r.X, r.Y, r.Width, r.Height - k)), chu, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            x += rong + (int)(4 * k);
        }
        return x - bd;
    }

    public void BatDauTuNgoai() { Focus(); BatDau(); }
    public void Huy() { if (DangGhi) Xong(null); }

    protected override void OnMouseDown(MouseEventArgs e) { if (!ChiXem) { Focus(); BatDau(); } base.OnMouseDown(e); }
    protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { if (DangGhi) Xong(null); Invalidate(); base.OnLostFocus(e); }
    protected override void OnHandleDestroyed(EventArgs e) { if (DangGhi) Xong(null); base.OnHandleDestroyed(e); }   // trang bị dựng lại giữa chừng → vẫn đăng ký lại phím

    void BatDau()
    {
        if (DangGhi || ChiXem) return;
        DangGhi = true;
        BatDauGhi?.Invoke();
        Invalidate();
    }

    void Xong(Keys? moi)
    {
        DangGhi = false;
        if (moi is Keys m) phim = m;
        Invalidate();
        KetThucGhi?.Invoke(moi);
    }

    protected override bool IsInputKey(Keys keyData) => DangGhi || base.IsInputKey(keyData);

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (!DangGhi) return base.ProcessCmdKey(ref msg, keyData);
        Nhan(keyData);
        return true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (DangGhi) { Nhan(e.KeyData); e.Handled = e.SuppressKeyPress = true; return; }
        if (e.KeyCode is Keys.Enter or Keys.Space) BatDau();
        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (DangGhi && e.KeyCode == Keys.PrintScreen) Nhan(e.KeyData);
        else if (DangGhi) Invalidate();   // nhả phím bổ trợ → cập nhật keycap đang giữ
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
        g.Clear(Ve.NenCha(this));
        g.SmoothingMode = SmoothingMode.AntiAlias;
        int cao = (int)(24 * k), y = (Height - cao) / 2;
        if (!DangGhi)
        {
            bool chuaDat = (phim & Keys.KeyCode) == Keys.None;
            if (chuaDat) TextRenderer.DrawText(g, CaiDat.TenPhim(phim), Font, new Point(0, y + (int)(3 * k)), GiaoDien.ChuNhat);
            else VeKeycap(g, TachPhim(phim), Font, 1, y, cao, k, GiaoDien.NenPhim, Focused && !ChiXem ? GiaoDien.Hong : GiaoDien.VienPhim, GiaoDien.Chu);
            return;
        }
        // Đang ghi: khung hồng, keycap các phím bổ trợ đang giữ + "…"
        var giu = new List<string>();
        var m = ModifierKeys;
        if (m.HasFlag(Keys.Control)) giu.Add("Ctrl");
        if (m.HasFlag(Keys.Shift)) giu.Add("Shift");
        if (m.HasFlag(Keys.Alt)) giu.Add("Alt");
        giu.Add("…");
        int rong = VeKeycap(g, giu.ToArray(), Font, (int)(5 * k), y, cao, k, GiaoDien.NenMucChon, GiaoDien.Hong, GiaoDien.HongNhat);
        using var d = Ve.BoGoc(new RectangleF(1, y - 4 * k, rong + 8 * k, cao + 8 * k), 8 * k);
        using var v = new Pen(GiaoDien.Hong, 1.5f);
        g.DrawPath(v, d);
    }
}
