using AcmePay.Application.Abstractions.Time;

namespace AcmePay.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}