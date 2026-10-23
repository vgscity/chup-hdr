using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace VgsChupHdr;

/// Control tự vẽ dùng chung cho cửa sổ chính (giao diện tối, bo góc, khử răng cưa).
/// Mỗi control tô nền cha trước rồi vẽ hình bo góc lên, nên đặt trên nền màu đặc.
static class Ve
{
    public static GraphicsPath BoGoc(RectangleF r, float bk)
    {
        var p = new GraphicsPath();
        float d = Math.Min(bk * 2, Math.Min(r.Width, r.Height));
        if (d <= 0) { p.AddRectangle(r); return p; }
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    /// Font biểu tượng: Segoe Fluent Icons (Windows 11), không có thì Segoe MDL2 Assets (Windows 10)
    public static readonly string FontBieuTuong =
        new System.Drawing.Text.InstalledFontCollection().Families.Any(f => f.Name == "Segoe Fluent Icons") ? "Segoe Fluent Icons" : "Segoe MDL2 Assets";

    public static Color NenCha(Control c)
    {
        for (var p = c.Parent; p != null; p = p.Parent)
            if (p.BackColor.A == 255) return p.BackColor;
        return GiaoDien.Nen;
    }

    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int giaTri, int size);
    /// Bo góc cửa sổ/popup kiểu Windows 11 (DWMWA_WINDOW_CORNER_PREFERENCE = DWMWCP_ROUND); Windows 10 bỏ qua
    public static void BoGocWin11(IntPtr h)
    {
        int tron = 2;
        try { DwmSetWindowAttribute(h, 33, ref tron, sizeof(int)); } catch { }
    }

    /// Thanh tiêu đề tối (Windows 10 20H1+ / 11)
    public static void ThanhTieuDeToi(Form f)
    {
        int bat = 1;
        try { DwmSetWindowAttribute(f.Handle, 20, ref bat, sizeof(int)); } catch { }
    }
}

/// Lớp nền cho control tự vẽ
abstract class ControlVe : Control
{
    protected ControlVe()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        ForeColor = GiaoDien.Chu;
    }

    protected float K => DeviceDpi / 96f;
    protected bool Hover;
    protected override void OnMouseEnter(EventArgs e) { Hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { Hover = false; Invalidate(); base.OnMouseLeave(e); }

    protected Graphics ChuanBi(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Ve.NenCha(this));
        g.SmoothingMode = SmoothingMode.AntiAlias;
        return g;
    }
}

/// Thẻ bo góc chứa nội dung (TableLayoutPanel bên trong để control con tự xếp)
sealed class The : Panel
{
    public Color MauVien = GiaoDien.VienThe;
    /// BackColor = màu thẻ, để control con (Ve.NenCha) tô góc đúng màu
    public Color MauNen { get => BackColor; set => BackColor = value; }

    public The()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = GiaoDien.NenThe;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Ve.NenCha(this));
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float k = DeviceDpi / 96f;
        using var duong = Ve.BoGoc(new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), 10 * k);
        using (var nen = new SolidBrush(MauNen)) g.FillPath(nen, duong);
        using var vien = new Pen(MauVien, 1f);
        g.DrawPath(vien, duong);
    }
}

/// Công tắc gạt bật/tắt
sealed class CongTac : ControlVe
{
    bool bat;
    public event EventHandler? DoiTrangThai;

    public CongTac() { Cursor = Cursors.Hand; TabStop = true; }

    public bool Bat
    {
        get => bat;
        set { if (bat == value) return; bat = value; Invalidate(); DoiTrangThai?.Invoke(this, EventArgs.Empty); }
    }
    /// Đặt mà không bắn sự kiện (khi nạp lại từ cài đặt)
    public void DatIm(bool v) { bat = v; Invalidate(); }

