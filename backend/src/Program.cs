using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using LiveStreamDVR.Api.Configuration;
using LiveStreamDVR.Api.Helpers;
using LiveStreamDVR.Api.OpenApi.Transformers;
using LiveStreamDVR.Api.Services.Capture;
using LiveStreamDVR.Api.Services.Discord;
using LiveStreamDVR.Api.Services.Storage;
using LiveStreamDVR.Api.Services.Twitch;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using Tenray.ZoneTree;
using Tenray.ZoneTree.Comparers;
using Tenray.ZoneTree.Options;
using Tenray.ZoneTree.Serializers;
using TwitchLib.EventSub.Webhooks.Extensions;

// Setup state dir structure
if (!Directory.Exists("config"))
    Directory.CreateDirectory("config");
if (!Directory.Exists("logs"))
    Directory.CreateDirectory("logs");

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables(prefix: "DVR_");

// Add services to the container.

var basicSection = builder.Configuration.GetSection(BasicOptions.ConfigurationKey);
var basicOptions = basicSection.Get<BasicOptions>() ?? throw new InvalidOperationException("Missing Basic key on settings.");

builder.Services.AddOptions<BasicOptions>()
                .Bind(basicSection)
                .ValidateDataAnnotations()
                .Validate(
                    opts => opts.PublicUri is not null && opts.PublicUri.IsAbsoluteUri && opts.PublicUri.Scheme is "http" or "https",
                    $"Configuration {BasicOptions.ConfigurationKey}.{nameof(BasicOptions.PublicUri)} must be a valid public URL.")
                .Validate(
                    opts => opts.PathPrefix is null || (opts.PathPrefix.StartsWith('/') && opts.PathPrefix.EndsWith('/')),
                    $"Configuration {BasicOptions.ConfigurationKey}.{nameof(BasicOptions.PathPrefix)} must start and end with a slash.")
                .ValidateOnStart();
builder.Services.AddOptions<BinariesOptions>()
                .BindConfiguration(BinariesOptions.ConfigurationKey)
                .ValidateDataAnnotations()
                .Validate(
                    opts => !string.IsNullOrWhiteSpace(opts.StreamLinkPath)
                            && PathEx.GetBinaryPath(opts.StreamLinkPath) is not null,
                    $"Configuration {BinariesOptions.ConfigurationKey}.{nameof(BinariesOptions.StreamLinkPath)} must point to an existing streamlink binary.")
                .Validate(
                    opts => !string.IsNullOrWhiteSpace(opts.FfmpegPath)
                            && PathEx.GetBinaryPath(opts.FfmpegPath) is not null,
                    $"Configuration {BinariesOptions.ConfigurationKey}.{nameof(BinariesOptions.FfmpegPath)} must point to an existing ffmpeg binary.")
                .ValidateOnStart();
builder.Services.AddOptions<CaptureOptions>()
                .BindConfiguration(CaptureOptions.ConfigurationKey)
                .ValidateDataAnnotations()
                .Validate(
                    opts => !string.IsNullOrWhiteSpace(opts.OutputDirectory) && Directory.Exists(opts.OutputDirectory),
                    $"Path on configuration {CaptureOptions.ConfigurationKey}.{nameof(CaptureOptions.OutputDirectory)} must exist and be a directory.")
                .ValidateOnStart();
builder.Services.AddOptions<DiscordOptions>()
                .BindConfiguration(DiscordOptions.ConfigurationKey)
                .ValidateDataAnnotations()
                .Validate(
                    opts =>
                    {
                        var uri = opts.WebhookUri;
                        return uri is null
                            || (uri.IsAbsoluteUri
                                && uri.Scheme == "https"
                                && uri.Host is "discord.com" or "ptb.discord.com" or "canary.discord.com"
                                && uri.AbsolutePath.StartsWith("/api/webhooks/", StringComparison.Ordinal)
                                && ulong.TryParse(
                                    uri.AbsolutePath.AsSpan()["/api/webhooks/".Length..uri.AbsolutePath.IndexOf('/', "/api/webhooks/".Length)],
                                    NumberStyles.None,
                                    CultureInfo.InvariantCulture,
                                    out _));
                    },
                    $"Configuration {DiscordOptions.ConfigurationKey}.{nameof(DiscordOptions.WebhookUri)} must be a valid discord webhook URL.")
                .ValidateOnStart();
builder.Services.AddOptions<TwitchOptions>()
                .BindConfiguration(TwitchOptions.ConfigurationKey)
                .ValidateDataAnnotations()
                .Validate(
                    opts => !string.IsNullOrWhiteSpace(opts.WebhookSecret) && opts.WebhookSecret.All(char.IsAscii),
                    $"Configuration {TwitchOptions.ConfigurationKey}.{nameof(TwitchOptions.WebhookSecret)} must be ASCII.")
                .ValidateOnStart();
