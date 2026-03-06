using System;
using GenerationRoguelite.Core;

namespace GenerationRoguelite.Era;

public enum EraType
{
    Primitive,
    Ancient,
    Medieval,
    EarlyModern,
    Modern,
    Future,
}

public enum WorldState
{
    Normal,
    Calamity,
    GoldenAge,
}

public readonly record struct EraSnapshot(
    EraType Era,
    string EraName,
    string Mechanic,
    string FavoredCheck,
    WorldState WorldState,
    float TerrainDensityOffset,
    float ScoreMultiplier,
    int EventDifficultyOffset,
    StatBonus StartBonus)
{
    public string BuildSummary()
    {
        var state = WorldState switch
        {
            WorldState.Calamity => "災厄",
            WorldState.GoldenAge => "黄金期",
            _ => "平時",
        };

        return $"時代:{EraName} / {Mechanic} / {state}";
    }
}

public sealed class EraManager
{
    private static readonly EraTemplate[] EraTemplates =
    [
        new(EraType.Primitive, "原始", "狩猟", "vitality", new StatBonus(2, 0, 0, 0, 0)),
        new(EraType.Ancient, "古代", "政治", "charisma", new StatBonus(0, 1, 1, 0, 0)),
        new(EraType.Medieval, "中世", "信仰", "luck", new StatBonus(1, 0, 0, 1, 0)),
        new(EraType.EarlyModern, "近世", "発明", "intelligence", new StatBonus(0, 2, 0, 0, 1)),
        new(EraType.Modern, "現代", "投資", "wealth", new StatBonus(0, 0, 0, 1, 2)),
        new(EraType.Future, "未来", "遺伝子改造", "vitality", new StatBonus(1, 1, 1, 1, 1)),
    ];

    public EraSnapshot Current { get; private set; }

    public EraManager()
    {
        Current = BuildSnapshot(1);
    }

    public void UpdateByGeneration(int generation)
    {
        Current = BuildSnapshot(generation);
    }

    public int GetCheckBonus(string? checkStat)
    {
        if (string.IsNullOrWhiteSpace(checkStat))
        {
            return 0;
        }

        var normalized = NormalizeStatKey(checkStat);
        var bonus = normalized == Current.FavoredCheck ? 2 : 0;

        if (Current.WorldState == WorldState.Calamity)
        {
            bonus -= 1;
        }
        else if (Current.WorldState == WorldState.GoldenAge)
        {
            bonus += 1;
        }

        return bonus;
    }

    private static EraSnapshot BuildSnapshot(int generation)
    {
        var eraIndex = Math.Clamp((generation - 1) / 3, 0, EraTemplates.Length - 1);
        var template = EraTemplates[eraIndex];
        var worldState = ResolveWorldState(generation);

        var densityOffset = worldState switch
        {
            WorldState.Calamity => 0.15f,
            WorldState.GoldenAge => -0.12f,
            _ => 0f,
        };

        var scoreMultiplier = worldState switch
        {
            WorldState.Calamity => 0.9f,
            WorldState.GoldenAge => 1.25f,
            _ => 1f,
        };

        var eventDifficultyOffset = worldState switch
        {
            WorldState.Calamity => 2,
            WorldState.GoldenAge => -1,
            _ => 0,
        };

        var worldBonus = worldState switch
        {
            WorldState.Calamity => new StatBonus(-1, 0, 0, 0, 0),
            WorldState.GoldenAge => new StatBonus(0, 1, 0, 1, 1),
            _ => StatBonus.Zero,
        };

        return new EraSnapshot(
            template.Era,
            template.EraName,
            template.Mechanic,
            template.FavoredCheck,
            worldState,
            densityOffset,
            scoreMultiplier,
            eventDifficultyOffset,
            template.StartBonus + worldBonus);
    }

    private static WorldState ResolveWorldState(int generation)
    {
        if (generation % 8 == 0)
        {
            return WorldState.GoldenAge;
        }

        if (generation % 5 == 0)
        {
            return WorldState.Calamity;
        }

        return WorldState.Normal;
    }

    private static string NormalizeStatKey(string key)
    {
        return key.ToLowerInvariant() switch
        {
            "体力" => "vitality",
            "知力" => "intelligence",
            "魅力" => "charisma",
            "運" => "luck",
            "財力" => "wealth",
            _ => key.ToLowerInvariant(),
        };
    }

    private readonly record struct EraTemplate(
        EraType Era,
        string EraName,
        string Mechanic,
        string FavoredCheck,
        StatBonus StartBonus);
}
