using System.Globalization;
using System.Text.Json;
using IchigoHoshimiya.DTO;
using IchigoHoshimiya.Entities.Wwm;

namespace IchigoHoshimiya.Services;

public static class WwmLookupMapper
{
    private const string RawBaseProperty = "base";

    public static WwmPlayerSnapshot ToSnapshot(WwmLookupJobDto job, WwmLookupType lookupType, string lookupQuery)
    {
        var result = job.Result!;
        var rawBase = GetRawBase(result.RawData);

        var snapshot = new WwmPlayerSnapshot
        {
            NumberId = result.NumberId,
            LookupJobId = Truncate(job.Id, 64) ?? string.Empty,
            DataUpdatedAtUtc = FromUnixMilliseconds(job.Updated),
            WasCached = job.Cached ?? false,
            WasStale = job.Stale ?? false,
            LookupType = lookupType,
            LookupQuery = Truncate(lookupQuery, 100) ?? string.Empty,
            ExternalId = Truncate(result.Id, 64) ?? string.Empty,
            Name = Truncate(result.Name, 100) ?? string.Empty,
            Server = result.Server,
            Region = Truncate(result.Region, 32),
            OverseaTag = Truncate(result.OverseaTag, 32),
            GuildName = Truncate(result.GuildName, 100),
            ClubId = Truncate(ToScalarString(result.ClubId), 64),
            DiscordAccountId = ParseUlong(GetString(rawBase, "discord_account_id")),
            Level = result.Level,
            School = result.School,
            IsMaster = result.IsMaster,
            BuildPower = result.BuildPower,
            MaxXiuweiKungfu = GetInt(rawBase, "max_xiuwei_kungfu"),
            CelestialPulls = result.CelestialPulls,
            SolemnPulls = result.SolemnPulls,
            HarmonicPulls = result.HarmonicPulls,
            OtherLegendPulls = result.OtherLegendPulls,
            AccountCreatedAtUtc = FromUnixSeconds(result.CreateTime),
            LoginAtUtc = FromUnixSeconds(result.LoginTime),
            LogoutAtUtc = FromUnixSeconds(result.LogoutTime),
            OnlineTimeSeconds = result.OnlineTime,
            IsOnline = GetBool(rawBase, "is_online"),
            HasPvpData = result.HasPvpData,
            TotalMatches = result.TotalMatches,
            Wins = result.Wins,
            Losses = result.Losses,
            WinRate = result.WinRate,
            WinRatePercent = result.WinRatePercent,
            WinningStreak = result.WinningStreak,
            PvpScore = result.PvpScore,
            SurfaceScore = result.SurfaceScore,
            PvpGrade = Truncate(ToScalarString(result.PvpGrade), 64),
            PvpSmallGrade = Truncate(ToScalarString(result.PvpSmallGrade), 64),
            PvpMaxGrade = Truncate(ToScalarString(result.PvpMaxGrade), 64),
            PvpMaxSmallGrade = Truncate(ToScalarString(result.PvpMaxSmallGrade), 64),
            PvpSeasonId = result.PvpSeasonId,
            LunjianGrade = Truncate(ToScalarString(result.LunjianGrade), 64),
            LunjianSmallGrade = Truncate(ToScalarString(result.LunjianSmallGrade), 64),
            MaxWuwoScore = result.MaxWuwoScore,
            LunjianMaxStreak = result.LunjianMaxStreak,
            LunjianTotal = result.LunjianTotal,
            ArenaRank = result.ArenaRank,
            MainWeaponId = result.Build?.Weapons?.Main?.Id,
            MainWeaponName = Truncate(result.Build?.Weapons?.Main?.Name, 100),
            SubWeaponId = result.Build?.Weapons?.Sub?.Id,
            SubWeaponName = Truncate(result.Build?.Weapons?.Sub?.Name, 100),
            GameplayTrailJson = ToRawJson(result.GameplayTrail),
            CurrentScenarioJson = ToRawJson(result.CurrentScenario),
            RawBaseJson = rawBase?.GetRawText()
        };

        if (result.Build is null)
        {
            return snapshot;
        }

        for (var i = 0; i < result.Build.Xinfa.Count; i++)
        {
            var xinfa = result.Build.Xinfa[i];

            snapshot.Xinfa.Add(new WwmSnapshotXinfa
            {
                Ordinal = i,
                XinfaId = xinfa.Id,
                Name = Truncate(xinfa.Name, 100)
            });
        }

        foreach (var skill in result.Build.Skills)
        {
            snapshot.Skills.Add(new WwmSnapshotSkill
            {
                Slot = skill.Slot,
                SkillId = skill.Id,
                Name = Truncate(skill.Name, 100)
            });
        }

        foreach (var gear in result.Build.Gear)
        {
            snapshot.Gear.Add(ToGear(gear));
        }

        return snapshot;
    }

