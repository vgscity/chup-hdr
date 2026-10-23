using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace VgsChupHdr;

/// Màu và kiểu chung cho menu khay + hộp giới thiệu: nền tối như thanh công cụ khung chọn, nhấn tím–hồng VGS.
static class GiaoDien
{
    public const string TrangVgs = "https://vgs.zone";
    public static readonly Color Nen = Color.FromArgb(31, 31, 31);
    public static readonly Color NenHover = Color.FromArgb(58, 42, 82);
    public static readonly Color Vien = Color.FromArgb(60, 60, 60);
    public static readonly Color Chu = Color.FromArgb(240, 240, 240);
    public static readonly Color ChuMo = Color.FromArgb(150, 150, 150);
    public static readonly Color Tim = Color.FromArgb(124, 58, 237);
    public static readonly Color Hong = Color.FromArgb(236, 72, 153);

    // Cửa sổ chính: phẳng, ít màu, tím–hồng chỉ cho nút chính, công tắc, mục đang chọn
    public static readonly Color NenCuaSo = Color.FromArgb(22, 22, 24);
    public static readonly Color NenThanhBen = Color.FromArgb(28, 28, 31);
    public static readonly Color VienCuaSo = Color.FromArgb(44, 44, 48);
    public static readonly Color NenThe = Color.FromArgb(30, 30, 33);
    public static readonly Color VienThe = Color.FromArgb(46, 46, 51);
    public static readonly Color NenHdr = Color.FromArgb(31, 26, 46);
    public static readonly Color VienHdr = Color.FromArgb(59, 47, 92);
    public static readonly Color NenNut = Color.FromArgb(42, 42, 47);
    public static readonly Color NenHoverNut = Color.FromArgb(58, 58, 64);
    public static readonly Color TatCongTac = Color.FromArgb(58, 58, 64);
    public static readonly Color NenMucChon = Color.FromArgb(42, 35, 64);
    public static readonly Color NenHoverMuc = Color.FromArgb(36, 36, 40);
    public static readonly Color ChuPhu = Color.FromArgb(168, 168, 174);
    public static readonly Color ChuNhat = Color.FromArgb(142, 142, 149);
    public static readonly Color TimNhat = Color.FromArgb(196, 181, 253);
    public static readonly Color HongNhat = Color.FromArgb(249, 168, 212);
    public static readonly Color XanhOk = Color.FromArgb(74, 222, 128);
    public static readonly Color CamCanhBao = Color.FromArgb(251, 146, 60);
    public static readonly Color NenPhim = Color.FromArgb(42, 42, 47);
    public static readonly Color VienPhim = Color.FromArgb(68, 68, 75);

    public static string PhienBan
    {
        get
        {
            var v = Application.ProductVersion;
            int cong = v.IndexOf('+');
            return cong > 0 ? v[..cong] : v;
        }
    }

    public static Image? Logo()
    {
        try
        {
            using var luong = typeof(GiaoDien).Assembly.GetManifestResourceStream("tray.png");
            return luong == null ? null : new Bitmap(luong);
        }
        catch { return null; }
    }

    public static void MoTrangVgs()
    {
        try { Process.Start(new ProcessStartInfo(TrangVgs) { UseShellExecute = true }); }
        catch (Exception e) { NhatKy.Ghi("Mở vgs.zone lỗi", e); }
    }

    public static LinearGradientBrush ButGradient(Rectangle r) => new(r, Tim, Hong, LinearGradientMode.ForwardDiagonal);
}

/// Thông tin vẽ thêm cho một mục menu khay (để ở ToolStripItem.Tag): biểu tượng bên trái,
/// phím tắt vẽ dạng keycap, hoặc công tắc gạt thay dấu tick.
sealed record MucMenu(string BieuTuong, Keys Phim = Keys.None, bool CongTac = false);

/// Menu khay cùng kiểu cửa sổ chính: nền phẳng tối, mục đang trỏ có nền tím bo góc, biểu tượng như thanh bên,
/// phím tắt dạng keycap, mục bật/tắt là công tắc gạt.
sealed class VeMenuToi : ToolStripRenderer
{
    static float K(ToolStripItem i) => (i.Owner?.DeviceDpi ?? 96) / 96f;

