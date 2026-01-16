using FalconTouch.Application.Payments;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FalconTouch.Infrastructure.Payments.Providers;

public class StripePaymentProvider : IPaymentProvider
{
    private readonly IConfiguration _config;
    private readonly ILogger<StripePaymentProvider> _logger;

    public StripePaymentProvider(IConfiguration config, ILogger<StripePaymentProvider> logger)
    {
        _config = config;
        _logger = logger;
    }

    public PaymentProviderType Type => PaymentProviderType.Stripe;

    public Task<PixPaymentProviderResult> CreatePixAsync(
        PixPaymentProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Stripe Pix is not configured.");
    }

    public Task<CardPaymentIntentResult> CreateCardPaymentIntentAsync(
        CardPaymentIntentRequest request,
        CancellationToken cancellationToken = default)
    {
        // Placeholder for real Stripe API calls.
        var publicKey = _config["PaymentProviders:Stripe:PublicKey"];
        _logger.LogInformation("Creating Stripe payment intent. PublicKey={PublicKey}", publicKey);

        var clientSecret = $"mock_{Guid.NewGuid():N}";
        return Task.FromResult(new CardPaymentIntentResult(clientSecret));
    }

    public Task<CardPaymentConfirmationResult> ConfirmCardPaymentAsync(
        CardPaymentConfirmationRequest request,
        CancellationToken cancellationToken = default)
    {
        var providerPaymentId = request.ProviderPaymentId ?? Guid.NewGuid().ToString("N");
        _logger.LogInformation("Confirming Stripe payment. ProviderPaymentId={ProviderPaymentId}", providerPaymentId);

        return Task.FromResult(new CardPaymentConfirmationResult(providerPaymentId, true));
    }
}
