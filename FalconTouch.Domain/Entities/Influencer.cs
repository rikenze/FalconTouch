namespace FalconTouch.Domain.Entities;

public class Influencer
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string CommissionType { get; set; } = "per_player";
    public decimal CommissionValue { get; set; }
    public int FollowerCount { get; set; }
    public decimal MinimumFollowerPercentage { get; set; }
    public decimal DiscountPercent { get; set; }
    public bool Active { get; set; } = true;

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
