using System;
using System.Collections.Generic;
using System.Linq;
using GenerationRoguelite.Character;
using GenerationRoguelite.Core;

namespace GenerationRoguelite.Meta;

public sealed record FamilyLawDefinition(
    string Id,
    string Name,
    int Cost,
    StatBonus StartBonus,
    string? CheckStat,
    int CheckBonus);

public sealed class FamilyLawManager
{
    private const int MaxActiveLaws = 20;

    private readonly List<FamilyLawDefinition> _catalog =
    [
        new("frugal", "質素倹約の家訓", 6, new StatBonus(0, 0, 0, 0, 5), "wealth", 1),
        new("martial", "武芸奨励の家訓", 8, new StatBonus(2, 0, 0, 0, 0), "vitality", 1),
        new("marriage", "婚姻重視の家訓", 10, new StatBonus(0, 0, 2, 0, 0), "charisma", 2),
        new("study", "学問尊重の家訓", 12, new StatBonus(0, 2, 0, 0, 0), "intelligence", 2),
        new("fortune", "運命受容の家訓", 14, new StatBonus(0, 0, 0, 2, 0), "luck", 2),
        new("prosperity", "繁栄追求の家訓", 16, new StatBonus(1, 1, 1, 1, 3), "wealth", 2),
    ];

    private readonly List<FamilyLawDefinition> _active = new();

    public int LegacyPoints { get; private set; }
    public IReadOnlyList<FamilyLawDefinition> ActiveLaws => _active;
    public string LastUnlockMessage { get; private set; } = string.Empty;

    public void RegisterGenerationResult(int generationScore)
    {
        LegacyPoints += Math.Clamp(generationScore / 900, 1, 14);
        TryUnlockOne();
    }

    public void ApplyGenerationStartBonuses(Stats stats)
    {
        foreach (var law in _active)
        {
            stats.ApplyBonus(law.StartBonus);
        }
    }

    public int GetCheckBonus(string? checkStat)
    {
        if (string.IsNullOrWhiteSpace(checkStat))
        {
            return 0;
        }

        var normalized = NormalizeStatKey(checkStat);

        var total = 0;
        foreach (var law in _active)
        {
            if (law.CheckStat == normalized)
            {
                total += law.CheckBonus;
            }
        }

        return total;
    }

    public IReadOnlyList<string> GetActiveLawNames(int maxCount)
    {
        return _active
            .Take(maxCount)
            .Select(law => law.Name)
            .ToArray();
    }

    public string BuildSummary()
    {
        if (_active.Count == 0)
        {
            return $"家訓: なし (Pt:{LegacyPoints})";
        }

        var sample = string.Join(" / ", _active.Take(2).Select(law => law.Name));
        return $"家訓:{_active.Count}件 Pt:{LegacyPoints} ({sample})";
    }

    private void TryUnlockOne()
    {
        LastUnlockMessage = string.Empty;

        if (_active.Count >= MaxActiveLaws)
        {
            return;
        }

        foreach (var law in _catalog)
        {
            if (_active.Any(active => active.Id == law.Id))
            {
                continue;
            }

            if (LegacyPoints < law.Cost)
            {
                continue;
            }

            LegacyPoints -= law.Cost;
            _active.Add(law);
            LastUnlockMessage = $"家訓制定: {law.Name}";
            return;
        }
    }

    private static string NormalizeStatKey(string stat)
    {
        return stat.ToLowerInvariant() switch
        {
            "体力" => "vitality",
            "知力" => "intelligence",
            "魅力" => "charisma",
            "運" => "luck",
            "財力" => "wealth",
            _ => stat.ToLowerInvariant(),
        };
    }
}
