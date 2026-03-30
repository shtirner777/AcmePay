using System.Net;
using System.Text.Json;
using AcmePay.IntegrationTests.TestHost;
using Xunit;

namespace AcmePay.IntegrationTests.Payments;

public sealed class HealthEndpointTests
{
    [Fact]
    public async Task GetHealth_ShouldUseInjectedClock()
    {
        await using var factory = new AcmePayApiFactory();
        factory.Clock.Set(new DateTimeOffset(2026, 3, 30, 18, 45, 0, TimeSpan.Zero));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var utc = document.RootElement.GetProperty("utc").GetDateTimeOffset();
        Assert.Equal(factory.Clock.UtcNow, utc);
    }
}
