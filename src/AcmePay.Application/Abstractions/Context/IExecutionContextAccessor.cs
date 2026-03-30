namespace AcmePay.Application.Abstractions.Context;

public interface IExecutionContextAccessor
{
    ExecutionContext GetCurrent();
}