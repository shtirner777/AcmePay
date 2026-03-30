using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AcmePay.Api.Contracts.Payments;
using AcmePay.IntegrationTests.TestHost;
using Xunit;

namespace AcmePay.IntegrationTests.Payments;

public sealed class PaymentLifecycleEndpointTests
{
    [Fact]
    public async Task AuthorizeCaptureRefund_ShouldPreserveClockValuesAcrossResponsesAndAuditTrail()
    {
        await using var factory = new AcmePayApiFactory();
        using var client = factory.CreateClient();

        var authorizedAt = new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero);
        var capturedAt = authorizedAt.AddMinutes(5);
        var refundedAt = authorizedAt.AddMinutes(10);

        factory.Clock.Set(authorizedAt);
        var authorize = await PaymentApiDriver.SendAuthorizeAsync(
            client,
            PaymentApiTestConstants.AuthorizePaymentIdempotencyKey,
            100m);
        var authorizePayload = await authorize.Content.ReadFromJsonAsync<AuthorizePaymentResponse>();
        Assert.Equal(HttpStatusCode.OK, authorize.StatusCode);
        Assert.NotNull(authorizePayload);
        Assert.Equal(authorizedAt, authorizePayload!.AuthorizedAtUtc);

        factory.Clock.Set(capturedAt);
        var capture = await PaymentApiDriver.SendCaptureAsync(
            client,
            authorizePayload.PaymentId,
            PaymentApiTestConstants.CapturePaymentIdempotencyKey,
            60m);
        var capturePayload = await capture.Content.ReadFromJsonAsync<CapturePaymentResponse>();
        Assert.Equal(HttpStatusCode.OK, capture.StatusCode);
        Assert.NotNull(capturePayload);
        Assert.Equal(capturedAt, capturePayload!.CapturedAtUtc);
        Assert.Equal(60m, capturePayload.CapturedAmount);
        Assert.Equal(40m, capturePayload.RemainingAuthorizedAmount);

        factory.Clock.Set(refundedAt);
        var refund = await PaymentApiDriver.SendRefundAsync(
            client,
            authorizePayload.PaymentId,
            PaymentApiTestConstants.RefundPaymentIdempotencyKey,
            10m);
        var refundPayload = await refund.Content.ReadFromJsonAsync<RefundPaymentResponse>();
        Assert.Equal(HttpStatusCode.OK, refund.StatusCode);
        Assert.NotNull(refundPayload);
        Assert.Equal(refundedAt, refundPayload!.RefundedAtUtc);
        Assert.Equal(10m, refundPayload.RefundedAmount);
        Assert.Equal(50m, refundPayload.RemainingRefundableAmount);
        Assert.Equal(PaymentApiTestConstants.PartiallyRefundedStatus, refundPayload.Status);

