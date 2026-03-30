using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AcmePay.Api.Contracts.Payments;
using AcmePay.Common.Constants;
using AcmePay.IntegrationTests.TestHost;
using Xunit;

namespace AcmePay.IntegrationTests.Payments;

public sealed class AuthorizePaymentEndpointTests
{
    [Fact]
    public async Task PostAuthorize_ShouldReturnOk_WithDeterministicTimeAndAuditMetadata()
    {
        await using var factory = new AcmePayApiFactory();
        factory.Clock.Set(new DateTimeOffset(2026, 3, 30, 12, 15, 0, TimeSpan.Zero));
        using var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, PaymentApiTestConstants.AuthorizeRoute)
        {
            Content = JsonContent.Create(PaymentApiTestConstants.CreateAuthorizeRequest(42.50m))
        };
        request.Headers.Add(HeaderNames.IdempotencyKey, PaymentApiTestConstants.AuthorizePaymentIdempotencyKey);
        request.Headers.Add(HeaderNames.CorrelationId, PaymentApiTestConstants.AuthorizeCorrelationId);
        request.Headers.Add(HeaderNames.TriggeredBy, PaymentApiTestConstants.TriggeredBy);

        var response = await client.SendAsync(request);
        var payload = await response.Content.ReadFromJsonAsync<AuthorizePaymentResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal(factory.Clock.UtcNow, payload!.AuthorizedAtUtc);
        Assert.Equal(PaymentApiTestConstants.MerchantId, payload.MerchantId);
        Assert.Equal(PaymentApiTestConstants.AuthorizedStatus, payload.Status);
        Assert.Equal(PaymentApiTestConstants.AuthorizeCorrelationId, response.Headers.GetValues(HeaderNames.CorrelationId).Single());

        var auditEntry = Assert.Single(factory.AuditLogWriter.Snapshot());
        Assert.Equal(factory.Clock.UtcNow, auditEntry.OccurredOnUtc);
        Assert.Equal(PaymentApiTestConstants.TriggeredBy, auditEntry.TriggeredBy);
        Assert.Equal(PaymentApiTestConstants.AuthorizeCorrelationId, auditEntry.CorrelationId);
    }

    [Fact]
    public async Task PostAuthorize_SameIdempotencyKeyAndPayload_ShouldReturnCachedResponse_WithoutSecondAuthorization()
    {
        await using var factory = new AcmePayApiFactory();
        using var client = factory.CreateClient();

        var first = await PaymentApiDriver.SendAuthorizeAsync(
            client,
            PaymentApiTestConstants.AuthorizePaymentIdempotencyKey,
            42.50m);
        var second = await PaymentApiDriver.SendAuthorizeAsync(
            client,
            PaymentApiTestConstants.AuthorizePaymentIdempotencyKey,
            42.50m);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var firstPayload = await first.Content.ReadFromJsonAsync<AuthorizePaymentResponse>();
        var secondPayload = await second.Content.ReadFromJsonAsync<AuthorizePaymentResponse>();

        Assert.Equal(firstPayload, secondPayload);
        Assert.Equal(1, factory.CardGateway.Calls);
        Assert.Equal(1, factory.PaymentRepository.Count);
        Assert.Single(factory.AuditLogWriter.Snapshot());
    }

    [Fact]
    public async Task PostAuthorize_SameIdempotencyKeyWithDifferentPayload_ShouldReturnConflict()
    {
        await using var factory = new AcmePayApiFactory();
        using var client = factory.CreateClient();

        var first = await PaymentApiDriver.SendAuthorizeAsync(
            client,
            PaymentApiTestConstants.AuthorizePaymentIdempotencyKey,
            42.50m);
        var second = await PaymentApiDriver.SendAuthorizeAsync(
            client,
            PaymentApiTestConstants.AuthorizePaymentIdempotencyKey,
            99.99m);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        using var problem = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        Assert.Equal("Idempotency conflict", problem.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task PostAuthorize_WithoutIdempotencyHeader_ShouldReturnBadRequest()
    {
        await using var factory = new AcmePayApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            PaymentApiTestConstants.AuthorizeRoute,
            PaymentApiTestConstants.CreateAuthorizeRequest(42.50m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostAuthorize_WithDeclinedCard_ShouldReturnUnprocessableEntity()
    {
        await using var factory = new AcmePayApiFactory();
        using var client = factory.CreateClient();

        var response = await PaymentApiDriver.SendAuthorizeAsync(
            client,
            PaymentApiTestConstants.SecondAuthorizePaymentIdempotencyKey,
            42.50m,
            PaymentApiTestConstants.DeclinedVisaPan);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Payment authorization failed", problem.RootElement.GetProperty("title").GetString());
        Assert.Empty(factory.PaymentRepository.Snapshot());
        Assert.Empty(factory.AuditLogWriter.Snapshot());
    }
}
