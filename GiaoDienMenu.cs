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

    public DauMenu(Action khiBam) : base("VGScreenshotHDR") { Click += (_, _) => khiBam(); }

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
        TextRenderer.DrawText(g, "VGScreenshotHDR", fTen, new Point(x, yTen), Color.White);
        int rongTen = TextRenderer.MeasureText("VGScreenshotHDR", fTen).Width;
        TextRenderer.DrawText(g, "v" + GiaoDien.PhienBan, fMo, new Point(x + rongTen, yTen + (int)(3 * k)), GiaoDien.ChuMo);
        TextRenderer.DrawText(g, MoTa, fMo, new Point(x, yTen + fTen.Height + (int)(2 * k)), Color.FromArgb(200, 200, 200));
    }
}

/// Hộp "Giới thiệu": app làm gì, phím tắt, dẫn về vgs.zone.
sealed class HopGioiThieu : Form
{
    public HopGioiThieu()
    {
        Text = "Giới thiệu VGScreenshotHDR";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = GiaoDien.Nen;
        ForeColor = GiaoDien.Chu;
        Font = new Font("Segoe UI", 10f);
        ShowInTaskbar = true;
        TopMost = true;
        try { Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath!); } catch { }

        // Bố cục tự co theo chữ (toạ độ cố định từng vỡ ở màn scale 150–200%): chỉ bề rộng cột chữ đặt theo DPI.
        AutoScaleMode = AutoScaleMode.None;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        float k = DeviceDpi / 96f;
        int Px(int v) => (int)(v * k);
        int rong = Px(440);

        var khung = new TableLayoutPanel
        {
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1, Padding = Padding.Empty, Margin = Padding.Empty,
        };
        Controls.Add(khung);

