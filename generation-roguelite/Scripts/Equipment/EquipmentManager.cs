using System.Collections.Generic;
using System.Linq;
using GenerationRoguelite.Character;

namespace GenerationRoguelite.EquipmentSystem;

public sealed class EquipmentManager
{
    private readonly Character.Inventory _inventory;

    public EquipmentManager(Character.Inventory inventory)
    {
        _inventory = inventory;
    }

    public IReadOnlyList<EquipmentData> GetAllItems()
    {
        return _inventory.CurrentItems
            .Select(EquipmentData.FromCharacterEquipment)
            .ToList();
    }

    public IReadOnlyDictionary<EquipmentSlot, EquipmentData> GetEquippedItems()
    {
        return _inventory.EquippedItems
            .ToDictionary(entry => entry.Key, entry => EquipmentData.FromCharacterEquipment(entry.Value));
    }

    public IReadOnlyList<EquipmentData> GetHeirloomCandidates()
    {
        return _inventory.GetHeirloomCandidates()
            .Select(EquipmentData.FromCharacterEquipment)
            .ToList();
    }

    public bool TryDesignateHeirloom(EquipmentData equipmentData, out string message)
    {
        var target = _inventory.CurrentItems
            .Concat(_inventory.EquippedItems.Values)
            .FirstOrDefault(item => item.Name == equipmentData.Name && item.Slot == equipmentData.Slot);

        if (target is null)
        {
            message = "指定対象の装備が見つかりません。";
            return false;
        }

        return _inventory.TryDesignateHeirloom(target, out message);
    }
}
