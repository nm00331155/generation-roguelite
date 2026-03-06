using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GenerationRoguelite.Meta;

public readonly record struct BondAssist(string BondName, int CheckBonus, string Message);

public sealed class BondManager
{
    private readonly Dictionary<string, int> _bondValues = new(StringComparer.Ordinal)
    {
        ["旅人"] = 15,
        ["鍛冶屋"] = 12,
        ["薬師"] = 10,
    };

    private readonly RandomNumberGenerator _rng = new();

    public BondManager()
    {
        _rng.Randomize();
    }

    public void RegisterOutcome(string eventText, bool success)
    {
        if (_bondValues.Count == 0)
        {
            return;
        }

        var target = PickBondByEvent(eventText);
        var delta = success ? 2 : -1;

        _bondValues[target] = Math.Clamp(_bondValues[target] + delta, 0, 100);
    }

    public bool TryGetAssist(string? checkStat, out BondAssist assist)
    {
        assist = default;

        if (string.IsNullOrWhiteSpace(checkStat) || _bondValues.Count == 0)
        {
            return false;
        }

        var strongest = _bondValues
            .OrderByDescending(pair => pair.Value)
            .First();

        var chance = 0.04f + strongest.Value * 0.003f;
        if (_rng.Randf() > chance)
        {
            return false;
        }

        var bonus = 1 + strongest.Value / 35;
        assist = new BondAssist(
            strongest.Key,
            bonus,
            $"盟友「{strongest.Key}」が支援! 判定+{bonus}");

        return true;
    }

    public IReadOnlyList<string> GetTopBondNames(int count)
    {
        return _bondValues
            .OrderByDescending(pair => pair.Value)
            .Take(count)
            .Select(pair => pair.Key)
            .ToArray();
    }

    public string BuildSummary()
    {
        var top = _bondValues
            .OrderByDescending(pair => pair.Value)
            .Take(2)
            .Select(pair => $"{pair.Key}:{pair.Value}");

        return $"縁: {string.Join(" / ", top)}";
    }

    private string PickBondByEvent(string eventText)
    {
        foreach (var key in _bondValues.Keys)
        {
            if (eventText.Contains(key, StringComparison.Ordinal))
            {
                return key;
            }
        }

        var keys = _bondValues.Keys.ToArray();
        return keys[(int)_rng.RandiRange(0, keys.Length - 1)];
    }
}
