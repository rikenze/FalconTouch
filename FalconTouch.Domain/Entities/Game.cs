using FalconTouch.Domain.Events;

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
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public static Game Create(int numberOfButtons, DateTime now)
    {
        if (numberOfButtons <= 0)
            throw new ArgumentException("Number of buttons must be greater than zero.");

        return new Game
        {
            StartedAt = now,
            IsActive = true,
            NumberOfButtons = numberOfButtons
        };
    }

    public void Deactivate(DateTime finishedAt, int? winnerId = null)
    {
        IsActive = false;
        FinishedAt = finishedAt;
        WinnerId = winnerId;
    }

    public GameClick RegisterClick(int userId, int buttonIndex, DateTime now)
    {
        if (!IsActive)
            throw new InvalidOperationException("Game is not active.");

        var reactionMs = (int)(now - StartedAt).TotalMilliseconds;

        var click = new GameClick
        {
            Game = this,
            UserId = userId,
            ButtonIndex = buttonIndex,
            ClickedAt = now,
            ReactionTimeMs = reactionMs
        };

        _domainEvents.Add(new GameClickRegisteredEvent(
            Id,
            userId,
            buttonIndex,
            reactionMs));

        return click;
    }

    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

