using System.Data;
using System.Data.Common;
using AcmePay.Application.Abstractions.Persistence;

namespace AcmePay.IntegrationTests.TestHost;

internal sealed class NoOpUnitOfWork : IUnitOfWork
{
    public DbConnection Connection { get; } = new FakeDbConnection();
    public DbTransaction? Transaction => null;

    public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private sealed class FakeDbConnection : DbConnection
    {
        public override string ConnectionString { get; set; } = string.Empty;
        public override string Database => "tests";
        public override string DataSource => "tests";
        public override string ServerVersion => "1.0";
        public override ConnectionState State => ConnectionState.Open;
        public override void ChangeDatabase(string databaseName) { }
        public override void Close() { }
        public override void Open() { }
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
        protected override DbCommand CreateDbCommand() => throw new NotSupportedException();
    }
}
