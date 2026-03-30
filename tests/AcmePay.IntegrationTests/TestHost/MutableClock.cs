using AcmePay.Application.Abstractions.Time;

namespace AcmePay.IntegrationTests.TestHost;

internal sealed class MutableClock(DateTimeOffset utcNow) : IClock
{
    public DateTimeOffset UtcNow { get; private set; } = utcNow;

    public void Set(DateTimeOffset utcNow) => UtcNow = utcNow;
    public void Advance(TimeSpan delta) => UtcNow = UtcNow.Add(delta);
}
