using System.Collections.Concurrent;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;
using Windows.Storage;
using Windows.Storage.Streams;

namespace VgsChupHdr.Quay;

/// Ghi MP4 (H.264 + AAC) bằng MediaStreamSource + MediaTranscoder (encoder phần cứng nếu có).
/// Khung hình vào bằng IDirect3DSurface (không copy về CPU); âm thanh là PCM16.
sealed class MaHoaVideo
{
    readonly record struct KhungVideo(IDirect3DSurface Surface, TimeSpan Luc, Action Tra);
    readonly record struct KhungAm(byte[] Pcm, TimeSpan Luc, TimeSpan Dai);

    readonly MediaStreamSource mss;
    readonly VideoStreamDescriptor moTaVideo;
    readonly AudioStreamDescriptor? moTaAm;
    readonly MediaEncodingProfile hoSo;
    readonly BlockingCollection<KhungVideo> video = new(new ConcurrentQueue<KhungVideo>(), 12);
    readonly BlockingCollection<KhungAm> am = new(new ConcurrentQueue<KhungAm>());
    readonly uint amRate, amKenh;
    readonly Func<TimeSpan> bayGio;   // đồng hồ chung của bộ quay
    TimeSpan amCuoi;
    public int KhungRot { get; private set; }
    public string? LoiMss { get; private set; }

    public MaHoaVideo(int rong, int cao, int fps, uint bitrate, AudioEncodingProperties? pcm, Func<TimeSpan> bayGio)
    {
        this.bayGio = bayGio;
        var vp = VideoEncodingProperties.CreateUncompressed(MediaEncodingSubtypes.Bgra8, (uint)rong, (uint)cao);
        vp.FrameRate.Numerator = (uint)fps; vp.FrameRate.Denominator = 1;
        moTaVideo = new VideoStreamDescriptor(vp);
        if (pcm != null)
        {
            moTaAm = new AudioStreamDescriptor(pcm);
            amRate = pcm.SampleRate; amKenh = pcm.ChannelCount;
            mss = new MediaStreamSource(moTaVideo, moTaAm);
        }
        else mss = new MediaStreamSource(moTaVideo);
        mss.BufferTime = TimeSpan.Zero;
        mss.CanSeek = false;
        mss.Starting += (_, e) => e.Request.SetActualStartPosition(TimeSpan.Zero);
        mss.SampleRequested += KhiXinMau;
        mss.Closed += (_, e) => { if (e.Request.Reason == MediaStreamSourceClosedReason.UnsupportedEncodingFormat || e.Request.Reason == MediaStreamSourceClosedReason.UnknownError) LoiMss = e.Request.Reason.ToString(); };

        hoSo = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.HD1080p);
        hoSo.Video.Width = (uint)rong; hoSo.Video.Height = (uint)cao;
        hoSo.Video.FrameRate.Numerator = (uint)fps; hoSo.Video.FrameRate.Denominator = 1;
        hoSo.Video.Bitrate = bitrate;
        if (pcm == null) hoSo.Audio = null;
        else { hoSo.Audio.SampleRate = pcm.SampleRate; hoSo.Audio.ChannelCount = pcm.ChannelCount; hoSo.Audio.Bitrate = 192_000; }
    }

    /// Chạy tới khi KetThuc() và hàng đợi cạn. Gọi một lần, giữ Task để chờ khi dừng.
    public async Task ChayAsync(string duongDan)
    {
        using var luong = await FileRandomAccessStream.OpenAsync(duongDan, FileAccessMode.ReadWrite, StorageOpenOptions.None, FileOpenDisposition.CreateAlways);
        var bo = new MediaTranscoder { HardwareAccelerationEnabled = true };
        var chuanBi = await bo.PrepareMediaStreamSourceTranscodeAsync(mss, luong, hoSo);
        if (!chuanBi.CanTranscode) throw new InvalidOperationException("Không mã hoá được: " + chuanBi.FailureReason);
        await chuanBi.TranscodeAsync();
        await luong.FlushAsync();
    }

    public void ThemKhung(IDirect3DSurface surface, TimeSpan luc, Action tra)
    {
        if (video.IsAddingCompleted) { tra(); return; }
        if (!video.TryAdd(new KhungVideo(surface, luc, tra))) { KhungRot++; tra(); }
    }

    public void ThemAmThanh(byte[] pcm, TimeSpan luc)
    {
        if (am.IsAddingCompleted || amRate == 0) return;
        var dai = TimeSpan.FromSeconds(pcm.Length / 2.0 / amKenh / amRate);
        am.TryAdd(new KhungAm(pcm, luc, dai));
    }

    public void KetThuc() { video.CompleteAdding(); am.CompleteAdding(); }

    void KhiXinMau(MediaStreamSource sender, MediaStreamSourceSampleRequestedEventArgs e)
    {
        var yeuCau = e.Request;
        var hoan = yeuCau.GetDeferral();
        Task.Run(() =>
        {
            try
            {
                if (yeuCau.StreamDescriptor == moTaVideo) yeuCau.Sample = LayVideo();
                else yeuCau.Sample = LayAm();
            }
            catch (Exception ex) { NhatKy.Ghi("SampleRequested lỗi", ex); yeuCau.Sample = null; }
            finally { hoan.Complete(); }
        });
    }

    MediaStreamSample? LayVideo()
    {
        while (true)
        {
            if (video.TryTake(out var k, 500))
            {
                var mau = MediaStreamSample.CreateFromDirect3D11Surface(k.Surface, k.Luc);
                var tra = k.Tra;
                mau.Processed += (_, _) => tra();
                return mau;
            }
            if (video.IsCompleted) return null;   // hết: kết thúc luồng video
        }
    }

    MediaStreamSample? LayAm()
    {
        while (true)
        {
            if (am.TryTake(out var k, 10))
            {
                // Mốc tăng dần: khối thật tới ngay sau đoạn im lặng vừa chèn có thể lệch lùi vài ms
                var luc = k.Luc < amCuoi ? amCuoi : k.Luc;
                var mau = MediaStreamSample.CreateFromBuffer(k.Pcm.AsBuffer(), luc);
                mau.Duration = k.Dai;
                amCuoi = luc + k.Dai;
                return mau;
            }
            if (am.IsCompleted) return null;
            // WASAPI loopback im lặng là không có dữ liệu → tự chèn im lặng để dòng thời gian không đứt.
            // Chèn ĐỦ tới đồng hồ thật: trước đây cứ chờ 40 ms mới chèn 20 ms nên dòng âm chạy nửa tốc độ, encoder xếp theo
            // mốc thời gian nên kéo luôn video xuống ~36–42 fps (thử Windows 27/10/2026, 4K: bỏ tiếng thì đủ 59,5 fps).
            var thieu = bayGio() - amCuoi;
            if (thieu < TimeSpan.FromMilliseconds(10)) continue;   // âm đã đuổi kịp đồng hồ: chờ dữ liệu thật
            var dai = thieu > TimeSpan.FromMilliseconds(100) ? TimeSpan.FromMilliseconds(100) : thieu;
            int soMau = (int)(amRate * dai.TotalSeconds);
            var im = new byte[soMau * amKenh * 2];
            dai = TimeSpan.FromSeconds((double)soMau / amRate);
            var mauIm = MediaStreamSample.CreateFromBuffer(im.AsBuffer(), amCuoi);
            mauIm.Duration = dai;
            amCuoi += dai;
            return mauIm;
        }
    }
}
