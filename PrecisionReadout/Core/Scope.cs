namespace PrecisionReadout;

// True while the game builds text for a surface we upgrade (hover card, info panel, tile status lines).
// Only the main thread draws those, so a plain counter is enough. Nested entries are allowed.
internal static class Scope
{
    private static int depth;

    internal static bool Active => depth > 0;

    internal static void Enter() => depth++;

    internal static void Exit()
    {
        if (depth > 0) depth--;
    }
}
