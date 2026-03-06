using System.Collections.Generic;
using GenerationRoguelite.Monetization;
using Godot;

namespace GenerationRoguelite.Core;

public partial class GameManager
{
    private int CalculateRawGenerationScore()
    {
        var events = Mathf.Max(1, _character.ClearedEvents);
        return _character.Age * _character.Stats.Sum * events;
    }

    private int CalculateGenerationScore()
    {
        var events = Mathf.Max(1, _character.ClearedEvents + _character.FailedEvents);
        var raw = _character.Age * _character.Stats.Sum * events + _generationScore;
        return Mathf.RoundToInt(raw * GetCurrentScoreMultiplier());
    }

    private float GetCurrentScoreMultiplier()
    {
        return _inflationBalancer.GetScoreMultiplier(
            _character.Generation,
            _totalScore,
            _eraManager.Current.ScoreMultiplier,
            _worldExpansionManager.Current);
    }

    private string BuildGenerationHighlight()
    {
        if (_recentEvents.Count == 0)
        {
            return "静かな生涯";
        }

        return _recentEvents.Peek();
    }

    private void RecordGenerationEvent(string text, bool important)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        _generationEventLog.Add(text);
        if (_generationEventLog.Count > 64)
        {
            _generationEventLog.RemoveAt(0);
        }

        if (important)
        {
            AppendRecentEvent(text);
        }
    }

    private void AppendRecentEvent(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        _recentEvents.Enqueue(text);
        while (_recentEvents.Count > 3)
        {
            _recentEvents.Dequeue();
        }
    }

    private string ApplyBattlePassRewards(IReadOnlyList<BattlePassReward> rewards)
    {
        if (rewards.Count == 0)
        {
            return string.Empty;
        }

        var messages = new List<string>();
        foreach (var reward in rewards)
        {
            switch (reward.Type)
            {
                case BattlePassRewardType.Cosmetic:
                    _cosmeticManager.ApplyBattlePassReward(reward.RewardId);
                    ApplyTheme();
                    break;

                case BattlePassRewardType.Gold:
                    _pendingInheritanceBonusWealth += 2;
                    break;

                case BattlePassRewardType.Ticket:
                    _retryTokenAvailable = true;
                    break;
            }

            messages.Add($"{(reward.PremiumTrack ? "P" : "F")}:{reward.DisplayName}");
        }

        return $"BP報酬 {string.Join(" / ", messages)}";
    }
}
