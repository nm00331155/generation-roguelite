using System;
using GenerationRoguelite.Core;

namespace GenerationRoguelite.Character;

public sealed class HeritageData
{
    public int BonusLifeYears { get; }
    public int WealthSeed { get; }
    public StatBonus InheritedBonus { get; }

    public HeritageData(int bonusLifeYears, int wealthSeed, StatBonus inheritedBonus)
    {
        BonusLifeYears = Math.Clamp(bonusLifeYears, 0, 20);
        WealthSeed = Math.Clamp(wealthSeed, 0, 600);
        InheritedBonus = inheritedBonus;
    }

    public static HeritageData FromParent(CharacterData parent)
    {
        var inherited = new StatBonus(
            Vitality: (int)MathF.Round(parent.Stats.Vitality * 0.35f),
            Intelligence: (int)MathF.Round(parent.Stats.Intelligence * 0.35f),
            Charisma: (int)MathF.Round(parent.Stats.Charisma * 0.35f),
            Luck: (int)MathF.Round(parent.Stats.Luck * 0.35f),
            Wealth: 0);

        var bonusLifeYears = Math.Clamp((int)MathF.Round(parent.Stats.Vitality * 0.15f), 0, 8);
        var wealthSeed = Math.Clamp(parent.Stats.Wealth / 3, 0, 400);

        return new HeritageData(bonusLifeYears, wealthSeed, inherited);
    }
}
