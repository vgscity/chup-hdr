using System.Diagnostics;
using NAudio.Wave;
using Windows.Media.MediaProperties;

namespace VgsChupHdr.Quay;

/// Tiếng hệ thống qua WASAPI loopback (NAudio). Float32 của thiết bị → PCM16, giữ nguyên sample rate / số kênh.
/// Mốc thời gian lấy từ đồng hồ chung của bộ quay (lúc nhận trừ độ dài khối) để khớp với hình.
sealed class AmThanh : IDisposable
{
    readonly WasapiLoopbackCapture bat;
    readonly Stopwatch dongHo;
    readonly int rate, kenh;
    public AudioEncodingProperties DinhDang { get; }
    public event Action<byte[], TimeSpan>? DuLieu;

    public AmThanh(Stopwatch dongHo)
    {
        this.dongHo = dongHo;
        bat = new WasapiLoopbackCapture();
        var wf = bat.WaveFormat;
        rate = wf.SampleRate; kenh = wf.Channels;
        DinhDang = AudioEncodingProperties.CreatePcm((uint)rate, (uint)kenh, 16);
        bat.DataAvailable += KhiCoDuLieu;
        NhatKy.Ghi($"Âm thanh loopback: {rate} Hz, {kenh} kênh, {wf.Encoding} {wf.BitsPerSample} bit");
    }

    void KhiCoDuLieu(object? s, WaveInEventArgs e)
    {
        if (e.BytesRecorded == 0) return;
        var wf = bat.WaveFormat;
        byte[] pcm;
        if (wf.Encoding == WaveFormatEncoding.IeeeFloat && wf.BitsPerSample == 32)
        {
            int n = e.BytesRecorded / 4;
            pcm = new byte[n * 2];
            for (int i = 0; i < n; i++)
            {
                float f = BitConverter.ToSingle(e.Buffer, i * 4);
                short v = (short)Math.Clamp(f * 32767f, -32768f, 32767f);
                pcm[i * 2] = (byte)v; pcm[i * 2 + 1] = (byte)(v >> 8);
            }
        }
        else if (wf.BitsPerSample == 16) { pcm = new byte[e.BytesRecorded]; Buffer.BlockCopy(e.Buffer, 0, pcm, 0, e.BytesRecorded); }
        else return;
        var dai = TimeSpan.FromSeconds(pcm.Length / 2.0 / kenh / rate);
        var luc = dongHo.Elapsed - dai;
        DuLieu?.Invoke(pcm, luc < TimeSpan.Zero ? TimeSpan.Zero : luc);
    }

    public void BatDau() => bat.StartRecording();
    public void Dung() { try { bat.StopRecording(); } catch { } }
    public void Dispose() { Dung(); bat.Dispose(); }
}
