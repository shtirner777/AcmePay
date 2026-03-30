using AcmePay.Application.Abstractions.Time;
using AcmePay.Api.DependencyInjection;
using AcmePay.Api.Endpoints;
using AcmePay.Api.Middleware;
using AcmePay.Infrastructure.DependencyInjection;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

builder.Services.AddOpenApi();
builder.Services.AddApiServices();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSerilogRequestLogging();

app.MapOpenApi();

app.MapGet("/", () => Results.Redirect("/openapi/v1.json"));
app.MapGet("/health", (IClock clock) => Results.Ok(new
    {
        status = "ok",
        service = "AcmePay.Api",
        utc = clock.UtcNow
    }))
    .WithName("HealthCheck")
    .WithTags("Health");

app.MapAuthorizePaymentEndpoint();
app.MapCapturePaymentEndpoint();
app.MapVoidPaymentEndpoint();
app.MapRefundPaymentEndpoint();

app.Run();

public partial class Program;
