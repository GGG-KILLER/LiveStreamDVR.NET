using System.Diagnostics;
using System.Threading.Channels;
using LiveStreamDVR.Api.Configuration;
using LiveStreamDVR.Api.Helpers;
using LiveStreamDVR.Api.Models;
using Microsoft.Extensions.Options;

namespace LiveStreamDVR.Api.Services;

public sealed class TwitchStreamCapturer(
    IServiceProvider serviceProvider,
    Channel<TwitchStream> streams,
    IOptionsMonitor<BinariesOptions> binariesOptionsMonitor,
    IOptionsMonitor<CaptureOptions> captureOptionsMonitor) : BackgroundService
{
    private readonly Channel<TwitchStream> _streams = streams ?? throw new ArgumentNullException(nameof(streams));
    private readonly List<Task> _capturesInProgress = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (stoppingToken.IsCancellationRequested)
        {
            var stream = await _streams.Reader.ReadAsync(stoppingToken);
            _capturesInProgress.Add(Capture(stream, stoppingToken));
        }
    }

    private async Task Capture(TwitchStream stream, CancellationToken cancellationToken = default)
    {
        var binariesOptions = binariesOptionsMonitor.CurrentValue;
        var captureOptions = captureOptionsMonitor.CurrentValue;

        await using var scope = serviceProvider.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TwitchStreamCapturer>>();
        using var _1 = logger.BeginScope("Capture of Stream: {Stream}", stream);

        try
        {
            Directory.CreateDirectory("logs");

            var outputDir = Path.Combine(captureOptions.OutputDirectory!, PathEx.SanitizeFileName(stream.UserName));
            Directory.CreateDirectory(outputDir);
            var outputFileTs = Path.Combine(outputDir, PathEx.SanitizeFileName($"{DateTime.Now:yyyy-MM-dd' 'HH:mm:ss} {stream.UserName} - {stream.Title} [{stream.Id}].ts"));
            var outputFileMp4 = Path.Combine(outputDir, PathEx.SanitizeFileName($"{DateTime.Now:yyyy-MM-dd' 'HH:mm:ss} {stream.UserName} - {stream.Title} [{stream.Id}].mp4"));

            {
                logger.LogInformation("Capturing stream {Stream}", stream);
                var streamlink = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = binariesOptions.StreamLinkPath,
                        ArgumentList =
                        {
                            "--hls-live-edge", "99999",
                            "--stream-timeout", "200",
                            "--stream-segment-timeout", "200",
                            "--stream-segment-threads", "5",
                            "--ffmpeg-fout", "mpegts",
                            "--twitch-disable-hosting",
                            "--twitch-disable-ads",
                            "--twitch-disable-reruns",
                            "--retry-streams", "10",
                            "--retry-max", "5",
                            "-o", outputFileTs,
                            "--url", $"https://twitch.tv/{stream.Login}",
                            "--default-stream", "1080p60,best"
                        }
                    }
                };
                if (captureOptions.ExtraStreamlinkFlags is not null && captureOptions.ExtraStreamlinkFlags.Length != 0)
                {
                    foreach (var flag in captureOptions.ExtraStreamlinkFlags)
                        streamlink.StartInfo.ArgumentList.Add(flag);
                }
                string streamlinkCommandLine = $"$ {streamlink.StartInfo.FileName} \"{string.Join("\", \"", streamlink.StartInfo.ArgumentList)}\"";
                logger.LogInformation("Using command line {CommandLine}", streamlinkCommandLine);

                using var streamlinkStdout = new StreamWriter(Path.Combine(outputDir, $"capture_{stream.Login}_{stream.Id}.stdout.log"));
                using var streamlinkStderr = new StreamWriter(Path.Combine(outputDir, $"capture_{stream.Login}_{stream.Id}.stderr.log"));
                streamlinkStdout.WriteLine(streamlinkCommandLine);
                streamlinkStderr.WriteLine(streamlinkCommandLine);

                var streamlinkEx = new ProcessEx(streamlink);
                streamlinkEx.OnStandardOutputLine += (_, line) => streamlinkStdout.WriteLine(line);
                streamlinkEx.OnStandardErrorLine += (_, line) => streamlinkStderr.WriteLine(line);
                await streamlinkEx.StartAndWaitAsync(cancellationToken);
                if (streamlink.ExitCode != 1)
                {
                    logger.LogError("Failed to capture stream {Stream}. Exit code was {ExitCode}.", stream, streamlink.ExitCode);
                    return;
                }
            }

            {
                logger.LogInformation("Remuxing stream {Stream}", stream);
                var ffmpeg = new Process()
                {
                    StartInfo =
                    {
                        FileName = binariesOptions.FfmpegPath,
                        ArgumentList =
                        {
                            "-i", outputFileTs,
                            "-c", "copy",
                            "-bsf:a", "aac_adtstoasc",
                            "-movflags", "faststart",
                            outputFileMp4
                        }
                    }
                };
                if (captureOptions.ExtraFfmpegFlags is not null && captureOptions.ExtraFfmpegFlags.Length != 0)
                {
                    foreach (var flag in captureOptions.ExtraFfmpegFlags)
                        ffmpeg.StartInfo.ArgumentList.Add(flag);
                }
                string ffmpegCommandLine = $"$ {ffmpeg.StartInfo.FileName} \"{string.Join("\", \"", ffmpeg.StartInfo.ArgumentList)}\"";
                logger.LogInformation("Using command line {CommandLine}", ffmpegCommandLine);

                using var ffmpegStdout = new StreamWriter(Path.Combine(outputDir, $"remux_{stream.Login}_{stream.Id}.stdout.log"));
                using var ffmpegStderr = new StreamWriter(Path.Combine(outputDir, $"remux_{stream.Login}_{stream.Id}.stderr.log"));
                ffmpegStdout.WriteLine($"$ {ffmpeg.StartInfo.FileName} \"{string.Join("\", \"", ffmpeg.StartInfo.ArgumentList)}\"");
                ffmpegStderr.WriteLine($"$ {ffmpeg.StartInfo.FileName} \"{string.Join("\", \"", ffmpeg.StartInfo.ArgumentList)}\"");

                var ffmpegEx = new ProcessEx(ffmpeg);
                ffmpegEx.OnStandardOutputLine += (_, line) => ffmpegStdout.WriteLine(line);
                ffmpegEx.OnStandardErrorLine += (_, line) => ffmpegStderr.WriteLine(line);
                await ffmpegEx.StartAndWaitAsync(cancellationToken);

                if (ffmpeg.ExitCode != 0)
                {
                    logger.LogError("Failed to remux {Stream}. Exit code was {ExitCode}.", stream, ffmpeg.ExitCode);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error capturing stream {Stream}", stream);
        }
    }
}
