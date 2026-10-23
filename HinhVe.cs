using System.Drawing;
using System.Drawing.Drawing2D;

namespace VgsChupHdr;

enum CongCu { Khong, But, Duong, MuiTen, Khung, DanhDau, Chu }

abstract class HinhVe
{
    public Color Mau;
    public float DoDay;
    public abstract void Ve(Graphics g);

    protected Pen TaoBut(bool tron = true)
    {
        var p = new Pen(Mau, DoDay);
        if (tron) { p.StartCap = LineCap.Round; p.EndCap = LineCap.Round; p.LineJoin = LineJoin.Round; }
        return p;
    }
}

sealed class HinhBut : HinhVe
{
    public List<PointF> Diem = new();
    public override void Ve(Graphics g)
    {
        if (Diem.Count == 1) { using var b = new SolidBrush(Mau); g.FillEllipse(b, Diem[0].X - DoDay / 2, Diem[0].Y - DoDay / 2, DoDay, DoDay); return; }
        if (Diem.Count < 2) return;
        using var p = TaoBut();
        g.DrawLines(p, Diem.ToArray());
    }
}

sealed class HinhDuong : HinhVe
{
    public PointF A, B;
    public bool MuiTen;
    public override void Ve(Graphics g)
    {
        using var p = TaoBut();
        if (MuiTen)
        {
            p.EndCap = LineCap.Flat;
            p.CustomEndCap = new AdjustableArrowCap(DoDay * 1.6f, DoDay * 2.2f, true);
        }
        if (A == B) { g.DrawLine(p, A, new PointF(B.X + 0.1f, B.Y)); return; }
        g.DrawLine(p, A, B);
    }
}

sealed class HinhKhung : HinhVe
{
    public RectangleF Vung;
    public override void Ve(Graphics g)
    {
        using var p = TaoBut(false);
        p.LineJoin = LineJoin.Miter;
        g.DrawRectangle(p, Vung.X, Vung.Y, Math.Max(1, Vung.Width), Math.Max(1, Vung.Height));
    }
}

sealed class HinhDanhDau : HinhVe
{
    public PointF A, B;
    public override void Ve(Graphics g)
    {
        using var p = new Pen(Color.FromArgb(110, Mau), DoDay) { StartCap = LineCap.Flat, EndCap = LineCap.Flat };
        g.DrawLine(p, A, A == B ? new PointF(B.X + 0.1f, B.Y) : B);
    }
}

sealed class HinhChu : HinhVe
{
    public string Chu = "";
    public PointF ViTri;
    public required Font Phong;
    public override void Ve(Graphics g)
    {
        using var b = new SolidBrush(Mau);
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        g.DrawString(Chu, Phong, b, ViTri);
    }
}
