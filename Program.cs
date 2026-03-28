using IchigoHoshimiya.Adapters;
using IchigoHoshimiya.BackgroundServices;
using IchigoHoshimiya.Context;
using IchigoHoshimiya.Helpers;
using IchigoHoshimiya.Interfaces;
using IchigoHoshimiya.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Hosting.Services.Commands;
using NetCord.Hosting.Services.ComponentInteractions;
using NetCord.Services;
using NetCord.Services.Commands;
using NetCord.Services.ComponentInteractions;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddSentry(options =>
{
    options.Dsn = builder.Configuration.GetValue<string>("Dsn");
    
    options.MinimumEventLevel = LogLevel.Error;
    
    options.Environment = builder.Environment.EnvironmentName;
    
    options.Release = builder.Configuration.GetValue<string>("SENTRY_RELEASE") ?? "ichigo-hoshimiya@1.0.0";
    
    options.AutoSessionTracking = true;
    
    options.Debug = builder.Environment.IsDevelopment();
});

builder.Services.Configure<AnimeThemesUpdaterSettings>(
    builder.Configuration.GetSection("AnimeThemesUpdater"));

builder.Services.AddHttpClient<AnimeThemesDbUpdateService>();
builder.Services.AddHttpClient<SeasonalCalendarDbUpdateService>();
builder.Services.AddHttpClient<RssSearcherAndPosterService>();

builder.Services.AddHostedService<AnimeThemesDbUpdateService>();
builder.Services.AddHostedService<SeasonalCalendarDbUpdateService>();
builder.Services.AddHostedService<RssSearcherAndPosterService>();
builder.Services.AddHostedService<TicketBackupService>();

builder.Services.AddSingleton<GrassToucherReleaserService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<GrassToucherReleaserService>());

// builder.Services.AddHostedService<DanseMacabreBackgroundService>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");


builder.Services.AddDbContext<AnimethemesDbContext>(options =>
    options.UseMySQL(connectionString!));

builder.Services.AddDbContext<IchigoContext>(options =>
    options.UseMySQL(connectionString!));


builder.Services.AddTransient<IClient, RestClientAdapter>();
builder.Services.AddSingleton<IPingService, PingService>();
builder.Services.AddHostedService<TicketBackupService>();
builder.Services.AddSingleton<ITwitterReplacementService, TwitterReplacementService>();
builder.Services.AddScoped<IAnimethemeService, AnimethemeService>();
builder.Services.AddScoped<ICalendarService, CalendarService>();
builder.Services.AddScoped<IRssService, RssService>();
builder.Services.AddScoped<IChooseService, ChooseService>();
builder.Services.AddScoped<ITouchGrassService, TouchGrassService>();
builder.Services.AddScoped<IScrimService, ScrimService>();
builder.Services.AddHostedService<ScrimAutoCloseService>();

builder.Services.Configure<HostOptions>(o =>
{
    o.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});


builder.Services
       .AddDiscordGateway(options =>
        {
            options.Intents = GatewayIntents.GuildMessages |
                              GatewayIntents.DirectMessages |
                              GatewayIntents.MessageContent |
                              GatewayIntents.DirectMessageReactions |
                              GatewayIntents.GuildMessageReactions;
        })
       .AddCommands<CommandContext>(options =>
        {
            // Suppress "Command not found"
            options.ResultHandler = new InlineResultHandler();
        })
       .AddApplicationCommands()
       .AddComponentInteractions<ButtonInteraction, ButtonInteractionContext>()
       .AddComponentInteractions<StringMenuInteraction, StringMenuInteractionContext>()
       .AddGatewayHandlers(typeof(Program).Assembly);

var host = builder.Build();

var colours = builder.Configuration
                     .GetSection("EmbedColours")
                     .Get<EmbedColours>();

EmbedHelper.Initialize(colours!);

// NetCord: Add commands from modules
host.AddModules(typeof(Program).Assembly);

// TODO: REMOVE THIS BLOCK — one-time refresh to push the "Not Available This Week" button to existing open scrim signups.
{
    await using var scope = host.Services.CreateAsyncScope();
    var scrimService = scope.ServiceProvider.GetRequiredService<IScrimService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var openSignups = await scrimService.GetExpiredOpenSignupsAsync(CancellationToken.None);
        // GetExpiredOpenSignupsAsync only returns signups past Friday — grab ALL open ones via the DB directly.
        // Use RefreshSignupMessageAsync which rebuilds embeds + components (including the new button).
        using var dbScope = host.Services.CreateScope();
        var db = dbScope.ServiceProvider.GetRequiredService<IchigoContext>();
        var allOpen = await db.ScrimSignups
            .Where(s => s.IsOpen && s.MessageId != null)
            .ToListAsync();
        foreach (var signup in allOpen)
        {
            try
            {
                await scrimService.RefreshSignupMessageAsync(signup.Id, CancellationToken.None);
                logger.LogInformation("Refreshed scrim signup {Id} (message {MessageId})", signup.Id, signup.MessageId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to refresh scrim signup {Id}", signup.Id);
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "One-time scrim refresh failed");
    }
}

await host.RunAsync();

file sealed class InlineResultHandler : ICommandResultHandler<CommandContext>
{
    private readonly CommandResultHandler<CommandContext> _default = new();

    public ValueTask HandleResultAsync(
        IExecutionResult result,
        CommandContext context,
        GatewayClient client,
        ILogger logger,
        IServiceProvider services)
    {
        return result is NotFoundResult
            ? ValueTask.CompletedTask
            : _default.HandleResultAsync(result, context, client, logger, services);
    }
}