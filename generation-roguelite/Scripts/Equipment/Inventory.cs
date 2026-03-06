using System.Collections.Generic;

namespace GenerationRoguelite.EquipmentSystem;

public sealed class Inventory
{
    private readonly List<EquipmentData> _items = new();

    public IReadOnlyList<EquipmentData> Items => _items;

    public int Capacity { get; }

    public Inventory(int capacity = 10)
    {
        Capacity = capacity;
    }

    public bool TryAdd(EquipmentData item)
    {
        if (_items.Count >= Capacity)
        {
            return false;
        }

        _items.Add(item);
        return true;
    }

    public bool Remove(EquipmentData item)
    {
        return _items.Remove(item);
    }

    public void Clear()
    {
        _items.Clear();
    }
}
