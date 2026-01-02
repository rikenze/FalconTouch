namespace FalconTouch.Domain.Events;

public record GameStartedEvent(int GameId, int NumberOfButtons) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
