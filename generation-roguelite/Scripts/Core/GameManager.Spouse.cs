using System.Collections.Generic;
using GenerationRoguelite.Navigator;
using Godot;

namespace GenerationRoguelite.Core;

public partial class GameManager
{
    private void TickSpouseCandidateFlow(double delta)
    {
        if (_hasSpouse || _phaseManager.CurrentPhase != LifePhase.Midlife || _activeEvent is not null)
        {
            return;
        }

        if (_character.Age < 31 || _partnerAttemptCount >= 3)
        {
            return;
        }

        _spouseEventCooldown -= (float)delta;
        if (_spouseEventCooldown > 0f)
        {
            return;
        }

        _spouseEventCooldown = _partnerAttemptCount == 0 ? 20f : _rng.RandfRange(24f, 36f);
        _partnerAttemptCount++;

        var candidateCount = _character.Stats.Charisma switch
        {
            < 10 => 1,
            < 20 => 2,
            _ => 3,
        };

        var candidates = new List<string>(candidateCount);
        for (var i = 0; i < candidateCount; i++)
        {
            candidates.Add($"{BuildRandomName()}({BuildRandomTrait()})");
        }

        var selected = candidates[(int)_rng.RandiRange(0, candidateCount - 1)];
        var score = _character.Stats.Charisma * 2 + _character.Stats.Luck;
        var threshold = (int)_rng.RandiRange(0, 50);

        if (score > threshold)
        {
            _hasSpouse = true;
            _spouseName = selected;
            _spouseVitality = (int)_rng.RandiRange(8, 18);
            _spouseIntelligence = (int)_rng.RandiRange(8, 18);
            _spouseCharisma = (int)_rng.RandiRange(8, 18);
            _spouseLuck = (int)_rng.RandiRange(8, 18);

            var successText =
                $"伴侶候補イベント(候補{candidateCount}人): {_spouseName}と結ばれた。\n"
                + $"判定: 魅力×2+運={score} / 目標{threshold} / 試行{_partnerAttemptCount}/3\n"
                + "壮年期後半に子供誕生イベントが予定された。";
            _eventLabel.Text = successText;
            RecordGenerationEvent(successText, true);
            Speak(new DialogueData("人生の相棒が見つかったね。", "navi_partner_success_01"));
            return;
        }

        var failText =
            $"伴侶候補イベント(候補{candidateCount}人): 求婚は実らなかった。\n"
            + $"判定: 魅力×2+運={score} / 目標{threshold} / 試行{_partnerAttemptCount}/3";

        if (_partnerAttemptCount >= 3)
        {
            _forcedAdoption = true;
            failText += "\n再挑戦上限に到達。次世代は養子ルートに固定。";
        }

        _eventLabel.Text = failText;
        RecordGenerationEvent(failText, false);
        Speak(new DialogueData("まだ出会いはあるはず。", "navi_partner_fail_01"));
    }

    private string BuildRandomName()
    {
        string[] first = ["ア", "イ", "ウ", "エ", "オ", "カ", "サ", "ト", "ナ", "ミ", "ユ", "リ"];
        string[] second = ["キ", "シ", "チ", "ハ", "マ", "ラ", "ル", "ノ", "モ", "ネ", "ヤ", "ワ"];
        return first[(int)_rng.RandiRange(0, first.Length - 1)] + second[(int)_rng.RandiRange(0, second.Length - 1)];
    }

    private string BuildRandomTrait()
    {
        string[] traits = ["誠実", "豪胆", "聡明", "温厚", "冒険家", "職人気質"];
        return traits[(int)_rng.RandiRange(0, traits.Length - 1)];
    }
}
