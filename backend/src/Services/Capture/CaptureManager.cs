using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using LiveStreamDVR.Api.Models;

namespace LiveStreamDVR.Api.Services.Capture;

public sealed class CaptureManager : ICaptureManager
{
    private readonly ConcurrentDictionary<string, TwitchCapture> _captures = [];
    private readonly Channel<TwitchCapture> _captureChannel = Channel.CreateUnbounded<TwitchCapture>(new UnboundedChannelOptions
    {
        AllowSynchronousContinuations = false,
        SingleReader = true,
        SingleWriter = false,
    });

    public IEnumerable<TwitchCapture> Captures => _captures.Values;

    public bool IsCapturing(string id) => _captures.ContainsKey(id);

    public bool TryGetCapture(string id, [NotNullWhen(true)] out TwitchCapture? capture) =>
        _captures.TryGetValue(id, out capture);

    public async Task EnqueueCaptureAsync(TwitchCapture capture, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(capture.Id))
            throw new ArgumentException("Capture must have an ID.", nameof(capture));

        _captures.TryAdd(capture.Id, capture);
        await _captureChannel.Writer.WriteAsync(capture, cancellationToken);
    }

    public ValueTask<TwitchCapture> GetNextCaptureInQueueAsync(CancellationToken cancellationToken = default) =>
        _captureChannel.Reader.ReadAsync(cancellationToken);

    public bool NotifyCaptureFinished(string id) => _captures.Remove(id, out _);
}
