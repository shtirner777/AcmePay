using System.Data.Common;

namespace AcmePay.Application.Abstractions.Persistence;

public interface IDbConnectionFactory
{
    Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
}