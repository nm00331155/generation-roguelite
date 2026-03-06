using System;
using GenerationRoguelite.Data;
using Godot;

namespace GenerationRoguelite.Core;

public partial class GameManager
{
    private void LoadPersistentState()
    {
        if (!_saveManager.TryLoad(out var data, out var message))
        {
            _saveStatus = $"save: {message}";
            return;
        }

        _totalScore = Math.Max(0, data.TotalScore);
        _nextGeneration = Math.Max(1, data.NextGeneration);
        _legendText = data.LegendText ?? string.Empty;
        _adoptionUsed = data.AdoptionUsed;
        _lastResortUsed = data.LastResortUsed;
        _timeManager.SetSpeedMultiplier(data.GameSpeed <= 0f ? 1f : data.GameSpeed);
        SetDebugOverlayEnabled(data.DebugOverlayEnabled);

        _iapManager.LoadState(data.Iap);
        _battlePassManager.LoadState(data.BattlePass);
        _cosmeticManager.LoadState(data.Cosmetic);
        _asyncSocialManager.LoadState(data.Social);

        ApplyPurchasedNavigatorProfiles(data.ActiveNavigatorProfile);
        _saveStatus = $"save: {message}";
    }

    private void SavePersistentState()
    {
        var data = new GameSaveData
        {
            TotalScore = _totalScore,
            NextGeneration = _nextGeneration,
            ActiveNavigatorProfile = _navigatorManager.ActiveProfileId,
            LegendText = _legendText,
            AdoptionUsed = _adoptionUsed,
            LastResortUsed = _lastResortUsed,
            GameSpeed = _timeManager.SpeedMultiplier,
            DebugOverlayEnabled = _debugOverlayEnabled,
            LastSaveTime = DateTime.UtcNow,
            Iap = _iapManager.BuildState(),
            BattlePass = _battlePassManager.BuildState(),
            Cosmetic = _cosmeticManager.BuildState(),
            Social = _asyncSocialManager.BuildState(),
        };

        _saveStatus = _saveManager.TrySave(data, out var message)
            ? $"save: {message}"
            : $"save: {message}";
    }
}
