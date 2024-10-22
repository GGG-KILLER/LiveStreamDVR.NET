using System.Diagnostics;
using System.Text;
using LiveStreamDVR.Api.Configuration;
using LiveStreamDVR.Api.Helpers;
using LiveStreamDVR.Api.Models;
using Microsoft.Extensions.Options;

namespace LiveStreamDVR.Api.Services.Capture;

public sealed class TwitchStreamCapturer(
    ILogger<TwitchStreamCapturer> logger,
    ICaptureManager captureManager,
    IOptionsMonitor<BinariesOptions> binariesOptionsMonitor,
    IOptionsMonitor<CaptureOptions> captureOptionsMonitor) : BackgroundService
{
    private readonly List<Task> _capturesInProgress = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("TwitchStreamCapturer started.");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                logger.LogInformation("Waiting for stream to start to capture...");
                var stream = await captureManager.GetNextCaptureInQueueAsync(stoppingToken).ConfigureAwait(false);

                logger.LogInformation("Cleaning up completed captures...");
                _capturesInProgress.RemoveAll(task => task.IsCompleted);

                logger.LogInformation("Starting to capture {Stream}", stream);
                _capturesInProgress.Add(CaptureAsync(stream, stoppingToken));
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing stream capture.");
            }
        }
        logger.LogInformation("TwitchStreamCapturer shutting down...");
        await Task.WhenAll(_capturesInProgress).ConfigureAwait(false);
        logger.LogInformation("TwitchStreamCapturer shut down.");
    }

    private async Task CaptureAsync(TwitchCapture stream, CancellationToken cancellationToken = default)
    {
        try
        {
            var binariesOptions = binariesOptionsMonitor.CurrentValue;
            var captureOptions = captureOptionsMonitor.CurrentValue;

            using var _1 = logger.BeginScope("Capture of Stream: {Stream}", stream);

            Directory.CreateDirectory("logs");

            var outputDir = Path.Combine(captureOptions.OutputDirectory, PathEx.SanitizeFileName(stream.UserName));
            var outputDirInfo = new DirectoryInfo(outputDir);
            if (!outputDirInfo.Exists)
            {
                outputDirInfo.Create();
            }
            if (OperatingSystem.IsLinux())
            {
                outputDirInfo.UnixFileMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
                                           | UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute
                                           | UnixFileMode.OtherRead | UnixFileMode.OtherExecute;
            }

            Directory.CreateDirectory(outputDir);
            var outputFileTs = Path.Combine(outputDir, PathEx.SanitizeFileName($"{stream.StartedAt:yyyy-MM-dd' 'HH:mm:ss} {stream.UserName} - {stream.Title} [{stream.Id}].ts"));
            var outputFileMp4 = Path.Combine(outputDir, PathEx.SanitizeFileName($"{stream.StartedAt:yyyy-MM-dd' 'HH:mm:ss} {stream.UserName} - {stream.Title} [{stream.Id}].mp4"));

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
                        },
                        UseShellExecute = false,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        StandardErrorEncoding = Encoding.UTF8,
                        StandardOutputEncoding = Encoding.UTF8,
                        WorkingDirectory = outputDir,
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
                await streamlinkStdout.WriteLineAsync(streamlinkCommandLine);
                await streamlinkStderr.WriteLineAsync(streamlinkCommandLine);

                var streamlinkEx = new ProcessEx(streamlink);
                streamlinkEx.OnStandardOutputLine += (_, line) =>
                {
                    streamlinkStdout.WriteLine(line);
                    streamlinkStdout.Flush();
                };
                streamlinkEx.OnStandardErrorLine += (_, line) =>
                {
                    streamlinkStderr.WriteLine(line);
                    streamlinkStderr.Flush();
                };
                await streamlinkEx.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);
                if (streamlink.ExitCode != 0)
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
                        },
                        UseShellExecute = false,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        StandardErrorEncoding = Encoding.UTF8,
                        StandardOutputEncoding = Encoding.UTF8,
                        WorkingDirectory = outputDir,
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
                await ffmpegStdout.WriteLineAsync($"$ {ffmpeg.StartInfo.FileName} \"{string.Join("\", \"", ffmpeg.StartInfo.ArgumentList)}\"");
                await ffmpegStderr.WriteLineAsync($"$ {ffmpeg.StartInfo.FileName} \"{string.Join("\", \"", ffmpeg.StartInfo.ArgumentList)}\"");

                var ffmpegEx = new ProcessEx(ffmpeg);
                ffmpegEx.OnStandardOutputLine += (_, line) =>
                {
                    ffmpegStdout.WriteLine(line);
                    ffmpegStdout.Flush();
                };
                ffmpegEx.OnStandardErrorLine += (_, line) =>
                {
                    ffmpegStderr.WriteLine(line);
                    ffmpegStderr.Flush();
                };
                await ffmpegEx.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);

                if (File.Exists(outputFileMp4) && OperatingSystem.IsLinux())
                {
                    File.SetUnixFileMode(
                        outputFileMp4,
                        UnixFileMode.UserRead | UnixFileMode.UserWrite
                        | UnixFileMode.GroupRead | UnixFileMode.GroupWrite
                        | UnixFileMode.OtherRead);
                }

                if (ffmpeg.ExitCode != 0)
                {
                    logger.LogError("Failed to remux {Stream}. Exit code was {ExitCode}.", stream, ffmpeg.ExitCode);
                    return;
                }

                File.Delete(outputFileTs);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error capturing stream {Stream}", stream);
        }
        finally
        {
            captureManager.NotifyCaptureFinished(stream.Id);
        }
    }
}
