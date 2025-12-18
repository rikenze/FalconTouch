namespace FalconTouch.Domain.Entities;

public class Game
{
    public int Id { get; set; }
    public string Name { get; set; } = "FalconTouch Round";
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public bool IsActive { get; set; }
    public int MinPlayers { get; set; } = 1000;
    public decimal Price { get; set; } = 12.00m;
    public int NumberOfButtons { get; set; } = 8;

    public int? WinnerId { get; set; }
    public User? Winner { get; set; }

    public ICollection<GameClick> Clicks { get; set; } = new List<GameClick>();
    public Prize? Prize { get; set; }
}
