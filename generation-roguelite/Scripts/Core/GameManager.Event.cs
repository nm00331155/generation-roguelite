using System;
using System.Collections.Generic;
using GenerationRoguelite.Events;
using Godot;

namespace GenerationRoguelite.Core;

public partial class GameManager
{
    private void ResolveActiveEvent(EventChoice choice, bool isTimeout)
    {
        if (_activeEvent is null)
        {
            return;
        }

        _eventLabel.AddThemeColorOverride("font_color", Colors.White);

        var checkBonus = _eraManager.GetCheckBonus(choice.CheckStat) + _familyLawManager.GetCheckBonus(choice.CheckStat);
        if (_bondManager.TryGetAssist(choice.CheckStat, out var assist))
        {
            checkBonus += assist.CheckBonus;
            _lastAssistText = assist.Message;
        }
        else
        {
            _lastAssistText = string.Empty;
        }

        checkBonus -= _eraManager.Current.EventDifficultyOffset;

        var scoreMultiplier = GetCurrentScoreMultiplier();
        var result = _eventManager.Resolve(choice, _character, checkBonus);
        var statDelta = result.Success ? choice.SuccessDelta : choice.FailDelta;
        if (result.Success)
        {
            _character.ClearedEvents += 1;
            _generationScore += Mathf.RoundToInt(20f * scoreMultiplier);
        }
        else
        {
            _character.FailedEvents += 1;
            _generationScore += Mathf.RoundToInt(4f * scoreMultiplier);

            if (!isTimeout && choice.FailLifeDamage >= 2f)
            {
                _currentDeathCause = _phaseManager.CurrentPhase == LifePhase.Elderly ? "病死" : "戦死";
            }

            if (_retryTokenAvailable && !isTimeout)
            {
                _retryTokenAvailable = false;
                _character.Stats.ApplyDelta(luck: 1);
                _lastAssistText += (string.IsNullOrWhiteSpace(_lastAssistText) ? string.Empty : " / ") + "やり直し権で被害軽減";
            }
        }

        var dropped = _inventory.TryRollDrop(
            _phaseManager.CurrentPhase,
            _character.Stats.Luck,
            result.Success,
            choice.DropChance,
            _eraManager.Current.EraName);
        if (dropped.Dropped && dropped.Item is not null)
        {
            _generationScore += dropped.Item.ValueScore;
            _lastDropText = dropped.Message;
            ApplyDropPresentation(dropped.Presentation);
            RecordGenerationEvent($"装備入手: {dropped.Item.Name}", true);
        }
        else if (dropped.PendingReplacement)
        {
            _lastDropText = dropped.Message;
            ApplyDropPresentation(dropped.Presentation);
        }
        else
        {
            _lastDropText = dropped.Message;
        }

        RefreshEquippedBonuses();

        _bondManager.RegisterOutcome(_activeEvent.EventText, result.Success);

        _eventLabel.Text =
            $"{(isTimeout ? "時間切れ" : "選択")} : {choice.Text}\n"
            + result.ResultText
            + (string.IsNullOrWhiteSpace(_lastAssistText) ? string.Empty : $"\n{_lastAssistText}")
            + $"\n{_lastDropText}";

        SpawnStatDeltaFloatText(statDelta);

        AppendRecentEvent(_activeEvent.EventText);
        AppendRecentEvent(result.ResultText);
        RecordGenerationEvent(result.ResultText, result.Success);

        Speak(_navigatorManager.OnEventResolved(result, isTimeout));

        _activeEvent = null;
        _currentTerrain = TerrainProfile.ForPhase(_phaseManager.CurrentPhase);
        _eventCooldown = RollEventCooldown();
        SavePersistentState();
    }

    private void SpawnStatDeltaFloatText(StatDelta delta)
    {
        var parts = new List<string>();
        AppendStatDeltaText(parts, "体", delta.Vitality);
        AppendStatDeltaText(parts, "知", delta.Intelligence);
        AppendStatDeltaText(parts, "魅", delta.Charisma);
        AppendStatDeltaText(parts, "運", delta.Luck);
        AppendStatDeltaText(parts, "財", delta.Wealth);

        if (parts.Count == 0)
        {
            return;
        }

        var total = delta.Vitality + delta.Intelligence + delta.Charisma + delta.Luck + delta.Wealth;
        var color = total >= 0
            ? new Color(0.298f, 0.686f, 0.314f)
            : new Color(0.957f, 0.263f, 0.212f);

        _floatTextSpawner.Spawn(
            string.Join(" ", parts),
            color,
            new Vector2(FloatTextX, FloatTextY));
    }

    private static void AppendStatDeltaText(List<string> parts, string label, int value)
    {
        if (value == 0)
        {
            return;
        }

        var sign = value > 0 ? "+" : string.Empty;
        parts.Add($"{sign}{value}{label}");
    }

    private void OnChoiceTapPressed()
    {
        if (TryHandleInventoryReplacementChoice(0))
        {
            return;
        }

        if (_activeEvent is null)
        {
            return;
        }

        ResolveActiveEvent(_activeEvent.TapChoice, false);
    }

