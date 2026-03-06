using System;
using System.Linq;
using GenerationRoguelite.Character;
using GenerationRoguelite.Events;
using GenerationRoguelite.Meta;
using Godot;

namespace GenerationRoguelite.Core;

public partial class GameManager
{
    private void BeginFuneral()
    {
        if (_isInFuneral)
        {
            return;
        }

        _isInFuneral = true;
        _avoidWindowRemaining = 0f;
        _activeEvent = null;
        _playerVerticalVelocity = 0f;
        _player.Position = new Vector2(_player.Position.X, _playerGroundY);
        _playerVisual.Rotation = 0f;
        _obstacleSpawner.ClearAll();
        ClearCollectibles();
        _dropFlashRemaining = 0f;
        _dropFlashRect.Visible = false;
        _dropFlashRect.Color = new Color(1f, 1f, 1f, 0f);

        var canUseAdoption = !_adoptionUsed;
        _lineageExtinct = !_hasSpouse && !canUseAdoption;
        _forcedAdoption = !_hasSpouse && canUseAdoption;
        _queuedHeirloom = _inventory.SelectHeirloomForNextGeneration();

        _lastGenerationRawScore = CalculateRawGenerationScore();
        _lastGenerationFinalScore = CalculateGenerationScore();

        _generationHistory.AddEntry(
            new GenerationHistoryEntry(
                Name: _currentCharacterName,
                Age: _character.Age,
                DeathCause: _currentDeathCause,
                LifePath: _lifePath,
                FinalStats: _character.Stats.Clone(),
                GenerationScore: _lastGenerationRawScore,
                ClearedEvents: [.. _generationEventLog],
                Equipments: _inventory.CurrentItems
                    .Select(item => item.Name)
                    .Concat(_inventory.EquippedItems.Values.Select(item => $"装備中:{item.Name}"))
                    .ToArray(),
                LineageExtinct: _lineageExtinct));

        _totalScore = _generationHistory.CalculateCumulativeScore();

        _familyLawManager.RegisterGenerationResult(_lastGenerationFinalScore);
        _collectionManager.RegisterDeathCause(_currentDeathCause);

        var record = new GenerationRecord(
            Generation: _character.Generation,
            Age: _character.Age,
            Era: _eraManager.Current.EraName,
            Score: _lastGenerationFinalScore,
            DeathCause: _currentDeathCause,
            Highlight: BuildGenerationHighlight(),
            Founder: _founderManager.CurrentFounder.Name,
            Heirloom: _queuedHeirloom is null
                ? "なし"
                : $"{_queuedHeirloom.Name}(+{_inventory.ActiveHeirloomLevel})");
        _familyTree.AddRecord(record);

        var unlockedTitles = _achievementManager.Evaluate(
            new AchievementContext(
                GenerationCount: _familyTree.Count,
                MaxAge: _familyTree.MaxAge,
                LegendaryCount: _inventory.LegendaryCount,
                DeathCauseCount: _collectionManager.DeathCauseCount,
                ActiveLawCount: _familyLawManager.ActiveLaws.Count));
        _collectionManager.RegisterTitles(unlockedTitles);

        _founderManager.UpdateUnlocks(_familyTree.Count, _collectionManager.DeathCauseCount);

        var legend = _familyTree.TryBuildLegendAtMilestone();
        if (!string.IsNullOrWhiteSpace(legend))
        {
            _legendText = legend;
        }

        _asyncSocialManager.SubmitGenerationScore(
            _character.Generation,
            _lastGenerationFinalScore,
            _founderManager.CurrentFounder.Name,
            _legendText);

        var seasonChanged = _battlePassManager.UpdateSeasonByGeneration(_character.Generation);
        if (seasonChanged)
        {
            _cosmeticManager.UpdateSeason(_battlePassManager.Season);
            ApplyTheme();
        }

        var battlePassRewards = _battlePassManager.AddGenerationProgress(_lastGenerationFinalScore);
        var battlePassSummary = ApplyBattlePassRewards(battlePassRewards);

        var unlockText = string.IsNullOrWhiteSpace(_familyLawManager.LastUnlockMessage)
            ? string.Empty
            : $"\n{_familyLawManager.LastUnlockMessage}";

        var titleText = unlockedTitles.Count == 0
            ? string.Empty
            : $"\n実績解除: {string.Join(" / ", unlockedTitles)}";

        var heirloomText = _queuedHeirloom is null
            ? string.Empty
            : $"\n家宝継承: {_queuedHeirloom.Name} (+{_inventory.ActiveHeirloomLevel})";

        var interstitialShown = _adManager.TryShowFuneralInterstitialPlaceholder(_character.Generation, out var interstitialMessage);
        var adText = interstitialShown ? interstitialMessage : interstitialMessage;

        var passText = string.IsNullOrWhiteSpace(battlePassSummary)
            ? string.Empty
            : $"\n{battlePassSummary}";

        _eventLabel.Text =
            $"第{_character.Generation}世代が生涯を終えた...\n"
            + $"世代スコア +{_lastGenerationFinalScore} / 次世代へ継承"
            + unlockText
            + titleText
            + heirloomText
            + $"\n{adText}"
            + passText
            + "\n弔い画面へ遷移中...";

        SetupFuneralOverlay(interstitialMessage);

        Speak(_navigatorManager.OnGenerationEnd());
        SavePersistentState();
    }

