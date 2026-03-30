using AcmePay.Api.Contracts.Payments;
using AcmePay.Api.Mapping;
using AcmePay.Application.Features.Payments.Authorize;

namespace AcmePay.Api.Endpoints;

public static class AuthorizePaymentEndpoint
{
    public static IEndpointRouteBuilder MapAuthorizePaymentEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/merchants/{merchantId}/payments/authorize", HandleAsync)
            .WithName("AuthorizePayment")
            .WithTags("Payments")
            .WithSummary("Authorize a payment")
            .WithDescription("Authorizes a card payment and reserves funds without capturing them.")
            .Produces<AuthorizePaymentResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> HandleAsync(
        string merchantId,
        AuthorizePaymentRequest request,
        HttpContext httpContext,
        AuthorizePaymentCommandHandler handler,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(nameof(AuthorizePaymentEndpoint));
        if (!EndpointRequestGuards.TryGetIdempotencyKey(httpContext, out var idempotencyKey, out var errorResult))
        {
            return errorResult!;
        }

        var command = request.ToCommand(
            merchantId,
            idempotencyKey);

        logger.LogInformation(
            "Authorizing payment for merchant {MerchantId} with idempotency key {IdempotencyKey}",
            merchantId,
            idempotencyKey);

        var result = await handler.Handle(command, cancellationToken);

        logger.LogInformation(
            "Payment authorized successfully. PaymentId: {PaymentId}, MerchantId: {MerchantId}",
            result.PaymentId,
            result.MerchantId);

        return Results.Ok(result.ToResponse());
    }
}