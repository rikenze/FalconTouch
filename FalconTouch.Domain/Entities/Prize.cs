namespace FalconTouch.Domain.Entities;

public class Prize
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public Game Game { get; set; } = default!;

    public string Description { get; set; } = default!;

    public ICollection<PrizeImage> Images { get; set; } = new List<PrizeImage>();
}
