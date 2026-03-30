namespace AcmePay.Application.Exceptions;

public sealed class ConcurrencyException(string message) : Exception(message);