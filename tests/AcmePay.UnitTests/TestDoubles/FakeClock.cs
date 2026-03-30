using AcmePay.Application.Abstractions.Time;

namespace AcmePay.UnitTests.TestDoubles;

internal sealed class FakeClock : IClock
{
    public FakeClock(DateTimeOffset utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTimeOffset UtcNow { get; private set; }

    public void Set(DateTimeOffset utcNow) => UtcNow = utcNow;
    public void Advance(TimeSpan delta) => UtcNow = UtcNow.Add(delta);
}
