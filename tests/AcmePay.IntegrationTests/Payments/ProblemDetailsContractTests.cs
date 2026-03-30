using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AcmePay.Api.Contracts.Payments;
using AcmePay.Common.Constants;
using AcmePay.IntegrationTests.TestHost;
using Xunit;

namespace AcmePay.IntegrationTests.Payments;

public sealed class ProblemDetailsContractTests
{
    [Fact]
    public async Task MissingIdempotencyHeader_ShouldReturnCanonicalProblemDetailsPayload()
    {
        await using var factory = new AcmePayApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            PaymentApiTestConstants.AuthorizeRoute,
            PaymentApiTestConstants.CreateAuthorizeRequest(42.50m));

        var payload = await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.BadRequest,
            "Missing required header",
            expectedErrors: false);

        Assert.Contains(HeaderNames.IdempotencyKey, payload.GetProperty("detail").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ValidationFailure_ShouldReturnCanonicalProblemDetailsPayloadWithErrorsExtension()
    {
        await using var factory = new AcmePayApiFactory();
        using var client = factory.CreateClient();

        var authorize = await PaymentApiDriver.SendAuthorizeAsync(
            client,
            PaymentApiTestConstants.AuthorizePaymentIdempotencyKey,
            100m);
        var authorizePayload = await authorize.Content.ReadFromJsonAsync<AuthorizePaymentResponse>();

        var response = await PaymentApiDriver.SendCaptureAsync(
            client,
            authorizePayload!.PaymentId,
            PaymentApiTestConstants.CapturePaymentIdempotencyKey,
            0m);

        var payload = await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.BadRequest,
            "Validation failed",
            expectedErrors: true);

        Assert.True(payload.GetProperty("errors").TryGetProperty("Amount", out var amountErrors));
        Assert.True(amountErrors.GetArrayLength() > 0);
    }

    [Fact]
    public async Task IdempotencyConflict_ShouldReturnCanonicalProblemDetailsPayload()
    {
        await using var factory = new AcmePayApiFactory();
        using var client = factory.CreateClient();

        await PaymentApiDriver.SendAuthorizeAsync(
            client,
            PaymentApiTestConstants.AuthorizePaymentIdempotencyKey,
            42.50m);

        var response = await PaymentApiDriver.SendAuthorizeAsync(
            client,
            PaymentApiTestConstants.AuthorizePaymentIdempotencyKey,
            99.99m);

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.Conflict,
            "Idempotency conflict",
            expectedErrors: false);
    }

    [Fact]
    public async Task DomainRuleViolation_ShouldReturnCanonicalProblemDetailsPayload()
    {
        await using var factory = new AcmePayApiFactory();
        using var client = factory.CreateClient();

        var authorize = await PaymentApiDriver.SendAuthorizeAsync(
            client,
            PaymentApiTestConstants.AuthorizePaymentIdempotencyKey,
            50m);
        var authorizePayload = await authorize.Content.ReadFromJsonAsync<AuthorizePaymentResponse>();

        var response = await PaymentApiDriver.SendCaptureAsync(
            client,
            authorizePayload!.PaymentId,
            PaymentApiTestConstants.CapturePaymentIdempotencyKey,
            60m);

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.Conflict,
            "Domain rule violation",
            expectedErrors: false);
    }

    private static async Task<JsonElement> AssertProblemDetailsAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatusCode,
        string expectedTitle,
        bool expectedErrors)
    {
        Assert.Equal(expectedStatusCode, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.True(root.TryGetProperty("type", out var type));
        Assert.True(root.TryGetProperty("title", out var title));
        Assert.True(root.TryGetProperty("status", out var status));
        Assert.True(root.TryGetProperty("detail", out var detail));
        Assert.True(root.TryGetProperty("traceId", out var traceId));

        Assert.Equal(expectedTitle, title.GetString());
        Assert.Equal((int)expectedStatusCode, status.GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(type.GetString()));
        Assert.False(string.IsNullOrWhiteSpace(detail.GetString()));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
        Assert.Equal(expectedErrors, root.TryGetProperty("errors", out _));

        return root.Clone();
    }
}
