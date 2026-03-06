using System;
using System.Collections.Generic;
using System.Linq;

namespace GenerationRoguelite.Meta;

public sealed class FamilyTeaching
{
    private readonly List<string> _teachings = new();

    public IReadOnlyList<string> Teachings => _teachings;

    public void UnlockByGenerationScore(int generationScore)
    {
        var tier = generationScore switch
        {
            >= 8000 => "逆境で笑え",
            >= 5000 => "先に守り、次に攻めよ",
            >= 2500 => "小さな利を積み上げよ",
            _ => "急がず、止まらず",
        };

        if (_teachings.Contains(tier, StringComparer.Ordinal))
        {
            return;
        }

        _teachings.Add(tier);
    }

    public string BuildSummary(int maxCount = 3)
    {
        if (_teachings.Count == 0)
        {
            return "家訓: なし";
        }

        var sample = string.Join(" / ", _teachings.Take(maxCount));
        return $"家訓:{_teachings.Count}件 ({sample})";
    }
}
