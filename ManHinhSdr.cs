using System.Runtime.InteropServices;

namespace VgsChupHdr;

/// Đọc mức trắng SDR (thanh "SDR content brightness") của từng màn hình qua DisplayConfig API.
/// Trả về hệ số: 1.0 = 80 nits (giá trị scRGB 1.0). Ví dụ thanh để 200 nits → 2.5.
static class ManHinhSdr
{
    const uint QDC_ONLY_ACTIVE_PATHS = 2;
    const uint DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME = 1;
    const uint DISPLAYCONFIG_DEVICE_INFO_GET_SDR_WHITE_LEVEL = 11;

    [StructLayout(LayoutKind.Sequential)]
    struct LUID { public uint LowPart; public int HighPart; }

    [StructLayout(LayoutKind.Sequential)]
    struct PATH_SOURCE_INFO { public LUID adapterId; public uint id; public uint modeInfoIdx; public uint statusFlags; }

    [StructLayout(LayoutKind.Sequential)]
    struct PATH_TARGET_INFO
    {
        public LUID adapterId; public uint id; public uint modeInfoIdx;
        public uint outputTechnology; public uint rotation; public uint scaling;
        public uint refreshRateNumerator; public uint refreshRateDenominator;
        public uint scanLineOrdering; public int targetAvailable; public uint statusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct PATH_INFO { public PATH_SOURCE_INFO sourceInfo; public PATH_TARGET_INFO targetInfo; public uint flags; }

    [StructLayout(LayoutKind.Sequential, Size = 64)]
    struct MODE_INFO { public uint infoType; public uint id; public LUID adapterId; }

    [StructLayout(LayoutKind.Sequential)]
    struct DEVICE_INFO_HEADER { public uint type; public uint size; public LUID adapterId; public uint id; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct SOURCE_DEVICE_NAME
    {
        public DEVICE_INFO_HEADER header;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string viewGdiDeviceName;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct SDR_WHITE_LEVEL { public DEVICE_INFO_HEADER header; public uint SDRWhiteLevel; }

    [DllImport("user32.dll")] static extern int GetDisplayConfigBufferSizes(uint flags, out uint numPaths, out uint numModes);
    [DllImport("user32.dll")] static extern int QueryDisplayConfig(uint flags, ref uint numPaths, [Out] PATH_INFO[] paths, ref uint numModes, [Out] MODE_INFO[] modes, IntPtr currentTopology);
    [DllImport("user32.dll")] static extern int DisplayConfigGetDeviceInfo(ref SOURCE_DEVICE_NAME info);
    [DllImport("user32.dll")] static extern int DisplayConfigGetDeviceInfo(ref SDR_WHITE_LEVEL info);

    /// Khoá: tên GDI của màn (\\.\DISPLAY1) → hệ số trắng SDR
    public static Dictionary<string, float> DocTatCa()
    {
        var kq = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        try
        {
            if (GetDisplayConfigBufferSizes(QDC_ONLY_ACTIVE_PATHS, out uint soPath, out uint soMode) != 0) return kq;
            var paths = new PATH_INFO[soPath];
            var modes = new MODE_INFO[soMode];
            if (QueryDisplayConfig(QDC_ONLY_ACTIVE_PATHS, ref soPath, paths, ref soMode, modes, IntPtr.Zero) != 0) return kq;

            for (int i = 0; i < soPath; i++)
            {
                var p = paths[i];
                var ten = new SOURCE_DEVICE_NAME
                {
                    header = new DEVICE_INFO_HEADER
                    {
                        type = DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME,
                        size = (uint)Marshal.SizeOf<SOURCE_DEVICE_NAME>(),
                        adapterId = p.sourceInfo.adapterId,
                        id = p.sourceInfo.id,
                    }
                };
                if (DisplayConfigGetDeviceInfo(ref ten) != 0) continue;

                var trang = new SDR_WHITE_LEVEL
                {
                    header = new DEVICE_INFO_HEADER
                    {
                        type = DISPLAYCONFIG_DEVICE_INFO_GET_SDR_WHITE_LEVEL,
                        size = (uint)Marshal.SizeOf<SDR_WHITE_LEVEL>(),
                        adapterId = p.targetInfo.adapterId,
                        id = p.targetInfo.id,
                    }
                };
                float heSo = 1f;
                if (DisplayConfigGetDeviceInfo(ref trang) == 0 && trang.SDRWhiteLevel > 0)
                    heSo = trang.SDRWhiteLevel / 1000f; // 1000 = 80 nits
                kq[ten.viewGdiDeviceName] = heSo;
            }
        }
        catch (Exception e) { NhatKy.Ghi("Đọc mức trắng SDR lỗi", e); }
        return kq;
    }
}