    protected override void OnCreateControl() { Size = new Size((int)(40 * K), (int)(22 * K)); base.OnCreateControl(); }
    protected override void OnClick(EventArgs e) { Bat = !Bat; base.OnClick(e); }
    protected override void OnKeyDown(KeyEventArgs e) { if (e.KeyCode is Keys.Space or Keys.Enter) Bat = !Bat; base.OnKeyDown(e); }
    protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = ChuanBi(e);
        var r = new RectangleF(1, 1, Width - 2, Height - 2);
        using (var duong = Ve.BoGoc(r, r.Height / 2))
        using (var nen = new SolidBrush(bat ? (Hover ? GiaoDien.Hong : GiaoDien.Tim) : (Hover ? Color.FromArgb(74, 74, 80) : GiaoDien.TatCongTac)))
            g.FillPath(nen, duong);
        if (Focused) { using var vien = new Pen(GiaoDien.Hong, 1.5f); using var d2 = Ve.BoGoc(r, r.Height / 2); g.DrawPath(vien, d2); }
        float n = r.Height - 6 * K;
        float x = bat ? r.Right - n - 3 * K : r.X + 3 * K;
        g.FillEllipse(Brushes.White, x, r.Y + 3 * K, n, n);
    }
}

/// Nút bo góc phẳng. Chinh = nền tím; Vien = chỉ viền.
sealed class NutPhang : ControlVe
{
    public bool Chinh, BieuTuongTron;
    public string BieuTuong = "";       // ký tự Segoe Fluent/MDL2, để trống nếu chỉ có chữ
    public Color? MauChu;

    public NutPhang(string chu, bool chinh = false)
    {
        Text = chu; Chinh = chinh;
        Cursor = Cursors.Hand; TabStop = true;
        Font = new Font("Segoe UI", 10f);
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        float k = K;
        if (BieuTuongTron) return new Size((int)(32 * k), (int)(32 * k));
        int rong = TextRenderer.MeasureText(Text, Font).Width + (int)(28 * k);
        if (BieuTuong != "") rong += (int)(22 * k);
        return new Size(Math.Max(rong, (int)(84 * k)), (int)(36 * k));
    }

