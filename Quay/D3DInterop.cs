using System.Runtime.InteropServices;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using WinRT;

namespace VgsChupHdr.Quay;

/// Cầu nối giữa Direct3D 11 (Vortice) và các API WinRT (Graphics.Capture, MediaStreamSample).
static class D3DInterop
{
    [DllImport("d3d11.dll")] static extern int CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);
    [DllImport("d3d11.dll")] static extern int CreateDirect3D11SurfaceFromDXGISurface(IntPtr dxgiSurface, out IntPtr graphicsSurface);
    [DllImport("user32.dll")] public static extern IntPtr MonitorFromPoint(System.Drawing.Point pt, uint flags);
    [DllImport("user32.dll")] public static extern IntPtr MonitorFromRect(ref Win32.RECT rc, uint flags);
    public const uint MONITOR_DEFAULTTONEAREST = 2;

    [ComImport, Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IDirect3DDxgiInterfaceAccess { IntPtr GetInterface([In] ref Guid iid); }

    [ComImport, Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IGraphicsCaptureItemInterop
    {
        IntPtr CreateForWindow(IntPtr window, [In] ref Guid iid);
        IntPtr CreateForMonitor(IntPtr monitor, [In] ref Guid iid);
    }
    static readonly Guid IID_GraphicsCaptureItem = new("79C3F95B-31F7-4EC2-A464-632EF5D30760");

    public static IDirect3DDevice TaoWinRtDevice(ID3D11Device device)
    {
        using var dxgi = device.QueryInterface<IDXGIDevice>();
        Marshal.ThrowExceptionForHR(CreateDirect3D11DeviceFromDXGIDevice(dxgi.NativePointer, out var p));
        try { return MarshalInterface<IDirect3DDevice>.FromAbi(p); } finally { Marshal.Release(p); }
    }

    public static IDirect3DSurface TaoWinRtSurface(ID3D11Texture2D texture)
    {
        using var dxgi = texture.QueryInterface<IDXGISurface>();
        Marshal.ThrowExceptionForHR(CreateDirect3D11SurfaceFromDXGISurface(dxgi.NativePointer, out var p));
        try { return MarshalInterface<IDirect3DSurface>.FromAbi(p); } finally { Marshal.Release(p); }
    }

    /// Texture D3D11 nằm sau một IDirect3DSurface của khung bắt được (người gọi Dispose).
    public static ID3D11Texture2D LayTexture(IDirect3DSurface surface)
    {
        var access = surface.As<IDirect3DDxgiInterfaceAccess>();
        var iid = typeof(ID3D11Texture2D).GUID;
        return new ID3D11Texture2D(access.GetInterface(ref iid));
    }

    public static GraphicsCaptureItem ItemManHinh(IntPtr hmon)
    {
        var interop = GraphicsCaptureItem.As<IGraphicsCaptureItemInterop>();
        var iid = IID_GraphicsCaptureItem;
        var p = interop.CreateForMonitor(hmon, ref iid);
        try { return GraphicsCaptureItem.FromAbi(p); } finally { Marshal.Release(p); }
    }

    public static GraphicsCaptureItem ItemCuaSo(IntPtr hwnd)
    {
        var interop = GraphicsCaptureItem.As<IGraphicsCaptureItemInterop>();
        var iid = IID_GraphicsCaptureItem;
        var p = interop.CreateForWindow(hwnd, ref iid);
        try { return GraphicsCaptureItem.FromAbi(p); } finally { Marshal.Release(p); }
    }
}
