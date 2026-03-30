using System.Security.Cryptography;
using System.Text;
using AcmePay.Application.Payments.Gateways;
using AcmePay.Core.Payments.Enums;

namespace AcmePay.UnitTests.TestDoubles;

internal sealed class DeterministicCardNetworkGateway : ICardNetworkGateway
{
    public int Calls { get; private set; }

    public Task<CardAuthorizationResult> AuthorizeAsync(
        CardAuthorizationRequest request,
        CancellationToken cancellationToken = default)
    {
        Calls++;

        var network = request.Pan.StartsWith("4", StringComparison.Ordinal)
            ? CardNetwork.Visa
            : CardNetwork.Other;

        if (request.Pan.EndsWith("0000", StringComparison.Ordinal))
        {
            return Task.FromResult(new CardAuthorizationResult(
                IsApproved: false,
                DeclineReason: "Declined by deterministic test gateway.",
                AuthorizationReference: string.Empty,
                Network: network,
                MaskedPan: $"**** **** **** {request.Pan[^4..]}",
                CardFingerprint: ComputeFingerprint(request)));
        }

        return Task.FromResult(new CardAuthorizationResult(
            IsApproved: true,
            DeclineReason: null,
            AuthorizationReference: ComputeReference(request),
            Network: network,
            MaskedPan: $"**** **** **** {request.Pan[^4..]}",
            CardFingerprint: ComputeFingerprint(request)));
    }

    private static string ComputeReference(CardAuthorizationRequest request)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{request.MerchantId}|{request.Amount:0.00}|{request.Currency}|{request.Pan}"));
        return $"AUTH-{Convert.ToHexString(bytes)[..16]}";
    }

    private static string ComputeFingerprint(CardAuthorizationRequest request)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{request.Pan}|{request.ExpiryMonth}|{request.ExpiryYear}"));
        return Convert.ToHexString(bytes);
    }
}
