using GenerationRoguelite.Action;
using GenerationRoguelite.Character;
using Godot;

namespace GenerationRoguelite.Core;

public partial class GameManager
{
    private void UpdateHud()
    {
        var scaledTotalScore = _inflationBalancer.FormatTotalScore(_totalScore, _character.Generation);

        _ageLabel.Text = $"年齢: {_character.Age}歳  フェーズ: {PhaseToText(_phaseManager.CurrentPhase)}";
        _phaseLabel.Text = $"フェーズ: {PhaseToText(_phaseManager.CurrentPhase)}";
        _lifeLabel.Text = $"残り寿命: {_character.RemainingLifeYears:F1}年";
        if (_lifeGauge is not null)
        {
            var lifeRatio = Mathf.Clamp(_character.RemainingLifeYears / 100f, 0f, 1f);
            _lifeGauge.Value = lifeRatio * 100f;
            _lifeGauge.Modulate = lifeRatio switch
            {
                > 0.8f => new Color(1f, 0.62f, 0.24f),
                > 0.5f => new Color(0.95f, 0.86f, 0.24f),
                > 0.2f => new Color(0.95f, 0.34f, 0.26f),
                _ => new Color(0.95f, 0.18f, 0.18f),
            };
        }
        _generationLabel.Text = $"名前: {_currentCharacterName} / 世代: {_character.Generation} / 次世代: {_nextGeneration}";
        _statsLabel.Text =
            $"体力:{_character.Stats.Vitality} "
            + $"知力:{_character.Stats.Intelligence} "
            + $"魅力:{_character.Stats.Charisma} "
            + $"運:{_character.Stats.Luck} "
            + $"財力:{_character.Stats.Wealth}";
        _scoreLabel.Text = $"世代スコア: {_generationScore} / 累計: {scaledTotalScore}";

        _metaLabel.Text =
            $"{_eraManager.Current.BuildSummary()}\n"
            + $"{_familyLawManager.BuildSummary()}\n"
            + $"{_bondManager.BuildSummary()}\n"
            + $"{_inventory.BuildCurrentSummary()} / {_inventory.BuildHeirloomSummary()}\n"
            + $"伴侶: {(_hasSpouse ? _spouseName : "未成立")} / 養子強制:{(_forcedAdoption ? "ON" : "OFF")}"
            + $" / 養子使用:{(_adoptionUsed ? "済" : "未")} / 最後の養子:{(_lastResortUsed ? "済" : "未")}";

        _collectionLabel.Text =
            $"{_familyTree.BuildRecentSummary()}\n"
            + $"{_collectionManager.BuildSummary()} / {_achievementManager.BuildSummary()}\n"
            + $"{_founderManager.BuildSummary()}\n"
            + $"{_generationHistory.BuildSummary()}\n"
            + $"{_inventory.BuildHeirloomCollectionSummary()}";

        _legendLabel.Text = string.IsNullOrWhiteSpace(_legendText)
            ? "家系の伝説: まだ記録なし"
            : _legendText;

        _monetizeLabel.Text =
            $"{_adManager.BuildSummary()}\n"
            + $"{_iapManager.BuildSummary()}\n"
            + $"{_battlePassManager.BuildSummary()} / {_cosmeticManager.BuildSummary()}\n"
            + $"報酬: 継承+{_pendingInheritanceBonusWealth} / Retry:{(_retryTokenAvailable ? "1" : "0")} / Shop+{_shopSlotBonus}";

        _onlineLabel.Text =
            $"{_asyncSocialManager.BuildSummary()}\n"
            + $"{_worldExpansionManager.BuildSummary()} / {_navigatorManager.BuildProfileSummary()}";

        _perfLabel.Text =
            $"{_perfSummary}\n"
            + $"{_saveStatus}";

        if (_activeEvent is not null)
        {
            _eventLabel.Text =
                $"{_activeEvent.EventText}\n"
                + $"A:{_activeEvent.TapChoice.Text} / B:{_activeEvent.SwipeChoice.Text} / C:{_activeEvent.TimeoutChoice.Text}\n"
                + $"残り: {_activeEvent.RemainingSeconds:F1}s";
        }

        _hintLabel.Text =
            $"操作: {PlayerAction.BuildHint(_phaseManager.CurrentPhase)} (PC: Space / A / S / W)\n"
            + "Debug: F5広告除去 F6パス F7ナビ購入 F8ナビ切替 F9継承広告 F10Retry広告 F11Shop広告";

        UpdateEventPanel();
    }

    private static string PhaseToText(LifePhase phase)
    {
        return phase switch
        {
            LifePhase.Childhood => "幼少期",
            LifePhase.Youth => "青年期",
            LifePhase.Midlife => "壮年期",
            LifePhase.Elderly => "老年期",
            _ => "不明",
        };
    }