    private void OnChoiceSwipePressed()
    {
        if (TryHandleInventoryReplacementChoice(1))
        {
            return;
        }

        if (_activeEvent is null)
        {
            return;
        }

        ResolveActiveEvent(_activeEvent.SwipeChoice, false);
    }

    private void OnChoiceTimeoutPressed()
    {
        if (TryHandleInventoryReplacementChoice(2))
        {
            return;
        }

        if (_activeEvent is null)
        {
            return;
        }

        ResolveActiveEvent(_activeEvent.TimeoutChoice, false);
    }

    private static EventChoice SelectLowestRiskChoice(EventData eventData)
    {
        EventChoice[] candidates =
        [
            eventData.TapChoice,
            eventData.SwipeChoice,
            eventData.TimeoutChoice,
        ];

        var selected = candidates[0];
        var selectedRisk = CalculateRiskScore(selected);

        for (var i = 1; i < candidates.Length; i++)
        {
            var candidateRisk = CalculateRiskScore(candidates[i]);
            if (candidateRisk < selectedRisk)
            {
                selected = candidates[i];
                selectedRisk = candidateRisk;
            }
        }

        return selected;
    }

    private static float CalculateRiskScore(EventChoice choice)
    {
        var failPenalty =
            Math.Abs(Math.Min(0, choice.FailDelta.Vitality))
            + Math.Abs(Math.Min(0, choice.FailDelta.Intelligence))
            + Math.Abs(Math.Min(0, choice.FailDelta.Charisma))
            + Math.Abs(Math.Min(0, choice.FailDelta.Luck))
            + Math.Abs(Math.Min(0, choice.FailDelta.Wealth));

        var checkRisk = string.IsNullOrWhiteSpace(choice.CheckStat) ? 0f : 0.5f;
        return (choice.FailLifeDamage * 10f) + failPenalty + checkRisk;
    }

    private float RollEventCooldown()
    {
        return _rng.RandfRange(EventIntervalMinSeconds, EventIntervalMaxSeconds);
    }

    private EventGenerationContext BuildGenerationContext()
    {
        return new EventGenerationContext(
            age: _character.Age,
            stats: _character.Stats,
            generation: _character.Generation,
            phase: _phaseManager.CurrentPhase,
            era: $"{_eraManager.Current.EraName}:{_worldExpansionManager.Current.Name}",
            lifePath: _lifePath,
            bonds: _bondManager.GetTopBondNames(3),
            familyTraits: _familyLawManager.GetActiveLawNames(3),
            recentEvents: new List<string>(_recentEvents),
            eraMechanic: $"{_eraManager.Current.Mechanic}/{_worldExpansionManager.Current.PromptTag}");
    }

    private void UpdateEventPanel()
    {
        if (_isInFuneral)
        {
            _eventPanel.Visible = false;
            return;
        }

        var replacement = _inventory.GetPendingReplacementPreview();
        if (replacement is not null)
        {
            _eventPanel.Visible = true;
            _eventBodyLabel.Text = replacement.Value.Summary;
            _choiceTapButton.Text = "A: 入替して採用";
            _choiceSwipeButton.Text = "B: 現状維持(破棄)";
            _choiceTimeoutButton.Text = "C: 詳細";
            _eventTimeBar.MaxValue = 1f;
            _eventTimeBar.Value = 1f;
            return;
        }

        if (_activeEvent is null)
        {
            _eventPanel.Visible = false;
            return;
        }

        _eventPanel.Visible = true;
        _eventBodyLabel.Text = _activeEvent.EventText;
        _choiceTapButton.Text = $"A: {_activeEvent.TapChoice.Text}";
        _choiceSwipeButton.Text = $"B: {_activeEvent.SwipeChoice.Text}";
        _choiceTimeoutButton.Text = $"C: {_activeEvent.TimeoutChoice.Text}";
        _eventTimeBar.MaxValue = EventLimitSeconds;
        _eventTimeBar.Value = _activeEvent.RemainingSeconds;
    }

    private bool TryHandleInventoryReplacementChoice(int choiceIndex)
    {
        if (!_inventory.HasPendingReplacement || _isInFuneral || _activeEvent is not null)
        {
            return false;
        }

        if (choiceIndex == 2)
        {
            var preview = _inventory.GetPendingReplacementPreview();
            if (preview is not null)
            {
                _eventLabel.AddThemeColorOverride("font_color", Colors.White);
                _eventLabel.Text = $"{preview.Value.Summary}\n{preview.Value.Detail}";
            }

            return true;
        }

        var acceptIncoming = choiceIndex == 0;
        var resolution = _inventory.ResolvePendingReplacement(acceptIncoming);
        if (!resolution.Applied)
        {
            return true;
        }

        _lastDropText = resolution.Message;
        _eventLabel.Text = resolution.Message;
        ApplyDropPresentation(resolution.Presentation);
        RefreshEquippedBonuses();
        return true;
    }
}
