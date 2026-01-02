using FalconTouch.Domain.Events;

namespace FalconTouch.Application.Games;

public interface IGameEventPublisher
{
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
