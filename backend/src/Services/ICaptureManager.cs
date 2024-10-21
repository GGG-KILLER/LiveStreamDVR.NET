using System.Diagnostics.CodeAnalysis;
using LiveStreamDVR.Api.Models;

namespace LiveStreamDVR.Api.Services;

public interface ICaptureManager
{
    /// <summary>
    /// Lists all in-progress captures.
    /// </summary>
    IEnumerable<TwitchCapture> Captures { get; }

    /// <summary>
    /// Returns whether the stream with the provided ID is being captured or not.
    /// </summary>
    /// <param name="id">The ID of the stream being captured.</param>
    /// <returns>Whether the stream is being captured.</returns>
    bool IsCapturing(string id);

    /// <summary>
    /// Attempts to get an on-progress capture with the provided ID.
    /// </summary>
    /// <param name="id">The ID of the stream being captured.</param>
    /// <param name="capture">The capture, if the stream is being captured.</param>
    /// <returns>Whether the stream is being captured or not.</returns>
    bool TryGetCapture(string id, [NotNullWhen(true)] out TwitchCapture? capture);

    /// <summary>
    /// Adds a stream to capture to the queue.
    /// </summary>
    /// <param name="capture">The stream to capture.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>Nothing.</returns>
    Task EnqueueCaptureAsync(TwitchCapture capture, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously gets the next item in the queue (or waits for one to become available).
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>The next item in the queue to capture.</returns>
    ValueTask<TwitchCapture> GetNextCaptureInQueueAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies that a stream has finished capturing.
    /// </summary>
    /// <param name="id">The ID of the stream being captured.</param>
    /// <returns>Whether the stream was still on the list of streams being captured.</returns>
    bool NotifyCaptureFinished(string id);
}
