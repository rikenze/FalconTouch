using FalconTouch.Application.Payments;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FalconTouch.Infrastructure.Payments.Providers;

public class EfiPaymentProvider : IPaymentProvider
{
    private readonly IConfiguration _config;
    private readonly ILogger<EfiPaymentProvider> _logger;

    public EfiPaymentProvider(IConfiguration config, ILogger<EfiPaymentProvider> logger)
    {
        _config = config;
        _logger = logger;
    }

    public PaymentProviderType Type => PaymentProviderType.Efi;

    public Task<PixPaymentProviderResult> CreatePixAsync(
        PixPaymentProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        // Placeholder for real Efi API calls.
        var baseUrl = _config["PaymentProviders:Efi:BaseUrl"];
        _logger.LogInformation("Creating Pix with Efi. BaseUrl={BaseUrl}", baseUrl);

        var providerPaymentId = Guid.NewGuid().ToString("N");
        var qrCode = "pix-qr-code-placeholder";
        var qrCodeImage = "pix-qr-image-placeholder";

        return Task.FromResult(new PixPaymentProviderResult(providerPaymentId, qrCode, qrCodeImage));
    }

    public Task<CardPaymentIntentResult> CreateCardPaymentIntentAsync(
        CardPaymentIntentRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Efi does not support card payment intents.");
    }

    public Task<CardPaymentConfirmationResult> ConfirmCardPaymentAsync(
        CardPaymentConfirmationRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Efi does not support card confirmations.");
    }
}
