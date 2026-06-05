# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Ichigo Hoshimiya** — a C# Discord bot built for personal/community use, specialized for anime communities and esports scrim coordination. Uses NetCord 1.0.0-alpha for Discord integration and Entity Framework Core with MySQL for persistence.

## Commands

```bash
dotnet build        # Build the project
dotnet run          # Run the bot
```

No test framework is configured. The `global.json` pins the SDK to .NET 10.0.0.

**Database migrations:**
```bash
dotnet ef migrations add <Name> --context IchigoContext
dotnet ef migrations add <Name> --context AnimethemesDbContext
dotnet ef database update --context IchigoContext
dotnet ef database update --context AnimethemesDbContext
```

## Architecture

The bot follows a layered architecture wired together via `Microsoft.Extensions.DependencyInjection` in `Program.cs`:

```
Discord Gateway Events
        ↓
Handlers/ + Modules/ (commands & interactions)
        ↓
Services/ (business logic, all interface-backed)
        ↓
Context/ (EF Core DbContexts)
        ↓
MySQL Database
```

**Two EF Core contexts** serve different domains:
- `AnimethemesDbContext` — read-heavy, synced from the AnimeThemes GraphQL API by `AnimeThemesDbUpdateService`. Contains `Anime`, `AnimeTheme`, `Artist`, `Song`, etc.
- `IchigoContext` — bot state: `AiringAnime`, `RssReminder`, `GrassToucher`, `TrackedTickets`, `ScrimSignup`.

**Command system** (NetCord) has three layers registered in `Program.cs`:
- `Modules/SlashCommands/` — application (slash) commands
- `Modules/TextCommands/` — legacy prefix-based commands
- `Modules/InteractionModules/` — button and string menu component interactions

**Background services** (`BackgroundServices/`) run on timers: `AnimeThemesDbUpdateService`, `SeasonalCalendarDbUpdateService`, `RssSearcherAndPosterService` (Jackett/RSS, every 30 min), `TicketBackupService`, `GrassToucherReleaserService`, `ScrimAutoCloseService`. `DanseMacabreBackgroundService` is currently commented out.

**Adapters/** wraps NetCord's `RestClient` behind an `IClient` interface (`RestClientAdapter`) so services don't take a hard dependency on the Discord client.

**Helpers/** — `EmbedHelper` is a static helper initialized at startup from `appsettings.json`'s `EmbedColours` section; use it for all embed construction to keep colors consistent.

## Configuration

All runtime config lives in `appsettings.json` (not committed — create locally):

| Key                                         | Purpose |
|---------------------------------------------|---------|
| `Token`                                     | Discord bot token |
| `Prefix`                                    | Text command prefix |
| `Discord:OwnerUserId`                       | Bot owner Discord user ID |
| `ConnectionStrings:DefaultConnection`       | MySQL connection string |
| `AnimeThemesUpdater`                        | GraphQL endpoint config |
| `EmbedColours`                              | Integer color values for embed categories |
| `Dsn`                                       | Sentry DSN for error tracking |
| `RefugeeBot`                                | Feature flag for refugee-server-specific behavior |
| Various `GuildId`/`ChannelId`/`RoleId` keys | Hardcoded server/channel/role IDs for specific Discord servers |

The bot is intentionally server-specific; many features reference hardcoded IDs for the author's Discord servers.

## Key Patterns

- All services are registered against interfaces in `Interfaces/`; inject the interface, not the concrete type.
- `IClient` (via `RestClientAdapter`) is the entry point for Discord REST calls from services.
- `EmbedHelper.Initialize()` must be called before any embeds are built — it's called once in `Program.cs` after `builder.Build()`.
- Background service exceptions are configured to be ignored (`BackgroundServiceExceptionBehavior.Ignore`) to prevent the host from crashing on transient failures.
- `FuzzySharp` is used in `AnimethemeService` for fuzzy anime/theme name matching.
- Follow SOLID principles when writing code. Consistently apply rider's refactoring to clean up unused usings etc. NO build warning introduction allowed.
- NO XML tags.