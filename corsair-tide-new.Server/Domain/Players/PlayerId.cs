namespace CorsairTide.Server.Domain.Players;

public record PlayerId(Guid Value)
{
    public static PlayerId New() => new(Guid.NewGuid());
    public static PlayerId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
