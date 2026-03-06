using System;
using GenerationRoguelite.Core;

namespace GenerationRoguelite.Character;

public sealed class CharacterData
{
    private const float BaseLifeYears = 75f;

    public int Generation { get; }
    public int Age { get; private set; }
    public float RemainingLifeYears { get; private set; }

    public Stats Stats { get; }

    public int ClearedEvents { get; set; }
    public int FailedEvents { get; set; }

    public bool IsDead => RemainingLifeYears <= 0f;

    public CharacterData(int generation, HeritageData? heritage = null)
    {
        Generation = generation;
        Age = 0;
        Stats = new Stats();

        if (heritage is not null)
        {
            Stats.ApplyDelta(
                vitality: heritage.InheritedBonus.Vitality,
                intelligence: heritage.InheritedBonus.Intelligence,
                charisma: heritage.InheritedBonus.Charisma,
                luck: heritage.InheritedBonus.Luck,
                wealth: heritage.WealthSeed);
        }

        RemainingLifeYears = BaseLifeYears + (heritage?.BonusLifeYears ?? 0);
    }

    public void AdvanceYear(LifePhase phase)
    {
        Age += 1;
        Stats.ApplyPhaseYearDelta(phase);

        RemainingLifeYears -= NaturalDecayFor(phase);
        RemainingLifeYears = MathF.Max(RemainingLifeYears, 0f);
    }

    public void ApplyLifeDamage(float years)
    {
        if (years <= 0f)
        {
            return;
        }

        RemainingLifeYears = MathF.Max(0f, RemainingLifeYears - years);
    }

    public void AdjustRemainingLife(float years)
    {
        if (MathF.Abs(years) <= 0.0001f)
        {
            return;
        }

        RemainingLifeYears = MathF.Max(0f, RemainingLifeYears + years);
    }

    private static float NaturalDecayFor(LifePhase phase)
    {
        return phase switch
        {
            LifePhase.Childhood => 0.5f,
            LifePhase.Youth => 0.8f,
            LifePhase.Midlife => 1.1f,
            LifePhase.Elderly => 1.8f,
            _ => 1.0f,
        };
    }
}
