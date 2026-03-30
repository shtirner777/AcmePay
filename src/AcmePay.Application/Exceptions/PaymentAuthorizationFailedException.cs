namespace AcmePay.Application.Exceptions;

public sealed class PaymentAuthorizationFailedException(string message) : Exception(message);