using System.Runtime.InteropServices;
using Vortice.D3DCompiler;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Windows.Graphics.DirectX.Direct3D11;

namespace VgsChupHdr.Quay;

/// Đổi khung bắt được (FP16 scRGB trên màn HDR hoặc BGRA8 trên màn SDR) thành BGRA8 SDR trên GPU, kèm cắt vùng.
/// Cùng công thức với Chup.DoiHdrSangSdr: chia theo mức trắng SDR, có nội dung HDR thì nén mềm từ 80 %, rồi sRGB.
sealed class ToneMapGpu : IDisposable
{
    const string HLSL = @"
Texture2D nguon : register(t0);
SamplerState lay : register(s0);
cbuffer ThamSo : register(b0) { float2 goc; float2 kich; float heSo; float goi; float laHdr; float nenHdr; };
struct VSOut { float4 pos : SV_Position; float2 uv : TEXCOORD0; };
VSOut VS(uint id : SV_VertexID) {
  VSOut o; float2 v = float2((id << 1) & 2, id & 2);
  o.pos = float4(v * float2(2, -2) + float2(-1, 1), 0, 1);
  o.uv = goc + v * kich; return o; }
float srgb(float c) { return c <= 0.0031308 ? 12.92 * c : 1.055 * pow(c, 1.0 / 2.4) - 0.055; }
float4 PS(VSOut i) : SV_Target {
  float4 p = nguon.Sample(lay, i.uv);
  if (laHdr < 0.5) return float4(p.rgb, 1);
  float3 c = p.rgb * heSo;
  if (nenHdr > 0.5) {
    float Y = dot(c, float3(0.2126, 0.7152, 0.0722));
    if (Y > goi) { float Ym = goi + (1 - goi) * (1 - exp(-(Y - goi) / (1 - goi))); c *= Ym / Y; }
  }
  c = saturate(c);
  return float4(srgb(c.r), srgb(c.g), srgb(c.b), 1); }
float4 PSThu(VSOut i) : SV_Target { return float4(nguon.Sample(lay, i.uv).rgb * heSo, 1); }";

    [StructLayout(LayoutKind.Sequential)]
    struct ThamSo { public float gocX, gocY, kichX, kichY, heSo, goi, laHdr, nenHdr; }

    public sealed class Khung
    {
        public required ID3D11Texture2D Texture;
        public required ID3D11RenderTargetView Rtv;
        public required IDirect3DSurface Surface;
        public bool DangDung;
    }

    readonly ID3D11Device device;
    readonly ID3D11DeviceContext ctx;
    readonly ID3D11VertexShader vs;
    readonly ID3D11PixelShader ps, psThu;
    readonly ID3D11SamplerState sampler;
    readonly ID3D11Buffer cb;
    readonly List<Khung> be = new();
    readonly ID3D11Texture2D thuNho, thuNhoDoc;   // bản thu nhỏ để dò nội dung HDR
    readonly ID3D11RenderTargetView thuNhoRtv;
    ThamSo ts;
    int demKhung;
    const int ThuW = 96, ThuH = 54, BeToiDa = 6;

    public int Rong { get; }
    public int Cao { get; }
    public bool CoNoiDungHdr { get; private set; }

    /// kichNguon: kích thước khung bắt được; vung: phần cần lấy (toạ độ trong khung); heSo = 80 / mức trắng SDR (nits).
    public ToneMapGpu(ID3D11Device device, ID3D11DeviceContext ctx, Size kichNguon, Rectangle vung, bool hdr, float heSo)
    {
        this.device = device; this.ctx = ctx;
        Rong = Math.Max(2, vung.Width & ~1); Cao = Math.Max(2, vung.Height & ~1);   // H.264 cần kích thước chẵn
        ts = new ThamSo
        {
            gocX = (float)vung.X / kichNguon.Width, gocY = (float)vung.Y / kichNguon.Height,
            kichX = (float)Rong / kichNguon.Width, kichY = (float)Cao / kichNguon.Height,
            heSo = heSo, goi = 0.80f, laHdr = hdr ? 1 : 0, nenHdr = 0,
        };
        vs = device.CreateVertexShader(BienDich("VS", "vs_5_0"));
        ps = device.CreatePixelShader(BienDich("PS", "ps_5_0"));
        psThu = device.CreatePixelShader(BienDich("PSThu", "ps_5_0"));
        sampler = device.CreateSamplerState(SamplerDescription.LinearClamp);
        cb = device.CreateBuffer(new BufferDescription(32, BindFlags.ConstantBuffer, ResourceUsage.Dynamic, CpuAccessFlags.Write));
        thuNho = device.CreateTexture2D(new Texture2DDescription
        {
            Width = ThuW, Height = ThuH, MipLevels = 1, ArraySize = 1, Format = Format.R16G16B16A16_Float,
            SampleDescription = new SampleDescription(1, 0), Usage = ResourceUsage.Default, BindFlags = BindFlags.RenderTarget,
        });
        thuNhoRtv = device.CreateRenderTargetView(thuNho);
        thuNhoDoc = device.CreateTexture2D(new Texture2DDescription
        {
            Width = ThuW, Height = ThuH, MipLevels = 1, ArraySize = 1, Format = Format.R16G16B16A16_Float,
            SampleDescription = new SampleDescription(1, 0), Usage = ResourceUsage.Staging, CPUAccessFlags = CpuAccessFlags.Read,
        });
    }