    protected override void OnCreateControl() { if (AutoSize) Size = GetPreferredSize(Size.Empty); base.OnCreateControl(); }
    protected override void OnTextChanged(EventArgs e) { if (AutoSize) Size = GetPreferredSize(Size.Empty); Invalidate(); base.OnTextChanged(e); }
    protected override void OnKeyDown(KeyEventArgs e) { if (e.KeyCode is Keys.Space or Keys.Enter) OnClick(EventArgs.Empty); base.OnKeyDown(e); }
    protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = ChuanBi(e);
        var r = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
        Color nen = Chinh ? (Hover ? GiaoDien.Hong : GiaoDien.Tim) : (Hover ? GiaoDien.NenHoverNut : GiaoDien.NenNut);
        using (var duong = BieuTuongTron ? new GraphicsPath() : Ve.BoGoc(r, 8 * K))
        {
            if (BieuTuongTron) duong.AddEllipse(r);
            if (!BieuTuongTron || Hover) { using var b = new SolidBrush(nen); g.FillPath(b, duong); }
            if (Focused) { using var v = new Pen(GiaoDien.Hong, 1.5f); g.DrawPath(v, duong); }
        }
        var mau = MauChu ?? (Chinh ? Color.White : GiaoDien.Chu);
        var ra = Rectangle.Round(r);
        if (BieuTuongTron)
        {
            using var f = new Font(Ve.FontBieuTuong, 11f);
            TextRenderer.DrawText(g, BieuTuong, f, ra, mau, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }
        if (BieuTuong != "")
        {
            using var f = new Font(Ve.FontBieuTuong, 10f);
            int rongChu = TextRenderer.MeasureText(Text, Font).Width;
            int x = (Width - rongChu - (int)(22 * K)) / 2;
            TextRenderer.DrawText(g, BieuTuong, f, new Rectangle(x, 0, (int)(20 * K), Height), mau, TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(g, Text, Font, new Rectangle(x + (int)(22 * K), 0, rongChu + 4, Height), mau, TextFormatFlags.VerticalCenter);
            return;
        }
        TextRenderer.DrawText(g, Text, Font, ra, mau, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}

/// Nút gạt nhiều lựa chọn (segmented control)
sealed class NutGat : ControlVe
{
    readonly string[] nhan;
    int chon;
    int dangHover = -1;
    public event Action<int>? ChonMoi;

    public NutGat(string[] nhan, int chon)
    {
        this.nhan = nhan; this.chon = chon;
        Cursor = Cursors.Hand;
        Font = new Font("Segoe UI", 9.5f);
    }

    int[] RongTung()
    {
        return nhan.Select(n => TextRenderer.MeasureText(n, Font).Width + (int)(24 * K)).ToArray();
    }

    protected override void OnCreateControl()
    {
        Size = new Size(RongTung().Sum() + (int)(6 * K), (int)(34 * K));
        base.OnCreateControl();
    }

    int OTai(int x)
    {
        int cx = (int)(3 * K);
        var r = RongTung();
        for (int i = 0; i < r.Length; i++) { if (x >= cx && x < cx + r[i]) return i; cx += r[i]; }
        return -1;
    }

    protected override void OnMouseMove(MouseEventArgs e) { int h = OTai(e.X); if (h != dangHover) { dangHover = h; Invalidate(); } base.OnMouseMove(e); }
    protected override void OnMouseLeave(EventArgs e) { dangHover = -1; base.OnMouseLeave(e); }
    protected override void OnMouseClick(MouseEventArgs e)
    {
        int i = OTai(e.X);
        if (i >= 0 && i != chon) { chon = i; Invalidate(); ChonMoi?.Invoke(i); }
        base.OnMouseClick(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = ChuanBi(e);
        float k = K;
        using (var nen = new SolidBrush(GiaoDien.NenNut))
        using (var d = Ve.BoGoc(new RectangleF(0, 0, Width - 1, Height - 1), 9 * k))
            g.FillPath(nen, d);
        int cx = (int)(3 * k);
        var r = RongTung();
        for (int i = 0; i < nhan.Length; i++)
        {
            var o = new RectangleF(cx, 3 * k, r[i], Height - 7 * k);
            if (i == chon || i == dangHover)
            {
                using var b = new SolidBrush(i == chon ? GiaoDien.Tim : GiaoDien.NenHoverNut);
                using var d = Ve.BoGoc(o, 7 * k);
                g.FillPath(b, d);
            }
            TextRenderer.DrawText(g, nhan[i], Font, Rectangle.Round(o), i == chon ? Color.White : GiaoDien.ChuPhu,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            cx += r[i];
        }
    }
}

/// Mục trên thanh bên: biểu tượng + chữ, nền tím nhạt khi đang chọn
sealed class MucDieuHuong : ControlVe
{
    public string BieuTuong;
    public bool DangChon;
    public Color? MauChu;

    public MucDieuHuong(string bieuTuong, string chu)
    {
        BieuTuong = bieuTuong; Text = chu;
        Cursor = Cursors.Hand;
        Font = new Font("Segoe UI", 10f);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = ChuanBi(e);
        float k = K;
        if (DangChon || Hover)
        {
            using var b = new SolidBrush(DangChon ? GiaoDien.NenMucChon : GiaoDien.NenHoverMuc);
            using var d = Ve.BoGoc(new RectangleF(0, 0, Width - 1, Height - 1), 8 * k);
            g.FillPath(b, d);
        }
        var mau = MauChu ?? (DangChon ? Color.White : GiaoDien.ChuPhu);
        using var f = new Font(Ve.FontBieuTuong, 11f);
        TextRenderer.DrawText(g, BieuTuong, f, new Rectangle((int)(10 * k), 0, (int)(24 * k), Height), DangChon ? GiaoDien.TimNhat : mau, TextFormatFlags.VerticalCenter);
        TextRenderer.DrawText(g, Text, Font, new Rectangle((int)(40 * k), 0, Width - (int)(44 * k), Height), mau,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}