// TODO: Uncomment when YouTube capturing is implemented.
// builder.Services.AddOptions<YoutubeOptions>()
//                 .BindConfiguration(YoutubeOptions.ConfigurationKey)
//                 .ValidateDataAnnotations()
//                 .ValidateOnStart();

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.AllowTrailingCommas = true;
        opts.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        opts.JsonSerializerOptions.IndentSize = 2;
        opts.JsonSerializerOptions.PropertyNameCaseInsensitive = false;
        opts.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
        opts.JsonSerializerOptions.RespectNullableAnnotations = true;
        opts.JsonSerializerOptions.RespectRequiredConstructorParameters = true;
        opts.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
        opts.JsonSerializerOptions.WriteIndented = true;
    });
builder.Services.AddOpenApi(opts =>
{
    opts.AddDocumentTransformer<AddPrefixToPathsTransformer>();
    opts.AddDocumentTransformer<AddOauthSecuritySchemeTransformer>();
});
builder.Services.AddAuthentication().AddJwtBearer();
builder.Services.AddAuthorization();

builder.Services.AddHttpClient();
builder.Services.AddHttpClient("TwitchOauth", client =>
{
    client.BaseAddress = new Uri("https://id.twitch.tv/oauth2/");
});
builder.Services.AddHttpClient("TwitchHelix", client =>
{
    client.BaseAddress = new Uri("https://api.twitch.tv/helix/");
});
builder.Services.AddSingleton<IDiscordWebhook, DiscordWebhook>();
builder.Services.AddSingleton<ITwitchClient, TwitchClient>();
builder.Services.AddSingleton<ICaptureManager, CaptureManager>();

var database = new ZoneTreeFactory<string, string>()
    .SetDataDirectory("config")
    .SetComparer(new StringOrdinalIgnoreCaseComparerAscending())
    .SetKeySerializer(new Utf8StringSerializer())
    .SetValueSerializer(new Utf8StringSerializer())
    .ConfigureDiskSegmentOptions(opts =>
    {
        opts.CompressionMethod = CompressionMethod.Zstd;
        opts.CompressionLevel = CompressionLevels.ZstdMax;
    })
    .ConfigureWriteAheadLogOptions(opts =>
    {
        // Don't want any risk of corruptions here.
        opts.WriteAheadLogMode = WriteAheadLogMode.Sync;
    })
    .OpenOrCreate();

var databaseMaintainer = database.CreateMaintainer();
databaseMaintainer.EnableJobForCleaningInactiveCaches = true;

builder.Services.AddSingleton(database);
builder.Services.AddSingleton(databaseMaintainer);
builder.Services.AddSingleton<IConfigurationRepository, ConfigurationRepository>();

builder.Services.AddTwitchLibEventSubWebhooks(opts =>
{
    var twitchOptions = builder.Configuration.GetRequiredSection(TwitchOptions.ConfigurationKey).Get<TwitchOptions>()!;
    opts.EnableLogging = true;
    opts.CallbackPath = "/hook/twitch";
    opts.Secret = twitchOptions.WebhookSecret!;
});
builder.Services.AddHostedService<TwitchEventSubService>();
builder.Services.AddHostedService<TwitchStreamCapturer>();
builder.Services.AddHostedService<ZoneTreeService>();

var app = builder.Build();

{
    var captureOptionsSnapshot = app.Services.GetRequiredService<IOptionsSnapshot<CaptureOptions>>();
    var discordOptionsSnapshot = app.Services.GetRequiredService<IOptionsSnapshot<DiscordOptions>>();
    var twitchOptionsSnapshot = app.Services.GetRequiredService<IOptionsSnapshot<TwitchOptions>>();

    var config = app.Services.GetRequiredService<IConfigurationRepository>();
    config.InitializeFromConfiguration(captureOptionsSnapshot, discordOptionsSnapshot, twitchOptionsSnapshot);
}

// Configure the HTTP request pipeline.
app.MapOpenApi();
app.MapScalarApiReference(opts =>
{
    opts.WithPreferredScheme("Bearer");
    if (!string.IsNullOrWhiteSpace(basicOptions.PathPrefix))
    {
        opts.WithOpenApiRoutePattern($"{basicOptions.PathPrefix.TrimEnd('/')}/openapi/{{documentName}}.json");
    }

    // Attempt to disable proxy.
    opts.ProxyUrl = "";
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseTwitchLibEventSubWebhooks();

app.MapControllers()
    .RequireAuthorization();

app.Run();
