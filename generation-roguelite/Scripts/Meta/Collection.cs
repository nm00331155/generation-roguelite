using System;
using System.Collections.Generic;
using System.Linq;

namespace GenerationRoguelite.Meta;

public sealed class CollectionManager
{
    private readonly HashSet<string> _deathCauses = new(StringComparer.Ordinal);
    private readonly HashSet<string> _titles = new(StringComparer.Ordinal);

    public int DeathCauseCount => _deathCauses.Count;

    public int TitleCount => _titles.Count;

    public void RegisterDeathCause(string cause)
    {
        if (string.IsNullOrWhiteSpace(cause))
        {
            return;
        }

        _deathCauses.Add(cause);
    }

    public void RegisterTitles(IEnumerable<string> titles)
    {
        foreach (var title in titles)
        {
            if (!string.IsNullOrWhiteSpace(title))
            {
                _titles.Add(title);
            }
        }
    }

    public string BuildSummary()
    {
        var causes = _deathCauses.Count == 0
            ? "なし"
            : string.Join(" / ", _deathCauses.Take(3));

        return $"死因図鑑:{_deathCauses.Count}件 称号:{_titles.Count}件 ({causes})";
    }
}
