using System.Security.Cryptography;
using System.Text;

namespace AcmePay.Application.Payments.Idempotency;

public static class IdempotencyRequestHasher
{
    public static string HashParts(params string[] parts)
    {
        ArgumentNullException.ThrowIfNull(parts);

        var canonical = string.Join("|", parts.Select(part => part?.Trim() ?? string.Empty));
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));

        return Convert.ToHexString(bytes);
    }
}
