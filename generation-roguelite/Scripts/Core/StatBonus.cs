namespace GenerationRoguelite.Core;

public readonly record struct StatBonus(
    int Vitality,
    int Intelligence,
    int Charisma,
    int Luck,
    int Wealth)
{
    public static readonly StatBonus Zero = new(0, 0, 0, 0, 0);

    public bool IsZero => this == Zero;

    public int Magnitude =>
        System.Math.Abs(Vitality)
        + System.Math.Abs(Intelligence)
        + System.Math.Abs(Charisma)
        + System.Math.Abs(Luck)
        + System.Math.Abs(Wealth);

    public static StatBonus operator +(StatBonus left, StatBonus right)
    {
        return new StatBonus(
            left.Vitality + right.Vitality,
            left.Intelligence + right.Intelligence,
            left.Charisma + right.Charisma,
            left.Luck + right.Luck,
            left.Wealth + right.Wealth);
    }

    public static StatBonus operator -(StatBonus left, StatBonus right)
    {
        return new StatBonus(
            left.Vitality - right.Vitality,
            left.Intelligence - right.Intelligence,
            left.Charisma - right.Charisma,
            left.Luck - right.Luck,
            left.Wealth - right.Wealth);
    }
}
