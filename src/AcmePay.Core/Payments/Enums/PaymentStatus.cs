namespace AcmePay.Core.Payments.Enums;

public enum PaymentStatus
{
    Authorized = 1,
    PartiallyCaptured = 2,
    Captured = 3,
    Voided = 4,
    PartiallyRefunded = 5,
    Refunded = 6
}