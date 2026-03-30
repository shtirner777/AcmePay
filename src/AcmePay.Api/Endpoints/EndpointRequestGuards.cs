using AcmePay.Api.ProblemDetails;
using AcmePay.Common.Constants;

namespace AcmePay.Api.Endpoints;

internal static class EndpointRequestGuards
{
    public static bool TryGetIdempotencyKey(
        HttpContext context,
        out string idempotencyKey,
        out IResult? errorResult)
    {
        if (context.Request.Headers.TryGetValue(HeaderNames.IdempotencyKey, out var headerValue)
            && !string.IsNullOrWhiteSpace(headerValue))
        {
            idempotencyKey = headerValue.ToString().Trim();
            errorResult = null;
            return true;
        }

        idempotencyKey = string.Empty;
        errorResult = ApiProblemResults.MissingRequiredHeader(context, HeaderNames.IdempotencyKey);
        return false;
    }
}
