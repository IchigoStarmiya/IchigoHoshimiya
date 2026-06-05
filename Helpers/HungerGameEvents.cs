namespace IchigoHoshimiya.Helpers;

// Tweak this file freely to change game flavour. Each event:
//   Participants : how many alive tributes the event consumes (1, 2, or 3).
//   Deaths       : indices into the picked participants that die. Empty = non-fatal.
//   Template     : the message. Use {0}, {1}, {2} for participant names in pick order.
//
// The game picks events whose Participants count fits the remaining alive pool,
// so adding more low-participant events keeps small games (5 tributes) flowing.
public sealed record HungerGameEvent(int Participants, int[] Deaths, string Template);

public static class HungerGameEvents
{
    public static readonly HungerGameEvent[] Day =
    [
        // Solo — survival flavour
        new(1, [], "**{0}** winrides to better guilds."),
        new(1, [], "**{0}** considers converting to nameless sword."),
        new(1, [], "**{0}** draws porn."),
        new(1, [], "**{0}** achieves new levels of freaked out."),
        new(1, [], "**{0}** is experimented on in Sey's basement."),
        new(1, [], "**{0}** changes to a chinese name."),

        // Solo — death
        new(1, [0], "MAI decides to shotcall for their guild. **{0}** fucking dies of cringe."),
        new(1, [0], "**{0}** gets permabenched in JUICED and executed."),
        new(1, [0], "**{0}** joins HONK. Dies of shame after 1 GvG."),
        new(1, [0], "**{0}** is flirted with by Ekra. Kills themselves afterwards."),
        new(1, [0], "**{0}** is forced to play with Space. Kills themselves after 1 game."),

        // Duo — non-fatal
        new(2, [], "**{0}** and **{1}** kiss."),
        new(2, [], "**{0}** and **{1}** ERP together in Obscuritas."),
        new(2, [], "**{0}** scares **{1}** duoq 3v3 and mald."),
        new(2, [], "**{0}** and **{1}** argue in their ticket."),

        // Duo — one death
        new(2, [1], "**{0}** kills **{1}** because Shizury told them to."),
        new(2, [1], "**{0}** strangles **{1}** with a length of wire."),
        new(2, [0], "**{1}** ambushes **{0}** from above. **{0}** never sees it coming."),
        new(2, [1], "**{0}** outruns **{1}** through the brambles, then doubles back with a knife."),
        new(2, [0], "**{0}** trips a snare meant for **{1}**."),
    ];

    public static readonly HungerGameEvent[] Night =
    [
        // Solo — flavour
        new(1, [], "**{0}** is haunted by Ekra in their sleep."),
        new(1, [], "**{0}** dreams up cancerous turtle comps."),
        new(1, [], "**{0}** is eepy."),
        new(1, [], "**{0}** dreams of better MMOs."),
        new(1, [], "**{0}** remembers the good old days of New World."),

        // Solo — death
        new(1, [0], "**{0}** uninstalls the game."),
        new(1, [0], "**{0}** dies waiting for GvG fixes."),
        new(1, [0], "**{0}**'s is teabagged and killed."),
        new(1, [0], "**{0}** messes with Shiroi and is killed."),

        // Duo
        new(2, [], "**{0}** and **{1}** huddle together for warmth."),
        new(2, [], "**{0}** and **{1}** trade stories until dawn."),
        new(2, [1], "**{0}** slits **{1}**'s throat in the dark."),
        new(2, [0], "**{1}** smothers **{0}** as they sleep."),
    ];

    public static readonly string[] WinnerLines =
    [
        "It is over. **{0}** survives."
    ];
}
