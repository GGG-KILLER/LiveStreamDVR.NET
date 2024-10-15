using System.Text.Json.Serialization;

namespace LiveStreamDVR.Api.Models;

public sealed record DiscordWebhookMessage
{
    [JsonPropertyName("username")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Username { get; init; } = null;

    [JsonPropertyName("avatar_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AvatarUrl { get; init; } = null;

    [JsonPropertyName("tts")]
    public bool EnableTts { get; init; } = false;

    [JsonPropertyName("content")]
    public required string Content { get; init; }

    [JsonPropertyName("allowed_mentions")]
    public DiscordWebhookAllowedMentions AllowedMentions { get; init; } = new();
}

public sealed record DiscordWebhookAllowedMentions
{
    [JsonPropertyName("parse")]
    public IEnumerable<string> AllowedMentionTypes { get; init; } = [];

    [JsonPropertyName("roles")]
    public IEnumerable<long> AllowedRolesToMention { get; init; } = [];

    [JsonPropertyName("users")]
    public IEnumerable<long> AllowedUsersToMention { get; init; } = [];

    [JsonPropertyName("replied_user")]
    public bool AllowMentionUserBeingRepliedTo { get; init; } = false;
}
