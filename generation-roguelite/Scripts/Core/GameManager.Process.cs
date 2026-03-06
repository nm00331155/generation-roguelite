using System;
using GenerationRoguelite.Events;
using GenerationRoguelite.Navigator;
using Godot;

namespace GenerationRoguelite.Core;

public partial class GameManager
{
    public override void _Process(double delta)
    {
        TickPhaseTransitionEffects(delta);
        TickDropFlash(delta);

        if (_isInFuneral)
        {
            TickFuneralSequence(delta);
            UpdateWillUiState();
            UpdateDebugOverlay();
            UpdateHud();
            return;
        }

        TickBackground(delta);
        TickPlayerMotion(delta);
        TickCollectibles(delta);
        TickElderlyCaneState(delta);
        UpdateChildhoodAcceleration();
        UpdateWillUiState();
        RefreshEquippedBonuses();

        _adManager.Tick(delta);

        var perfTick = _performanceMonitor.Tick(delta);
        if (perfTick.HasSample)
        {
            _perfSummary = perfTick.Summary;
            if (perfTick.ShouldTrimCaches)
            {
                _eventManager.ResetCache();
                _perfSummary += " / cache-trim";
            }
        }

        var asyncMessage = _asyncSocialManager.Tick(delta);
        if (!string.IsNullOrWhiteSpace(asyncMessage))
        {
            _legendText = asyncMessage;
        }

        _ddaController.Tick(delta);
        var context = BuildGenerationContext();
        _eventManager.TickCache(delta, context);

        _avoidWindowRemaining -= (float)delta;
        if (_avoidWindowRemaining < 0f)
        {
            _avoidWindowRemaining = 0f;
        }

        _parryWindowRemaining -= (float)delta;
        if (_parryWindowRemaining < 0f)
        {
            _parryWindowRemaining = 0f;
        }

        var densityAdjustment =
            _ddaController.GetDensityAdjustment()
            + _eraManager.Current.TerrainDensityOffset
            + _worldExpansionManager.Current.TerrainDensityOffset;
        _obstacleSpawner.SetTerrain(_currentTerrain, densityAdjustment);
        _obstacleSpawner.SetDdaAdjustments(
            _ddaController.GetSpawnIntervalOffset(),
            _ddaController.GetSpeedAdjustment());
        _obstacleSpawner.Tick(delta, _phaseManager.CurrentPhase);

        if (_activeEvent is null)
        {
            var hasObstacleCluster = _obstacleSpawner.HasApproachingCluster(_player.Position.X, 3, 560f);
            var warning = _navigatorManager.OnObstacleApproaching(hasObstacleCluster);
            if (warning.HasText)
            {
                Speak(warning);
            }
        }

        var interactions = _obstacleSpawner.ResolvePlayerInteractions(GetPlayerRect(), _avoidWindowRemaining > 0f);
        if (interactions.Hits > 0)
        {
            var damagePerHit = BaseObstacleLifeDamage * PhaseManager.ObstacleDamageMultiplier(_phaseManager.CurrentPhase);
            var parryApplied = _phaseManager.CurrentPhase == LifePhase.Midlife && _parryWindowRemaining > 0f;
            if (parryApplied)
            {
                damagePerHit *= 0.2f;
                _parryWindowRemaining = 0f;
            }

            if (_phaseManager.CurrentPhase == LifePhase.Elderly)
            {
                damagePerHit = MathF.Max(5f, damagePerHit);
            }

            var damage = interactions.Hits * damagePerHit;
            _character.ApplyLifeDamage(damage);
            _eventLabel.Text = parryApplied
                ? $"受け流し成功! 被害を80%軽減 (寿命-{damage:F1})"
                : $"障害物に衝突! 寿命-{damage:F1}";
            _currentDeathCause = _phaseManager.CurrentPhase == LifePhase.Elderly ? "事故死" : "戦死";
            RecordGenerationEvent($"障害物衝突: 寿命-{damage:F1}", true);
        }

        for (var i = 0; i < interactions.Hits; i++)
        {
            _ddaController.RegisterResult(false);
        }

        for (var i = 0; i < interactions.Avoided; i++)
        {
            _ddaController.RegisterResult(true);
        }

        TickEventFlow(delta, context);
        TickSpouseCandidateFlow(delta);

        if (!_regretShown
            && _phaseManager.CurrentPhase == LifePhase.Elderly
            && _character.Age >= 60
            && _activeEvent is null)
        {
            _regretShown = true;
            var regretText = _regretGenerator.BuildRegret(_character.Age, _recentEvents);
            _eventLabel.Text = regretText;
            AppendRecentEvent(regretText);
            Speak(new DialogueData("過去の選択が、今のあなたを作ってる。", "navi_regret_01"));
        }

        var chatter = _navigatorManager.TickIdle(delta);
        if (chatter.HasText && _activeEvent is null)
        {
            Speak(chatter);
        }

        var advancedYears = _timeManager.ConsumeAdvancedYears(delta);
        for (var i = 0; i < advancedYears; i++)
        {
            _character.AdvanceYear(_phaseManager.CurrentPhase);
            _phaseManager.UpdateByAge(_character.Age);
            _generationScore += Mathf.Max(1, _character.Stats.Sum / 10);

            if (_character.IsDead)
            {
                break;
            }
        }

        if (_character.IsDead)
        {
            BeginFuneral();
        }

        UpdateDebugOverlay();
        UpdateHud();
    }

    private void TickEventFlow(double delta, EventGenerationContext context)
    {
        if (_activeEvent is not null)
        {
            _activeEvent.Tick(delta);
            if (_activeEvent.IsExpired)
            {
                ResolveActiveEvent(SelectLowestRiskChoice(_activeEvent), true);
            }

            return;
        }

        _eventCooldown -= (float)delta;
        if (_eventCooldown > 0f)
        {
            return;
        }

        var generated = _eventManager.CreateEvent(context);
        _activeEvent = new EventData(
            generated.EventText,
            generated.TapChoice,
            generated.SwipeChoice,
            generated.TimeoutChoice,
            EventLimitSeconds,
            generated.Terrain);
        _currentTerrain = _activeEvent.Terrain;
        _obstacleSpawner.SetTerrain(
            _currentTerrain,
            _ddaController.GetDensityAdjustment()
            + _eraManager.Current.TerrainDensityOffset
            + _worldExpansionManager.Current.TerrainDensityOffset);
        _obstacleSpawner.SetDdaAdjustments(
            _ddaController.GetSpawnIntervalOffset(),
            _ddaController.GetSpeedAdjustment());

        _eventLabel.Text =
            $"{_activeEvent.EventText}\n"
            + $"Tap: {_activeEvent.TapChoice.Text} / Swipe: {_activeEvent.SwipeChoice.Text}\n"
            + $"制限時間: {_activeEvent.RemainingSeconds:F1}s\n"
            + $"時代効果: {_eraManager.Current.BuildSummary()} / {_worldExpansionManager.BuildSummary()}";

        RecordGenerationEvent(_activeEvent.EventText, true);

        Speak(_navigatorManager.OnEventPresented(_activeEvent));

        _eventCooldown = RollEventCooldown();
    }
}
