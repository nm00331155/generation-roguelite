using System;
using System.Collections.Generic;
using Godot;

namespace GenerationRoguelite.Monetization;

public enum RewardAdType
{
    InheritanceBoost,
    EventRetry,
    ShopSlot,
}

public readonly record struct RewardAdResult(
    RewardAdType Type,
    int WealthBonus,
    bool RetryToken,
    int ShopSlotBonus,
    string Message);

public sealed class AdManager
{
    private readonly RandomNumberGenerator _rng = new();
    private readonly Dictionary<RewardAdType, int> _rewardLastGeneration = new();

    private float _sessionSeconds;
    private int _lastInterstitialGeneration;
    private float _lastInterstitialSeconds;

    private int _nextGenerationInterval = 2;
    private float _nextMinuteInterval = 10f;

    private int _lastFuneralInterstitialGeneration;
    private float _lastFuneralInterstitialSeconds;

    public bool AdsRemoved { get; set; }

    public AdManager()
    {
        _rng.Randomize();
        RollNextInterstitialWindow();
    }

    public void Tick(double delta)
    {
        _sessionSeconds += (float)delta;
    }

    public bool ShouldShowInterstitial(int generation)
    {
        if (AdsRemoved)
        {
            return false;
        }

        var generationReady = generation - _lastInterstitialGeneration >= _nextGenerationInterval;
        var timeReady = _sessionSeconds - _lastInterstitialSeconds >= _nextMinuteInterval * 60f;

        if (!generationReady || !timeReady)
        {
            return false;
        }

        _lastInterstitialGeneration = generation;
        _lastInterstitialSeconds = _sessionSeconds;
        RollNextInterstitialWindow();
        return true;
    }

    public bool TryShowFuneralInterstitialPlaceholder(int generation, out string message)
    {
        if (AdsRemoved)
        {
            message = "広告除去購入済みのため、弔い画面の広告はスキップ。";
            return false;
        }

        var generationReady = generation - _lastFuneralInterstitialGeneration >= 2;
        var timeReady = _sessionSeconds - _lastFuneralInterstitialSeconds >= 600f;
        if (!generationReady || !timeReady)
        {
            message = "広告条件未達（世代差2以上 かつ 経過10分以上が必要）。";
            return false;
        }

        _lastFuneralInterstitialGeneration = generation;
        _lastFuneralInterstitialSeconds = _sessionSeconds;
        message = "[広告プレースホルダー] 弔い画面インタースティシャル表示ポイント";
        return true;
    }

    public bool TryWatchBirthInheritanceBonusAdPlaceholder(int generation, out int bonusWealth, out string message)
    {
        bonusWealth = 0;

        if (_rewardLastGeneration.TryGetValue(RewardAdType.InheritanceBoost, out var usedAt) && usedAt == generation)
        {
            message = "この世代では既に遺産ボーナス広告を利用済み。";
            return false;
        }

        _rewardLastGeneration[RewardAdType.InheritanceBoost] = generation;
        bonusWealth = (int)_rng.RandiRange(10, 20);
        message = $"[広告プレースホルダー] 遺産ボーナス +{bonusWealth}";
        return true;
    }

    public bool TryWatchRewardAd(RewardAdType type, int generation, out RewardAdResult result)
    {
        if (_rewardLastGeneration.TryGetValue(type, out var usedAt) && usedAt == generation)
        {
            result = new RewardAdResult(type, 0, false, 0, "この世代では既に同種のリワード広告を利用済み。");
            return false;
        }

        _rewardLastGeneration[type] = generation;

        result = type switch
        {
            RewardAdType.InheritanceBoost => new RewardAdResult(
                type,
                WealthBonus: 10,
                RetryToken: false,
                ShopSlotBonus: 0,
                Message: "リワード広告視聴: 次世代の初期財力+10"),

            RewardAdType.EventRetry => new RewardAdResult(
                type,
                WealthBonus: 0,
                RetryToken: true,
                ShopSlotBonus: 0,
                Message: "リワード広告視聴: イベントやり直し権+1"),

            RewardAdType.ShopSlot => new RewardAdResult(
                type,
                WealthBonus: 0,
                RetryToken: false,
                ShopSlotBonus: 1,
                Message: "リワード広告視聴: ショップ枠+1 (当世代のみ)"),

            _ => new RewardAdResult(type, 0, false, 0, "不明な広告報酬。"),
        };

        return true;
    }

    public string BuildSummary()
    {
        var interstitialState = AdsRemoved
            ? "強制広告OFF"
            : $"強制広告ON(次目安: {_nextGenerationInterval}世代 / {_nextMinuteInterval:F0}分)";

        return $"広告: {interstitialState}";
    }

    private void RollNextInterstitialWindow()
    {
        _nextGenerationInterval = (int)_rng.RandiRange(2, 3);
        _nextMinuteInterval = _rng.RandfRange(10f, 15f);
    }
}