    /// Gắn biểu tượng / phím / công tắc vào mục. ShortcutKeyDisplayString chỉ để giữ chỗ cột bên phải cho keycap/công tắc.
    public static T Gan<T>(T muc, string bieuTuong, Keys phim = Keys.None, bool congTac = false) where T : ToolStripMenuItem
    {
        muc.Tag = new MucMenu(bieuTuong, phim, congTac);
        if (congTac) muc.ShortcutKeyDisplayString = "   ";
        else if (phim != Keys.None) DatPhim(muc, phim);
        return muc;
    }

    public static void DatPhim(ToolStripMenuItem muc, Keys phim)
    {
        if (muc.Tag is MucMenu m) muc.Tag = m with { Phim = phim };
        // keycap rộng hơn chữ trần: chừa thêm khoảng cho viền + khe giữa các phím
        muc.ShortcutKeyDisplayString = (phim & Keys.KeyCode) == Keys.None ? Chu.ChuaDatPhim : string.Join("  ", O_Phim.TachPhim(phim)) + "  ";
    }

    protected override void Initialize(ToolStrip ts)
    {
        base.Initialize(ts);
        ts.BackColor = GiaoDien.NenThanhBen;
        ts.ForeColor = GiaoDien.Chu;
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e) => e.Graphics.Clear(GiaoDien.NenThanhBen);
    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e) { }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var p = new Pen(GiaoDien.VienCuaSo);
        e.Graphics.DrawRectangle(p, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        float k = K(e.Item);
        int y = e.Item.Height / 2;
        using var p = new Pen(GiaoDien.VienCuaSo);
        e.Graphics.DrawLine(p, 12 * k, y, e.Item.Width - 12 * k, y);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (e.Item is DauMenu) return;
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float k = K(e.Item);
        if (e.Item.Selected && e.Item.Enabled)
        {
            using var b = new SolidBrush(GiaoDien.NenMucChon);
            using var d = Ve.BoGoc(new RectangleF(2 * k, 1, e.Item.Width - 4 * k, e.Item.Height - 2), 7 * k);
            g.FillPath(b, d);
        }
        if (e.Item.Tag is MucMenu m && m.BieuTuong != "")
        {
            // Cột trái của menu cố định ~25px (padding trái không đẩy được chữ) → biểu tượng 9pt nằm gọn trong cột đó.
            // Font biểu tượng có khoảng trống dưới lớn → kéo ô vẽ lên cho cân giữa dòng.
            using var f = new Font(Ve.FontBieuTuong, 9f);
            var mau = e.Item.ForeColor == GiaoDien.Hong ? GiaoDien.Hong : e.Item.Selected ? GiaoDien.TimNhat : GiaoDien.ChuPhu;
            TextRenderer.DrawText(g, m.BieuTuong, f, new Rectangle((int)(5 * k), (int)(-2 * k), (int)(18 * k), e.Item.Height), mau,
                TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
        }
    }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e) { }   // trạng thái bật/tắt vẽ bằng công tắc ở cột phải

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float k = K(e.Item);
        var mi = e.Item as ToolStripMenuItem;
        var m = e.Item.Tag as MucMenu;
        bool cotPhai = mi?.ShortcutKeyDisplayString != null && e.Text == mi.ShortcutKeyDisplayString;
        if (!cotPhai)
        {
            e.TextColor = !e.Item.Enabled ? GiaoDien.ChuNhat : e.Item.ForeColor == GiaoDien.Hong ? GiaoDien.Hong : GiaoDien.Chu;
            base.OnRenderItemText(e);
            return;
        }
        var r = e.TextRectangle;
        int phai = e.Item.Width - (int)(16 * k);
        if (m is { CongTac: true })
        {
            // công tắc gạt, cùng kiểu CongTac trong cửa sổ chính
            float w = 32 * k, h = 18 * k;
            var o = new RectangleF(phai - w, (e.Item.Height - h) / 2, w, h);
            using (var d = Ve.BoGoc(o, h / 2))
            using (var b = new SolidBrush(mi!.Checked ? GiaoDien.Tim : GiaoDien.TatCongTac)) g.FillPath(b, d);
            float n = h - 5 * k;
            float x = mi.Checked ? o.Right - n - 2.5f * k : o.X + 2.5f * k;
            g.FillEllipse(Brushes.White, x, o.Y + 2.5f * k, n, n);
            return;
        }
        if (m != null && (m.Phim & Keys.KeyCode) != Keys.None)
        {
            using var f = new Font("Segoe UI", 8f);
            var phan = O_Phim.TachPhim(m.Phim);
            int cao = (int)(20 * k);
            // đo trước để căn phải
            int rong = phan.Sum(p => Math.Max(cao, TextRenderer.MeasureText(p, f).Width + (int)(12 * k))) + (phan.Length - 1) * (int)(4 * k);
            O_Phim.VeKeycap(g, phan, f, phai - rong, (e.Item.Height - cao) / 2, cao, k, GiaoDien.NenPhim, GiaoDien.VienPhim, GiaoDien.ChuPhu);
            return;
        }
        TextRenderer.DrawText(g, e.Text, e.TextFont, new Rectangle(r.X, r.Y, phai - r.X, r.Height), GiaoDien.ChuNhat,
            TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
    }

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        e.ArrowColor = GiaoDien.ChuNhat;
        base.OnRenderArrow(e);
    }
}

