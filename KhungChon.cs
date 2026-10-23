using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace VgsChupHdr;

/// Lớp phủ toàn màn hình kiểu Lightshot: ảnh đứng, kéo chọn vùng, vẽ chú thích, chép/lưu.
sealed class KhungChon : Form
{
    readonly KetQuaChup kq;
    readonly Bitmap anh;
    readonly Action<string>? thongBao;

    Rectangle? vung;                 // toạ độ client (= toạ độ ảnh)
    bool dangChonMoi, dangDiChuyen;
    int tayCamKeo = -1;
    Point diemBatDau, diemChuot, diemKeoTruoc;
    Rectangle vungTruocKeo;

    CongCu congCu = CongCu.Khong;
    readonly List<HinhVe> hinh = new();
    HinhVe? dangVe;
    Color mau = Color.FromArgb(255, 59, 48);
    readonly float tiLe;             // DPI/96
    TextBox? oChu;
    PointF viTriChu;
    readonly Font phongChu;

    readonly FlowLayoutPanel thanh;
    readonly Dictionary<CongCu, Button> nutCongCu = new();
    Button nutMau = null!;
    readonly ToolTip goiY = new();
    readonly ContextMenuStrip menuMau = new();

    static readonly Color NenThanh = Color.FromArgb(31, 31, 31);
    static readonly Color NenNut = Color.FromArgb(45, 45, 45);
    static readonly Color NenChon = Color.FromArgb(124, 58, 237);

    public KhungChon(KetQuaChup ketQua, Action<string>? thongBao)
    {
        kq = ketQua; anh = ketQua.Anh; this.thongBao = thongBao;

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        Bounds = kq.VungAo;
        Cursor = Cursors.Cross;
        BackColor = Color.Black;
        KeyPreview = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        DoubleBuffered = true;

        tiLe = Math.Max(1f, DeviceDpi / 96f);
        phongChu = new Font("Segoe UI", 16f * tiLe, FontStyle.Bold, GraphicsUnit.Pixel);

        thanh = new FlowLayoutPanel
        {
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false,
            BackColor = NenThanh, Padding = new Padding(4), Margin = Padding.Empty, Visible = false,
        };
        TaoThanhCongCu();
        Controls.Add(thanh);
        TaoMenuMau();

        MouseDown += KhiNhanChuot;
        MouseMove += KhiDiChuot;
        MouseUp += KhiThaChuot;
        MouseDoubleClick += (_, e) => { if (e.Button == MouseButtons.Left && vung is Rectangle v && v.Contains(e.Location) && congCu == CongCu.Khong) Chep(); };
        Shown += (_, _) => { Activate(); BringToFront(); };
    }

    // ───── thanh công cụ ─────
    void TaoThanhCongCu()
    {
        ThemNut(CongCu.But, "", VgsChupHdr.Chu.ButVe);
        ThemNut(CongCu.Duong, "", VgsChupHdr.Chu.DuongThang);
        ThemNut(CongCu.MuiTen, "", VgsChupHdr.Chu.MuiTen);
        ThemNut(CongCu.Khung, "", VgsChupHdr.Chu.KhungChuNhat);
        ThemNut(CongCu.DanhDau, "", VgsChupHdr.Chu.ButDanhDau);
        ThemNut(CongCu.Chu, "", VgsChupHdr.Chu.ChuGo);
        nutMau = ThemNutHanhDong("", VgsChupHdr.Chu.MauVe, (_, _) => menuMau.Show(nutMau, new Point(0, nutMau.Height)));
        nutMau.ForeColor = mau;
        ThemNutHanhDong("", VgsChupHdr.Chu.HoanTac, (_, _) => HoanTac());
        ThemKhoang();
        ThemNutHanhDong("", VgsChupHdr.Chu.ChepClipboard, (_, _) => Chep());
        ThemNutHanhDong("", VgsChupHdr.Chu.LuuFile, (_, _) => LuuHoiDuongDan());
        ThemNutHanhDong("", VgsChupHdr.Chu.DongEsc, (_, _) => Close());
    }