        // ── dải đầu gradient: logo + tên + phiên bản
        var dai = new DaiGradient { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = Padding.Empty, Padding = new Padding(Px(24), Px(22), Px(24), Px(22)), MinimumSize = new Size(rong, 0) };
        var hangDau = new FlowLayoutPanel { AutoSize = true, BackColor = Color.Transparent, Margin = Padding.Empty, WrapContents = false };
        var logo = new PictureBox { Image = GiaoDien.Logo(), SizeMode = PictureBoxSizeMode.Zoom, Size = new Size(Px(56), Px(56)), Margin = new Padding(0, 0, Px(16), 0), BackColor = Color.Transparent };
        var cotTen = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, BackColor = Color.Transparent, Margin = Padding.Empty, WrapContents = false };
        cotTen.Controls.Add(Nhan("VGScreenshotHDR", new Font("Segoe UI Semibold", 17f), Color.White, 0));
        cotTen.Controls.Add(Nhan($"Phiên bản {GiaoDien.PhienBan} · by VGS", new Font("Segoe UI", 9.5f), Color.FromArgb(240, 240, 240), 0));
        hangDau.Controls.Add(logo);
        hangDau.Controls.Add(cotTen);
        dai.Controls.Add(hangDau);
        khung.Controls.Add(dai);

        // ── nội dung
        var than = new TableLayoutPanel
        {
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1,
            Padding = new Padding(Px(24), Px(18), Px(24), Px(20)), Margin = Padding.Empty,
        };
        than.Controls.Add(Nhan("Bật HDR thì nút PrtScn của Windows chụp ra ảnh bạc màu, cháy sáng. "
            + "VGScreenshotHDR đọc thẳng dữ liệu HDR của màn hình rồi chuyển về đúng màu mắt bạn thấy.", Font, GiaoDien.Chu, rong - Px(48)));

        var bangPhim = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, Margin = new Padding(0, Px(14), 0, Px(4)) };
        foreach (var (phim, viec) in new[]
        {
            ("PrtScn", "Chọn vùng, vẽ chú thích, chép / lưu"),
            ("Shift+PrtScn", "Chụp toàn màn hình"),
            ("Alt+PrtScn", "Chụp cửa sổ đang mở"),
        })
        {
            var nhanPhim = Nhan(phim, new Font("Segoe UI Semibold", 10f), GiaoDien.Hong, 0);
            nhanPhim.Margin = new Padding(0, Px(2), Px(16), Px(2));
            var nhanViec = Nhan(viec, Font, GiaoDien.Chu, 0);
            nhanViec.Margin = new Padding(0, Px(2), 0, Px(2));
            bangPhim.Controls.Add(nhanPhim);
            bangPhim.Controls.Add(nhanViec);
        }
        than.Controls.Add(bangPhim);
        var nhanCongCu = Nhan("Trong khung chọn: P bút · L đường · A mũi tên · R khung · M đánh dấu · T chữ · Ctrl+Z hoàn tác",
            new Font("Segoe UI", 9f), GiaoDien.ChuMo, rong - Px(48));
        nhanCongCu.Margin = new Padding(0, Px(6), 0, Px(14));
        than.Controls.Add(nhanCongCu);

        var lienKet = new LinkLabel
        {
            AutoSize = true,
            MaximumSize = new Size(rong - Px(48), 0),
            UseMnemonic = false,   // không thì "&" bị nuốt thành phím tắt
            Text = "Thêm tiện ích miễn phí cho game thủ & dân văn phòng tại vgs.zone",
            ForeColor = GiaoDien.Chu,
            LinkColor = GiaoDien.Hong,
            ActiveLinkColor = Color.White,
            VisitedLinkColor = GiaoDien.Hong,
            Margin = new Padding(0, 0, 0, Px(16)),
        };
        lienKet.LinkArea = new LinkArea(lienKet.Text.IndexOf("vgs.zone", StringComparison.Ordinal), "vgs.zone".Length);
        lienKet.LinkClicked += (_, _) => GiaoDien.MoTrangVgs();
        than.Controls.Add(lienKet);

        var hangNut = new TableLayoutPanel { AutoSize = true, ColumnCount = 3, Margin = Padding.Empty, MinimumSize = new Size(rong - Px(48), 0) };
        hangNut.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        hangNut.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        hangNut.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var nutVgs = TaoNut("Mở vgs.zone", true, k);
        nutVgs.Click += (_, _) => GiaoDien.MoTrangVgs();
        var nutDong = TaoNut("Đóng", false, k);
        nutDong.Click += (_, _) => Close();
        hangNut.Controls.Add(nutVgs, 0, 0);
        hangNut.Controls.Add(nutDong, 2, 0);
        than.Controls.Add(hangNut);
        khung.Controls.Add(than);
        CancelButton = nutDong;
    }

    static Label Nhan(string chu, Font font, Color mau, int rongToiDa) => new()
    {
        Text = chu,
        Font = font,
        ForeColor = mau,
        BackColor = Color.Transparent,
        AutoSize = true,
        UseMnemonic = false,
        MaximumSize = new Size(rongToiDa, 0),
        Margin = Padding.Empty,
    };

    /// Panel tô nền gradient tím–hồng để nhãn trong suốt bên trên thấy được nền (FlowLayoutPanel để Padding có tác dụng)
    internal sealed class DaiGradient : FlowLayoutPanel
    {
        public DaiGradient() { DoubleBuffered = true; ResizeRedraw = true; }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (ClientRectangle.Width <= 0 || ClientRectangle.Height <= 0) return;
            using var nen = GiaoDien.ButGradient(ClientRectangle);
            e.Graphics.FillRectangle(nen, ClientRectangle);
        }
    }

    static Button TaoNut(string chu, bool chinh, float k)
    {
        var b = new Button
        {
            Text = chu,
            AutoSize = true,
            MinimumSize = new Size((int)((chinh ? 150 : 100) * k), (int)(38 * k)),
            Padding = new Padding((int)(10 * k), 0, (int)(10 * k), 0),
            Margin = Padding.Empty,
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