    private void StartGeneration(HeritageData? heritage)
    {
        _character = new CharacterData(_nextGeneration, heritage);
        _nextGeneration += 1;
        _currentCharacterName = $"継承者{_character.Generation:D2}";
        _currentInheritanceSeed = heritage?.WealthSeed ?? 0;

        _timeManager.Reset();
        _phaseManager.Reset();
        _phaseManager.UpdateByAge(_character.Age);

        _eventManager.ResetCache();
        _ddaController.Reset();
        _recentEvents.Clear();
        _generationEventLog.Clear();
        _inventory.ResetGeneration();

        _lifePath = "未選択";
        _lifePathSelected = false;
        _currentDeathCause = "老衰";
        _regretShown = false;
        _lastDropText = "ドロップなし";
        _lastAssistText = string.Empty;
        _retryTokenAvailable = false;
        _shopSlotBonus = 0;
        _spouseEventCooldown = 0f;
        _hasSpouse = false;
        _spouseName = "未婚";
        _spouseVitality = 10;
        _spouseIntelligence = 10;
        _spouseCharisma = 10;
        _spouseLuck = 10;
        _partnerAttemptCount = 0;
        _forcedAdoption = false;
        _birthAdBonus = 0;
        _adoptedPreviewBonus = StatBonus.Zero;
        _adoptedPreviewName = "なし";
        _adoptedBaseBonus = StatBonus.Zero;
        _nextChildIsBiological = true;
        _lineageExtinct = false;
        _willCandidateIndex = 0;
        _willStatusText = string.Empty;
        _willCandidates.Clear();

        _eraManager.UpdateByGeneration(_character.Generation);
        _worldExpansionManager.UpdateByProgress(_character.Generation, _totalScore);

        if (_battlePassManager.UpdateSeasonByGeneration(_character.Generation))
        {
            _cosmeticManager.UpdateSeason(_battlePassManager.Season);
            ApplyTheme();
        }

        _founderManager.ApplyInitialFounderBonus(_character.Stats, _character.Generation);
        _character.Stats.ApplyBonus(_eraManager.Current.StartBonus);
        _familyLawManager.ApplyGenerationStartBonuses(_character.Stats);
        if (_queuedHeirloom is not null)
        {
            _character.Stats.ApplyBonus(_queuedHeirloom.TotalStatBonus);
            _character.AdjustRemainingLife(_queuedHeirloom.LifespanModifier);
        }

        if (_pendingInheritanceBonusWealth > 0)
        {
            _character.Stats.ApplyDelta(wealth: _pendingInheritanceBonusWealth);
            _pendingInheritanceBonusWealth = 0;
        }

        if (_pendingAdoptedSkillBonus != StatBonus.Zero)
        {
            _character.Stats.ApplyBonus(_pendingAdoptedSkillBonus);
            _pendingAdoptedSkillBonus = StatBonus.Zero;
        }

        if (_pendingAdoptedBaseBonus != StatBonus.Zero)
        {
            _character.Stats.ApplyBonus(_pendingAdoptedBaseBonus);
            _pendingAdoptedBaseBonus = StatBonus.Zero;
        }

        var socialOfferText = string.Empty;
        if (_asyncSocialManager.TryConsumeBondOffer(out var socialBonus, out var socialMessage))
        {
            _character.Stats.ApplyBonus(socialBonus);
            socialOfferText = socialMessage;
        }

        _currentTerrain = TerrainProfile.ForPhase(_phaseManager.CurrentPhase);
        _obstacleSpawner.Reset();
        _obstacleSpawner.SetTerrain(
            _currentTerrain,
            _eraManager.Current.TerrainDensityOffset + _worldExpansionManager.Current.TerrainDensityOffset);

        _activeEvent = null;
        _eventCooldown = RollEventCooldown();
        _avoidWindowRemaining = 0f;
        _parryWindowRemaining = 0f;
        _elderlyCaneRemaining = 0f;
        _walkSpeedScale = 1f;
        _playerVerticalVelocity = 0f;
        _attackAnimationRemaining = 0f;
        _player.Position = new Vector2(_player.Position.X, _playerGroundY);
        _playerVisual.Rotation = 0f;
        ClearCollectibles();
        _collectibleSpawnCooldown = _rng.RandfRange(CollectibleSpawnMinSeconds, CollectibleSpawnMaxSeconds);
        _appliedEquipmentBonus = StatBonus.Zero;
        _appliedEquipmentLifespanModifier = 0;
        _generationScore = 0;
        _isInFuneral = false;
        _funeralPanelVisible = false;
        _funeralFadeProgress = 0f;
        _funeralPanelDelay = 0f;
        _funeralOverlay.Visible = false;
        _funeralPanel.Visible = false;
        _nextGenerationPanel.Visible = false;
        _nextGenerationButton.Visible = false;
        _willButton.Visible = false;
        _willPanel.Visible = false;
        _funeralFadeRect.Color = new Color(0f, 0f, 0f, 0f);
        _phaseSlowMotionRemaining = 0f;
        _phaseBannerElapsed = 0f;
        _phaseBannerActive = false;
        _phaseBannerLabel.Visible = false;
        _phaseBannerLabel.Position = new Vector2(PhaseBannerStartX, _phaseBannerBaseY);
        _phaseBannerLabel.Modulate = Colors.White;
        _dropFlashRemaining = 0f;
        _dropFlashRect.Visible = false;
        _dropFlashRect.Color = new Color(1f, 1f, 1f, 0f);
        _lastPhaseForTransition = _phaseManager.CurrentPhase;
        Engine.TimeScale = 1f;

        _eventLabel.Text =
            $"{_currentCharacterName} の人生が始まった。\n"
            + _eraManager.Current.BuildSummary()
            + $" / {_worldExpansionManager.BuildSummary()}";

        if (_queuedHeirloom is not null)
        {
            _eventLabel.Text += $"\n継承家宝: {_queuedHeirloom.Name} (+{_inventory.ActiveHeirloomLevel})";
        }

        if (!string.IsNullOrWhiteSpace(socialOfferText))
        {
            _eventLabel.Text += $"\n{socialOfferText}";
        }

        if (_pendingAdoptedSkillName != "なし")
        {
            _eventLabel.Text += $"\n養子スキル: {_pendingAdoptedSkillName}";
            _pendingAdoptedSkillName = "なし";
        }

        RecordGenerationEvent($"{_currentCharacterName}が誕生", true);

        ApplyPlayerVisualForPhase(_phaseManager.CurrentPhase);
        ApplyPhaseHudTheme(_phaseManager.CurrentPhase);
        Speak(_navigatorManager.OnGenerationStart());
        SavePersistentState();
    }

