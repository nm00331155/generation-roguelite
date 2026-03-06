using System;
using System.Collections.Generic;
using GenerationRoguelite.Character;
using Godot;

namespace GenerationRoguelite.Meta;

public sealed record GenerationHistoryEntry(
    string Name,
    int Age,
    string DeathCause,
    string LifePath,
    Stats FinalStats,
    int GenerationScore,
    IReadOnlyList<string> ClearedEvents,
    IReadOnlyList<string> Equipments,
    bool LineageExtinct);

public sealed class GenerationHistory
{
    private const float BaseMultiplier = 1.0f;
    private const float MultiplierStep = 0.1f;
    private const float MultiplierCap = 2.0f;

    private readonly List<GenerationHistoryEntry> _entries = [];

    public IReadOnlyList<GenerationHistoryEntry> Entries => _entries;

    public void AddEntry(GenerationHistoryEntry entry)
    {
        _entries.Add(entry);
    }

    public int CalculateCumulativeScore()
    {
        if (_entries.Count == 0)
        {
            return 0;
        }

        var total = 0f;
        var streak = 0;
        foreach (var entry in _entries)
        {
            streak = entry.LineageExtinct ? 1 : streak + 1;
            var multiplier = ResolveMultiplier(streak);
            total += entry.GenerationScore * multiplier;
        }

        return Mathf.RoundToInt(total);
    }

    public string BuildSummary()
    {
        if (_entries.Count == 0)
        {
            return "世代履歴: なし";
        }

        var latest = _entries[^1];
        var cumulative = CalculateCumulativeScore();
        return $"世代履歴: {_entries.Count}件 / 最新 {latest.Name}({latest.Age}歳) / 累計 {cumulative}";
    }

    private static float ResolveMultiplier(int streak)
    {
        if (streak <= 1)
        {
            return BaseMultiplier;
        }

        var value = BaseMultiplier + (streak - 1) * MultiplierStep;
        return MathF.Min(value, MultiplierCap);
    }
}