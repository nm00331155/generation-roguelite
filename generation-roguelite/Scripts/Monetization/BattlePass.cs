using System;
using System.Collections.Generic;

namespace GenerationRoguelite.Monetization;

public enum BattlePassRewardType
{
    Gold,
    Cosmetic,
    Ticket,
}

public readonly record struct BattlePassReward(
    int Level,
    bool PremiumTrack,
    BattlePassRewardType Type,
    string RewardId,
    string DisplayName);

public sealed class BattlePassState
{
    public int Season { get; set; } = 1;

    public int Points { get; set; }

    public int Level { get; set; } = 1;
}

public sealed class BattlePassManager
{
    private const int PointsPerLevel = 100;
    private const int MaxLevel = 50;

    public int Season { get; private set; } = 1;

    public int Points { get; private set; }

    public int Level { get; private set; } = 1;

    public bool PremiumEnabled { get; private set; }

    public void SetPremiumEnabled(bool enabled)
    {
        PremiumEnabled = enabled;
    }

    public bool UpdateSeasonByGeneration(int generation)
    {
        var targetSeason = Math.Max(1, 1 + (generation - 1) / 6);
        if (targetSeason == Season)
        {
            return false;
        }

        Season = targetSeason;
        Points = 0;
        Level = 1;
        return true;
    }

    public IReadOnlyList<BattlePassReward> AddGenerationProgress(int generationScore)
    {
        var earned = Math.Clamp(generationScore / 120, 6, 80);
        Points += earned;

        var targetLevel = Math.Clamp(1 + Points / PointsPerLevel, 1, MaxLevel);
        if (targetLevel <= Level)
        {
            return [];
        }

        var rewards = new List<BattlePassReward>();
        for (var lv = Level + 1; lv <= targetLevel; lv++)
        {
            rewards.Add(BuildFreeReward(lv));
            if (PremiumEnabled)
            {
                rewards.Add(BuildPremiumReward(lv));
            }
        }

        Level = targetLevel;
        return rewards;
    }

    public string BuildSummary()
    {
        return $"BP S{Season} Lv{Level} Pt{Points} {(PremiumEnabled ? "Premium" : "Free")}";
    }

    public BattlePassState BuildState()
    {
        return new BattlePassState
        {
            Season = Season,
            Points = Points,
            Level = Level,
        };
    }

    public void LoadState(BattlePassState? state)
    {
        Season = Math.Max(1, state?.Season ?? 1);
        Points = Math.Max(0, state?.Points ?? 0);
        Level = Math.Clamp(state?.Level ?? 1, 1, MaxLevel);
    }

    private static BattlePassReward BuildFreeReward(int level)
    {
        if (level % 3 == 0)
        {
            return new BattlePassReward(
                level,
                PremiumTrack: false,
                BattlePassRewardType.Cosmetic,
                RewardId: $"theme_free_{level:00}",
                DisplayName: $"無料テーマ Lv{level}");
        }

        return new BattlePassReward(
            level,
            PremiumTrack: false,
            BattlePassRewardType.Gold,
            RewardId: $"gold_{level:00}",
            DisplayName: $"ゴールド補給 Lv{level}");
    }

    private static BattlePassReward BuildPremiumReward(int level)
    {
        if (level % 2 == 0)
        {
            return new BattlePassReward(
                level,
                PremiumTrack: true,
                BattlePassRewardType.Cosmetic,
                RewardId: $"theme_premium_{level:00}",
                DisplayName: $"限定テーマ Lv{level}");
        }

        return new BattlePassReward(
            level,
            PremiumTrack: true,
            BattlePassRewardType.Ticket,
            RewardId: $"ticket_{level:00}",
            DisplayName: $"レアチケット Lv{level}");
    }
}