    private string BuildGameStateLabel()
    {
        if (_isInFuneral)
        {
            return "Funeral";
        }

        if (_activeEvent is not null)
        {
            return "Event";
        }

        return _timeManager.IsPaused ? "Paused" : "Playing";
    }

    private string BuildEraLabel()
    {
        return _eraManager.Current.EraName;
    }

    private void UpdateDebugOverlay()
    {
        if (!_debugOverlayEnabled)
        {
            return;
        }

        _debugOverlay.UpdateMetrics(
            (float)Engine.GetFramesPerSecond(),
            GetViewportRect().Size,
            BuildGameStateLabel(),
            _character.Age,
            PhaseToText(_phaseManager.CurrentPhase),
            BuildEraLabel());
    }

    private void ApplyMobileUiPolish()
    {
        _hud.OffsetBottom = 340f;

        ApplyFontSize(_ageLabel, 42);
        ApplyFontSize(_phaseLabel, 42);
        ApplyFontSize(_lifeLabel, 42);
        ApplyFontSize(_generationLabel, 42);
        ApplyFontSize(_statsLabel, 42);
        ApplyFontSize(_scoreLabel, 42);
        ApplyFontSize(_eventLabel, 42);
        ApplyFontSize(_navigatorLabel, 42);
        ApplyFontSize(_phaseBannerLabel, 54);

        if (_lifeGauge is not null)
        {
            _lifeGauge.CustomMinimumSize = new Vector2(0f, 36f);
        }

        _phaseLabel.Visible = false;
        _statsLabel.Visible = false;
        _eventLabel.Visible = false;
        _hintLabel.Visible = false;
        _metaLabel.Visible = false;
        _collectionLabel.Visible = false;
        _legendLabel.Visible = false;
        _monetizeLabel.Visible = false;
        _onlineLabel.Visible = false;
        _perfLabel.Visible = false;

        _willPanel.OffsetLeft = 80f;
        _willPanel.OffsetTop = 220f;
        _willPanel.OffsetRight = 1000f;
        _willPanel.OffsetBottom = 1420f;

        ConfigureTapButton(_willButton, 180f, 42);
        ConfigureTapButton(_willPrevButton, 0f, 42);
        ConfigureTapButton(_willNextButton, 0f, 42);
        ConfigureTapButton(_willApplyButton, 0f, 42);
        ConfigureTapButton(_willCloseButton, 0f, 42);

        _willPrevButton.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _willNextButton.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        ConfigureTapButton(_nextGenerationButton, 0f, 42);
        ConfigureTapButton(_biologicalButton, 240f, 42);
        ConfigureTapButton(_adoptedButton, 240f, 42);
        ConfigureTapButton(_birthButton, 240f, 42);
        ConfigureTapButton(_birthAdBonusButton, 320f, 42);

        _biologicalButton.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _adoptedButton.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _birthButton.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _birthAdBonusButton.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        ApplyFontSize(_willCandidateLabel, 42);
        ApplyFontSize(_willDetailLabel, 42);
        ApplyFontSize(_funeralSummaryLabel, 54);
        ApplyFontSize(_funeralDeathCauseLabel, 42);
        ApplyFontSize(_funeralSnapshotLabel, 42);
        ApplyFontSize(_funeralScoreLabel, 42);
        ApplyFontSize(_funeralInterstitialLabel, 42);
        ApplyFontSize(_inheritanceLabel, 42);
        ApplyFontSize(_childTypeLabel, 42);
        ApplyFontSize(_childPreviewLabel, 42);
        ApplyFontSize(_uniqueSkillLabel, 42);

        var willTitleLabel = GetNodeOrNull<Label>("UI/WillPanel/Margin/VBox/TitleLabel");
        if (willTitleLabel is not null)
        {
            ApplyFontSize(willTitleLabel, 54);
        }

        var tombstoneLabel = GetNodeOrNull<Label>("UI/FuneralOverlay/FuneralPanel/Margin/VBox/TombstoneLabel");
        if (tombstoneLabel is not null)
        {
            ApplyFontSize(tombstoneLabel, 42);
        }
    }

    private static void ApplyFontSize(Control control, int size)
    {
        control.AddThemeFontSizeOverride("font_size", size);
    }

    private static void ConfigureTapButton(Button button, float minWidth, int fontSize)
    {
        button.CustomMinimumSize = new Vector2(
            Mathf.Max(button.CustomMinimumSize.X, minWidth),
            Mathf.Max(button.CustomMinimumSize.Y, 144f));
        button.AddThemeFontSizeOverride("font_size", fontSize);
    }
}
