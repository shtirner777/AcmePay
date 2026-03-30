using System.Security.Cryptography;
using System.Text;
using AcmePay.Application.Payments.Gateways;
using AcmePay.Core.Payments.Enums;
using AcmePay.Infrastructure.Security;

namespace AcmePay.Infrastructure.Gateways;

public sealed class MockCardNetworkGateway : ICardNetworkGateway
{
    public Task<CardAuthorizationResult> AuthorizeAsync(
        CardAuthorizationRequest request,
        CancellationToken cancellationToken = default)
    {
        var network = DetectNetwork(request.Pan);

        if (request.Pan.EndsWith("0000", StringComparison.Ordinal))
        {
            return Task.FromResult(new CardAuthorizationResult(
                IsApproved: false,
                DeclineReason: "Mock decline from card network.",
                AuthorizationReference: string.Empty,
                Network: network,
                MaskedPan: Mask(request.Pan),
                CardFingerprint: CardFingerprintGenerator.Generate(request.Pan, request.ExpiryMonth, request.ExpiryYear)));
        }

        var authorizationReference = BuildAuthorizationReference(request);

        return Task.FromResult(new CardAuthorizationResult(
            IsApproved: true,
            DeclineReason: null,
            AuthorizationReference: authorizationReference,
            Network: network,
            MaskedPan: Mask(request.Pan),
            CardFingerprint: CardFingerprintGenerator.Generate(request.Pan, request.ExpiryMonth, request.ExpiryYear)));
    }

    private static CardNetwork DetectNetwork(string pan)
    {
        if (string.IsNullOrWhiteSpace(pan))
        {
            return CardNetwork.Unknown;
        }

        if (pan.StartsWith('4'))
        {
            return CardNetwork.Visa;
        }

        if (pan.Length >= 2 && int.TryParse(pan[..2], out var firstTwo) && firstTwo is >= 51 and <= 55)
        {
            return CardNetwork.Mastercard;
        }

        if (pan.StartsWith("34") || pan.StartsWith("37"))
        {
            return CardNetwork.Amex;
        }

        if (pan.StartsWith("6011") || pan.StartsWith("65"))
        {
            return CardNetwork.Discover;
        }

        return CardNetwork.Other;
    }

    private static string Mask(string pan)
    {
        var normalized = pan.Trim();
        if (normalized.Length < 4)
        {
            return "****";
        }

        var last4 = normalized[^4..];
        return $"**** **** **** {last4}";
    }

    private static string BuildAuthorizationReference(CardAuthorizationRequest request)
    {
        var raw = $"{request.MerchantId}|{request.Amount:0.00}|{request.Currency}|{request.Pan}|{Guid.NewGuid():N}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return $"AUTH-{Convert.ToHexString(bytes)[..16]}";
    }
}