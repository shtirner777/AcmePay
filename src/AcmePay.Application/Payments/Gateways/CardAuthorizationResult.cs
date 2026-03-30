using AcmePay.Core.Payments.Enums;

namespace AcmePay.Application.Payments.Gateways;

public sealed record CardAuthorizationResult(
    bool IsApproved,
    string? DeclineReason,
    string AuthorizationReference,
    CardNetwork Network,
    string MaskedPan,
    string CardFingerprint);