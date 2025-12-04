namespace FalconTouch.Application.Payments;

public record CreatePaymentCommand(
    int UserId,
    decimal Amount,
    string Currency,
    string Provider,        // "Efi", "Stripe"
    string? CouponCode
);