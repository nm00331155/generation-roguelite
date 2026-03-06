using GenerationRoguelite.Core;
using Godot;

namespace GenerationRoguelite.Events;

public sealed class EventData
{
    public string EventText { get; }
    public EventChoice TapChoice { get; }
    public EventChoice SwipeChoice { get; }
    public EventChoice TimeoutChoice { get; }
    public TerrainProfile Terrain { get; }

    public float RemainingSeconds { get; private set; }
    public bool IsExpired => RemainingSeconds <= 0f;

    public EventData(
        string eventText,
        EventChoice tapChoice,
        EventChoice swipeChoice,
        EventChoice timeoutChoice,
        float limitSeconds,
        TerrainProfile? terrain = null)
    {
        EventText = eventText;
        TapChoice = tapChoice;
        SwipeChoice = swipeChoice;
        TimeoutChoice = timeoutChoice;
        Terrain = terrain ?? TerrainProfile.Default;
        RemainingSeconds = limitSeconds;
    }

    public void Tick(double delta)
    {
        RemainingSeconds -= (float)delta;
        if (RemainingSeconds < 0f)
        {
            RemainingSeconds = 0f;
        }
    }
}

public sealed class TerrainProfile
{
    public static readonly TerrainProfile Default = new(0.7f, "enemy", 1.0f);

    public float ObstacleDensity { get; }
    public string ObstacleType { get; }
    public float SpeedModifier { get; }

    public TerrainProfile(float obstacleDensity, string obstacleType, float speedModifier)
    {
        ObstacleDensity = Mathf.Clamp(obstacleDensity, 0.1f, 1.5f);
        ObstacleType = string.IsNullOrWhiteSpace(obstacleType) ? "enemy" : obstacleType;
        SpeedModifier = Mathf.Clamp(speedModifier, 0.6f, 1.6f);
    }

    public static TerrainProfile ForPhase(LifePhase phase)
    {
        return phase switch
        {
            LifePhase.Childhood => new TerrainProfile(0.2f, "none", 1.0f),
            LifePhase.Youth => new TerrainProfile(0.9f, "enemy", 1.0f),
            LifePhase.Midlife => new TerrainProfile(0.8f, "enemy", 1.0f),
            LifePhase.Elderly => new TerrainProfile(0.5f, "hazard", 1.0f),
            _ => Default,
        };
    }
}

public sealed class EventChoice
{
    public string Text { get; }
    public string? CheckStat { get; }
    public int Difficulty { get; }
    public float DropChance { get; }

    public StatDelta SuccessDelta { get; }
    public StatDelta FailDelta { get; }

    public float SuccessLifeDamage { get; }
    public float FailLifeDamage { get; }

    public string SuccessText { get; }
    public string FailText { get; }

    public EventChoice(
        string text,
        string? checkStat,
        int difficulty,
        StatDelta successDelta,
        StatDelta failDelta,
        float successLifeDamage,
        float failLifeDamage,
        string successText,
        string failText,
        float dropChance = 0.5f)
    {
        Text = text;
        CheckStat = checkStat;
        Difficulty = difficulty;
        DropChance = Mathf.Clamp(dropChance, 0f, 1f);
        SuccessDelta = successDelta;
        FailDelta = failDelta;
        SuccessLifeDamage = successLifeDamage;
        FailLifeDamage = failLifeDamage;
        SuccessText = successText;
        FailText = failText;
    }
}

public readonly record struct StatDelta(
    int Vitality,
    int Intelligence,
    int Charisma,
    int Luck,
    int Wealth)
{
    public static readonly StatDelta Zero = new(0, 0, 0, 0, 0);
}

public readonly record struct EventResolution(bool Success, string ResultText);
