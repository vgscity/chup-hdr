using System.Drawing.Drawing2D;

namespace VgsChupHdr.Quay;

/// Thanh nhỏ nổi lúc đang quay: chấm đỏ nhấp nháy, đồng hồ, nút dừng; kéo được; không lọt vào khung quay nếu đặt ngoài vùng.
sealed class ThanhQuay : Form
{
    readonly BoQuay quay;
    readonly Action dung;
    readonly System.Windows.Forms.Timer nhip = new() { Interval = 500 };
    readonly Label gio = new();
    readonly Button nutDung = new();
    bool nhay;
    Point diemKeo;

    public ThanhQuay(BoQuay quay, Rectangle vungQuay, Action dung)
    {
        this.quay = quay; this.dung = dung;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false; TopMost = true; StartPosition = FormStartPosition.Manual;
        BackColor = Color.FromArgb(26, 22, 38); ForeColor = Color.White;
        float tiLe = Math.Max(1f, DeviceDpi / 96f);
        Size = new Size((int)(190 * tiLe), (int)(40 * tiLe));
        // Đặt ngay trên vùng quay (ngoài vùng để không lọt vào video); không đủ chỗ thì đặt dưới
        var man = Screen.FromRectangle(vungQuay).Bounds;
        int x = Math.Clamp(vungQuay.X + vungQuay.Width / 2 - Width / 2, man.Left, man.Right - Width);
        int y = vungQuay.Top - Height - 8 >= man.Top ? vungQuay.Top - Height - 8 : Math.Min(vungQuay.Bottom + 8, man.Bottom - Height);
        Location = new Point(x, y);

        gio.AutoSize = false; gio.TextAlign = ContentAlignment.MiddleLeft;
        gio.Font = new Font("Segoe UI", 11f * tiLe, FontStyle.Bold, GraphicsUnit.Pixel);
        gio.Bounds = new Rectangle((int)(30 * tiLe), 0, (int)(90 * tiLe), Height);
        nutDung.Text = Chu.DungQuay; nutDung.FlatStyle = FlatStyle.Flat; nutDung.FlatAppearance.BorderSize = 0;
        nutDung.BackColor = Color.FromArgb(220, 53, 69); nutDung.ForeColor = Color.White;
        nutDung.Font = new Font("Segoe UI", 10f * tiLe, FontStyle.Bold, GraphicsUnit.Pixel);
        nutDung.Bounds = new Rectangle((int)(122 * tiLe), (int)(7 * tiLe), (int)(60 * tiLe), Height - (int)(14 * tiLe));
        nutDung.Click += (_, _) => dung();
        Controls.Add(gio); Controls.Add(nutDung);
        nhip.Tick += (_, _) => { nhay = !nhay; gio.Text = quay.ThoiLuong.ToString(@"mm\:ss"); Invalidate(new Rectangle(0, 0, (int)(30 * tiLe), Height)); };
        nhip.Start();
        foreach (Control c in new Control[] { this, gio })
        {
            c.MouseDown += (_, e) => { if (e.Button == MouseButtons.Left) diemKeo = e.Location; };
            c.MouseMove += (_, e) => { if (e.Button == MouseButtons.Left) Location = new Point(Location.X + e.X - diemKeo.X, Location.Y + e.Y - diemKeo.Y); };
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        float tiLe = Math.Max(1f, DeviceDpi / 96f);
        using var b = new SolidBrush(nhay ? Color.FromArgb(255, 59, 48) : Color.FromArgb(120, 255, 59, 48));
        float d = 10 * tiLe;
        e.Graphics.FillEllipse(b, 12 * tiLe, (Height - d) / 2, d, d);
    }

    protected override bool ShowWithoutActivation => true;
    protected override CreateParams CreateParams
    {
        get { var cp = base.CreateParams; cp.ExStyle |= 0x08000000 /* WS_EX_NOACTIVATE */ | 0x00000080 /* WS_EX_TOOLWINDOW */; return cp; }
    }

    protected override void OnFormClosed(FormClosedEventArgs e) { nhip.Stop(); base.OnFormClosed(e); }
}
