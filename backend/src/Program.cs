using System.Threading.Channels;
using LiveStreamDVR.Api.Configuration;
using LiveStreamDVR.Api.Models;
using LiveStreamDVR.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Scalar.AspNetCore;
using TwitchLib.EventSub.Webhooks.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables(prefix: "DVR_");

// Add services to the container.

builder.Services.Configure<BasicOptions>(builder.Configuration.GetSection(BasicOptions.ConfigurationKey));
builder.Services.Configure<BinariesOptions>(builder.Configuration.GetSection(BinariesOptions.ConfigurationKey));
builder.Services.Configure<CaptureOptions>(builder.Configuration.GetSection(CaptureOptions.ConfigurationKey));
builder.Services.Configure<DiscordOptions>(builder.Configuration.GetSection(DiscordOptions.ConfigurationKey));
builder.Services.Configure<TwitchOptions>(builder.Configuration.GetSection(TwitchOptions.ConfigurationKey));
builder.Services.Configure<YoutubeOptions>(builder.Configuration.GetSection(YoutubeOptions.ConfigurationKey));

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(JwtBearerDefaults.AuthenticationScheme);
builder.Services.AddSingleton<IDiscordWebhook, DiscordWebhook>();
builder.Services.AddTwitchLibEventSubWebhooks(opts =>
{
    var twitchOptions = builder.Configuration.GetRequiredSection(TwitchOptions.ConfigurationKey).Get<TwitchOptions>()!;
    opts.EnableLogging = true;
    opts.CallbackPath = "/hook/twitch";
    opts.Secret = twitchOptions.WebhookSecret!;
});
builder.Services.AddSingleton(Channel.CreateUnbounded<TwitchStream>(new UnboundedChannelOptions
{
    AllowSynchronousContinuations = false,
    SingleReader = true,
    SingleWriter = false,
}));
builder.Services.AddHostedService<TwitchEventSubService>();
builder.Services.AddHostedService<TwitchStreamCapturer>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapOpenApi();
app.MapScalarApiReference();

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseTwitchLibEventSubWebhooks();

app.MapControllers();

app.Run();
