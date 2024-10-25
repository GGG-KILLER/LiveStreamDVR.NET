namespace LiveStreamDVR.Api.Services.Storage;

public interface ITwitchRepository
{
    string? GetStreamerId(string login);

    void SetStreamerId(string login, string id);

    string? GetStreamerLogin(string id);

    void SetStreamerLogin(string id, string login);
}
