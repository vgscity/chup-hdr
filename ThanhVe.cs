using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace VgsChupHdr;

/// Thanh công cụ vẽ dạng viên thuốc nổi dưới vùng chọn: công cụ | chấm màu | hoàn tác, lưu, đóng | Xong.
/// Một control tự vẽ (khử răng cưa) thay cho dãy Button vuông; nền trong suốt để thấy ảnh chụp ở 2 đầu bo tròn.
sealed class ThanhVe : Control
{
    enum Loai { CongCu, Mau, MauKhac, HanhDong, Xong, Ke }
    sealed record Muc(Loai Loai, string BieuTuong, string GoiY, CongCu CongCu = CongCu.Khong, Color Mau = default, Action? Lam = null)
    {
        public RectangleF O;
    }

    readonly List<Muc> muc = new();
    readonly float k;
    readonly ToolTip goiY = new();
    readonly Font fBieuTuong, fChu;
    int dangHover = -1;
    CongCu congCuChon;
    Color mauChon;
    bool mauTuy;   // màu chọn từ hộp màu (không nằm trong 5 chấm)

    public event Action<CongCu>? ChonCongCu;
    public event Action<Color>? ChonMau;
    public Func<Color, Color?>? HoiMauKhac;   // mở hộp chọn màu, trả null nếu huỷ

    static readonly Color Nen = Color.FromArgb(28, 28, 31), Vien = Color.FromArgb(52, 52, 58);

    public ThanhVe(float tiLe, Color mauDau, Action hoanTac, Action luu, Action dong, Action xong)
    {
        k = tiLe;
        mauChon = mauDau;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
        fBieuTuong = new Font(Ve.FontBieuTuong, 12f * k, GraphicsUnit.Pixel);
        fChu = new Font("Segoe UI Semibold", 12.5f * k, GraphicsUnit.Pixel);

        muc.Add(new Muc(Loai.CongCu, "", Chu.ButVe + " (P)", CongCu.But));
        muc.Add(new Muc(Loai.CongCu, "", Chu.DuongThang + " (L)", CongCu.Duong));
        muc.Add(new Muc(Loai.CongCu, "", Chu.MuiTen + " (A)", CongCu.MuiTen));
        muc.Add(new Muc(Loai.CongCu, "", Chu.KhungChuNhat + " (R)", CongCu.Khung));
        muc.Add(new Muc(Loai.CongCu, "", Chu.ButDanhDau + " (M)", CongCu.DanhDau));
        muc.Add(new Muc(Loai.CongCu, "", Chu.ChuGo + " (T)", CongCu.Chu));
        muc.Add(new Muc(Loai.Ke, "", ""));
        var bang = Chu.BangMau;
        foreach (int i in new[] { 0, 2, 3, 4, 7 })   // đỏ, vàng, xanh lá, xanh dương, trắng; màu khác qua chấm cầu vồng
            muc.Add(new Muc(Loai.Mau, "", bang[i].ten, Mau: bang[i].mau));
        muc.Add(new Muc(Loai.MauKhac, "", Chu.MauKhac));
        muc.Add(new Muc(Loai.Ke, "", ""));
        muc.Add(new Muc(Loai.HanhDong, "", Chu.HoanTac, Lam: hoanTac));
        muc.Add(new Muc(Loai.HanhDong, "", Chu.LuuFile, Lam: luu));
        muc.Add(new Muc(Loai.HanhDong, "", Chu.DongEsc, Lam: dong));
        muc.Add(new Muc(Loai.Xong, "", Chu.ChepClipboard, Lam: xong));
        mauTuy = !muc.Any(m => m.Loai == Loai.Mau && m.Mau.ToArgb() == mauDau.ToArgb());
        XepCho();
    }

    public CongCu CongCuChon { get => congCuChon; set { congCuChon = value; Invalidate(); } }
    public Color MauChon
    {
        get => mauChon;
        set { mauChon = value; mauTuy = !muc.Any(m => m.Loai == Loai.Mau && m.Mau.ToArgb() == value.ToArgb()); Invalidate(); }
    }

    void XepCho()
    {
        float pad = 6 * k, x = pad, cao = 46 * k, nut = 34 * k, cham = 26 * k;
        foreach (var m in muc)
        {
            float rong = m.Loai switch
            {
                Loai.Ke => 17 * k,
                Loai.Mau or Loai.MauKhac => cham,
                Loai.Xong => TextRenderer.MeasureText(Chu.Xong, fChu).Width + 30 * k,
                _ => nut,
            };
            float c = m.Loai is Loai.Mau or Loai.MauKhac ? cham : nut;
            m.O = new RectangleF(x, (cao - c) / 2, rong, c);
            x += rong + (m.Loai == Loai.Ke ? 0 : 2 * k);
        }
        Size = new Size((int)Math.Ceiling(x + pad), (int)Math.Ceiling(cao));
    }

