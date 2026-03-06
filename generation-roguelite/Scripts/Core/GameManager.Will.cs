using System;
using GenerationRoguelite.Character;
using Godot;

namespace GenerationRoguelite.Core;

public partial class GameManager
{
    private void UpdateWillUiState()
    {
        if (_isInFuneral || _phaseManager.CurrentPhase != LifePhase.Elderly)
        {
            _willButton.Visible = false;
            _willPanel.Visible = false;
            return;
        }

        _willButton.Visible = true;
        if (_willPanel.Visible)
        {
            RefreshWillPanel();
        }
    }

    private void OnWillButtonPressed()
    {
        if (_phaseManager.CurrentPhase != LifePhase.Elderly || _isInFuneral)
        {
            return;
        }

        _willPanel.Visible = !_willPanel.Visible;
        if (_willPanel.Visible)
        {
            RefreshWillPanel();
        }
    }

    private void OnWillCloseButtonPressed()
    {
        _willPanel.Visible = false;
    }

    private void OnWillPrevButtonPressed()
    {
        if (_willCandidates.Count == 0)
        {
            return;
        }

        _willCandidateIndex = (_willCandidateIndex - 1 + _willCandidates.Count) % _willCandidates.Count;
        RefreshWillPanel();
    }

    private void OnWillNextButtonPressed()
    {
        if (_willCandidates.Count == 0)
        {
            return;
        }

        _willCandidateIndex = (_willCandidateIndex + 1) % _willCandidates.Count;
        RefreshWillPanel();
    }

    private void OnWillApplyButtonPressed()
    {
        if (_willCandidates.Count == 0)
        {
            _willStatusText = "指定できる家宝候補がありません。";
            RefreshWillPanel();
            return;
        }

        var target = _willCandidates[_willCandidateIndex];
        if (_inventory.TryDesignateHeirloom(target, out var message))
        {
            _eventLabel.AddThemeColorOverride("font_color", Colors.White);
            _eventLabel.Text = message;
        }

        _willStatusText = message;
        RefreshWillPanel();
    }

    private void RefreshWillPanel()
    {
        _willCandidates.Clear();
        _willCandidates.AddRange(_inventory.GetHeirloomCandidates());

        if (_willCandidates.Count == 0)
        {
            _willCandidateIndex = 0;
            _willCandidateLabel.Text = "家宝候補なし\nレア以上の装備を用意すると指定できます。";
            _willDetailLabel.Text = string.IsNullOrWhiteSpace(_willStatusText)
                ? "遺言待機中"
                : _willStatusText;
            _willPrevButton.Disabled = true;
            _willNextButton.Disabled = true;
            _willApplyButton.Disabled = true;
            return;
        }

        _willCandidateIndex = Math.Clamp(_willCandidateIndex, 0, _willCandidates.Count - 1);
        var target = _willCandidates[_willCandidateIndex];
        var designatedText = _inventory.IsDesignatedHeirloom(target) ? " [指定中]" : string.Empty;
        var totalBonus = target.TotalStatBonus;

        _willCandidateLabel.Text =
            $"候補 {_willCandidateIndex + 1}/{_willCandidates.Count}: {target.Name}{designatedText}\n"
            + $"レア度:{target.Rarity} / 枠:{WillSlotToText(target.Slot)} / 時代:{target.Era}";

        _willDetailLabel.Text =
            $"補正 体:{totalBonus.Vitality} 知:{totalBonus.Intelligence} 魅:{totalBonus.Charisma} 運:{totalBonus.Luck} 財:{totalBonus.Wealth} / 寿命:{target.LifespanModifier:+0;-0;0}\n"
            + (string.IsNullOrWhiteSpace(_willStatusText) ? "遺言で家宝を指定できます。" : _willStatusText);

        _willPrevButton.Disabled = _willCandidates.Count <= 1;
        _willNextButton.Disabled = _willCandidates.Count <= 1;
        _willApplyButton.Disabled = false;
    }

    private static string WillSlotToText(EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.Weapon => "武器",
            EquipmentSlot.Armor => "防具",
            EquipmentSlot.Accessory => "装飾",
            _ => "不明",
        };
    }
}
