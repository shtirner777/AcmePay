using AcmePay.Api.Contracts.Payments;
using AcmePay.Api.Mapping;
using AcmePay.Application.Features.Payments.Capture;

namespace AcmePay.Api.Endpoints;

public static class CapturePaymentEndpoint
{
    public static IEndpointRouteBuilder MapCapturePaymentEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/merchants/{merchantId}/payments/{paymentId:guid}/capture", HandleAsync)
            .WithName("CapturePayment")
            .WithTags("Payments")
            .WithSummary("Capture a payment")
            .WithDescription("Captures all or part of a previously authorized payment.")
            .Produces<CapturePaymentResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> HandleAsync(
        string merchantId,
        Guid paymentId,
        CapturePaymentRequest request,
        HttpContext httpContext,
        CapturePaymentCommandHandler handler,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(nameof(CapturePaymentEndpoint));
        if (!EndpointRequestGuards.TryGetIdempotencyKey(httpContext, out var idempotencyKey, out var errorResult))
        {
            return errorResult!;
        }

        var command = request.ToCommand(merchantId, paymentId, idempotencyKey);

        logger.LogInformation(
            "Capturing payment {PaymentId} for merchant {MerchantId} with idempotency key {IdempotencyKey}",
            paymentId,
            merchantId,
            idempotencyKey);

        var result = await handler.Handle(command, cancellationToken);

        logger.LogInformation(
            "Payment captured successfully. PaymentId: {PaymentId}, MerchantId: {MerchantId}, CapturedAmount: {CapturedAmount}",
            result.PaymentId,
            result.MerchantId,
            result.CapturedAmount);

        return Results.Ok(result.ToResponse());
    }
}
