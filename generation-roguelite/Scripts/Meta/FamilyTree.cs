using System;
using System.Collections.Generic;
using System.Linq;

namespace GenerationRoguelite.Meta;

public sealed record GenerationRecord(
    int Generation,
    int Age,
    string Era,
    int Score,
    string DeathCause,
    string Highlight,
    string Founder,
    string Heirloom);

public sealed class FamilyTree
{
    private readonly List<GenerationRecord> _records = new();

    public IReadOnlyList<GenerationRecord> Records => _records;

    public int Count => _records.Count;

    public int MaxAge => _records.Count == 0 ? 0 : _records.Max(record => record.Age);

    public void AddRecord(GenerationRecord record)
    {
        _records.Add(record);
    }

    public string BuildRecentSummary(int count = 3)
    {
        if (_records.Count == 0)
        {
            return "家系図: まだ記録なし";
        }

        var recent = _records
            .TakeLast(count)
            .Select(record => $"{record.Generation}世({record.Age}歳/{record.DeathCause})");

        return $"家系図: {string.Join(" -> ", recent)}";
    }

    public string TryBuildLegendAtMilestone()
    {
        if (_records.Count == 0 || _records.Count % 10 != 0)
        {
            return string.Empty;
        }

        var best = _records.OrderByDescending(record => record.Score).First();
        var latest = _records[^1];
        var averageAge = _records.Average(record => record.Age);

        return
            $"家系の伝説: 第{latest.Generation}世で節目を迎えた。"
            + $"最盛期は第{best.Generation}世({best.Score}点)。"
            + $"平均寿命は{averageAge:F1}歳、死因『{latest.DeathCause}』も記録に刻まれた。";
    }
}
