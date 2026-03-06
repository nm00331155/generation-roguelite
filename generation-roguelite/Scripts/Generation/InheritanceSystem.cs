using System;
using GenerationRoguelite.Character;
using GenerationRoguelite.Core;

namespace GenerationRoguelite.Generation;

public sealed class InheritanceSystem
{
    public HeritageData BuildHeritage(CharacterData parent, int adBonusWealth = 0)
    {
        var baseHeritage = HeritageData.FromParent(parent);
        var totalSeed = Math.Clamp(baseHeritage.WealthSeed + adBonusWealth, 0, 600);

        return new HeritageData(
            baseHeritage.BonusLifeYears,
            totalSeed,
            baseHeritage.InheritedBonus);
    }

    public StatBonus CalculateAdoptedChildBonus(int generation)
    {
        var vitality = generation % 2 == 0 ? 2 : 1;
        var intelligence = generation % 3 == 0 ? 2 : 1;
        var charisma = generation % 4 == 0 ? 2 : 1;
        var luck = generation % 5 == 0 ? 2 : 1;

        return new StatBonus(vitality, intelligence, charisma, luck, 0);
    }
}
