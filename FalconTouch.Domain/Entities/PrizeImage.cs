namespace FalconTouch.Domain.Entities;

public class PrizeImage
{
    public int Id { get; set; }
    public int PrizeId { get; set; }
    public Prize Prize { get; set; } = default!;

    public byte[] Image { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
