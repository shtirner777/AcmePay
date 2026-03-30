namespace AcmePay.Core.Exceptions;

public sealed class DomainRuleViolationException(string message) : Exception(message);