    Button TaoNut(string kyHieu, string goiYChu)
    {
        int c = (int)(36 * tiLe);
        var b = new Button
        {
            Text = kyHieu, Width = c, Height = c, Margin = new Padding(2),
            FlatStyle = FlatStyle.Flat, BackColor = NenNut, ForeColor = Color.White,
            Font = new Font("Segoe MDL2 Assets", 13f * tiLe, GraphicsUnit.Pixel),
            TabStop = false, Cursor = Cursors.Hand,
        };
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 70, 70);
        goiY.SetToolTip(b, goiYChu);
        thanh.Controls.Add(b);
        return b;
    }

    void ThemNut(CongCu cc, string kyHieu, string goiYChu)
    {
        var b = TaoNut(kyHieu, goiYChu);
        b.Click += (_, _) => ChonCongCu(congCu == cc ? CongCu.Khong : cc);
        nutCongCu[cc] = b;
    }

    Button ThemNutHanhDong(string kyHieu, string goiYChu, EventHandler khiBam)
    {
        var b = TaoNut(kyHieu, goiYChu);
        b.Click += khiBam;
        return b;
    }

    void ThemKhoang() => thanh.Controls.Add(new Panel { Width = (int)(10 * tiLe), Height = 1, Margin = Padding.Empty, BackColor = NenThanh });

    void ChonCongCu(CongCu cc)
    {
        KetThucChu(true);
        congCu = cc;
        foreach (var (k, b) in nutCongCu) b.BackColor = k == cc ? NenChon : NenNut;
        Cursor = cc == CongCu.Khong ? Cursors.Cross : cc == CongCu.Chu ? Cursors.IBeam : Cursors.Cross;
    }

    void TaoMenuMau()
    {
        foreach (var (ten, m) in VgsChupHdr.Chu.BangMau)
        {
            var o = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(o)) { g.Clear(m); g.DrawRectangle(Pens.Gray, 0, 0, 15, 15); }
            var muc = new ToolStripMenuItem(ten, o);
            muc.Click += (_, _) => DatMau(m);
            menuMau.Items.Add(muc);
        }
        menuMau.Items.Add(new ToolStripSeparator());
        var khac = new ToolStripMenuItem(VgsChupHdr.Chu.MauKhac);
        khac.Click += (_, _) => { using var cd = new ColorDialog { Color = mau, FullOpen = true }; if (cd.ShowDialog(this) == DialogResult.OK) DatMau(cd.Color); };
        menuMau.Items.Add(khac);
    }

    void DatMau(Color m)
    {
        mau = m; nutMau.ForeColor = m;
        if (oChu != null) oChu.ForeColor = m;
    }

    void CapNhatThanh()
    {
        if (vung is not Rectangle v || dangChonMoi || tayCamKeo >= 0 || dangDiChuyen) { thanh.Visible = false; return; }
        int cach = (int)(6 * tiLe);
        int x = Math.Clamp(v.Right - thanh.Width, 0, Math.Max(0, ClientSize.Width - thanh.Width));
        int y = v.Bottom + cach;
        if (y + thanh.Height > ClientSize.Height) y = v.Top - thanh.Height - cach;
        if (y < 0) y = Math.Max(0, v.Bottom - thanh.Height - cach);
        thanh.Location = new Point(x, y);
        thanh.Visible = true;
        thanh.BringToFront();
    }

    // ───── vẽ ─────
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.DrawImageUnscaled(anh, 0, 0);

        if (vung is Rectangle v)
        {
            var st = g.Save();
            g.SetClip(v);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            foreach (var h in hinh) h.Ve(g);
            dangVe?.Ve(g);
            g.Restore(st);

            using (var toi = new SolidBrush(Color.FromArgb(120, 0, 0, 0)))
            using (var r = new Region(ClientRectangle))
            {
                r.Exclude(v);
                g.FillRegion(toi, r);
            }

            using var vien = new Pen(Color.White, 1f) { DashStyle = DashStyle.Dash };
            g.DrawRectangle(vien, v.X - 1, v.Y - 1, v.Width + 1, v.Height + 1);

            if (!dangChonMoi)
                foreach (var tc in TayCam(v)) { g.FillRectangle(Brushes.White, tc); g.DrawRectangle(Pens.Black, tc); }

            VeNhan(g, $"{v.Width} × {v.Height}", new Point(v.X, v.Y - (int)(24 * tiLe)), v);
        }
        else
        {
            using var toi = new SolidBrush(Color.FromArgb(120, 0, 0, 0));
            g.FillRectangle(toi, ClientRectangle);
            using var p = new Pen(Color.FromArgb(180, 255, 255, 255), 1f);
            g.DrawLine(p, diemChuot.X, 0, diemChuot.X, ClientSize.Height);
            g.DrawLine(p, 0, diemChuot.Y, ClientSize.Width, diemChuot.Y);
            VeNhan(g, VgsChupHdr.Chu.HuongDanKeo,
                new Point(diemChuot.X + 16, diemChuot.Y + 16), null);
        }
    }

    void VeNhan(Graphics g, string chu, Point viTri, Rectangle? vungGoc)
    {
        using var phong = new Font("Segoe UI", 12f * tiLe, GraphicsUnit.Pixel);
        var kich = g.MeasureString(chu, phong);
        var o = new RectangleF(viTri.X, viTri.Y, kich.Width + 10, kich.Height + 4);
        if (o.Y < 0 && vungGoc is Rectangle v) o.Y = v.Y + 4;
        if (o.Right > ClientSize.Width) o.X = ClientSize.Width - o.Width;
        if (o.Bottom > ClientSize.Height) o.Y = ClientSize.Height - o.Height;
        using var nen = new SolidBrush(Color.FromArgb(200, 20, 20, 20));
        g.FillRectangle(nen, o);
        g.DrawString(chu, phong, Brushes.White, o.X + 5, o.Y + 2);
    }

    // ───── tay cầm ─────
    Rectangle[] TayCam(Rectangle v)
    {
        int c = (int)(8 * tiLe), n = c / 2;
        int cx = v.X + v.Width / 2, cy = v.Y + v.Height / 2;
        var d = new[] { (v.Left, v.Top), (cx, v.Top), (v.Right, v.Top), (v.Right, cy), (v.Right, v.Bottom), (cx, v.Bottom), (v.Left, v.Bottom), (v.Left, cy) };
        return d.Select(p => new Rectangle(p.Item1 - n, p.Item2 - n, c, c)).ToArray();
    }

    int TimTayCam(Point p)
    {
        if (vung is not Rectangle v) return -1;
        var ds = TayCam(v);
        for (int i = 0; i < ds.Length; i++) { var r = ds[i]; r.Inflate(3, 3); if (r.Contains(p)) return i; }
        return -1;
    }

    static Cursor ConTroTayCam(int i) => i switch
    {
        0 or 4 => Cursors.SizeNWSE, 2 or 6 => Cursors.SizeNESW, 1 or 5 => Cursors.SizeNS, 3 or 7 => Cursors.SizeWE, _ => Cursors.Cross,
    };

    static Rectangle Chuan(Point a, Point b) =>
        Rectangle.FromLTRB(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));

    Rectangle KeoTayCam(Rectangle goc, int tc, int dx, int dy)
    {
        int l = goc.Left, t = goc.Top, r = goc.Right, b = goc.Bottom;
        if (tc is 0 or 6 or 7) l += dx;
        if (tc is 2 or 3 or 4) r += dx;
        if (tc is 0 or 1 or 2) t += dy;
        if (tc is 4 or 5 or 6) b += dy;
        return Chuan(new Point(l, t), new Point(r, b));
    }

    // ───── chuột ─────
    void KhiNhanChuot(object? s, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            if (oChu != null) { KetThucChu(false); return; }
            if (vung != null) { vung = null; thanh.Visible = false; Invalidate(); } else Close();
            return;
        }
        if (e.Button != MouseButtons.Left) return;
        var p = e.Location;
        bool vuaDongChu = oChu != null;
        KetThucChu(true);

        if (vung is Rectangle v)
        {
            int tc = TimTayCam(p);
            if (tc >= 0) { tayCamKeo = tc; vungTruocKeo = v; diemBatDau = p; CapNhatThanh(); return; }
            if (v.Contains(p))
            {
                if (congCu == CongCu.Chu) { if (!vuaDongChu) BatDauChu(p); return; }
                if (congCu != CongCu.Khong) { BatDauVe(p); return; }
                dangDiChuyen = true; diemKeoTruoc = p; CapNhatThanh(); return;
            }
        }
        dangChonMoi = true; diemBatDau = p; vung = new Rectangle(p, Size.Empty);
        CapNhatThanh(); Invalidate();
    }

    void KhiDiChuot(object? s, MouseEventArgs e)
    {
        var p = e.Location; diemChuot = p;
        if (dangChonMoi) { vung = Chuan(diemBatDau, p); Invalidate(); return; }
        if (tayCamKeo >= 0) { vung = KeoTayCam(vungTruocKeo, tayCamKeo, p.X - diemBatDau.X, p.Y - diemBatDau.Y); Invalidate(); return; }
        if (dangDiChuyen && vung is Rectangle v)
        {
            v.Offset(p.X - diemKeoTruoc.X, p.Y - diemKeoTruoc.Y);
            v.X = Math.Clamp(v.X, 0, Math.Max(0, ClientSize.Width - v.Width));
            v.Y = Math.Clamp(v.Y, 0, Math.Max(0, ClientSize.Height - v.Height));
            vung = v; diemKeoTruoc = p; Invalidate(); return;
        }
        if (dangVe != null) { TiepVe(p); Invalidate(); return; }

        if (vung == null) { Invalidate(); return; }
        int tc = TimTayCam(p);
        if (tc >= 0) Cursor = ConTroTayCam(tc);
        else if (vung.Value.Contains(p)) Cursor = congCu == CongCu.Khong ? Cursors.SizeAll : congCu == CongCu.Chu ? Cursors.IBeam : Cursors.Cross;
        else Cursor = Cursors.Cross;
    }

    void KhiThaChuot(object? s, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        if (dangChonMoi)
        {
            dangChonMoi = false;
            if (vung is Rectangle v && (v.Width < 3 || v.Height < 3)) vung = null;
        }
        tayCamKeo = -1; dangDiChuyen = false;
        if (dangVe != null) { hinh.Add(dangVe); dangVe = null; }
        CapNhatThanh(); Invalidate();
    }

    // ───── vẽ chú thích ─────
    void BatDauVe(Point p)
    {
        float day = 3f * tiLe;
        dangVe = congCu switch
        {
            CongCu.But => new HinhBut { Mau = mau, DoDay = day, Diem = { p } },
            CongCu.Duong => new HinhDuong { Mau = mau, DoDay = day, A = p, B = p },
            CongCu.MuiTen => new HinhDuong { Mau = mau, DoDay = day, A = p, B = p, MuiTen = true },
            CongCu.Khung => new HinhKhung { Mau = mau, DoDay = day, Vung = new RectangleF(p, SizeF.Empty) },
            CongCu.DanhDau => new HinhDanhDau { Mau = mau, DoDay = 14f * tiLe, A = p, B = p },
            _ => null,
        };
        diemBatDau = p;
    }

    void TiepVe(Point p)
    {
        switch (dangVe)
        {
            case HinhBut b: b.Diem.Add(p); break;
            case HinhDuong d: d.B = p; break;
            case HinhDanhDau dd: dd.B = p; break;
            case HinhKhung k: k.Vung = Chuan(diemBatDau, p); break;
        }
    }

    void BatDauChu(Point p)
    {
        viTriChu = p;
        oChu = new TextBox
        {
            Location = new Point(p.X, p.Y), Width = (int)(220 * tiLe), Font = phongChu, ForeColor = mau,
            BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle,
        };
        oChu.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; KetThucChu(true); }
            else if (e.KeyCode == Keys.Escape) { e.SuppressKeyPress = true; KetThucChu(false); }
        };
        oChu.TextChanged += (_, _) =>
        {
            if (oChu == null) return;
            var w = TextRenderer.MeasureText(oChu.Text + "W", phongChu).Width + 12;
            oChu.Width = Math.Max((int)(220 * tiLe), w);
        };
        Controls.Add(oChu); oChu.BringToFront(); oChu.Focus();
    }

    void KetThucChu(bool luu)
    {
        if (oChu == null) return;
        var o = oChu; oChu = null;
        var chu = o.Text;
        Controls.Remove(o); o.Dispose();
        if (luu && !string.IsNullOrWhiteSpace(chu))
            hinh.Add(new HinhChu { Chu = chu, Mau = mau, DoDay = 1, Phong = phongChu, ViTri = new PointF(viTriChu.X + 2, viTriChu.Y + 2) });
        Invalidate();
    }

    void HoanTac()
    {
        if (oChu != null) { KetThucChu(false); return; }
        if (hinh.Count > 0) { hinh.RemoveAt(hinh.Count - 1); Invalidate(); }
    }

    // ───── phím ─────
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (oChu != null && oChu.Focused) return base.ProcessCmdKey(ref msg, keyData); // đang gõ chữ: để TextBox xử lý
        switch (keyData)
        {
            case Keys.Escape: Close(); return true;
            case Keys.Enter: if (vung != null) Chep(); return true;
            case Keys.Control | Keys.C: if (vung != null) Chep(); return true;
            case Keys.Control | Keys.S: if (vung != null) LuuHoiDuongDan(); return true;
            case Keys.Control | Keys.Z: HoanTac(); return true;
            case Keys.Control | Keys.A: vung = ClientRectangle; CapNhatThanh(); Invalidate(); return true;
            case Keys.P: ChonCongCu(CongCu.But); return true;
            case Keys.L: ChonCongCu(CongCu.Duong); return true;
            case Keys.A: ChonCongCu(CongCu.MuiTen); return true;
            case Keys.R: ChonCongCu(CongCu.Khung); return true;
            case Keys.M: ChonCongCu(CongCu.DanhDau); return true;
            case Keys.T: ChonCongCu(CongCu.Chu); return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    // ───── xuất ─────
    Bitmap TaoAnhKetQua()
    {
        var v = vung!.Value;
        v.Intersect(ClientRectangle);
        var kqAnh = new Bitmap(Math.Max(1, v.Width), Math.Max(1, v.Height));
        using var g = Graphics.FromImage(kqAnh);
        g.DrawImage(anh, new Rectangle(0, 0, v.Width, v.Height), v, GraphicsUnit.Pixel);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TranslateTransform(-v.X, -v.Y);
        foreach (var h in hinh) h.Ve(g);
        return kqAnh;
    }

    void Chep()
    {
        KetThucChu(true);
        if (vung == null) return;
        try
        {
            using var a = TaoAnhKetQua();
            XuatAnh.Chep(a);
            string loi = VgsChupHdr.Chu.DaChep;
            if (CaiDat.HienTai.TuLuuKhiChep) loi += VgsChupHdr.Chu.VaLuu(Path.GetFileName(XuatAnh.Luu(a)));
            thongBao?.Invoke(loi);
        }
        catch (Exception e) { NhatKy.Ghi("Chép ảnh lỗi", e); thongBao?.Invoke(VgsChupHdr.Chu.LoiChep(e.Message)); }
        Close();
    }

    void LuuHoiDuongDan()
    {
        KetThucChu(true);
        if (vung == null) return;
        using var hop = new SaveFileDialog
        {
            Filter = VgsChupHdr.Chu.LocFile,
            FileName = XuatAnh.TenFileMoi(),
            InitialDirectory = CaiDat.HienTai.ThuMuc,
            Title = VgsChupHdr.Chu.LuuAnhChup,
        };
        if (hop.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            using var a = TaoAnhKetQua();
            XuatAnh.LuuTheoDuongDan(a, hop.FileName);
            thongBao?.Invoke(VgsChupHdr.Chu.DaLuu(Path.GetFileName(hop.FileName)));
        }
        catch (Exception e) { NhatKy.Ghi("Lưu ảnh lỗi", e); thongBao?.Invoke(VgsChupHdr.Chu.LoiLuu(e.Message)); }
        Close();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        base.OnFormClosed(e);
        phongChu.Dispose();
        kq.Dispose();
    }
}