/// Đầu menu: logo, tên, phiên bản, câu giới thiệu — nền phẳng như đầu thanh bên cửa sổ chính. Bấm vào mở cửa sổ chính.
sealed class DauMenu : ToolStripMenuItem
{
    readonly Image? logo = GiaoDien.Logo();
    static string MoTa => Chu.MoTaNgan;

    // Menu thả xuống tính bề rộng theo Text của mục (bỏ qua GetPreferredSize) → đặt Text bằng câu giới thiệu
    // + khoảng đệm cho logo, để câu không bị cắt (bản tiếng Anh dài hơn từng bị cắt "…highlig").
    public DauMenu(Action khiBam) : base(MoTa + "    ")
    {
        AccessibleName = Chu.TenApp;
        Click += (_, _) => khiBam();
    }

    float TiLe => (Owner?.DeviceDpi ?? 96) / 96f;

    public override Size GetPreferredSize(Size constrainingSize)
    {
        float k = TiLe;
        using var fMo = new Font(Font.FontFamily, Font.Size * 0.9f);
        int rongChu = TextRenderer.MeasureText(MoTa, fMo).Width;
        return new Size((int)(60 * k) + rongChu + (int)(16 * k), (int)(58 * k));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float k = TiLe;
        var r = new Rectangle(Point.Empty, Size);
        g.Clear(GiaoDien.NenThanhBen);
        if (Selected)
        {
            using var b = new SolidBrush(GiaoDien.NenHoverMuc);
            using var d = Ve.BoGoc(new RectangleF(5 * k, 2 * k, r.Width - 10 * k, r.Height - 4 * k), 8 * k);
            g.FillPath(b, d);
        }

        int coLogo = (int)(34 * k);
        var oLogo = new Rectangle((int)(14 * k), (r.Height - coLogo) / 2, coLogo, coLogo);
        if (logo != null) { g.InterpolationMode = InterpolationMode.HighQualityBicubic; g.DrawImage(logo, oLogo); }

        int x = oLogo.Right + (int)(12 * k);
        using var fTen = new Font("Segoe UI Semibold", Font.Size * 1.1f);
        using var fMo = new Font(Font.FontFamily, Font.Size * 0.9f);
        int yTen = (int)(10 * k);
        TextRenderer.DrawText(g, Chu.TenApp, fTen, new Point(x, yTen), Color.White);
        int rongTen = TextRenderer.MeasureText(Chu.TenApp, fTen).Width;
        TextRenderer.DrawText(g, "v" + GiaoDien.PhienBan, fMo, new Point(x + rongTen, yTen + (int)(2 * k)), GiaoDien.ChuNhat);
        TextRenderer.DrawText(g, MoTa, fMo, new Point(x, yTen + fTen.Height + (int)(1 * k)), GiaoDien.ChuNhat);
    }
}
