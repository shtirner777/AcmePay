using AcmePay.Api.ProblemDetails;
using AcmePay.Application.Exceptions;
using AcmePay.Core.Exceptions;
using FluentValidation;

namespace AcmePay.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException exception)
        {
            logger.LogWarning(exception,
                "Validation failed for request {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            var errors = exception.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(x => x.ErrorMessage).ToArray());

            await WriteProblemAsync(
                context,
                StatusCodes.Status400BadRequest,
                "Validation failed",
                "One or more validation errors occurred.",
                new { errors });
        }
        catch (IdempotencyConflictException exception)
        {
            logger.LogWarning(exception,
                "Idempotency conflict for request {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            await WriteProblemAsync(
                context,
                StatusCodes.Status409Conflict,
                "Idempotency conflict",
                exception.Message);
        }
        catch (PaymentAuthorizationFailedException exception)
        {
            logger.LogWarning(exception,
                "Payment authorization declined for request {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            await WriteProblemAsync(
                context,
                StatusCodes.Status422UnprocessableEntity,
                "Payment authorization failed",
                exception.Message);
        }
        catch (NotFoundException exception)
        {
            logger.LogWarning(exception,
                "Resource not found for request {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            await WriteProblemAsync(
                context,
                StatusCodes.Status404NotFound,
                "Resource not found",
                exception.Message);
        }
        catch (ConcurrencyException exception)
        {
            logger.LogWarning(exception,
                "Concurrency conflict for request {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            await WriteProblemAsync(
                context,
                StatusCodes.Status409Conflict,
                "Concurrency conflict",
                exception.Message);
        }
        catch (BusinessException exception)
        {
            logger.LogWarning(exception,
                "Business exception for request {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            await WriteProblemAsync(
                context,
                StatusCodes.Status409Conflict,
                "Business rule violation",
                exception.Message);
        }
        catch (DomainRuleViolationException exception)
        {
            logger.LogWarning(exception,
                "Domain rule violation for request {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            await WriteProblemAsync(
                context,
                StatusCodes.Status409Conflict,
                "Domain rule violation",
                exception.Message);
        }
        catch (Exception exception)
        {
            logger.LogError(exception,
                "Unhandled exception for request {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            await WriteProblemAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "Internal server error",
                "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        int statusCode,
        string title,
        string detail,
        object? extensions = null)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsync(
            ApiProblemDetailsFactory.CreateProblemJson(
                context,
                statusCode,
                title,
                detail,
                extensions));
    }
}
