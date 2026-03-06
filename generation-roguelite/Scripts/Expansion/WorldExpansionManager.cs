namespace GenerationRoguelite.Expansion;

public enum WorldExpansionTheme
{
    Original,
    Isekai,
    SciFi,
}

public readonly record struct WorldExpansionSnapshot(
    WorldExpansionTheme Theme,
    string Name,
    string PromptTag,
    float TerrainDensityOffset,
    float InflationFactor);

public sealed class WorldExpansionManager
{
    public WorldExpansionSnapshot Current { get; private set; } =
        new(WorldExpansionTheme.Original, "原典世界", "fantasy_classic", 0f, 1f);

    public void UpdateByProgress(int generation, int totalScore)
    {
        if (generation >= 18 || totalScore >= 240_000)
        {
            Current = new WorldExpansionSnapshot(
                WorldExpansionTheme.SciFi,
                "SF編",
                "future_scifi",
                0.08f,
                1.35f);
            return;
        }

        if (generation >= 12 || totalScore >= 120_000)
        {
            Current = new WorldExpansionSnapshot(
                WorldExpansionTheme.Isekai,
                "異世界編",
                "isekai_branch",
                0.03f,
                1.18f);
            return;
        }

        Current = new WorldExpansionSnapshot(
            WorldExpansionTheme.Original,
            "原典世界",
            "fantasy_classic",
            0f,
            1f);
    }

    public string BuildSummary()
    {
        return $"世界拡張: {Current.Name}";
    }
}
