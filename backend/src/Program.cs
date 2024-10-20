using System.Text.Json;
using System.Text.Json.Serialization;
using LiveStreamDVR.Api.Configuration;
using LiveStreamDVR.Api.OpenApi.Transformers;
using LiveStreamDVR.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using TwitchLib.EventSub.Webhooks.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables(prefix: "DVR_");

// Add services to the container.

var basicSection = builder.Configuration.GetSection(BasicOptions.ConfigurationKey);
var basicOptions = basicSection.Get<BasicOptions>() ?? throw new InvalidOperationException("Missing Basic key on settings.");

builder.Services.Configure<BasicOptions>(basicSection);
builder.Services.Configure<BinariesOptions>(builder.Configuration.GetSection(BinariesOptions.ConfigurationKey));
builder.Services.Configure<CaptureOptions>(builder.Configuration.GetSection(CaptureOptions.ConfigurationKey));
builder.Services.Configure<DiscordOptions>(builder.Configuration.GetSection(DiscordOptions.ConfigurationKey));
builder.Services.Configure<TwitchOptions>(builder.Configuration.GetSection(TwitchOptions.ConfigurationKey));
builder.Services.Configure<YoutubeOptions>(builder.Configuration.GetSection(YoutubeOptions.ConfigurationKey));

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
    if (!string.IsNullOrWhiteSpace(basicOptions.PathPrefix))
    {
        opts.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            var newPaths = new OpenApiPaths();
            foreach (var path in document.Paths)
            {
                newPaths.Add($"{basicOptions.PathPrefix.TrimEnd('/')}/{path.Key.TrimStart('/')}", path.Value);
            }
            document.Paths = newPaths;

            return Task.CompletedTask;
        });
    }
    opts.AddDocumentTransformer<AddOauthSecuritySchemeTransformer>();
});
builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(JwtBearerDefaults.AuthenticationScheme);

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

builder.Services.AddTwitchLibEventSubWebhooks(opts =>
{
    var twitchOptions = builder.Configuration.GetRequiredSection(TwitchOptions.ConfigurationKey).Get<TwitchOptions>()!;
    opts.EnableLogging = true;
    opts.CallbackPath = "/hook/twitch";
    opts.Secret = twitchOptions.WebhookSecret!;
});
builder.Services.AddHostedService<TwitchEventSubService>();
builder.Services.AddHostedService<TwitchStreamCapturer>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapOpenApi();
app.MapScalarApiReference(opts =>
{
    opts.WithPreferredScheme("Bearer");
    if (!string.IsNullOrWhiteSpace(basicOptions.PathPrefix))
    {
        opts.WithOpenApiRoutePattern($"{basicOptions.PathPrefix.TrimEnd('/')}/openapi/{{documentName}}.json");
    }
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseTwitchLibEventSubWebhooks();

app.MapControllers()
    .RequireAuthorization();

app.Run();