    public static void ApplyToPlayer(WwmPlayer player, WwmPlayerSnapshot snapshot)
    {
        player.ExternalId = snapshot.ExternalId;
        player.Name = snapshot.Name;
        player.Region = snapshot.Region;
        player.Server = snapshot.Server;
        player.AccountCreatedAtUtc = snapshot.AccountCreatedAtUtc;
        player.LastSeenAtUtc = snapshot.FetchedAtUtc;
    }

    private static WwmGear ToGear(WwmGearDto dto)
    {
        var gear = new WwmGear
        {
            SlotName = Truncate(dto.SlotName, 64),
            SlotOrd = dto.SlotOrd,
            ItemNo = dto.ItemNo,
            ItemIndex = dto.Index,
            Durability = dto.Durability,
            Suit = dto.Suit,
            SuitName = Truncate(dto.SuitName, 100),
            Retoned = dto.Retoned,
            RetoneNo = dto.RetoneNo,
            RetoneAtUtc = FromUnixSeconds(dto.RetoneTs),
            RetoneCount = dto.RetoneCount
        };

        for (var i = 0; i < dto.RetoneHistory.Count; i++)
        {
            gear.RetoneHistory.Add(new WwmGearRetone
            {
                Ordinal = i,
                RetoneNo = dto.RetoneHistory[i]
            });
        }

        for (var i = 0; i < dto.BaseAttrs.Count; i++)
        {
            var attr = dto.BaseAttrs[i];

            gear.BaseAttrs.Add(new WwmGearBaseAttr
            {
                Ordinal = i,
                RawName = Truncate(attr.RawName, 64),
                Name = Truncate(attr.Name, 128),
                Value = attr.Value
            });
        }

        for (var i = 0; i < dto.Affixes.Count; i++)
        {
            var affix = dto.Affixes[i];

            gear.Affixes.Add(new WwmGearAffix
            {
                Ordinal = i,
                AffixId = affix.Id,
                Name = Truncate(affix.Name, 1000),
                Value = affix.Value,
                Type = Truncate(affix.Type, 32),
                Active = affix.Active,
                DisplayValue = Truncate(affix.DisplayValue, 64)
            });
        }

        return gear;
    }

    private static JsonElement? GetRawBase(JsonElement? rawData) =>
        rawData is { ValueKind: JsonValueKind.Object } element &&
        element.TryGetProperty(RawBaseProperty, out var baseElement) &&
        baseElement.ValueKind == JsonValueKind.Object
            ? baseElement
            : null;

    private static bool TryGetProperty(JsonElement? element, string propertyName, out JsonElement value)
    {
        if (element is { ValueKind: JsonValueKind.Object } owner)
        {
            return owner.TryGetProperty(propertyName, out value);
        }

        value = default;

        return false;
    }

    private static string? GetString(JsonElement? element, string propertyName) =>
        TryGetProperty(element, propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int? GetInt(JsonElement? element, string propertyName) =>
        TryGetProperty(element, propertyName, out var value) &&
        value.ValueKind == JsonValueKind.Number &&
        value.TryGetInt32(out var parsed)
            ? parsed
            : null;

    private static bool? GetBool(JsonElement? element, string propertyName)
    {
        if (!TryGetProperty(element, propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when value.TryGetInt32(out var parsed) => parsed != 0,
            _ => null
        };
    }

    private static ulong? ParseUlong(string? value) =>
        ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;

    private static string? ToScalarString(JsonElement? element) => element?.ValueKind switch
    {
        null or JsonValueKind.Null or JsonValueKind.Undefined => null,
        JsonValueKind.String => element.Value.GetString(),
        _ => element.Value.GetRawText()
    };

    private static string? ToRawJson(JsonElement? element) => element?.ValueKind switch
    {
        null or JsonValueKind.Null or JsonValueKind.Undefined => null,
        _ => element.Value.GetRawText()
    };

    private static DateTime? FromUnixMilliseconds(long? milliseconds) =>
        milliseconds is null or <= 0
            ? null
            : DateTimeOffset.FromUnixTimeMilliseconds(milliseconds.Value).UtcDateTime;

    private static DateTime? FromUnixSeconds(double? seconds) =>
        seconds is null or <= 0
            ? null
            : DateTimeOffset.FromUnixTimeMilliseconds((long)(seconds.Value * 1000)).UtcDateTime;

    private static string? Truncate(string? value, int maxLength) =>
        value is not null && value.Length > maxLength ? value[..maxLength] : value;
}
