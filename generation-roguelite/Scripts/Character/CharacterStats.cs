using System;

namespace GenerationRoguelite.Character;

public sealed class CharacterStats
{
    public int Health { get; set; } = 10;

    public int Intelligence { get; set; } = 10;

    public int Charm { get; set; } = 10;

    public int Luck { get; set; } = 10;

    public int Wealth { get; set; } = 10;

    public int Sum => Health + Intelligence + Charm + Luck + Wealth;

    public float CalculateBaseLifespan()
    {
        return 70f + Health * 0.5f;
    }

    public CharacterStats Clone()
    {
        return new CharacterStats
        {
            Health = Health,
            Intelligence = Intelligence,
            Charm = Charm,
            Luck = Luck,
            Wealth = Wealth,
        };
    }

    public void ApplyDelta(int health = 0, int intelligence = 0, int charm = 0, int luck = 0, int wealth = 0)
    {
        Health = Math.Clamp(Health + health, 1, 999);
        Intelligence = Math.Clamp(Intelligence + intelligence, 1, 999);
        Charm = Math.Clamp(Charm + charm, 1, 999);
        Luck = Math.Clamp(Luck + luck, 1, 999);
        Wealth = Math.Clamp(Wealth + wealth, 0, 9999);
    }
}
