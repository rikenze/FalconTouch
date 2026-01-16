namespace FalconTouch.Application.Payments;

public interface IPaymentProviderFacade
{
    Task<PixPaymentProviderResult> CreatePixAsync(
        PaymentProviderType providerType,
        PixPaymentProviderRequest request,
        CancellationToken cancellationToken = default);

    Task<CardPaymentIntentResult> CreateCardPaymentIntentAsync(
        PaymentProviderType providerType,
        CardPaymentIntentRequest request,
        CancellationToken cancellationToken = default);

    Task<CardPaymentConfirmationResult> ConfirmCardPaymentAsync(
        PaymentProviderType providerType,
        CardPaymentConfirmationRequest request,
        CancellationToken cancellationToken = default);
}