    int MucTai(Point p)
    {
        for (int i = 0; i < muc.Count; i++) if (muc[i].Loai != Loai.Ke && muc[i].O.Contains(p)) return i;
        return -1;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        int h = MucTai(e.Location);
        if (h != dangHover)
        {
            dangHover = h;
            Cursor = h >= 0 ? Cursors.Hand : Cursors.Default;
            goiY.SetToolTip(this, h >= 0 ? muc[h].GoiY : "");
            Invalidate();
        }
        base.OnMouseMove(e);
    }

    protected override void OnMouseLeave(EventArgs e) { dangHover = -1; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        int i = MucTai(e.Location);
        if (i < 0 || e.Button != MouseButtons.Left) return;
        var m = muc[i];
        switch (m.Loai)
        {
            case Loai.CongCu: ChonCongCu?.Invoke(congCuChon == m.CongCu ? CongCu.Khong : m.CongCu); break;
            case Loai.Mau: MauChon = m.Mau; ChonMau?.Invoke(m.Mau); break;
            case Loai.MauKhac:
                if (HoiMauKhac?.Invoke(mauChon) is Color moi) { MauChon = moi; ChonMau?.Invoke(moi); }
                break;
            default: m.Lam?.Invoke(); break;
        }
        base.OnMouseClick(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var r = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
        using (var d = Ve.BoGoc(r, r.Height / 2))
        {
            using (var b = new SolidBrush(Nen)) g.FillPath(b, d);
            using var v = new Pen(Vien, 1f);
            g.DrawPath(v, d);
        }

        for (int i = 0; i < muc.Count; i++)
        {
            var m = muc[i];
            var o = m.O;
            bool hover = i == dangHover;
            switch (m.Loai)
            {
                case Loai.Ke:
                    using (var p = new Pen(Vien, 1f)) g.DrawLine(p, o.X + o.Width / 2, o.Y + 7 * k, o.X + o.Width / 2, o.Bottom - 7 * k);
                    break;
                case Loai.CongCu or Loai.HanhDong:
                {
                    bool chon = m.Loai == Loai.CongCu && m.CongCu == congCuChon;
                    if (chon || hover)
                    {
                        using var b = new SolidBrush(chon ? GiaoDien.Tim : Color.FromArgb(52, 52, 58));
                        g.FillEllipse(b, o);
                    }
                    TextRenderer.DrawText(g, m.BieuTuong, fBieuTuong, Rectangle.Round(o), chon ? Color.White : Color.FromArgb(200, 200, 206),
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                    break;
                }
                case Loai.Mau or Loai.MauKhac:
                {
                    float n = (hover ? 18 : 16) * k;
                    var c = new RectangleF(o.X + (o.Width - n) / 2, o.Y + (o.Height - n) / 2, n, n);
                    bool chon = m.Loai == Loai.Mau ? !mauTuy && m.Mau.ToArgb() == mauChon.ToArgb() : mauTuy;
                    if (m.Loai == Loai.Mau) { using var b = new SolidBrush(m.Mau); g.FillEllipse(b, c); }
                    else if (mauTuy) { using var b = new SolidBrush(mauChon); g.FillEllipse(b, c); }
                    else
                    {
                        // Chấm cầu vồng = "màu khác…"
                        using var cv = new GraphicsPath();
                        cv.AddEllipse(c);
                        using var pg = new PathGradientBrush(cv) { CenterColor = Color.White, SurroundColors = new[] { Color.Red, Color.Yellow, Color.Lime, Color.Cyan, Color.Blue, Color.Magenta } };
                        g.FillEllipse(pg, c);
                    }
                    if (m.Mau.ToArgb() == Color.White.ToArgb() || m.Mau.GetBrightness() < 0.15f) { using var v = new Pen(Color.FromArgb(90, 90, 96)); g.DrawEllipse(v, c); }
                    if (chon)
                    {
                        var ngoai = RectangleF.Inflate(c, 3.5f * k, 3.5f * k);
                        using var v = new Pen(Color.White, 1.5f * k);
                        g.DrawEllipse(v, ngoai);
                    }
                    break;
                }
                case Loai.Xong:
                {
                    using var d = Ve.BoGoc(o, o.Height / 2);
                    using (var b = new SolidBrush(hover ? GiaoDien.Hong : GiaoDien.Tim)) g.FillPath(b, d);
                    TextRenderer.DrawText(g, Chu.Xong, fChu, Rectangle.Round(o), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    break;
                }
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { goiY.Dispose(); fBieuTuong.Dispose(); fChu.Dispose(); }
        base.Dispose(disposing);
    }
}
