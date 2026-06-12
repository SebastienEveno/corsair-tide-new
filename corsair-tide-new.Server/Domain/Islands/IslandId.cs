namespace CorsairTide.Server.Domain.Islands;

public record IslandId(Guid Value)
{
    public static IslandId New() => new(Guid.NewGuid());
    public static IslandId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
