using System;
using System.Collections.Generic;
using System.Linq;
using GenerationRoguelite.Core;
using Godot;

namespace GenerationRoguelite.Character;

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary,
}

public enum EquipmentSlot
{
    Weapon,
    Armor,
    Accessory,
}

public enum StatType
{
    Vitality,
    Intelligence,
    Charisma,
    Luck,
    Wealth,
}

public sealed record Equipment(
    string Name,
    ItemRarity Rarity,
    IReadOnlyDictionary<StatType, int> StatBonuses,
    int LifespanModifier,
    bool IsHeirloom,
    string Era,
    EquipmentSlot Slot)
{
    public StatBonus TotalStatBonus
    {
        get
        {
            return new StatBonus(
                GetStat(StatType.Vitality),
                GetStat(StatType.Intelligence),
                GetStat(StatType.Charisma),
                GetStat(StatType.Luck),
                GetStat(StatType.Wealth));
        }
    }

    public int ValueScore => ((int)Rarity + 1) * 20 + TotalStatBonus.Magnitude + Math.Abs(LifespanModifier) * 2;

    public string BuildShortSummary()
    {
        return $"{Name}({Rarity}/{Slot})";
    }

    private int GetStat(StatType type)
    {
        return StatBonuses.TryGetValue(type, out var value) ? value : 0;
    }
}

public readonly record struct DropPresentation(string Tone, Color TextColor, bool ShouldFlash);

public readonly record struct InventoryDropResult(
    bool Dropped,
    bool PendingReplacement,
    Equipment? Item,
    string Message,
    DropPresentation Presentation);

public readonly record struct InventoryReplacementPreview(
    Equipment Incoming,
    Equipment Candidate,
    string Summary,
    string Detail);

public readonly record struct InventoryReplacementResolution(
    bool Applied,
    bool AcceptedIncoming,
    string Message,
    DropPresentation Presentation);

public readonly record struct HeirloomEntry(
    string Name,
    ItemRarity Rarity,
    int Level,
    string Era);

public sealed class Inventory
{
    private const int MaxInventorySlots = 10;
    private const int MaxHeirloomHistory = 8;
    private const float SuccessDropChance = 0.5f;

    private readonly List<Equipment> _currentItems = new();
    private readonly Dictionary<EquipmentSlot, Equipment> _equippedItems = new();
    private readonly List<Equipment> _heirlooms = new();
    private readonly List<HeirloomEntry> _heirloomEntries = new();
    private readonly RandomNumberGenerator _rng = new();

    private PendingReplacementState? _pendingReplacement;
    private string _designatedHeirloomKey = string.Empty;
    private string _activeHeirloomKey = string.Empty;
    private int _activeHeirloomLevel;

    public IReadOnlyList<Equipment> CurrentItems => _currentItems;
    public IReadOnlyDictionary<EquipmentSlot, Equipment> EquippedItems => _equippedItems;
    public IReadOnlyList<Equipment> Heirlooms => _heirlooms;
    public IReadOnlyList<HeirloomEntry> HeirloomEntries => _heirloomEntries;
    public int ActiveHeirloomLevel => _activeHeirloomLevel;

    public bool HasPendingReplacement => _pendingReplacement is not null;

    public int LegendaryCount { get; private set; }

    public Inventory()
    {
        _rng.Randomize();
    }

    public void ResetGeneration()
    {
        _currentItems.Clear();
        _equippedItems.Clear();
        _pendingReplacement = null;
    }

    public void ResetHeirloomProgress()
    {
        _designatedHeirloomKey = string.Empty;
        _activeHeirloomKey = string.Empty;
        _activeHeirloomLevel = 0;
    }

    public IReadOnlyList<Equipment> GetHeirloomCandidates()
    {
        return _currentItems
            .Concat(_equippedItems.Values)
            .Where(item => item.Rarity >= ItemRarity.Rare)
            .OrderByDescending(item => item.Rarity)
            .ThenByDescending(item => item.ValueScore)
            .ToList();
    }

    public bool IsDesignatedHeirloom(Equipment item)
    {
        if (item.Rarity < ItemRarity.Rare)
        {
            return false;
        }

        return string.Equals(_designatedHeirloomKey, BuildEquipmentKey(item), StringComparison.Ordinal);
    }

