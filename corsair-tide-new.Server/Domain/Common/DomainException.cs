namespace CorsairTide.Server.Domain.Common;

/// <summary>Thrown when a domain invariant is violated.</summary>
public class DomainException(string message) : Exception(message);
