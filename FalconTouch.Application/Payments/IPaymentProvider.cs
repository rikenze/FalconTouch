namespace FalconTouch.Application.Payments;

public interface IPaymentProvider
{
    PaymentProviderType Type { get; }

    Task<PixPaymentProviderResult> CreatePixAsync(
        PixPaymentProviderRequest request,
        CancellationToken cancellationToken = default);

    Task<CardPaymentIntentResult> CreateCardPaymentIntentAsync(
        CardPaymentIntentRequest request,
        CancellationToken cancellationToken = default);

    Task<CardPaymentConfirmationResult> ConfirmCardPaymentAsync(
        CardPaymentConfirmationRequest request,
        CancellationToken cancellationToken = default);
}
