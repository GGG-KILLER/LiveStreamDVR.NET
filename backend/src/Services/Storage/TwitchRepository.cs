using Tenray.ZoneTree;

namespace LiveStreamDVR.Api.Services.Storage;

public sealed class TwitchRepository(IZoneTree<string, string> database) : ITwitchRepository
{
    public string? GetStreamerId(string login) =>
        database.TryGet($"twitch.users.{login}.id", out var id) ? id : null;

    public void SetStreamerId(string login, string id)
    {
        database.AtomicUpsert($"twitch.users.{login}.id", id);
        database.AtomicUpsert($"twitch.users.{id}.login", login);
    }

    public string? GetStreamerLogin(string id) =>
        database.TryGet($"twitch.users.{id}.login", out var login) ? login : null;

    public void SetStreamerLogin(string id, string login)
    {
        database.AtomicUpsert($"twitch.users.{id}.login", login);
        database.AtomicUpsert($"twitch.users.{login}.id", id);
    }
}
