using System.Collections.Generic;

namespace Subspace;

/// <summary>
/// Tracks one type of resource/item and a quantity.
/// </summary>
public class InventoryItem
{
    public string Id { get; }
    public string DisplayName { get; }
    public int Quantity { get; set; }

    public InventoryItem(string id, string displayName, int quantity = 0)
    {
        Id = id;
        DisplayName = displayName;
        Quantity = quantity;
    }
}

/// <summary>
/// A flat key→quantity inventory.  Items that reach zero are kept in the list
/// (so the UI can still show them) but are automatically clamped to ≥ 0.
/// </summary>
public static class ItemId
{
    // Raw materials
    public const string METAL_ORE    = "metal_ore";
    public const string FUEL_CELL    = "fuel_cell";
    public const string FOOD_RATION  = "food_ration";
    public const string WATER        = "water";
    public const string MEDICINE     = "medicine";
    public const string WIRE         = "wire";
    public const string HULL_PLATE   = "hull_plate";
    // Currency
    public const string CREDITS      = "credits";
}

public class Inventory
{
    private readonly Dictionary<string, InventoryItem> _items = new();

    public Inventory()
    {
        // Register every known item so the UI always has an entry
        Register(ItemId.CREDITS,     "Credits",     500);
        Register(ItemId.METAL_ORE,   "Metal Ore",   20);
        Register(ItemId.FUEL_CELL,   "Fuel Cells",  10);
        Register(ItemId.FOOD_RATION, "Food Rations",30);
        Register(ItemId.WATER,       "Water",       40);
        Register(ItemId.MEDICINE,    "Medicine",    5);
        Register(ItemId.WIRE,        "Wire",        15);
        Register(ItemId.HULL_PLATE,  "Hull Plate",  8);
    }

    private void Register(string id, string name, int startQty = 0)
    {
        _items[id] = new InventoryItem(id, name, startQty);
    }

    public int Get(string id) => _items.TryGetValue(id, out var item) ? item.Quantity : 0;

    public void Add(string id, int amount)
    {
        if (_items.TryGetValue(id, out var item))
            item.Quantity = System.Math.Max(0, item.Quantity + amount);
    }

    /// <summary>Returns false (and does nothing) if there is insufficient stock.</summary>
    public bool Spend(string id, int amount)
    {
        if (!_items.TryGetValue(id, out var item) || item.Quantity < amount)
            return false;
        item.Quantity -= amount;
        return true;
    }

    public IEnumerable<InventoryItem> AllItems => _items.Values;
}
