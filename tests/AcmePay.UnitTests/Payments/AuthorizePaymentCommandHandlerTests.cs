using AcmePay.Application.Exceptions;
using AcmePay.Application.Features.Payments.Authorize;
using AcmePay.Application.Payments.Audit;
using AcmePay.Application.Payments.Idempotency;
using AcmePay.UnitTests.TestDoubles;
using Xunit;

namespace AcmePay.UnitTests.Payments;

public sealed class AuthorizePaymentCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldUseInjectedClockAcrossDomainResultAuditAndIdempotency()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 3, 30, 12, 34, 56, TimeSpan.Zero));
        var repository = new InMemoryPaymentRepository();
        var idempotencyStore = new ConfigurableIdempotencyStore();
        var auditWriter = new InMemoryAuditLogWriter();
        var handler = new AuthorizePaymentCommandHandler(
            new AuthorizePaymentCommandValidator(clock),
            repository,
            idempotencyStore,
            auditWriter,
            new PaymentAuditLogFactory(),
            new DeterministicCardNetworkGateway(),
            new NoOpUnitOfWork(),
            clock,
            new FakeExecutionContextAccessor(PaymentTestConstants.TriggeredBy, PaymentTestConstants.CorrelationId));

        var command = PaymentTestData.CreateAuthorizeCommand();

        var result = await handler.Handle(command);

        Assert.Equal(clock.UtcNow, result.AuthorizedAtUtc);
        Assert.NotEqual(Guid.Empty, result.PaymentId);
        Assert.Equal(clock.UtcNow, idempotencyStore.LastTryBeginRequest!.RequestedAtUtc);
        Assert.Equal(idempotencyStore.LastTryBeginRequest, idempotencyStore.LastCompletedRequest);

        var payment = Assert.Single(repository.Payments);
        Assert.Equal(clock.UtcNow, payment.AuthorizedAtUtc);
        Assert.Equal(clock.UtcNow, payment.LastModifiedAtUtc);

        var auditEntry = Assert.Single(auditWriter.Entries);
        Assert.Equal(clock.UtcNow, auditEntry.OccurredOnUtc);
        Assert.Equal(PaymentTestConstants.TriggeredBy, auditEntry.TriggeredBy);
        Assert.Equal(PaymentTestConstants.CorrelationId, auditEntry.CorrelationId);
    }

    [Fact]
    public async Task Handle_WhenIdempotencyAlreadyCompleted_ShouldReturnCachedResponseWithoutCallingGateway()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 3, 30, 13, 0, 0, TimeSpan.Zero));
        var gateway = new DeterministicCardNetworkGateway();
        var cachedResponse = new AuthorizePaymentResult(
            PaymentTestConstants.CachedPaymentId,
            PaymentTestConstants.MerchantId,
            42.50m,
            PaymentTestConstants.CurrencyCode,
            PaymentTestConstants.AuthorizedStatus,
            "AUTH-CACHED",
            clock.UtcNow.AddMinutes(-5));

        var idempotencyStore = new ConfigurableIdempotencyStore
        {
            StateToReturn = IdempotencyExecutionState.Completed,
            CachedResponseFactory = _ => cachedResponse
        };

        var handler = new AuthorizePaymentCommandHandler(
            new AuthorizePaymentCommandValidator(clock),
            new InMemoryPaymentRepository(),
            idempotencyStore,
            new InMemoryAuditLogWriter(),
            new PaymentAuditLogFactory(),
            gateway,
            new NoOpUnitOfWork(),
            clock,
            new FakeExecutionContextAccessor());

        var result = await handler.Handle(PaymentTestData.CreateAuthorizeCommand());

        Assert.Equal(cachedResponse, result);
        Assert.Equal(0, gateway.Calls);
    }

    [Fact]
    public async Task Handle_WhenIdempotencyPayloadConflicts_ShouldThrowIdempotencyConflictException()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 3, 30, 13, 15, 0, TimeSpan.Zero));
        var handler = new AuthorizePaymentCommandHandler(
            new AuthorizePaymentCommandValidator(clock),
            new InMemoryPaymentRepository(),
            new ConfigurableIdempotencyStore
            {
                StateToReturn = IdempotencyExecutionState.Conflict
            },
            new InMemoryAuditLogWriter(),
            new PaymentAuditLogFactory(),
            new DeterministicCardNetworkGateway(),
            new NoOpUnitOfWork(),
            clock,
            new FakeExecutionContextAccessor());

        var exception = await Assert.ThrowsAsync<IdempotencyConflictException>(() =>
            handler.Handle(PaymentTestData.CreateAuthorizeCommand()));

        Assert.Contains("different request payload", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
