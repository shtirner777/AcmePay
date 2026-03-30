using System.Security.Cryptography;
using System.Text;

namespace AcmePay.Infrastructure.Security;

internal static class CardFingerprintGenerator
{
    public static string Generate(string pan, int expiryMonth, int expiryYear)
    {
        var raw = $"{pan.Trim()}|{expiryMonth}|{expiryYear}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }
}