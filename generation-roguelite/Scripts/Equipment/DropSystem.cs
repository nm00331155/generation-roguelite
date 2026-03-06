using GenerationRoguelite.Character;
using GenerationRoguelite.Core;

namespace GenerationRoguelite.EquipmentSystem;

public sealed class DropSystem
{
    private readonly Character.Inventory _inventory;

    public DropSystem(Character.Inventory inventory)
    {
        _inventory = inventory;
    }

    public InventoryDropResult TryRollDrop(LifePhase phase, int luck, bool eventSuccess, float dropChance, string eraName)
    {
        return _inventory.TryRollDrop(phase, luck, eventSuccess, dropChance, eraName);
    }

    public InventoryReplacementPreview? GetPendingReplacementPreview()
    {
        return _inventory.GetPendingReplacementPreview();
    }

    public InventoryReplacementResolution ResolvePendingReplacement(bool acceptIncoming)
    {
        return _inventory.ResolvePendingReplacement(acceptIncoming);
    }
}