    public bool TryDesignateHeirloom(Equipment item, out string message)
    {
        if (item.Rarity < ItemRarity.Rare)
        {
            message = "家宝に指定できるのはレア以上の装備のみ。";
            return false;
        }

        var key = BuildEquipmentKey(item);
        var exists = _currentItems.Concat(_equippedItems.Values)
            .Any(owned => string.Equals(BuildEquipmentKey(owned), key, StringComparison.Ordinal));
        if (!exists)
        {
            message = "指定対象の装備が見つかりません。";
            return false;
        }

        _designatedHeirloomKey = key;
        message = $"遺言更新: {item.Name} を家宝に指定。";
        return true;
    }

    public InventoryDropResult TryRollDrop(LifePhase phase, int luck, bool eventSuccess, float dropChance, string eraName)
    {
        if (_pendingReplacement is not null)
        {
            return new InventoryDropResult(
                Dropped: false,
                PendingReplacement: true,
                Item: _pendingReplacement.Incoming,
                Message: "入れ替え待ちの装備があるため、新規ドロップを保留。",
                Presentation: _pendingReplacement.Presentation);
        }

        if (!eventSuccess)
        {
            return new InventoryDropResult(false, false, null, "ドロップなし", DefaultPresentation);
        }

        var normalizedDropChance = float.IsNaN(dropChance)
            ? SuccessDropChance
            : Mathf.Clamp(dropChance, 0f, 1f);

        if (_rng.Randf() > normalizedDropChance)
        {
            return new InventoryDropResult(false, false, null, "ドロップなし", DefaultPresentation);
        }

        var rarity = RollRarity(luck);
        var slot = RollSlot();
        var item = BuildItem(phase, eraName, rarity, slot);
        var presentation = BuildDropPresentation(rarity);

        if (rarity == ItemRarity.Legendary)
        {
            LegendaryCount += 1;
        }

        if (TryAutoEquip(item, out var equipMessage))
        {
            return new InventoryDropResult(
                Dropped: true,
                PendingReplacement: false,
                Item: item,
                Message: $"{BuildRarityMessage(item, presentation)} / {equipMessage}",
                Presentation: presentation);
        }

        if (_currentItems.Count < MaxInventorySlots)
        {
            _currentItems.Add(item);
            return new InventoryDropResult(
                Dropped: true,
                PendingReplacement: false,
                Item: item,
                Message: $"{BuildRarityMessage(item, presentation)} / バッグへ収納",
                Presentation: presentation);
        }

        var candidate = SelectReplacementCandidate();
        if (candidate is null)
        {
            return new InventoryDropResult(false, false, null, "バッグ状態が不整合のためドロップを破棄", DefaultPresentation);
        }

        var equipIncoming = ShouldReplaceEquipped(item, out var equippedToStore);
        _pendingReplacement = new PendingReplacementState(item, candidate, equipIncoming, equippedToStore, presentation);

        return new InventoryDropResult(
            Dropped: false,
            PendingReplacement: true,
            Item: item,
            Message: $"インベントリ満杯: {item.Name} の入れ替えが必要",
            Presentation: presentation);
    }

    public InventoryReplacementPreview? GetPendingReplacementPreview()
    {
        if (_pendingReplacement is null)
        {
            return null;
        }

        var summary =
            $"新規: {_pendingReplacement.Incoming.BuildShortSummary()}\n"
            + $"候補: {_pendingReplacement.Candidate.BuildShortSummary()}";

        var detail = _pendingReplacement.EquipIncoming
            ? "A: 入替して採用（装備更新） / B: 破棄して現状維持 / C: 詳細再表示"
            : "A: 入替して採用（バッグ追加） / B: 破棄して現状維持 / C: 詳細再表示";

        return new InventoryReplacementPreview(
            _pendingReplacement.Incoming,
            _pendingReplacement.Candidate,
            summary,
            detail);
    }

