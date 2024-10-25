using System.Diagnostics;
using System.Text;
using LiveStreamDVR.Api.Configuration;
using LiveStreamDVR.Api.Helpers;
using LiveStreamDVR.Api.Models;
using LiveStreamDVR.Api.Services.Storage;
using Microsoft.Extensions.Options;

namespace LiveStreamDVR.Api.Services.Capture;

public sealed class TwitchStreamCapturer(
    ILogger<TwitchStreamCapturer> logger,
    ICaptureManager captureManager,
    IOptionsMonitor<BinariesOptions> binariesOptionsMonitor,
    IOptionsMonitor<CaptureOptions> captureOptionsMonitor,
    IConfigurationRepository configuration) : BackgroundService
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
                _capturesInProgress.Add(ProcessCaptureAsync(stream, stoppingToken));
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

    private async Task ProcessCaptureAsync(TwitchCapture stream, CancellationToken cancellationToken = default)
    {
        try
        {
            var binariesOptions = binariesOptionsMonitor.CurrentValue;

            using var _1 = logger.BeginScope("Capture of Stream: {Stream}", stream);

            Directory.CreateDirectory("logs");

            var outputDir = Path.Combine(captureOptionsMonitor.CurrentValue.OutputDirectory, PathEx.SanitizeFileName(stream.UserName));
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

            var outputFileTs = Path.Combine(outputDir, PathEx.SanitizeFileName($"{stream.StartedAt:yyyy-MM-dd' 'HH:mm:ss} {stream.UserName} - {stream.Title} [{stream.Id}].ts"));
            var outputFileMp4 = Path.Combine(outputDir, PathEx.SanitizeFileName($"{stream.StartedAt:yyyy-MM-dd' 'HH:mm:ss} {stream.UserName} - {stream.Title} [{stream.Id}].mp4"));

            var captureResult = await CaptureStreamAsync(stream, outputDir, outputFileTs, cancellationToken)
                .ConfigureAwait(false);
            if (!captureResult)
                return;

            await RemuxStreamCaptureAsync(stream, outputDir, outputFileTs, outputFileMp4, cancellationToken)
                .ConfigureAwait(false);
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

    private async Task<bool> CaptureStreamAsync(
        TwitchCapture stream,
        string workingDirectory,
        string outputFile,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Capturing stream {Stream}", stream);
        var streamlink = new Process()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = binariesOptionsMonitor.CurrentValue.StreamLinkPath,
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
                            "-o", outputFile,
                            "--url", $"https://twitch.tv/{stream.Login}",
                            "--default-stream", "1080p60,best"
                        },
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                StandardErrorEncoding = Encoding.UTF8,
                StandardOutputEncoding = Encoding.UTF8,
                WorkingDirectory = workingDirectory,
            }
        };

        var streamlinkExtraFlags = configuration.StreamlinkExtraCommandLine;
        if (!string.IsNullOrWhiteSpace(streamlinkExtraFlags))
        {
            foreach (var flag in CommandLineSplitter.SplitArguments(streamlinkExtraFlags))
                streamlink.StartInfo.ArgumentList.Add(flag);
        }

        string streamlinkCommandLine = "$ " + CommandLineSplitter.JoinArguments([streamlink.StartInfo.FileName, .. streamlink.StartInfo.ArgumentList]);
        logger.LogInformation("Using command line {CommandLine}", streamlinkCommandLine);
        var streamlinkStdout = new StreamWriter(Path.Combine("logs", $"capture_{stream.Login}_{stream.Id}.stdout.log"));
        var streamlinkStderr = new StreamWriter(Path.Combine("logs", $"capture_{stream.Login}_{stream.Id}.stderr.log"));
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
            return false;
        }

        return true;
    }

    private async Task RemuxStreamCaptureAsync(
        TwitchCapture stream,
        string workingDirectory,
        string inputFile,
        string outputFile,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Remuxing stream {Stream}", stream);
        var ffmpeg = new Process()
        {
            StartInfo =
                    {
                        FileName = binariesOptionsMonitor.CurrentValue.FfmpegPath,
                        ArgumentList =
                        {
                            "-i", inputFile,
                            "-c", "copy",
                            "-bsf:a", "aac_adtstoasc",
                            "-movflags", "faststart",
                            outputFile
                        },
                        UseShellExecute = false,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        StandardErrorEncoding = Encoding.UTF8,
                        StandardOutputEncoding = Encoding.UTF8,
                        WorkingDirectory = workingDirectory,
                    }
        };

        var ffmpegExtraFlags = configuration.FfmpegExtraCommandLine;
        if (!string.IsNullOrWhiteSpace(ffmpegExtraFlags))
        {
            foreach (var flag in CommandLineSplitter.SplitArguments(ffmpegExtraFlags))
                ffmpeg.StartInfo.ArgumentList.Add(flag);
        }

        string ffmpegCommandLine = "$ " + CommandLineSplitter.JoinArguments([ffmpeg.StartInfo.FileName, .. ffmpeg.StartInfo.ArgumentList]);
        logger.LogInformation("Using command line {CommandLine}", ffmpegCommandLine);
        var ffmpegStdout = new StreamWriter(Path.Combine("logs", $"remux_{stream.Login}_{stream.Id}.stdout.log"));
        var ffmpegStderr = new StreamWriter(Path.Combine("logs", $"remux_{stream.Login}_{stream.Id}.stderr.log"));
        await ffmpegStdout.WriteLineAsync(ffmpegCommandLine);
        await ffmpegStderr.WriteLineAsync(ffmpegCommandLine);

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

        if (File.Exists(outputFile) && OperatingSystem.IsLinux())
        {
            File.SetUnixFileMode(
                outputFile,
                UnixFileMode.UserRead | UnixFileMode.UserWrite
                | UnixFileMode.GroupRead | UnixFileMode.GroupWrite
                | UnixFileMode.OtherRead);
        }

        if (ffmpeg.ExitCode != 0)
        {
            logger.LogError("Failed to remux {Stream}. Exit code was {ExitCode}.", stream, ffmpeg.ExitCode);
            return;
        }

        File.Delete(inputFile);
    }
}
