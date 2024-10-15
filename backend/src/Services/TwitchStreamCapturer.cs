

using System.Diagnostics;
using System.Threading.Channels;
using LiveStreamDVR.Api.Configuration;
using LiveStreamDVR.Api.Helpers;
using LiveStreamDVR.Api.Models;
using Microsoft.Extensions.Options;

namespace LiveStreamDVR.Api.Services;

public sealed class TwitchStreamCapturer(
    ServiceProvider serviceProvider,
    Channel<TwitchStream> streams,
    IOptionsMonitor<BinariesOptions> binariesOptions,
    IOptionsMonitor<CaptureOptions> captureOptions) : BackgroundService
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

    private async Task Capture(TwitchStream twitchStream, CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TwitchStreamCapturer>>();
        using var _1 = logger.BeginScope("Capture of Stream: {Stream}", twitchStream);

        try
        {
            Directory.CreateDirectory("logs");

            var outputDir = Path.Combine(
                captureOptions.CurrentValue.OutputDirectory!,
                PathEx.SanitizeFileName(twitchStream.UserName));
            Directory.CreateDirectory(outputDir);

            // $ streamlink --hls-live-edge 99999 --stream-timeout 200 --stream-segment-timeout 200 --stream-segment-threads 5 --ffmpeg-fout mpegts --twitch-disable-hosting --twitch-api-header=Authorization=OAuth ****** --twitch-disable-ads --twitch-disable-reruns --retry-streams 10 --retry-max 5 -o /usr/local/share/twitchautomator/data/storage/vods/michimochievee/2024-10-10 01-13-39 MichiMochievee - 🔴 ADVENTURING WITH MY SHINY POKEMON! - POKEMON WHITE pt.3  !merch !tts !game !discord !collab [41650896199].ts --url https://twitch.tv/michimochievee --default-stream 1080p60,best
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = binariesOptions.CurrentValue.FfmpegPath,
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
                        "-o", PathEx.SanitizeFileName($"{DateTime.Now:yyyy-MM-dd' 'HH:mm:ss} {twitchStream.UserName} - {twitchStream.Title} [{twitchStream.Id}].ts")
                    }
                }
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error capturing stream {Stream}", twitchStream);
        }
    }
}
