using AcmePay.Api.Context;
using AcmePay.Application.Abstractions.Context;
using AcmePay.Application.Features.Payments.Authorize;
using AcmePay.Application.Features.Payments.Capture;
using AcmePay.Application.Features.Payments.Refund;
using AcmePay.Application.Features.Payments.Void;
using AcmePay.Application.Payments.Audit;
using FluentValidation;

namespace AcmePay.Api.DependencyInjection;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services.AddScoped<IExecutionContextAccessor, HttpExecutionContextAccessor>();
        services.AddScoped<IPaymentAuditLogFactory, PaymentAuditLogFactory>();

        services.AddScoped<AuthorizePaymentCommandHandler>();
        services.AddScoped<CapturePaymentCommandHandler>();
        services.AddScoped<VoidPaymentCommandHandler>();
        services.AddScoped<RefundPaymentCommandHandler>();

        services.AddValidatorsFromAssemblyContaining<AuthorizePaymentCommandValidator>();

        return services;
    }
}
