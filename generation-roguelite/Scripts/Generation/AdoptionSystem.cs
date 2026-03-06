using GenerationRoguelite.Core;

namespace GenerationRoguelite.Generation;

public sealed class AdoptionSystem
{
    public bool ShouldForceAdoption(bool hasPartner, int age)
    {
        return !hasPartner && age >= 56;
    }

    public StatBonus BuildAdoptedSkillBonus(int generation)
    {
        var vitality = generation % 2 == 0 ? 1 : 0;
        var intelligence = generation % 3 == 0 ? 1 : 0;
        var charisma = generation % 4 == 0 ? 1 : 0;
        var luck = generation % 5 == 0 ? 2 : 1;
        return new StatBonus(vitality, intelligence, charisma, luck, 0);
    }

    public string DescribeAdoptedSkill(StatBonus bonus)
    {
        if (bonus.Luck >= 2)
        {
            return "天運の才";
        }

        if (bonus.Intelligence > 0)
        {
            return "学識の芽";
        }

        if (bonus.Charisma > 0)
        {
            return "交渉の才";
        }

        return "堅実な体力";
    }
}
