using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GenerationRoguelite.Expansion;

public sealed class CosmeticState
{
    public string ActiveThemeId { get; set; } = "theme_default";

    public int ActiveSeason { get; set; } = 1;

    public List<string> UnlockedThemeIds { get; set; } = ["theme_default"];
}

public sealed class CosmeticManager
{
    private readonly HashSet<string> _unlockedThemes = new(StringComparer.Ordinal)
    {
        "theme_default",
    };

    public int ActiveSeason { get; private set; } = 1;

    public string ActiveThemeId { get; private set; } = "theme_default";

    public IReadOnlyList<string> UnlockedThemes => _unlockedThemes.OrderBy(id => id).ToArray();

    public bool UnlockTheme(string themeId)
    {
        if (string.IsNullOrWhiteSpace(themeId))
        {
            return false;
        }

        return _unlockedThemes.Add(themeId);
    }

    public void ApplyBattlePassReward(string rewardId)
    {
        if (string.IsNullOrWhiteSpace(rewardId))
        {
            return;
        }

        if (UnlockTheme(rewardId))
        {
            ActiveThemeId = rewardId;
        }
    }

    public bool UpdateSeason(int season)
    {
        season = Math.Max(1, season);
        if (ActiveSeason == season)
        {
            return false;
        }

        ActiveSeason = season;
        var seasonalTheme = $"theme_season_{season:00}";
        UnlockTheme(seasonalTheme);
        ActiveThemeId = seasonalTheme;
        return true;
    }

    public Color ResolveBackgroundColor()
    {
        if (ActiveThemeId.Contains("premium", StringComparison.Ordinal))
        {
            return new Color(0.11f, 0.07f, 0.16f);
        }

        if (ActiveThemeId.Contains("season", StringComparison.Ordinal))
        {
            var hue = (ActiveSeason % 8) / 8f;
            return Color.FromHsv(hue, 0.35f, 0.18f);
        }

        if (ActiveThemeId.Contains("free", StringComparison.Ordinal))
        {
            return new Color(0.08f, 0.12f, 0.13f);
        }

        return new Color(0.07f, 0.08f, 0.11f);
    }

    public string BuildSummary()
    {
        return $"コスメ: S{ActiveSeason} / {ActiveThemeId} / 所持{_unlockedThemes.Count}";
    }

    public CosmeticState BuildState()
    {
        return new CosmeticState
        {
            ActiveThemeId = ActiveThemeId,
            ActiveSeason = ActiveSeason,
            UnlockedThemeIds = [.. _unlockedThemes.OrderBy(id => id)],
        };
    }

    public void LoadState(CosmeticState? state)
    {
        _unlockedThemes.Clear();
        _unlockedThemes.Add("theme_default");

        if (state is null)
        {
            ActiveSeason = 1;
            ActiveThemeId = "theme_default";
            return;
        }

        ActiveSeason = Math.Max(1, state.ActiveSeason);

        foreach (var theme in state.UnlockedThemeIds)
        {
            if (!string.IsNullOrWhiteSpace(theme))
            {
                _unlockedThemes.Add(theme);
            }
        }

        ActiveThemeId = _unlockedThemes.Contains(state.ActiveThemeId)
            ? state.ActiveThemeId
            : "theme_default";
    }
}
