namespace AcmePay.Application.Exceptions;

public sealed class BusinessException(string message) : Exception(message);