        var auditEntries = factory.AuditLogWriter.Snapshot().OrderBy(x => x.OccurredOnUtc).ToArray();
        Assert.Equal(3, auditEntries.Length);
        Assert.Equal(authorizedAt, auditEntries[0].OccurredOnUtc);
        Assert.Equal(capturedAt, auditEntries[1].OccurredOnUtc);
        Assert.Equal(refundedAt, auditEntries[2].OccurredOnUtc);
    }

    [Fact]
    public async Task AuthorizeThenVoid_ShouldReturnConfiguredClockValue()
    {
        await using var factory = new AcmePayApiFactory();
        using var client = factory.CreateClient();

        var authorizedAt = new DateTimeOffset(2026, 3, 30, 14, 0, 0, TimeSpan.Zero);
        var voidedAt = authorizedAt.AddMinutes(2);

        factory.Clock.Set(authorizedAt);
        var authorize = await PaymentApiDriver.SendAuthorizeAsync(
            client,
            PaymentApiTestConstants.AuthorizePaymentIdempotencyKey,
            80m);
        var authorizePayload = await authorize.Content.ReadFromJsonAsync<AuthorizePaymentResponse>();

        factory.Clock.Set(voidedAt);
        var response = await PaymentApiDriver.SendVoidAsync(
            client,
            authorizePayload!.PaymentId,
            PaymentApiTestConstants.VoidPaymentIdempotencyKey);
        var payload = await response.Content.ReadFromJsonAsync<VoidPaymentResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal(voidedAt, payload!.VoidedAtUtc);
        Assert.Equal(PaymentApiTestConstants.VoidedStatus, payload.Status);

        var auditEntries = factory.AuditLogWriter.Snapshot().OrderBy(x => x.OccurredOnUtc).ToArray();
        Assert.Equal(2, auditEntries.Length);
        Assert.Equal(voidedAt, auditEntries[1].OccurredOnUtc);
    }

    [Fact]
    public async Task Capture_WithSameIdempotencyKeyAndPayload_ShouldReturnCachedResponse()
    {
        await using var factory = new AcmePayApiFactory();
        using var client = factory.CreateClient();

        var authorize = await PaymentApiDriver.SendAuthorizeAsync(
            client,
            PaymentApiTestConstants.AuthorizePaymentIdempotencyKey,
            100m);
        var authorizePayload = await authorize.Content.ReadFromJsonAsync<AuthorizePaymentResponse>();

        var first = await PaymentApiDriver.SendCaptureAsync(
            client,
            authorizePayload!.PaymentId,
            PaymentApiTestConstants.CapturePaymentIdempotencyKey,
            40m);
        var second = await PaymentApiDriver.SendCaptureAsync(
            client,
            authorizePayload.PaymentId,
            PaymentApiTestConstants.CapturePaymentIdempotencyKey,
            40m);

        var firstPayload = await first.Content.ReadFromJsonAsync<CapturePaymentResponse>();
        var secondPayload = await second.Content.ReadFromJsonAsync<CapturePaymentResponse>();

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(firstPayload, secondPayload);
        Assert.Equal(2, factory.AuditLogWriter.Snapshot().Count);
    }

    [Fact]
    public async Task Capture_WithSameIdempotencyKeyAndDifferentPayload_ShouldReturnConflict()
    {
        await using var factory = new AcmePayApiFactory();
        using var client = factory.CreateClient();

        var authorize = await PaymentApiDriver.SendAuthorizeAsync(
            client,
            PaymentApiTestConstants.AuthorizePaymentIdempotencyKey,
            100m);
        var authorizePayload = await authorize.Content.ReadFromJsonAsync<AuthorizePaymentResponse>();

        var first = await PaymentApiDriver.SendCaptureAsync(
            client,
            authorizePayload!.PaymentId,
            PaymentApiTestConstants.CapturePaymentIdempotencyKey,
            40m);
        var second = await PaymentApiDriver.SendCaptureAsync(
            client,
            authorizePayload.PaymentId,
            PaymentApiTestConstants.CapturePaymentIdempotencyKey,
            50m);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        using var problem = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        Assert.Equal("Idempotency conflict", problem.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Capture_AboveRemainingAuthorizedAmount_ShouldReturnConflict()
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

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Domain rule violation", problem.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Void_AfterCapture_ShouldReturnConflict()
    {
        await using var factory = new AcmePayApiFactory();
        using var client = factory.CreateClient();

        var authorize = await PaymentApiDriver.SendAuthorizeAsync(
            client,
            PaymentApiTestConstants.AuthorizePaymentIdempotencyKey,
            100m);
        var authorizePayload = await authorize.Content.ReadFromJsonAsync<AuthorizePaymentResponse>();
        await PaymentApiDriver.SendCaptureAsync(
            client,
            authorizePayload!.PaymentId,
            PaymentApiTestConstants.CapturePaymentIdempotencyKey,
            10m);

        var response = await PaymentApiDriver.SendVoidAsync(
            client,
            authorizePayload.PaymentId,
            PaymentApiTestConstants.VoidPaymentIdempotencyKey);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Refund_AboveRemainingRefundableAmount_ShouldReturnConflict()
    {
        await using var factory = new AcmePayApiFactory();
        using var client = factory.CreateClient();

        var authorize = await PaymentApiDriver.SendAuthorizeAsync(
            client,
            PaymentApiTestConstants.AuthorizePaymentIdempotencyKey,
            100m);
        var authorizePayload = await authorize.Content.ReadFromJsonAsync<AuthorizePaymentResponse>();
        await PaymentApiDriver.SendCaptureAsync(
            client,
            authorizePayload!.PaymentId,
            PaymentApiTestConstants.CapturePaymentIdempotencyKey,
            50m);

        var response = await PaymentApiDriver.SendRefundAsync(
            client,
            authorizePayload.PaymentId,
            PaymentApiTestConstants.RefundPaymentIdempotencyKey,
            60m);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Refund_WithSameIdempotencyKeyAndPayload_ShouldReturnCachedResponse()
    {
        await using var factory = new AcmePayApiFactory();
        using var client = factory.CreateClient();

        var authorize = await PaymentApiDriver.SendAuthorizeAsync(
            client,
            PaymentApiTestConstants.AuthorizePaymentIdempotencyKey,
            100m);
        var authorizePayload = await authorize.Content.ReadFromJsonAsync<AuthorizePaymentResponse>();
        await PaymentApiDriver.SendCaptureAsync(
            client,
            authorizePayload!.PaymentId,
            PaymentApiTestConstants.CapturePaymentIdempotencyKey,
            70m);

        var first = await PaymentApiDriver.SendRefundAsync(
            client,
            authorizePayload.PaymentId,
            PaymentApiTestConstants.RefundPaymentIdempotencyKey,
            20m);
        var second = await PaymentApiDriver.SendRefundAsync(
            client,
            authorizePayload.PaymentId,
            PaymentApiTestConstants.RefundPaymentIdempotencyKey,
            20m);

        var firstPayload = await first.Content.ReadFromJsonAsync<RefundPaymentResponse>();
        var secondPayload = await second.Content.ReadFromJsonAsync<RefundPaymentResponse>();

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(firstPayload, secondPayload);
    }


    [Fact]
    public async Task Refund_TotalAcrossMultipleRequestsAboveCapturedAmount_ShouldReturnConflict()
    {
        await using var factory = new AcmePayApiFactory();
        using var client = factory.CreateClient();

        var authorize = await PaymentApiDriver.SendAuthorizeAsync(
            client,
            PaymentApiTestConstants.AuthorizePaymentIdempotencyKey,
            100m);
        var authorizePayload = await authorize.Content.ReadFromJsonAsync<AuthorizePaymentResponse>();
        await PaymentApiDriver.SendCaptureAsync(
            client,
            authorizePayload!.PaymentId,
            PaymentApiTestConstants.CapturePaymentIdempotencyKey,
            100m);
        await PaymentApiDriver.SendRefundAsync(
            client,
            authorizePayload.PaymentId,
            PaymentApiTestConstants.RefundPaymentIdempotencyKey,
            60m);

        var secondRefund = await PaymentApiDriver.SendRefundAsync(
            client,
            authorizePayload.PaymentId,
            PaymentApiTestConstants.SecondRefundPaymentIdempotencyKey,
            50m);

        Assert.Equal(HttpStatusCode.Conflict, secondRefund.StatusCode);

        using var problem = JsonDocument.Parse(await secondRefund.Content.ReadAsStringAsync());
        Assert.Equal("Domain rule violation", problem.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Void_WithSameIdempotencyKeyAfterCompletion_ShouldReturnCachedResponse()
    {
        await using var factory = new AcmePayApiFactory();
        using var client = factory.CreateClient();

        var authorize = await PaymentApiDriver.SendAuthorizeAsync(
            client,
            PaymentApiTestConstants.AuthorizePaymentIdempotencyKey,
            80m);
        var authorizePayload = await authorize.Content.ReadFromJsonAsync<AuthorizePaymentResponse>();

        var first = await PaymentApiDriver.SendVoidAsync(
            client,
            authorizePayload!.PaymentId,
            PaymentApiTestConstants.VoidPaymentIdempotencyKey);
        var second = await PaymentApiDriver.SendVoidAsync(
            client,
            authorizePayload.PaymentId,
            PaymentApiTestConstants.VoidPaymentIdempotencyKey);

        var firstPayload = await first.Content.ReadFromJsonAsync<VoidPaymentResponse>();
        var secondPayload = await second.Content.ReadFromJsonAsync<VoidPaymentResponse>();

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(firstPayload, secondPayload);
        Assert.Equal(2, factory.AuditLogWriter.Snapshot().Count);
    }

}
