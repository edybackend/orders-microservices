namespace Orders.Domain;

/// <summary>Thrown when an operation would break a business rule (an invariant of the domain).</summary>
public sealed class DomainException(string message) : Exception(message);
