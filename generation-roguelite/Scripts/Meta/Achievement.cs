using System.Collections.Generic;
using System.Linq;

namespace GenerationRoguelite.Meta;

public readonly record struct AchievementContext(
    int GenerationCount,
    int MaxAge,
    int LegendaryCount,
    int DeathCauseCount,
    int ActiveLawCount);

public sealed class AchievementManager
{
    private readonly Dictionary<string, string> _catalog = new()
    {
        ["lineage_3"] = "継承者",
        ["lineage_10"] = "十代の家系",
        ["long_life"] = "長寿の証",
        ["legend_hunter"] = "聖遺物蒐集家",
        ["death_lexicon"] = "死因蒐集家",
        ["law_keeper"] = "家訓守り",
    };

    private readonly HashSet<string> _unlocked = [];

    public IReadOnlyCollection<string> UnlockedTitles => _unlocked
        .Select(key => _catalog[key])
        .ToArray();

    public IReadOnlyList<string> Evaluate(AchievementContext context)
    {
        var newlyUnlocked = new List<string>();

        TryUnlock("lineage_3", context.GenerationCount >= 3, newlyUnlocked);
        TryUnlock("lineage_10", context.GenerationCount >= 10, newlyUnlocked);
        TryUnlock("long_life", context.MaxAge >= 80, newlyUnlocked);
        TryUnlock("legend_hunter", context.LegendaryCount >= 3, newlyUnlocked);
        TryUnlock("death_lexicon", context.DeathCauseCount >= 3, newlyUnlocked);
        TryUnlock("law_keeper", context.ActiveLawCount >= 3, newlyUnlocked);

        return newlyUnlocked;
    }

    public string BuildSummary()
    {
        return $"実績: {_unlocked.Count}/{_catalog.Count}";
    }

    private void TryUnlock(string id, bool condition, ICollection<string> output)
    {
        if (!condition || _unlocked.Contains(id))
        {
            return;
        }

        _unlocked.Add(id);
        output.Add(_catalog[id]);
    }
}
