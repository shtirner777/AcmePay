using AcmePay.Application.Abstractions.Persistence;
using AcmePay.Application.Abstractions.Time;
using AcmePay.Application.Payments.Audit;
using AcmePay.Application.Payments.Gateways;
using AcmePay.Application.Payments.Idempotency;
using AcmePay.Application.Payments.Repositories;
using AcmePay.Infrastructure.Audit;
using AcmePay.Infrastructure.Gateways;
using AcmePay.Infrastructure.Idempotency;
using AcmePay.Infrastructure.Persistence.Connections;
using AcmePay.Infrastructure.Persistence.Repositories;
using AcmePay.Infrastructure.Persistence.Transactions;
using AcmePay.Infrastructure.Time;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AcmePay.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException(
                                   "Connection string 'DefaultConnection' was not found.");

        services.AddScoped<IDbConnectionFactory>(_ => new NpgsqlConnectionFactory(connectionString));
        services.AddScoped<IUnitOfWork, DapperUnitOfWork>();

        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IIdempotencyStore, IdempotencyStore>();
        services.AddScoped<IAuditLogWriter, AuditLogWriter>();
        services.AddScoped<ICardNetworkGateway, MockCardNetworkGateway>();

        services.AddSingleton<IClock, SystemClock>();

        return services;
    }
}