    public InventoryReplacementResolution ResolvePendingReplacement(bool acceptIncoming)
    {
        if (_pendingReplacement is null)
        {
            return new InventoryReplacementResolution(false, false, "入れ替え待ちはありません。", DefaultPresentation);
        }

        var pending = _pendingReplacement;
        _pendingReplacement = null;

        if (!acceptIncoming)
        {
            return new InventoryReplacementResolution(
                Applied: true,
                AcceptedIncoming: false,
                Message: $"{pending.Incoming.Name} を破棄した。",
                Presentation: pending.Presentation);
        }

        _currentItems.Remove(pending.Candidate);

        if (pending.EquipIncoming)
        {
            if (pending.EquippedToStore is not null)
            {
                _currentItems.Add(pending.EquippedToStore);
            }

            _equippedItems[pending.Incoming.Slot] = pending.Incoming;
            return new InventoryReplacementResolution(
                Applied: true,
                AcceptedIncoming: true,
                Message: $"{pending.Candidate.Name} と入れ替え、{pending.Incoming.Name} を装備した。",
                Presentation: pending.Presentation);
        }

        _currentItems.Add(pending.Incoming);
        return new InventoryReplacementResolution(
            Applied: true,
            AcceptedIncoming: true,
            Message: $"{pending.Candidate.Name} と入れ替え、{pending.Incoming.Name} を収納した。",
            Presentation: pending.Presentation);
    }

    public Equipment? SelectHeirloomForNextGeneration()
    {
        var selected = ResolveDesignatedHeirloomCandidate() ?? ResolveFallbackHeirloomCandidate();
        if (selected is null)
        {
            return null;
        }

        if (selected.Rarity < ItemRarity.Rare)
        {
            return null;
        }

        var key = BuildEquipmentKey(selected);
        if (string.Equals(_activeHeirloomKey, key, StringComparison.Ordinal))
        {
            _activeHeirloomLevel = Math.Min(10, _activeHeirloomLevel + 1);
        }
        else
        {
            _activeHeirloomKey = key;
            _activeHeirloomLevel = 1;
        }

        _designatedHeirloomKey = key;

        var heirloom = ApplyHeirloomGrowth(selected, _activeHeirloomLevel);

        _heirlooms.Add(heirloom);
        _heirloomEntries.Add(new HeirloomEntry(
            Name: heirloom.Name,
            Rarity: heirloom.Rarity,
            Level: _activeHeirloomLevel,
            Era: heirloom.Era));

        if (_heirlooms.Count > MaxHeirloomHistory)
        {
            _heirlooms.RemoveAt(0);
        }

        if (_heirloomEntries.Count > MaxHeirloomHistory)
        {
            _heirloomEntries.RemoveAt(0);
        }

        return heirloom;
    }

    public StatBonus GetEquippedStatBonus()
    {
        var total = StatBonus.Zero;
        foreach (var item in _equippedItems.Values)
        {
            total += item.TotalStatBonus;
        }

        return total;
    }

    public int GetEquippedLifespanModifier()
    {
        var total = 0;
        foreach (var item in _equippedItems.Values)
        {
            total += item.LifespanModifier;
        }

        return total;
    }

    public string BuildCurrentSummary()
    {
        var weapon = _equippedItems.TryGetValue(EquipmentSlot.Weapon, out var w) ? w.Name : "空";
        var armor = _equippedItems.TryGetValue(EquipmentSlot.Armor, out var a) ? a.Name : "空";
        var accessory = _equippedItems.TryGetValue(EquipmentSlot.Accessory, out var x) ? x.Name : "空";

        var sample = _currentItems.Count == 0
            ? "空"
            : string.Join(" / ", _currentItems.TakeLast(2).Select(item => item.Name));

        return
            $"装備枠: 武器[{weapon}] 防具[{armor}] 装飾[{accessory}]"
            + $" / バッグ:{_currentItems.Count}/{MaxInventorySlots} ({sample})";
    }

    public string BuildHeirloomSummary()
    {
        if (_heirloomEntries.Count == 0)
        {
            return "家宝: なし";
        }

        var latest = _heirloomEntries[^1];
        return $"家宝:{_heirloomEntries.Count} 最新:{latest.Name}({latest.Rarity}/+{latest.Level})";
    }

    public string BuildHeirloomCollectionSummary()
    {
        if (_heirloomEntries.Count == 0)
        {
            return "家宝一覧: なし";
        }

        var preview = _heirloomEntries
            .TakeLast(3)
            .Select(entry => $"{entry.Name}(+{entry.Level})");
        return $"家宝一覧: {string.Join(" / ", preview)}";
    }

    private ItemRarity RollRarity(int luck)
    {
        var rates = BuildRarityRates(luck);
        var roll = _rng.Randf() * 100f;
        if (roll < rates.Legendary)
        {
            return ItemRarity.Legendary;
        }

        if (roll < rates.Legendary + rates.Epic)
        {
            return ItemRarity.Epic;
        }

        if (roll < rates.Legendary + rates.Epic + rates.Rare)
        {
            return ItemRarity.Rare;
        }

        if (roll < rates.Legendary + rates.Epic + rates.Rare + rates.Uncommon)
        {
            return ItemRarity.Uncommon;
        }

        return ItemRarity.Common;
    }

