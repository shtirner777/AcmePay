using System.Text.Json;

namespace AcmePay.Api.ProblemDetails;

public static class ApiProblemDetailsFactory
{
    public static string CreateProblemJson(
        HttpContext context,
        int statusCode,
        string title,
        string detail,
        object? extensions = null)
    {
        var payload = new Dictionary<string, object?>
        {
            ["type"] = $"https://httpstatuses.com/{statusCode}",
            ["title"] = title,
            ["status"] = statusCode,
            ["detail"] = detail,
            ["traceId"] = context.TraceIdentifier
        };

        if (extensions is not null)
        {
            foreach (var property in extensions.GetType().GetProperties())
            {
                payload[property.Name] = property.GetValue(extensions);
            }
        }

        return JsonSerializer.Serialize(payload);
    }
}
