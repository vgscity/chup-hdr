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
    const string MoTa = "Chụp màn hình HDR đúng màu, không cháy sáng";

    public DauMenu(Action khiBam) : base("VGS Chụp HDR") { Click += (_, _) => khiBam(); }

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
        TextRenderer.DrawText(g, "VGS Chụp HDR", fTen, new Point(x, yTen), Color.White);
        int rongTen = TextRenderer.MeasureText("VGS Chụp HDR", fTen).Width;
        TextRenderer.DrawText(g, "v" + GiaoDien.PhienBan, fMo, new Point(x + rongTen, yTen + (int)(3 * k)), GiaoDien.ChuMo);
        TextRenderer.DrawText(g, MoTa, fMo, new Point(x, yTen + fTen.Height + (int)(2 * k)), Color.FromArgb(200, 200, 200));
    }
}

/// Hộp "Giới thiệu": app làm gì, phím tắt, dẫn về vgs.zone.
sealed class HopGioiThieu : Form
{
    public HopGioiThieu()
    {
        Text = "Giới thiệu VGS Chụp HDR";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = GiaoDien.Nen;
        ForeColor = GiaoDien.Chu;
        Font = new Font("Segoe UI", 10f);
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96, 96);
        ClientSize = new Size(460, 470);
        ShowInTaskbar = true;
        TopMost = true;
        try { Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath!); } catch { }

        var dai = new Panel { Dock = DockStyle.Top, Height = 110 };
        dai.Paint += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var nen = GiaoDien.ButGradient(dai.ClientRectangle)) g.FillRectangle(nen, dai.ClientRectangle);
            float k = DeviceDpi / 96f;
            using var logo = GiaoDien.Logo();
            var oLogo = new Rectangle((int)(24 * k), (int)(27 * k), (int)(56 * k), (int)(56 * k));
            if (logo != null) { g.InterpolationMode = InterpolationMode.HighQualityBicubic; g.DrawImage(logo, oLogo); }
            using var fTen = new Font("Segoe UI Semibold", 17f);
            using var fPb = new Font("Segoe UI", 9.5f);
            int x = oLogo.Right + (int)(16 * k);
            TextRenderer.DrawText(g, "VGS Chụp HDR", fTen, new Point(x, (int)(28 * k)), Color.White);
            TextRenderer.DrawText(g, $"Phiên bản {GiaoDien.PhienBan} · by VGS", fPb, new Point(x, (int)(62 * k)), Color.FromArgb(235, 235, 235));
        };
        Controls.Add(dai);

        var noiDung = new Label
        {
            AutoSize = false,
            Location = new Point(24, 126),
            Size = new Size(412, 230),
            ForeColor = GiaoDien.Chu,
            Text =
                "Bật HDR thì nút PrtScn của Windows chụp ra ảnh bạc màu, cháy sáng. "
                + "VGS Chụp HDR đọc thẳng dữ liệu HDR của màn hình rồi chuyển về đúng màu mắt bạn thấy.\n\n"
                + "PrtScn\t\tChọn vùng, vẽ chú thích, chép / lưu\n"
                + "Shift+PrtScn\tChụp toàn màn hình\n"
                + "Alt+PrtScn\tChụp cửa sổ đang mở\n\n"
                + "Trong khung chọn: P bút · L đường · A mũi tên · R khung · M đánh dấu · T chữ · Ctrl+Z hoàn tác.",
        };
        Controls.Add(noiDung);

        var lienKet = new LinkLabel
        {
            AutoSize = true,
            Location = new Point(24, 370),
            Text = "Thêm tiện ích miễn phí cho game thủ & dân văn phòng tại vgs.zone",
            LinkColor = GiaoDien.Hong,
            ActiveLinkColor = Color.White,
            VisitedLinkColor = GiaoDien.Hong,
        };
        lienKet.LinkArea = new LinkArea(lienKet.Text.IndexOf("vgs.zone", StringComparison.Ordinal), "vgs.zone".Length);
        lienKet.LinkClicked += (_, _) => GiaoDien.MoTrangVgs();
        Controls.Add(lienKet);

        var nutVgs = TaoNut("Mở vgs.zone", true);
        nutVgs.Location = new Point(24, 412);
        nutVgs.Click += (_, _) => GiaoDien.MoTrangVgs();
        Controls.Add(nutVgs);

        var nutDong = TaoNut("Đóng", false);
        nutDong.Location = new Point(436 - nutDong.Width, 412);
        nutDong.Click += (_, _) => Close();
        Controls.Add(nutDong);
        CancelButton = nutDong;
    }

    static Button TaoNut(string chu, bool chinh)
    {
        var b = new Button
        {
            Text = chu,
            Size = new Size(chinh ? 150 : 100, 36),
            FlatStyle = FlatStyle.Flat,
            BackColor = chinh ? GiaoDien.Tim : Color.FromArgb(45, 45, 45),
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
        };
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = chinh ? GiaoDien.Hong : Color.FromArgb(70, 70, 70);
        return b;
    }

    static HopGioiThieu? dangMo;
    public static void Hien()
    {
        if (dangMo != null && !dangMo.IsDisposed) { dangMo.Activate(); return; }
        dangMo = new HopGioiThieu();
        dangMo.FormClosed += (_, _) => dangMo = null;
        dangMo.Show();
    }
}
