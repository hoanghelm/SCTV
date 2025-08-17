using Microsoft.Extensions.Logging;
using SIPSorcery.Net;
using SIPSorcery.SIP.App;
using System.Diagnostics;
using System.Net;

namespace Streaming.Service.Sources
{
    public class RtpFFmpegVideoSource : IDisposable
    {
        private readonly string _videoFilePath;
        private readonly int _rtpPort;
        private readonly string _sdpFilePath;
        private readonly ILogger _logger;
        
        private Process? _ffmpegProcess;
        private RTPSession? _rtpSession;
        private SDPAudioVideoMediaFormat? _videoFormat;
        private bool _isRunning;
        private bool _isDisposed;
        
        public event Action<IPEndPoint, SDPMediaTypesEnum, RTPPacket>? OnRtpPacketReceived;

        public RtpFFmpegVideoSource(string videoFilePath, int rtpPort, ILogger logger)
        {
            _videoFilePath = videoFilePath;
            _rtpPort = rtpPort;
            _sdpFilePath = Path.Combine(Path.GetTempPath(), $"stream_{rtpPort}.sdp");
            _logger = logger;
        }

        public async Task<SDPAudioVideoMediaFormat> StartAsync()
        {
            if (_isRunning && _videoFormat.HasValue)
                return _videoFormat.Value;
                
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(RtpFFmpegVideoSource));

            try
            {
                if (File.Exists(_sdpFilePath))
                {
                    File.Delete(_sdpFilePath);
                }
                await StartFFmpegProcess();

                await WaitForSdpFile();

                await SetupRtpSession();

                _isRunning = true;
                _logger.LogInformation($"RTP FFmpeg video source started on port {_rtpPort}");
                
                return _videoFormat.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to start RTP FFmpeg video source: {ex.Message}");
                Stop();
                throw;
            }
        }

