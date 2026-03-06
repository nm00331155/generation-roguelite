using System;
using System.Collections.Generic;
using System.Linq;

namespace GenerationRoguelite.Meta;

public sealed class DeathCollection
{
    private readonly HashSet<string> _causes = new(StringComparer.Ordinal);

    public int Count => _causes.Count;

    public void Register(string cause)
    {
        if (string.IsNullOrWhiteSpace(cause))
        {
            return;
        }

        _causes.Add(cause.Trim());
    }

    public IReadOnlyList<string> GetAll()
    {
        return _causes.OrderBy(value => value).ToList();
    }

    public string BuildSummary(int maxCount = 3)
    {
        if (_causes.Count == 0)
        {
            return "死因図鑑: なし";
        }

        var sample = string.Join(" / ", _causes.Take(maxCount));
        return $"死因図鑑:{_causes.Count}件 ({sample})";
    }
}
