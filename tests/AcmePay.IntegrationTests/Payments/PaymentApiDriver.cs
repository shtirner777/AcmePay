using System.Net.Http.Json;
using AcmePay.Common.Constants;

namespace AcmePay.IntegrationTests.Payments;

internal static class PaymentApiDriver
{
    public static Task<HttpResponseMessage> SendAuthorizeAsync(
        HttpClient client,
        string idempotencyKey,
        decimal amount,
        string? pan = null,
        string correlationId = PaymentApiTestConstants.AuthorizeCorrelationId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, PaymentApiTestConstants.AuthorizeRoute)
        {
            Content = JsonContent.Create(PaymentApiTestConstants.CreateAuthorizeRequest(amount, pan))
        };

        ApplyHeaders(request, idempotencyKey, correlationId);
        return client.SendAsync(request);
    }

    public static Task<HttpResponseMessage> SendCaptureAsync(
        HttpClient client,
        Guid paymentId,
        string idempotencyKey,
        decimal amount,
        string correlationId = PaymentApiTestConstants.LifecycleCorrelationId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, PaymentApiTestConstants.CaptureRoute(paymentId))
        {
            Content = JsonContent.Create(PaymentApiTestConstants.CreateCaptureRequest(amount))
        };

        ApplyHeaders(request, idempotencyKey, correlationId);
        return client.SendAsync(request);
    }

    public static Task<HttpResponseMessage> SendRefundAsync(
        HttpClient client,
        Guid paymentId,
        string idempotencyKey,
        decimal amount,
        string correlationId = PaymentApiTestConstants.LifecycleCorrelationId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, PaymentApiTestConstants.RefundRoute(paymentId))
        {
            Content = JsonContent.Create(PaymentApiTestConstants.CreateRefundRequest(amount))
        };

        ApplyHeaders(request, idempotencyKey, correlationId);
        return client.SendAsync(request);
    }

    public static Task<HttpResponseMessage> SendVoidAsync(
        HttpClient client,
        Guid paymentId,
        string idempotencyKey,
        string correlationId = PaymentApiTestConstants.LifecycleCorrelationId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, PaymentApiTestConstants.VoidRoute(paymentId));

        ApplyHeaders(request, idempotencyKey, correlationId);
        return client.SendAsync(request);
    }

    private static void ApplyHeaders(
        HttpRequestMessage request,
        string idempotencyKey,
        string correlationId)
    {
        request.Headers.Add(HeaderNames.IdempotencyKey, idempotencyKey);
        request.Headers.Add(HeaderNames.CorrelationId, correlationId);
        request.Headers.Add(HeaderNames.TriggeredBy, PaymentApiTestConstants.TriggeredBy);
    }
}
