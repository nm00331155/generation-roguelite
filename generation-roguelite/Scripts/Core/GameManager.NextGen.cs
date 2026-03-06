using System;
using GenerationRoguelite.Character;

namespace GenerationRoguelite.Core;

public partial class GameManager
{
    private void OnBiologicalButtonPressed()
    {
        if (_forcedAdoption)
        {
            return;
        }

        _nextChildIsBiological = true;
        RefreshNextGenerationPreview();
    }

    private void OnAdoptedButtonPressed()
    {
        _nextChildIsBiological = false;
        RefreshNextGenerationPreview();
    }

    private void OnBirthAdBonusButtonPressed()
    {
        if (_lineageExtinct)
        {
            if (_lastResortUsed)
            {
                _eventLabel.Text = "最後の養子は使用済みです。";
                return;
            }

            _lastResortUsed = true;
            _lineageExtinct = false;
            _forcedAdoption = true;
            _nextChildIsBiological = false;
            _adoptionUsed = true;

            _typeButtons.Visible = true;
            _biologicalButton.Disabled = true;
            _adoptedButton.Disabled = false;
            _birthButton.Text = "誕生";
            _birthAdBonusButton.Visible = false;
            _eventLabel.Text = "AD: Last-resort adoption would show here";

            RefreshNextGenerationPreview();
            SavePersistentState();
            return;
        }

        if (_adManager.TryWatchBirthInheritanceBonusAdPlaceholder(_character.Generation, out var bonusWealth, out var message))
        {
            _birthAdBonus += bonusWealth;
            _eventLabel.Text = message;
            RefreshNextGenerationPreview();
            return;
        }

        _eventLabel.Text = message;
    }

    private void OnBirthButtonPressed()
    {
        if (_lineageExtinct)
        {
            _nextGeneration = 1;
            _queuedHeirloom = null;
            _inventory.ResetHeirloomProgress();
            _adoptionUsed = false;
            _birthAdBonus = 0;
            _pendingAdoptedSkillName = "なし";
            _pendingAdoptedSkillBonus = StatBonus.Zero;
            _pendingAdoptedBaseBonus = StatBonus.Zero;
            StartGeneration(null);
            return;
        }

        var heritage = BuildSelectedHeritage();
        StartGeneration(heritage);
    }

    private HeritageData BuildSelectedHeritage()
    {
        var heirloomBonus = _queuedHeirloom is null ? 0 : 5;
        var baseWealth = Math.Max(0, (int)MathF.Round(_character.Stats.Wealth * 0.6f) + heirloomBonus + _birthAdBonus);

        if (_nextChildIsBiological)
        {
            _pendingAdoptedSkillName = "なし";
            _pendingAdoptedSkillBonus = StatBonus.Zero;
            _pendingAdoptedBaseBonus = StatBonus.Zero;
            _adoptedPreviewName = "なし";
            _adoptedPreviewBonus = StatBonus.Zero;
            _adoptedBaseBonus = StatBonus.Zero;

            return new HeritageData(
                bonusLifeYears: Math.Clamp((int)MathF.Round(_character.Stats.Vitality * 0.2f), 0, 10),
                wealthSeed: baseWealth,
                inheritedBonus: new StatBonus(
                    Vitality: BuildBiologicalInheritedStat(_character.Stats.Vitality, _spouseVitality),
                    Intelligence: BuildBiologicalInheritedStat(_character.Stats.Intelligence, _spouseIntelligence),
                    Charisma: BuildBiologicalInheritedStat(_character.Stats.Charisma, _spouseCharisma),
                    Luck: BuildBiologicalInheritedStat(_character.Stats.Luck, _spouseLuck),
                    Wealth: 0));
        }

        if (_adoptedPreviewName == "なし")
        {
            var preview = RollAdoptedUniqueSkill();
            _adoptedPreviewName = preview.Name;
            _adoptedPreviewBonus = preview.Bonus;
        }

        if (_adoptedBaseBonus == StatBonus.Zero)
        {
            _adoptedBaseBonus = RollAdoptedBaseBonus();
        }

        _pendingAdoptedSkillName = _adoptedPreviewName;
        _pendingAdoptedSkillBonus = _adoptedPreviewBonus;
        _pendingAdoptedBaseBonus = _adoptedBaseBonus;
        _adoptionUsed = true;

        return new HeritageData(
            bonusLifeYears: 1,
            wealthSeed: baseWealth,
            inheritedBonus: StatBonus.Zero);
    }

