namespace AcmePay.Common.Constants;

public static class HeaderNames
{
    public const string IdempotencyKey = "Idempotency-Key";
    public const string CorrelationId = "X-Correlation-Id";
    public const string TriggeredBy = "X-Triggered-By";
}