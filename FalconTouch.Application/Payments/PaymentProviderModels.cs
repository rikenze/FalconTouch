namespace FalconTouch.Application.Payments;

public enum PaymentProviderType
{
    Efi,
    Stripe
}

public record PixPaymentProviderRequest(decimal Amount);

public record PixPaymentProviderResult(
    string ProviderPaymentId,
    string QrCode,
    string QrCodeImage);

public record CardPaymentIntentRequest(int Amount);

public record CardPaymentIntentResult(string ClientSecret);

public record CardPaymentConfirmationRequest(decimal Amount, string? ProviderPaymentId);

public record CardPaymentConfirmationResult(string ProviderPaymentId, bool Paid);
