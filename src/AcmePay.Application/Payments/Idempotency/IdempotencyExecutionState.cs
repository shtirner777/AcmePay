namespace AcmePay.Application.Payments.Idempotency;

public enum IdempotencyExecutionState
{
    Claimed = 1,
    Completed = 2,
    InProgress = 3,
    Conflict = 4
}