    private void TickFuneralSequence(double delta)
    {
        if (!_funeralPanelVisible)
        {
            _funeralFadeProgress += (float)delta / FuneralFadeDuration;
            var alpha = Mathf.Clamp(_funeralFadeProgress, 0f, 1f) * 0.85f;
            _funeralFadeRect.Color = new Color(0f, 0f, 0f, alpha);

            if (_funeralFadeProgress < 1f)
            {
                return;
            }

            _funeralPanelVisible = true;
            _funeralPanel.Visible = true;
            _funeralPanelDelay = 0f;
            return;
        }

        if (_nextGenerationPanel.Visible)
        {
            return;
        }

        _funeralPanelDelay += (float)delta;
        if (_funeralPanelDelay >= FuneralDisplayDelaySeconds)
        {
            _nextGenerationButton.Visible = true;
        }
    }

    private void SetupFuneralOverlay(string interstitialMessage)
    {
        _funeralOverlay.Visible = true;
        _funeralPanel.Visible = false;
        _nextGenerationPanel.Visible = false;
        _nextGenerationButton.Visible = false;
        _funeralPanelVisible = false;
        _funeralFadeProgress = 0f;
        _funeralPanelDelay = 0f;
        _funeralFadeRect.Color = new Color(0f, 0f, 0f, 0f);

        _funeralSummaryLabel.Text =
            $"故人: {_currentCharacterName} / {_character.Age}歳 / 人生パス: {_lifePath}\n"
            + $"伴侶: {_spouseName}";
        _funeralDeathCauseLabel.Text = $"死因: {_currentDeathCause}";
        _funeralSnapshotLabel.Text = BuildSnapshotText();
        _funeralScoreLabel.Text =
            $"世代スコア(素点): {_lastGenerationRawScore}\n"
            + $"世代スコア(最終): {_lastGenerationFinalScore}\n"
            + $"累計スコア(世代倍率適用): {_totalScore}";
        _funeralInterstitialLabel.Text = interstitialMessage;
    }

