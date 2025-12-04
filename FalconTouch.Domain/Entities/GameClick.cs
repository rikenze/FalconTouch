namespace FalconTouch.Domain.Entities;

public class GameClick
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public Game Game { get; set; } = default!;

    public int UserId { get; set; }
    public User User { get; set; } = default!;

    public int ButtonIndex { get; set; }
    public DateTime ClickedAt { get; set; }

    // tempo de reação em ms desde o início
    public int ReactionTimeMs { get; set; }
}
