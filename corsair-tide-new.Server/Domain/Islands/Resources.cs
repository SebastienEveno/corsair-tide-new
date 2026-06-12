namespace CorsairTide.Server.Domain.Islands;

/// <summary>
/// Immutable value object representing a quantity of the four pirate resources.
/// All arithmetic respects the floor-at-zero rule (resources never go negative).
/// </summary>
public record Resources(decimal Wood, decimal Gold, decimal Rum, decimal Food)
{
    public static readonly Resources Zero = new(0, 0, 0, 0);

    public Resources Add(Resources other) => new(
        Wood + other.Wood,
        Gold + other.Gold,
        Rum + other.Rum,
        Food + other.Food);

    /// <summary>Subtracts cost, flooring each component at zero.</summary>
    public Resources Subtract(Resources cost) => new(
        Math.Max(0, Wood - cost.Wood),
        Math.Max(0, Gold - cost.Gold),
        Math.Max(0, Rum - cost.Rum),
        Math.Max(0, Food - cost.Food));

    /// <summary>Caps each component at the corresponding capacity value.</summary>
    public Resources CapAt(Resources capacity) => new(
        Math.Min(Wood, capacity.Wood),
        Math.Min(Gold, capacity.Gold),
        Math.Min(Rum, capacity.Rum),
        Math.Min(Food, capacity.Food));

    public bool CanAfford(Resources cost) =>
        Wood >= cost.Wood &&
        Gold >= cost.Gold &&
        Rum >= cost.Rum &&
        Food >= cost.Food;
}
