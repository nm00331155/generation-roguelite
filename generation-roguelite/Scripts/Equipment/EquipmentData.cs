using GenerationRoguelite.Character;
using GenerationRoguelite.Core;

namespace GenerationRoguelite.EquipmentSystem;

public sealed record EquipmentData(
    string Id,
    string Name,
    string Category,
    EquipmentSlot Slot,
    ItemRarity Rarity,
    StatBonus Bonus,
    int LifespanModifier,
    bool IsHeirloom,
    string Era)
{
    public int Score => ((int)Rarity + 1) * 20 + Bonus.Magnitude + System.Math.Abs(LifespanModifier) * 2;

    public static EquipmentData FromCharacterEquipment(Character.Equipment equipment)
    {
        var category = equipment.Slot switch
        {
            EquipmentSlot.Weapon => "weapon",
            EquipmentSlot.Armor => "armor",
            EquipmentSlot.Accessory => "accessory",
            _ => "misc",
        };

        var id = $"{equipment.Era}:{category}:{equipment.Name}";

        return new EquipmentData(
            id,
            equipment.Name,
            category,
            equipment.Slot,
            equipment.Rarity,
            equipment.TotalStatBonus,
            equipment.LifespanModifier,
            equipment.IsHeirloom,
            equipment.Era);
    }
}
