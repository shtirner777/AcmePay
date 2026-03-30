using AcmePay.Application.Abstractions.Context;
using AppExecutionContext = AcmePay.Application.Abstractions.Context.ExecutionContext;

namespace AcmePay.UnitTests.TestDoubles;

internal sealed class FakeExecutionContextAccessor : IExecutionContextAccessor
{
    private readonly AppExecutionContext _executionContext;

    public FakeExecutionContextAccessor(string triggeredBy = "unit-test", string? correlationId = "corr-unit")
    {
        _executionContext = new AppExecutionContext(triggeredBy, correlationId);
    }

    public AppExecutionContext GetCurrent() => _executionContext;
}
