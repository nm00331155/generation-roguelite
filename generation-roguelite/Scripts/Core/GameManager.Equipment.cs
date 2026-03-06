using Godot;

namespace GenerationRoguelite.Core;

public partial class GameManager
{
    private void RefreshEquippedBonuses()
    {
        var currentBonus = _inventory.GetEquippedStatBonus();
        if (currentBonus != _appliedEquipmentBonus)
        {
            var diff = currentBonus - _appliedEquipmentBonus;
            _character.Stats.ApplyBonus(diff);
            _appliedEquipmentBonus = currentBonus;
        }

        var lifeModifier = _inventory.GetEquippedLifespanModifier();
        if (lifeModifier == _appliedEquipmentLifespanModifier)
        {
            return;
        }

        var deltaLife = lifeModifier - _appliedEquipmentLifespanModifier;
        _character.AdjustRemainingLife(deltaLife);
        _appliedEquipmentLifespanModifier = lifeModifier;
    }
}
