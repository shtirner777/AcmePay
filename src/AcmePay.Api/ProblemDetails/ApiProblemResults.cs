using System.Text;

namespace AcmePay.Api.ProblemDetails;

public static class ApiProblemResults
{
    private static IResult Problem(
        HttpContext context,
        int statusCode,
        string title,
        string detail,
        object? extensions = null)
    {
        return Results.Content(
            ApiProblemDetailsFactory.CreateProblemJson(context, statusCode, title, detail, extensions),
            contentType: "application/problem+json",
            contentEncoding: Encoding.UTF8,
            statusCode: statusCode);
    }

    public static IResult MissingRequiredHeader(HttpContext context, string headerName)
    {
        return Problem(
            context,
            StatusCodes.Status400BadRequest,
            "Missing required header",
            $"Header '{headerName}' is required.");
    }
}
