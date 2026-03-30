using AcmePay.Application.Abstractions.Persistence;
using AcmePay.Application.Abstractions.Time;
using AcmePay.Application.Payments.Audit;
using AcmePay.Application.Payments.Gateways;
using AcmePay.Application.Payments.Idempotency;
using AcmePay.Application.Payments.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AcmePay.IntegrationTests.TestHost;

internal sealed class AcmePayApiFactory : WebApplicationFactory<Program>
{
    public MutableClock Clock { get; } = new(new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero));
    public InMemoryPaymentRepository PaymentRepository { get; } = new();
    public InMemoryIdempotencyStore IdempotencyStore { get; } = new();
    public InMemoryAuditLogWriter AuditLogWriter { get; } = new();
    public DeterministicCardNetworkGateway CardGateway { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IClock>();
            services.RemoveAll<IUnitOfWork>();
            services.RemoveAll<IPaymentRepository>();
            services.RemoveAll<IIdempotencyStore>();
            services.RemoveAll<IAuditLogWriter>();
            services.RemoveAll<ICardNetworkGateway>();

            services.AddSingleton(Clock);
            services.AddSingleton<IClock>(sp => sp.GetRequiredService<MutableClock>());

            services.AddSingleton(PaymentRepository);
            services.AddSingleton<IPaymentRepository>(sp => sp.GetRequiredService<InMemoryPaymentRepository>());

            services.AddSingleton(IdempotencyStore);
            services.AddSingleton<IIdempotencyStore>(sp => sp.GetRequiredService<InMemoryIdempotencyStore>());

            services.AddSingleton(AuditLogWriter);
            services.AddSingleton<IAuditLogWriter>(sp => sp.GetRequiredService<InMemoryAuditLogWriter>());

            services.AddSingleton(CardGateway);
            services.AddSingleton<ICardNetworkGateway>(sp => sp.GetRequiredService<DeterministicCardNetworkGateway>());

            services.AddScoped<IUnitOfWork, NoOpUnitOfWork>();
        });
    }
}
