using System.Data.Common;
using AcmePay.Application.Abstractions.Persistence;
using Npgsql;

namespace AcmePay.Infrastructure.Persistence.Connections;

public sealed class NpgsqlConnectionFactory(string connectionString) : IDbConnectionFactory
{
    private readonly string _connectionString = !string.IsNullOrWhiteSpace(connectionString)
        ? connectionString
        : throw new ArgumentException("Connection string is required.", nameof(connectionString));

    public async Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}