        private async Task StartFFmpegProcess()
        {
            var ffmpegArgs = BuildFFmpegCommand();
            
            _logger.LogInformation($"Starting FFmpeg with command: ffmpeg {ffmpegArgs}");

            _ffmpegProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = ffmpegArgs,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            _ffmpegProcess.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    if (e.Data.Contains("error") || e.Data.Contains("failed") || e.Data.Contains("unable"))
                    {
                        _logger.LogWarning($"FFmpeg: {e.Data}");
                    }
                    else if (e.Data.Contains("Input #") || e.Data.Contains("Stream #") || e.Data.Contains("Output #"))
                    {
                        _logger.LogInformation($"FFmpeg: {e.Data}");
                    }
                    else
                    {
                        _logger.LogDebug($"FFmpeg: {e.Data}");
                    }
                }
            };

            _ffmpegProcess.Start();
            _ffmpegProcess.BeginErrorReadLine();

            _logger.LogInformation($"FFmpeg process started with PID: {_ffmpegProcess.Id}");
            
            await Task.Delay(2000);
        }

        private string BuildFFmpegCommand()
        {
            uint ssrc = (uint)(38106908 + _rtpPort);

			return $"-fflags +genpts -re -stream_loop -1 -i \"{_videoFilePath}\" " +
				   $"-map 0:v:0 " +
				   $"-vf scale=1280:720:force_original_aspect_ratio=decrease,pad=1280:720:(ow-iw)/2:(oh-ih)/2:black " +
				   $"-c:v libx264 -preset ultrafast -tune zerolatency " +
				   $"-profile:v baseline -level 3.1 " +
				   $"-pix_fmt yuv420p " +
				   $"-r 30 " +
				   $"-g 30 " +
				   $"-b:v 2000k " +
				   $"-maxrate 2000k " +
				   $"-bufsize 4000k " +
				   $"-avoid_negative_ts make_zero " +
				   $"-fflags +discardcorrupt " +
				   $"-an " +
				   $"-ssrc {ssrc} " +
				   $"-f rtp rtp://127.0.0.1:{_rtpPort} " +
				   $"-sdp_file \"{_sdpFilePath}\"";
		}

        private async Task WaitForSdpFile()
        {
            var timeout = TimeSpan.FromSeconds(15);
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation($"Waiting for SDP file: {_sdpFilePath}");

            while (!File.Exists(_sdpFilePath) && stopwatch.Elapsed < timeout)
            {
                await Task.Delay(500);
                
                if (stopwatch.Elapsed.TotalSeconds % 3 < 0.5)
                {
                    _logger.LogInformation($"Still waiting for SDP file... ({stopwatch.Elapsed.TotalSeconds:F0}s elapsed)");
                }
            }

            if (!File.Exists(_sdpFilePath))
            {
                if (_ffmpegProcess != null && !_ffmpegProcess.HasExited)
                {
                    _logger.LogError($"FFmpeg process is still running but SDP file not created. Process ID: {_ffmpegProcess.Id}");
                }
                else if (_ffmpegProcess != null)
                {
                    _logger.LogError($"FFmpeg process exited with code: {_ffmpegProcess.ExitCode}");
                }
                
                throw new FileNotFoundException($"SDP file was not created within {timeout.TotalSeconds} seconds: {_sdpFilePath}");
            }

            _logger.LogInformation($"SDP file created successfully after {stopwatch.Elapsed.TotalSeconds:F1} seconds");
        }

        private async Task SetupRtpSession()
        {
            var sdpContent = await File.ReadAllTextAsync(_sdpFilePath);
            _logger.LogDebug($"SDP Content:\n{sdpContent}");

            var sdp = SDP.ParseSDPDescription(sdpContent);
            
            var videoAnn = sdp.Media.FirstOrDefault(x => x.Media == SDPMediaTypesEnum.video);
            if (videoAnn == null)
            {
                throw new InvalidOperationException("No video media found in SDP");
            }

            _videoFormat = videoAnn.MediaFormats.Values.First();
            _logger.LogInformation($"Video format: {_videoFormat}");

            _rtpSession = new RTPSession(false, false, false, IPAddress.Loopback, _rtpPort);
            _rtpSession.AcceptRtpFromAny = true;

            var videoFormats = new List<SDPAudioVideoMediaFormat> { _videoFormat.Value };
            var videoTrack = new MediaStreamTrack(SDPMediaTypesEnum.video, false, 
                videoFormats, MediaStreamStatusEnum.RecvOnly);
            
            if (videoAnn.SsrcAttributes?.Count > 0)
            {
                videoTrack.Ssrc = videoAnn.SsrcAttributes.First().SSRC;
                _logger.LogInformation($"Using SSRC: {videoTrack.Ssrc}");
            }

            _rtpSession.addTrack(videoTrack);
            _rtpSession.SetRemoteDescription(SdpType.answer, sdp);

            var dummyEndPoint = new IPEndPoint(IPAddress.Loopback, 0);
            _rtpSession.SetDestination(SDPMediaTypesEnum.video, dummyEndPoint, dummyEndPoint);

            _rtpSession.OnRtpPacketReceived += (ep, media, rtpPkt) =>
            {
                OnRtpPacketReceived?.Invoke(ep, media, rtpPkt);
            };

            await _rtpSession.Start();
            _logger.LogInformation("RTP session started successfully");
        }

        public void Stop()
        {
            if (!_isRunning)
                return;

            _isRunning = false;

            try
            {
                _rtpSession?.Close("Shutting down");
                _rtpSession = null;

                if (_ffmpegProcess != null && !_ffmpegProcess.HasExited)
                {
                    _ffmpegProcess.Kill();
                    _ffmpegProcess.WaitForExit(3000);
                }

                _ffmpegProcess?.Dispose();
                _ffmpegProcess = null;

                if (File.Exists(_sdpFilePath))
                {
                    File.Delete(_sdpFilePath);
                }

                _logger.LogInformation("RTP FFmpeg video source stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error stopping RTP FFmpeg video source: {ex.Message}");
            }
        }

        public SDPAudioVideoMediaFormat? GetVideoFormat()
        {
            return _isRunning && _videoFormat.HasValue ? _videoFormat : null;
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            Stop();
        }
    }
}