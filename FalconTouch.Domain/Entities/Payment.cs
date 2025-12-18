namespace FalconTouch.Domain.Entities;

public enum PaymentStatus
{
    Pending,
    Paid,
    Failed,
    Refunded
}

public class Payment
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = default!;
    public int? GameId { get; set; }
    public Game? Game { get; set; }

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "BRL";
    public string Provider { get; set; } = default!; // "Efi", "Stripe"
    public string ProviderPaymentId { get; set; } = default!;
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public string? CouponCode { get; set; }
    public int? InfluencerId { get; set; }
    public Influencer? Influencer { get; set; }
    public decimal? CommissionAmount { get; set; }
    public decimal? DiscountPercent { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
