using FalconTouch.Application.Payments;

namespace FalconTouch.Infrastructure.Payments;

public class PaymentProviderFactory : IPaymentProviderFactory
{
    private readonly IEnumerable<IPaymentProvider> _providers;

    public PaymentProviderFactory(IEnumerable<IPaymentProvider> providers)
    {
        _providers = providers;
    }

    public IPaymentProvider Get(PaymentProviderType type)
    {
        // Simple factory: resolve the provider that matches the requested type.
        var provider = _providers.FirstOrDefault(p => p.Type == type);
        if (provider is null)
        {
            throw new InvalidOperationException($"Payment provider '{type}' is not registered.");
        }

        return provider;
    }
}
