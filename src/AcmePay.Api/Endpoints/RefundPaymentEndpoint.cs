using AcmePay.Api.Contracts.Payments;
using AcmePay.Api.Mapping;
using AcmePay.Application.Features.Payments.Refund;

namespace AcmePay.Api.Endpoints;

public static class RefundPaymentEndpoint
{
    public static IEndpointRouteBuilder MapRefundPaymentEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/merchants/{merchantId}/payments/{paymentId:guid}/refund", HandleAsync)
            .WithName("RefundPayment")
            .WithTags("Payments")
            .WithSummary("Refund a payment")
            .WithDescription("Refunds all or part of a previously captured payment.")
            .Produces<RefundPaymentResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> HandleAsync(
        string merchantId,
        Guid paymentId,
        RefundPaymentRequest request,
        HttpContext httpContext,
        RefundPaymentCommandHandler handler,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(nameof(RefundPaymentEndpoint));
        if (!EndpointRequestGuards.TryGetIdempotencyKey(httpContext, out var idempotencyKey, out var errorResult))
        {
            return errorResult!;
        }

        var command = request.ToCommand(merchantId, paymentId, idempotencyKey);

        logger.LogInformation(
            "Refunding payment {PaymentId} for merchant {MerchantId} with idempotency key {IdempotencyKey}",
            paymentId,
            merchantId,
            idempotencyKey);

        var result = await handler.Handle(command, cancellationToken);

        logger.LogInformation(
            "Payment refunded successfully. PaymentId: {PaymentId}, MerchantId: {MerchantId}, RefundedAmount: {RefundedAmount}",
            result.PaymentId,
            result.MerchantId,
            result.RefundedAmount);

        return Results.Ok(result.ToResponse());
    }
}
