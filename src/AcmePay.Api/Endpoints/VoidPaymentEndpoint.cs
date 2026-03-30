using AcmePay.Api.Contracts.Payments;
using AcmePay.Api.Mapping;
using AcmePay.Application.Features.Payments.Void;

namespace AcmePay.Api.Endpoints;

public static class VoidPaymentEndpoint
{
    public static IEndpointRouteBuilder MapVoidPaymentEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/merchants/{merchantId}/payments/{paymentId:guid}/void", HandleAsync)
            .WithName("VoidPayment")
            .WithTags("Payments")
            .WithSummary("Void a payment")
            .WithDescription("Voids a previously authorized payment before it is captured.")
            .Produces<VoidPaymentResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> HandleAsync(
        string merchantId,
        Guid paymentId,
        HttpContext httpContext,
        VoidPaymentCommandHandler handler,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(nameof(VoidPaymentEndpoint));
        if (!EndpointRequestGuards.TryGetIdempotencyKey(httpContext, out var idempotencyKey, out var errorResult))
        {
            return errorResult!;
        }

        var command = VoidPaymentMappings.ToCommand(merchantId, paymentId, idempotencyKey);

        logger.LogInformation(
            "Voiding payment {PaymentId} for merchant {MerchantId} with idempotency key {IdempotencyKey}",
            paymentId,
            merchantId,
            idempotencyKey);

        var result = await handler.Handle(command, cancellationToken);

        logger.LogInformation(
            "Payment voided successfully. PaymentId: {PaymentId}, MerchantId: {MerchantId}",
            result.PaymentId,
            result.MerchantId);

        return Results.Ok(result.ToResponse());
    }
}
