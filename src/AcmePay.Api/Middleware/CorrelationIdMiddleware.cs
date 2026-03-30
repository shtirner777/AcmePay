using AcmePay.Common.Constants;
using Serilog.Context;

namespace AcmePay.Api.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderNames.CorrelationId, out var headerValue)
                            && !string.IsNullOrWhiteSpace(headerValue)
            ? headerValue.ToString().Trim()
            : context.TraceIdentifier;

        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderNames.CorrelationId] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
