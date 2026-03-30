using System.Data.Common;
using AcmePay.Application.Abstractions.Persistence;

namespace AcmePay.Infrastructure.Persistence.Transactions;

public sealed class DapperUnitOfWork(IDbConnectionFactory connectionFactory) : IUnitOfWork
{
    private DbConnection? _connection;
    private DbTransaction? _transaction;

    public DbConnection Connection =>
        _connection ?? throw new InvalidOperationException("Connection has not been opened. Call BeginTransactionAsync first.");

    public DbTransaction? Transaction => _transaction;

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            return;
        }

        _connection ??= await connectionFactory.OpenConnectionAsync(cancellationToken);
        _transaction = await _connection.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            return;
        }

        await _transaction.CommitAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            return;
        }

        await _transaction.RollbackAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }
}