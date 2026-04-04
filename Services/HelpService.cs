using System.Reflection;
using IchigoHoshimiya.Helpers;
using IchigoHoshimiya.Interfaces;
using Microsoft.Extensions.Configuration;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using NetCord.Services.Commands;

namespace IchigoHoshimiya.Services;

public class HelpService(IConfiguration configuration) : IHelpService
{
    // Maps declaring module type name → display category. Types sharing a name are merged.
    private static readonly Dictionary<string, string> CategoryMap = new()
    {
        { "AnilistSlashCommandModule",        "AniList"       },
        { "ThemeSlashCommandModule",           "Anime Themes"  },
        { "CalendarSlashCommandModule",        "Calendar"      },
        { "RssSlashCommandModule",             "Notifications" },
        { "ScrimSlashCommandModule",           "Scrim"         },
        { "VoiceTimerSlashCommandModule",      "Voice & Timer" },
        { "TouchingGrassSlashCommandModule",   "Touch Grass"   },
        { "PingSlashCommandModule",            "Utility"       },
        { "ChooseSlashCommand",                "Utility"       },
        { "ThreadSlashCommandModule",          "Utility"       },
        { "StarmiyaSlashCommandModule",        "Utility"       },
        { "VodSlashCommandModule",             "Utility"       },
        { "TeamspeakSlashCommandModule",       "Utility"       },
    };

    // Text command methods to exclude from the "text only" field (owner-use commands).
    private static readonly HashSet<string> ExcludedTextModules = ["OwnerCommandModule"];

    // Descriptions for text-only commands that have no slash equivalent.
    private static readonly Dictionary<string, string> TextOnlyDescriptions = new()
    {
        { "cd", "Count down from 5 to 1 with one-second intervals." }
    };

    public EmbedProperties GetHelpEmbed()
    {
        var prefix = configuration["Prefix"] ?? ";";
        var assembly = Assembly.GetExecutingAssembly();

        var slashFields = BuildSlashFields(assembly);
        var textOnlyField = BuildTextOnlyField(assembly, slashFields, prefix);

        var fields = new List<EmbedFieldProperties>(slashFields.Values);
        if (textOnlyField is not null)
            fields.Add(textOnlyField);

        return new EmbedProperties
        {
            Title = "Ichigo Hoshimiya — Command Reference",
            Description = $"All slash commands also work as text commands with the `{prefix}` prefix. Made with ❤️ by _starmiya_",
            Color = EmbedHelper.Build().Color,
            Fields = fields,
            Footer = new EmbedFooterProperties { Text = $"Prefix: {prefix}" }
        };
    }

    private static Dictionary<string, EmbedFieldProperties> BuildSlashFields(Assembly assembly)
    {
        // Preserve insertion order so categories appear in the order first encountered.
        var grouped = new Dictionary<string, List<string>>();

        var slashModuleTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract &&
                        t.IsSubclassOf(typeof(ApplicationCommandModule<ApplicationCommandContext>)));

        foreach (var type in slashModuleTypes)
        {
            if (!CategoryMap.TryGetValue(type.Name, out var category))
                continue;

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                var attr = method.GetCustomAttribute<SlashCommandAttribute>();
                if (attr is null) continue;

                if (!grouped.TryGetValue(category, out var lines))
                {
                    lines = [];
                    grouped[category] = lines;
                }

                lines.Add($"`/{attr.Name}` — {attr.Description}");
            }
        }

        return grouped.ToDictionary(
            kvp => kvp.Key,
            kvp => new EmbedFieldProperties
            {
                Name = kvp.Key,
                Value = string.Join("\n", kvp.Value)
            });
    }

    private static EmbedFieldProperties? BuildTextOnlyField(
        Assembly assembly,
        Dictionary<string, EmbedFieldProperties> slashFields,
        string prefix)
    {
        var slashNames = new HashSet<string>(
            slashFields.Values
                       .SelectMany(f => f.Value!.Split('\n'))
                       .Select(line => line.Split('`')[1].TrimStart('/')),
            StringComparer.OrdinalIgnoreCase);

        var textModuleTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract &&
                        t.IsSubclassOf(typeof(CommandModule<CommandContext>)) &&
                        !ExcludedTextModules.Contains(t.Name));

        var lines = new List<string>();

        foreach (var type in textModuleTypes)
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                var attr = method.GetCustomAttribute<CommandAttribute>();
                if (attr is null) continue;

                var name = attr.Aliases.FirstOrDefault();
                if (name is null || slashNames.Contains(name)) continue;

                var description = TextOnlyDescriptions.GetValueOrDefault(name, "—");
                lines.Add($"`{prefix}{name}` — {description}");
            }
        }

        if (lines.Count == 0)
            return null;

        return new EmbedFieldProperties
        {
            Name = "Text Only",
            Value = string.Join("\n", lines)
        };
    }
}