    private Equipment BuildItem(LifePhase phase, string eraName, ItemRarity rarity, EquipmentSlot slot)
    {
        var rarityText = rarity switch
        {
            ItemRarity.Common => "コモン",
            ItemRarity.Uncommon => "アンコモン",
            ItemRarity.Rare => "レア",
            ItemRarity.Epic => "エピック",
            ItemRarity.Legendary => "レジェンダリー",
            _ => "不明",
        };

        var phasePrefix = phase switch
        {
            LifePhase.Childhood => "好奇心",
            LifePhase.Youth => "闘志",
            LifePhase.Midlife => "熟練",
            LifePhase.Elderly => "叡智",
            _ => "無名",
        };

        var slotText = slot switch
        {
            EquipmentSlot.Weapon => "武器",
            EquipmentSlot.Armor => "防具",
            EquipmentSlot.Accessory => "装飾",
            _ => "装備",
        };

        var bonusValue = rarity switch
        {
            ItemRarity.Common => 1,
            ItemRarity.Uncommon => 2,
            ItemRarity.Rare => 3,
            ItemRarity.Epic => 5,
            ItemRarity.Legendary => 8,
            _ => 1,
        };

        var bonus = phase switch
        {
            LifePhase.Childhood => new StatBonus(0, bonusValue, 0, bonusValue, 0),
            LifePhase.Youth => new StatBonus(bonusValue, 0, bonusValue, 0, 0),
            LifePhase.Midlife => new StatBonus(0, bonusValue, 0, 0, bonusValue),
            LifePhase.Elderly => new StatBonus(0, 0, 0, bonusValue, bonusValue),
            _ => StatBonus.Zero,
        };

        var suffix = rarity switch
        {
            ItemRarity.Common => "護符",
            ItemRarity.Uncommon => "指輪",
            ItemRarity.Rare => "紋章",
            ItemRarity.Epic => "家宝",
            ItemRarity.Legendary => "聖遺物",
            _ => "遺物",
        };

        var statBonuses = ToStatDictionary(bonus);
        var lifespanModifier = rarity switch
        {
            ItemRarity.Common => 0,
            ItemRarity.Uncommon => 0,
            ItemRarity.Rare => 1,
            ItemRarity.Epic => 2,
            ItemRarity.Legendary => 3,
            _ => 0,
        };

        return new Equipment(
            Name: $"{rarityText} {eraName} {phasePrefix}の{suffix}({slotText})",
            Rarity: rarity,
            StatBonuses: statBonuses,
            LifespanModifier: lifespanModifier,
            IsHeirloom: false,
            Era: eraName,
            Slot: slot);
    }

    private (float Common, float Uncommon, float Rare, float Epic, float Legendary) BuildRarityRates(int luck)
    {
        var common = 60f;
        var uncommon = 25f;
        var rare = 10f;
        var epic = 4f;
        var legendary = 1f;

        var upperTierBonus = Mathf.Max(0f, luck) * 0.5f;
        rare += upperTierBonus;
        epic += upperTierBonus;
        legendary += upperTierBonus;

        var totalBoost = upperTierBonus * 3f;
        common = Mathf.Max(1f, common - totalBoost * 0.8f);
        uncommon = Mathf.Max(1f, uncommon - totalBoost * 0.2f);

        var sum = common + uncommon + rare + epic + legendary;
        if (sum <= 0f)
        {
            return (60f, 25f, 10f, 4f, 1f);
        }

        var normalize = 100f / sum;
        return (common * normalize, uncommon * normalize, rare * normalize, epic * normalize, legendary * normalize);
    }

    private EquipmentSlot RollSlot()
    {
        return (EquipmentSlot)_rng.RandiRange((int)EquipmentSlot.Weapon, (int)EquipmentSlot.Accessory);
    }

    private bool TryAutoEquip(Equipment item, out string message)
    {
        if (!_equippedItems.TryGetValue(item.Slot, out var current))
        {
            _equippedItems[item.Slot] = item;
            message = $"{SlotToText(item.Slot)}に装備";
            return true;
        }

        if (item.ValueScore <= current.ValueScore)
        {
            message = string.Empty;
            return false;
        }

        if (_currentItems.Count >= MaxInventorySlots)
        {
            message = string.Empty;
            return false;
        }

        _currentItems.Add(current);
        _equippedItems[item.Slot] = item;
        message = $"{SlotToText(item.Slot)}を更新装備";
        return true;
    }

