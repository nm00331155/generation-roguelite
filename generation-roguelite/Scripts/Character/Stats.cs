using System;
using GenerationRoguelite.Core;

namespace GenerationRoguelite.Character;

public sealed class Stats
{
    public int Vitality { get; private set; }
    public int Intelligence { get; private set; }
    public int Charisma { get; private set; }
    public int Luck { get; private set; }
    public int Wealth { get; private set; }

    public int Sum => Vitality + Intelligence + Charisma + Luck + Wealth;

    public Stats(
        int vitality = 10,
        int intelligence = 10,
        int charisma = 10,
        int luck = 10,
        int wealth = 10)
    {
        Vitality = ClampStat(vitality);
        Intelligence = ClampStat(intelligence);
        Charisma = ClampStat(charisma);
        Luck = ClampStat(luck);
        Wealth = Math.Clamp(wealth, 0, 9_999);
    }

    public void ApplyPhaseYearDelta(LifePhase phase)
    {
        switch (phase)
        {
            case LifePhase.Childhood:
                ApplyDelta(vitality: 1, intelligence: 1, luck: 1);
                break;
            case LifePhase.Youth:
                ApplyDelta(vitality: 1, intelligence: 1, charisma: 1, wealth: 1);
                break;
            case LifePhase.Midlife:
                ApplyDelta(intelligence: 1, charisma: 1, wealth: 1);
                break;
            case LifePhase.Elderly:
                ApplyDelta(vitality: -1, charisma: -1, luck: 1);
                break;
        }
    }

    public void ApplyDelta(
        int vitality = 0,
        int intelligence = 0,
        int charisma = 0,
        int luck = 0,
        int wealth = 0)
    {
        Vitality = ClampStat(Vitality + vitality);
        Intelligence = ClampStat(Intelligence + intelligence);
        Charisma = ClampStat(Charisma + charisma);
        Luck = ClampStat(Luck + luck);
        Wealth = Math.Clamp(Wealth + wealth, 0, 9_999);
    }

    public void ApplyBonus(StatBonus bonus)
    {
        ApplyDelta(bonus.Vitality, bonus.Intelligence, bonus.Charisma, bonus.Luck, bonus.Wealth);
    }

    public int GetValue(string key)
    {
        return key.ToLowerInvariant() switch
        {
            "vitality" or "体力" => Vitality,
            "intelligence" or "知力" => Intelligence,
            "charisma" or "魅力" => Charisma,
            "luck" or "運" => Luck,
            "wealth" or "財力" => Wealth,
            _ => 0,
        };
    }

    public Stats Clone()
    {
        return new Stats(Vitality, Intelligence, Charisma, Luck, Wealth);
    }

    public override string ToString()
    {
        return $"体:{Vitality} 知:{Intelligence} 魅:{Charisma} 運:{Luck} 財:{Wealth}";
    }

    private static int ClampStat(int value)
    {
        return Math.Clamp(value, 1, 999);
    }
}
