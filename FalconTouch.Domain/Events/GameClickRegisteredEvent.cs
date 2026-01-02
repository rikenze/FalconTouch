namespace FalconTouch.Domain.Events;

public record GameClickRegisteredEvent(
    int GameId,
    int UserId,
    int ButtonIndex,
    int ReactionTimeMs) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
