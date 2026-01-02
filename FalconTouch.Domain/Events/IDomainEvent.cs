namespace FalconTouch.Domain.Events;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
