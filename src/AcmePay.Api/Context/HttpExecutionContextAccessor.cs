using AcmePay.Application.Abstractions.Context;
using AcmePay.Common.Constants;
using ExecutionContext = AcmePay.Application.Abstractions.Context.ExecutionContext;

namespace AcmePay.Api.Context;

public sealed class HttpExecutionContextAccessor(IHttpContextAccessor httpContextAccessor) : IExecutionContextAccessor
{
    public ExecutionContext GetCurrent()
    {
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext is null)
        {
            return new ExecutionContext(
                TriggeredBy: "system",
                CorrelationId: null);
        }

        var triggeredBy = GetHeader(httpContext, HeaderNames.TriggeredBy)
                          ?? "merchant-api";

        var correlationId = GetHeader(httpContext, HeaderNames.CorrelationId)
                            ?? httpContext.TraceIdentifier;

        return new ExecutionContext(
            TriggeredBy: triggeredBy,
            CorrelationId: correlationId);
    }

    private static string? GetHeader(HttpContext httpContext, string headerName)
    {
        return httpContext.Request.Headers.TryGetValue(headerName, out var values)
            ? values.ToString()
            : null;
    }
}