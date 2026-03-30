namespace AcmePay.Application.Payments.Gateways;

public interface ICardNetworkGateway
{
    Task<CardAuthorizationResult> AuthorizeAsync(
        CardAuthorizationRequest request,
        CancellationToken cancellationToken = default);
}