namespace LiveStreamDVR.Api.Models;

public sealed class TwitchGetChannelInfoResponse
{
    public required List<TwitchChannelInfo> Data { get; set; }
}

public sealed class TwitchChannelInfo
{
    public required string BroadcasterId { get; set; }

    public required string BroadcasterLogin { get; set; }

    public required string BroadcasterName { get; set; }

    public required string BroadcasterLanguage { get; set; }

    public required string GameId { get; set; }

    public required string GameName { get; set; }

    public required string Title { get; set; }

    public required long Delay { get; set; }

    public required List<string> Tags { get; set; }

    public required List<string> ContentClassificationLabels { get; set; }

    public required bool IsBrandedContent { get; set; }
}
