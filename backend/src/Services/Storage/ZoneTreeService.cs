using Tenray.ZoneTree;

namespace LiveStreamDVR.Api.Services.Storage;

public sealed class ZoneTreeService(IMaintainer databaseMaintainer) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await databaseMaintainer.WaitForBackgroundThreadsAsync();
    }
}