    private string BuildSnapshotText()
    {
        if (_generationEventLog.Count == 0)
        {
            return "ハイライト\n◇ 静かな生涯\n◇ 記録なし\n◇ 記録なし";
        }

        var picks = _generationEventLog
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Distinct()
            .Take(3)
            .Select(text => $"◇ {TrimForSnapshot(text)}")
            .ToList();

        while (picks.Count < 3)
        {
            picks.Add("◇ 記録なし");
        }

        return "ハイライト\n" + string.Join("\n", picks);
    }

    private static string TrimForSnapshot(string text)
    {
        var normalized = text.Replace('\n', ' ').Trim();
        return normalized.Length <= 24 ? normalized : normalized[..24] + "...";
    }

    private void OnNextGenerationButtonPressed()
    {
        _funeralPanel.Visible = false;
        _nextGenerationPanel.Visible = true;

        if (_lineageExtinct)
        {
            _typeButtons.Visible = false;
            _actionButtons.Visible = true;
            _birthButton.Text = "新たな家系を始める";
            _birthAdBonusButton.Visible = !_lastResortUsed;
            _birthAdBonusButton.Text = "最後の養子(広告)";

            _inheritanceLabel.Text = $"累計スコア: {_totalScore}";
            _childTypeLabel.Text = "この家系は途絶えた。";
            _childPreviewLabel.Text = _lastResortUsed
                ? "最後の養子は使用済みです。新しい家系を始めてください。"
                : "広告を見て最後の養子を迎えるか、新しい家系を始められます。";
            _uniqueSkillLabel.Text = $"総世代数: {_familyTree.Count} / 最長寿: {_familyTree.MaxAge}";

            Speak(_navigatorManager.OnGameOver());
            return;
        }

        _typeButtons.Visible = true;
        _actionButtons.Visible = true;
        _birthButton.Text = "誕生";
        _birthAdBonusButton.Visible = true;
        _birthAdBonusButton.Text = "広告を見て遺産ボーナス";

        if (_hasSpouse)
        {
            _nextChildIsBiological = true;
            _biologicalButton.Disabled = false;
            _adoptedButton.Disabled = true;
        }
        else
        {
            _nextChildIsBiological = false;
            _biologicalButton.Disabled = true;
            _adoptedButton.Disabled = false;
        }

        RefreshNextGenerationPreview();
    }

}
