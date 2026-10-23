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

/// Bảng màu tối cho ContextMenuStrip
sealed class BangMauToi : ProfessionalColorTable
{
    public override Color ToolStripDropDownBackground => GiaoDien.Nen;
    public override Color ImageMarginGradientBegin => GiaoDien.Nen;
    public override Color ImageMarginGradientMiddle => GiaoDien.Nen;
    public override Color ImageMarginGradientEnd => GiaoDien.Nen;
    public override Color MenuBorder => GiaoDien.Vien;
    public override Color MenuItemBorder => GiaoDien.NenHover;
    public override Color MenuItemSelected => GiaoDien.NenHover;
    public override Color MenuItemSelectedGradientBegin => GiaoDien.NenHover;
    public override Color MenuItemSelectedGradientEnd => GiaoDien.NenHover;
    public override Color MenuItemPressedGradientBegin => GiaoDien.NenHover;
    public override Color MenuItemPressedGradientEnd => GiaoDien.NenHover;
    public override Color SeparatorDark => GiaoDien.Vien;
    public override Color SeparatorLight => GiaoDien.Vien;
    public override Color CheckBackground => GiaoDien.Nen;
    public override Color CheckSelectedBackground => GiaoDien.NenHover;
    public override Color CheckPressedBackground => GiaoDien.NenHover;
}

sealed class VeMenuToi : ToolStripProfessionalRenderer
{
    public VeMenuToi() : base(new BangMauToi()) { RoundedEdges = false; }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        // Cột phím tắt (ShortcutKeyDisplayString) màu mờ hơn tên mục
        bool laPhimTat = e.Item is ToolStripMenuItem mi && mi.ShortcutKeyDisplayString != null && e.Text == mi.ShortcutKeyDisplayString;
        bool mauRieng = e.Item.ForeColor == GiaoDien.Hong;   // mục dẫn về vgs.zone giữ màu hồng
        e.TextColor = !e.Item.Enabled || laPhimTat ? GiaoDien.ChuMo : mauRieng ? GiaoDien.Hong : GiaoDien.Chu;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var r = e.ImageRectangle;
        r.Inflate(-1, -1);
        using (var nen = GiaoDien.ButGradient(r)) g.FillEllipse(nen, r);
        using var p = new Pen(Color.White, Math.Max(1.5f, r.Width / 8f)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLines(p, new[]
        {
            new PointF(r.Left + r.Width * 0.28f, r.Top + r.Height * 0.52f),
            new PointF(r.Left + r.Width * 0.44f, r.Top + r.Height * 0.68f),
            new PointF(r.Left + r.Width * 0.72f, r.Top + r.Height * 0.34f),
        });
    }

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        e.ArrowColor = GiaoDien.ChuMo;
        base.OnRenderArrow(e);
    }
}

/// Dải đầu menu: logo, tên, phiên bản, câu giới thiệu ngắn. Bấm vào mở hộp giới thiệu.
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
        return new Size((int)(56 * k) + rongChu + (int)(16 * k), (int)(54 * k));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float k = TiLe;
        var r = new Rectangle(Point.Empty, Size);

        using (var nen = new LinearGradientBrush(r, Color.FromArgb(Selected ? 70 : 45, GiaoDien.Tim), Color.FromArgb(Selected ? 70 : 45, GiaoDien.Hong), LinearGradientMode.Horizontal))
            g.FillRectangle(nen, r);

        int coLogo = (int)(32 * k);
        var oLogo = new Rectangle((int)(12 * k), (r.Height - coLogo) / 2, coLogo, coLogo);
        if (logo != null) { g.InterpolationMode = InterpolationMode.HighQualityBicubic; g.DrawImage(logo, oLogo); }

        int x = oLogo.Right + (int)(12 * k);
        using var fTen = new Font(Font.FontFamily, Font.Size * 1.15f, FontStyle.Bold);
        using var fMo = new Font(Font.FontFamily, Font.Size * 0.9f);
        int yTen = (int)(9 * k);
        TextRenderer.DrawText(g, Chu.TenApp, fTen, new Point(x, yTen), Color.White);
        int rongTen = TextRenderer.MeasureText(Chu.TenApp, fTen).Width;
        TextRenderer.DrawText(g, "v" + GiaoDien.PhienBan, fMo, new Point(x + rongTen, yTen + (int)(3 * k)), GiaoDien.ChuMo);
        TextRenderer.DrawText(g, MoTa, fMo, new Point(x, yTen + fTen.Height + (int)(2 * k)), Color.FromArgb(200, 200, 200));
    }
}
