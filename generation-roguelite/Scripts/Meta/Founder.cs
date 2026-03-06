using System.Collections.Generic;
using GenerationRoguelite.Character;
using GenerationRoguelite.Core;

namespace GenerationRoguelite.Meta;

public sealed record FounderArchetype(
    string Name,
    StatBonus StartBonus,
    int UnlockGeneration,
    int UnlockDeathCauseCount);

public sealed class FounderManager
{
    private readonly List<FounderArchetype> _catalog =
    [
        new("平凡な始祖", StatBonus.Zero, 0, 0),
        new("戦士の始祖", new StatBonus(4, 0, 1, 0, 0), 5, 1),
        new("商人の始祖", new StatBonus(0, 1, 1, 0, 5), 7, 2),
        new("呪われた始祖", new StatBonus(0, 0, 0, 5, 0), 10, 3),
    ];

    public FounderArchetype CurrentFounder { get; private set; }

    public FounderManager()
    {
        CurrentFounder = _catalog[0];
    }

    public void UpdateUnlocks(int generationCount, int deathCauseCount)
    {
        var selected = _catalog[0];
        foreach (var founder in _catalog)
        {
            if (generationCount >= founder.UnlockGeneration
                && deathCauseCount >= founder.UnlockDeathCauseCount)
            {
                selected = founder;
            }
        }

        CurrentFounder = selected;
    }

    public void ApplyInitialFounderBonus(Stats stats, int generation)
    {
        if (generation != 1)
        {
            return;
        }

        stats.ApplyBonus(CurrentFounder.StartBonus);
    }

    public string BuildSummary()
    {
        return $"始祖: {CurrentFounder.Name}";
    }
}
