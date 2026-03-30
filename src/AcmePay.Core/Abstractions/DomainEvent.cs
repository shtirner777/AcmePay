namespace AcmePay.Core.Abstractions;

public abstract record DomainEvent(DateTimeOffset OccurredOnUtc);