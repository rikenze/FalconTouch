using FalconTouch.Application.Payments;

namespace FalconTouch.Infrastructure.Payments;

public class PaymentProviderFacade : IPaymentProviderFacade
{
    private readonly IPaymentProviderFactory _factory;

    public PaymentProviderFacade(IPaymentProviderFactory factory)
    {
        _factory = factory;
    }

    public Task<PixPaymentProviderResult> CreatePixAsync(
        PaymentProviderType providerType,
        PixPaymentProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        return _factory.Get(providerType).CreatePixAsync(request, cancellationToken);
    }

    public Task<CardPaymentIntentResult> CreateCardPaymentIntentAsync(
        PaymentProviderType providerType,
        CardPaymentIntentRequest request,
        CancellationToken cancellationToken = default)
    {
        return _factory.Get(providerType).CreateCardPaymentIntentAsync(request, cancellationToken);
    }

    public Task<CardPaymentConfirmationResult> ConfirmCardPaymentAsync(
        PaymentProviderType providerType,
        CardPaymentConfirmationRequest request,
        CancellationToken cancellationToken = default)
    {
        return _factory.Get(providerType).ConfirmCardPaymentAsync(request, cancellationToken);
    }
}