    private bool ShouldReplaceEquipped(Equipment incoming, out Equipment equippedToStore)
    {
        equippedToStore = default!;
        if (!_equippedItems.TryGetValue(incoming.Slot, out var current))
        {
            return false;
        }

        if (incoming.ValueScore <= current.ValueScore)
        {
            return false;
        }

        equippedToStore = current;
        return true;
    }

    private Equipment? SelectReplacementCandidate()
    {
        if (_currentItems.Count == 0)
        {
            return null;
        }

        return _currentItems
            .OrderBy(item => item.ValueScore)
            .ThenBy(item => item.Rarity)
            .First();
    }

    private Equipment? ResolveDesignatedHeirloomCandidate()
    {
        if (string.IsNullOrWhiteSpace(_designatedHeirloomKey))
        {
            return null;
        }

        return _currentItems
            .Concat(_equippedItems.Values)
            .Where(item => item.Rarity >= ItemRarity.Rare)
            .FirstOrDefault(item => string.Equals(BuildEquipmentKey(item), _designatedHeirloomKey, StringComparison.Ordinal));
    }

    private Equipment? ResolveFallbackHeirloomCandidate()
    {
        var candidates = GetHeirloomCandidates();
        return candidates.Count == 0 ? null : candidates[0];
    }

    private static Equipment ApplyHeirloomGrowth(Equipment source, int level)
    {
        var growth = Math.Clamp(level, 1, 10);
        var boostedStatBonuses = source.StatBonuses.ToDictionary(
            pair => pair.Key,
            pair => pair.Value > 0 ? pair.Value + growth : pair.Value);
        var boostedLifespan = source.LifespanModifier >= 0
            ? source.LifespanModifier + growth
            : source.LifespanModifier;

        return source with
        {
            StatBonuses = boostedStatBonuses,
            LifespanModifier = boostedLifespan,
            IsHeirloom = true,
        };
    }

    private static string BuildEquipmentKey(Equipment equipment)
    {
        return $"{equipment.Name}|{equipment.Rarity}|{equipment.Slot}|{equipment.Era}";
    }

    private static string BuildRarityMessage(Equipment item, DropPresentation presentation)
    {
        return $"[{presentation.Tone}] {item.Name}";
    }

    private static string SlotToText(EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.Weapon => "武器枠",
            EquipmentSlot.Armor => "防具枠",
            EquipmentSlot.Accessory => "装飾枠",
            _ => "装備枠",
        };
    }

    private static IReadOnlyDictionary<StatType, int> ToStatDictionary(StatBonus bonus)
    {
        return new Dictionary<StatType, int>
        {
            [StatType.Vitality] = bonus.Vitality,
            [StatType.Intelligence] = bonus.Intelligence,
            [StatType.Charisma] = bonus.Charisma,
            [StatType.Luck] = bonus.Luck,
            [StatType.Wealth] = bonus.Wealth,
        };
    }

    private static DropPresentation BuildDropPresentation(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => new DropPresentation("白", Colors.White, false),
            ItemRarity.Uncommon => new DropPresentation("緑", new Color(0.45f, 0.96f, 0.56f), false),
            ItemRarity.Rare => new DropPresentation("青", new Color(0.43f, 0.66f, 1f), false),
            ItemRarity.Epic => new DropPresentation("紫", new Color(0.77f, 0.52f, 0.98f), false),
            ItemRarity.Legendary => new DropPresentation("金", new Color(1f, 0.87f, 0.26f), true),
            _ => DefaultPresentation,
        };
    }

    private static readonly DropPresentation DefaultPresentation = new("白", Colors.White, false);

    private sealed class PendingReplacementState
    {
        public Equipment Incoming { get; }
        public Equipment Candidate { get; }
        public bool EquipIncoming { get; }
        public Equipment? EquippedToStore { get; }
        public DropPresentation Presentation { get; }

        public PendingReplacementState(
            Equipment incoming,
            Equipment candidate,
            bool equipIncoming,
            Equipment? equippedToStore,
            DropPresentation presentation)
        {
            Incoming = incoming;
            Candidate = candidate;
            EquipIncoming = equipIncoming;
            EquippedToStore = equippedToStore;
            Presentation = presentation;
        }
    }
}
