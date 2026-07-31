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
dotnet ef migrations add <Name> --context WwmDbContext -o Migrations/Wwm
dotnet ef database update --context IchigoContext
dotnet ef database update --context AnimethemesDbContext
dotnet ef database update --context WwmDbContext
```

`dotnet ef` defaults the host environment to Development, where `Program.cs` skips the
`GrassToucherReleaserService` registration that `TouchGrassService` depends on, so the design-time
service provider fails to build. Prefix every `dotnet ef` command with
`DOTNET_ENVIRONMENT=Production ASPNETCORE_ENVIRONMENT=Production`.

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

**Three EF Core contexts** serve different domains (all on the same `DefaultConnection`):
- `AnimethemesDbContext` — read-heavy, synced from the AnimeThemes GraphQL API by `AnimeThemesDbUpdateService`. Contains `Anime`, `AnimeTheme`, `Artist`, `Song`, etc.
- `IchigoContext` — bot state: `AiringAnime`, `RssReminder`, `GrassToucher`, `TrackedTickets`, `ScrimSignup`.
- `WwmDbContext` — Where Winds Meet player data scraped from the wwm.ratz-gg.net lookup API by `WwmLookupService`. Append-only build history: `WwmPlayer` (one row per `number_id`) → `WwmPlayerSnapshot` (one per *distinct build*, not per lookup) → `WwmGear` → `WwmGearAffix`/`WwmGearBaseAttr`/`WwmGearRetone`, plus `WwmSnapshotXinfa`/`WwmSnapshotSkill` and the `WwmTrackedPlayer` watchlist. Deliberately stores only stat/combat/build data — cosmetic and social fields (elegance score, badges, film/community stats) are dropped. Migrations live in `Migrations/Wwm`.

The WWM lookup endpoint answers a POST in one of two shapes: a queued job (`id` + `status: "pending"`, poll `GET /wwm-api/lookup/{id}` until `done`) or, on a cache hit, the full result inline with no `id`. Upstream caches each scrape for **4 hours**; `POST /wwm-api/lookup?fresh=1` bypasses that and queues a real scan, which is what `forceFresh: true` sends. The tracker always forces, since a cached copy can be four hours stale. Job statuses seen in the wild: `pending`, `in_progress`, `done`, `error`, and `claimed` — the last meaning the player made their profile private, which surfaces as `WwmProfileClaimedException` and disables tracking for them rather than burning a scan slot every sweep.

Upstream serialises scans through a queue that drives a game client, so `Wwm:RequestDelaySeconds` (gap between players) and `Wwm:PollIntervalMs` (job status re-poll) are deliberately generous. A sweep that outruns `Wwm:CheckIntervalMinutes` logs a warning — `PeriodicTimer` then just fires again immediately, so the real cadence degrades gracefully rather than piling up.

**Snapshots are written only when the build changes.** `WwmBuildFingerprint` hashes the major choices — weapons, inners (xinfa), mystics (skills), equipped gear identity, and which attunements are slotted and active. Affix and base-attr values, durability and retone counters are excluded: they drift on their own and would otherwise produce a new row on every check. Those values are still stored in full on whichever snapshots do get written. `WwmLookupService.Store` compares the new hash against the player's most recent snapshot and returns `WwmLookupOutcome(Snapshot, BuildChanged: false)` without inserting when they match.

`WwmSnapshotTrackerService` sweeps `wwm_tracked_player` every `Wwm:CheckIntervalMinutes` (default 5), forcing a fresh scan per player. Commands: `/wwmplayer` is public and shows a player's recent builds; `/wwmtrack`, `/wwmuntrack` and `/wwmtracked` are owner-only. All of them accept either a name or a numeric UID — `/wwmtrack` resolves a name by running the lookup first, since only the API can map a name to a UID.

`WwmProfileFormatter` renders build overviews and enforces Discord's 4096-character embed description cap itself: it takes up to five snapshots and returns how many actually fit, dropping the oldest as needed. The "Download last 7 days" button (`WwmExportButtonModule`, custom id `wwm-export:{numberId}`) replies ephemerally with a markdown attachment rendered by `WwmBuildMarkdownWriter` — every build in that window with full gear tables, base stats, affix values and attunements. Being a file it has no character ceiling, so it carries the rolled numbers the embed overview deliberately omits.

The `wwm_session` cookie is only needed to trigger a *fresh* scan — cached results are served to anyone. An expired cookie returns HTTP 200 with `{"error":"login_required","needsLogin":true}`, surfaced as `WwmAuthenticationException`; the tracker then pauses the cycle and posts to `Wwm:AlertChannelId`. `WwmSettings` is injected as `IOptionsMonitor`, so rotating `Wwm:SessionCookie` in `appsettings.json` takes effect without a restart — edit the copy the running host actually reads (the csproj copies it to the output directory).

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
| `Wwm`                                       | Where Winds Meet lookup API base URL, `wwm_session` cookie, poll interval and timeout |
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