    static byte[] BienDich(string ham, string profile)
    {
        var r = Compiler.Compile(HLSL, ham, "tonemap.hlsl", profile, out Blob ma, out Blob loi);
        if (r.Failure || ma == null) throw new InvalidOperationException("Biên dịch shader lỗi: " + (loi?.AsString() ?? r.ToString()));
        return ma.AsBytes();
    }

    /// Lấy một khung BGRA8 rảnh trong bể (null nếu bể đầy → rớt khung).
    public Khung? Lay()
    {
        lock (be)
        {
            var k = be.FirstOrDefault(x => !x.DangDung);
            if (k == null)
            {
                if (be.Count >= BeToiDa) return null;
                var tex = device.CreateTexture2D(new Texture2DDescription
                {
                    Width = (uint)Rong, Height = (uint)Cao, MipLevels = 1, ArraySize = 1, Format = Format.B8G8R8A8_UNorm,
                    SampleDescription = new SampleDescription(1, 0), Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                });
                k = new Khung { Texture = tex, Rtv = device.CreateRenderTargetView(tex), Surface = D3DInterop.TaoWinRtSurface(tex) };
                be.Add(k);
            }
            k.DangDung = true;
            return k;
        }
    }

    public void Tra(Khung k) { lock (be) k.DangDung = false; }

    /// Vẽ khung nguồn (texture của Graphics.Capture, cùng device) vào khung đích.
    public void Ve(ID3D11Texture2D nguon, Khung dich)
    {
        using var srv = device.CreateShaderResourceView(nguon);
        // Cứ 15 khung dò một lần xem có nội dung sáng hơn trắng SDR không (chỉ màn HDR)
        if (ts.laHdr > 0.5f && demKhung++ % 15 == 0) DoHdr(srv);
        GhiThamSo();
        ctx.OMSetRenderTargets(dich.Rtv);
        ctx.RSSetViewport(0, 0, Rong, Cao);
        VeTamGiac(srv, ps);
    }

    void DoHdr(ID3D11ShaderResourceView srv)
    {
        ctx.OMSetRenderTargets(thuNhoRtv);
        ctx.RSSetViewport(0, 0, ThuW, ThuH);
        GhiThamSo();
        VeTamGiac(srv, psThu);
        ctx.CopyResource(thuNhoDoc, thuNho);
        var m = ctx.Map(thuNhoDoc, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None);
        try
        {
            int vuot = 0;
            unsafe
            {
                for (int y = 0; y < ThuH; y++)
                {
                    Half* p = (Half*)((byte*)m.DataPointer + y * (int)m.RowPitch);
                    for (int x = 0; x < ThuW; x++)
                    {
                        float Y = 0.2126f * (float)p[x * 4] + 0.7152f * (float)p[x * 4 + 1] + 0.0722f * (float)p[x * 4 + 2];
                        if (Y > 1.02f) vuot++;
                    }
                }
            }
            CoNoiDungHdr = vuot > ThuW * ThuH / 1000;
            ts.nenHdr = CoNoiDungHdr ? 1 : 0;
        }
        finally { ctx.Unmap(thuNhoDoc, 0); }
    }

    void GhiThamSo()
    {
        var m = ctx.Map(cb, 0, MapMode.WriteDiscard, Vortice.Direct3D11.MapFlags.None);
        try { Marshal.StructureToPtr(ts, m.DataPointer, false); } finally { ctx.Unmap(cb, 0); }
    }

    void VeTamGiac(ID3D11ShaderResourceView srv, ID3D11PixelShader shader)
    {
        ctx.IASetInputLayout(null);
        ctx.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        ctx.VSSetShader(vs);
        ctx.VSSetConstantBuffer(0, cb);
        ctx.PSSetShader(shader);
        ctx.PSSetConstantBuffer(0, cb);
        ctx.PSSetShaderResource(0, srv);
        ctx.PSSetSampler(0, sampler);
        ctx.Draw(3, 0);
        ctx.PSSetShaderResource(0, null);
    }

    public void Dispose()
    {
        lock (be) { foreach (var k in be) { k.Rtv.Dispose(); k.Texture.Dispose(); } be.Clear(); }
        thuNhoRtv.Dispose(); thuNho.Dispose(); thuNhoDoc.Dispose();
        cb.Dispose(); sampler.Dispose(); ps.Dispose(); psThu.Dispose(); vs.Dispose();
    }
}
