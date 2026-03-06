using System;

namespace GenerationRoguelite.Core;

public sealed class PhaseManager
{
    public LifePhase CurrentPhase { get; private set; } = LifePhase.Childhood;

    public event Action<LifePhase>? PhaseChanged;

    public void Reset()
    {
        CurrentPhase = LifePhase.Childhood;
    }

    public void UpdateByAge(int age)
    {
        var nextPhase = ResolvePhase(age);
        if (nextPhase == CurrentPhase)
        {
            return;
        }

        CurrentPhase = nextPhase;
        PhaseChanged?.Invoke(CurrentPhase);
    }

    public static LifePhase ResolvePhase(int age)
    {
        if (age <= 12)
        {
            return LifePhase.Childhood;
        }

        if (age <= 30)
        {
            return LifePhase.Youth;
        }

        if (age <= 55)
        {
            return LifePhase.Midlife;
        }

        return LifePhase.Elderly;
    }

    public static float ObstacleDamageMultiplier(LifePhase phase)
    {
        return phase switch
        {
            LifePhase.Childhood => 0.0f,
            LifePhase.Youth => 1.0f,
            LifePhase.Midlife => 1.5f,
            LifePhase.Elderly => 3.0f,
            _ => 1.0f,
        };
    }

    public static float ObstacleSpeedMultiplier(LifePhase phase)
    {
        return phase switch
        {
            LifePhase.Childhood => 0.0f,
            LifePhase.Youth => 1.0f,
            LifePhase.Midlife => 1.2f,
            LifePhase.Elderly => 0.8f,
            _ => 1.0f,
        };
    }
}
