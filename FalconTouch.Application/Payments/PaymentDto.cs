namespace FalconTouch.Application.Payments;

public record PaymentDto(
    int Id,
    decimal Amount,
    string Currency,
    string Provider,
    string ProviderPaymentId,
    string Status,
    string? CouponCode,
    DateTime CreatedAt
);