    private int BuildBiologicalInheritedStat(int parentStat, int partnerStat)
    {
        var average = (int)MathF.Round((parentStat + partnerStat) * 0.5f);
        var variance = (int)_rng.RandiRange(-5, 5);
        return Math.Clamp(average + variance, 1, 999);
    }

    private StatBonus RollAdoptedBaseBonus()
    {
        var vitality = (int)_rng.RandiRange(5, 15);
        var intelligence = (int)_rng.RandiRange(5, 15);
        var charisma = (int)_rng.RandiRange(5, 15);
        var luck = (int)_rng.RandiRange(5, 15);
        var wealth = (int)_rng.RandiRange(5, 15);

        return new StatBonus(
            Vitality: vitality - 10,
            Intelligence: intelligence - 10,
            Charisma: charisma - 10,
            Luck: luck - 10,
            Wealth: wealth - 10);
    }

    private (string Name, StatBonus Bonus) RollAdoptedUniqueSkill()
    {
        return ((int)_rng.RandiRange(0, 4)) switch
        {
            0 => ("鋼の心", new StatBonus(5, 0, 0, 0, 0)),
            1 => ("知恵の灯", new StatBonus(0, 5, 0, 0, 0)),
            2 => ("微笑みの才", new StatBonus(0, 0, 5, 0, 0)),
            3 => ("幸運の導き", new StatBonus(0, 0, 0, 5, 0)),
            _ => ("商才", new StatBonus(0, 0, 0, 0, 10)),
        };
    }

    private void RefreshNextGenerationPreview()
    {
        var heirloomBonus = _queuedHeirloom is null ? 0 : 5;
        var inheritance = Math.Max(0, (int)MathF.Round(_character.Stats.Wealth * 0.6f) + heirloomBonus + _birthAdBonus);
        _inheritanceLabel.Text = $"引き継ぎ遺産: {inheritance} (家宝 +{heirloomBonus} / 広告 +{_birthAdBonus})";

        if (_nextChildIsBiological)
        {
            var vitalityAvg = (int)MathF.Round((_character.Stats.Vitality + _spouseVitality) * 0.5f);
            var intelligenceAvg = (int)MathF.Round((_character.Stats.Intelligence + _spouseIntelligence) * 0.5f);
            var charismaAvg = (int)MathF.Round((_character.Stats.Charisma + _spouseCharisma) * 0.5f);
            var luckAvg = (int)MathF.Round((_character.Stats.Luck + _spouseLuck) * 0.5f);

            _childTypeLabel.Text = "子の種類: 実子（両親平均 ±5）";
            _childPreviewLabel.Text =
                $"初期能力値目安: 体{vitalityAvg} 知{intelligenceAvg} 魅{charismaAvg} 運{luckAvg}";
            _uniqueSkillLabel.Text = "ユニークスキル: なし";
            return;
        }

        if (_adoptedPreviewName == "なし")
        {
            var preview = RollAdoptedUniqueSkill();
            _adoptedPreviewName = preview.Name;
            _adoptedPreviewBonus = preview.Bonus;
        }

        if (_adoptedBaseBonus == StatBonus.Zero)
        {
            _adoptedBaseBonus = RollAdoptedBaseBonus();
        }

        _childTypeLabel.Text = "子の種類: 養子（能力5-15ランダム）";
        _childPreviewLabel.Text = "初期能力値プレビュー: 体/知/魅/運/財 = 各5〜15";
        _uniqueSkillLabel.Text = $"ユニークスキル候補: {_adoptedPreviewName}";
    }
}
