using FalconTouch.Application.Games;
using FalconTouch.Domain.Events;
using Microsoft.AspNetCore.SignalR;
using FalconTouch.Api.Hubs;

namespace FalconTouch.Api.Events;

public class SignalRGameEventPublisher : IGameEventPublisher
{
    private readonly IHubContext<GameHub> _hub;

    public SignalRGameEventPublisher(IHubContext<GameHub> hub)
    {
        _hub = hub;
    }

    public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        return domainEvent switch
        {
            GameStartedEvent started => _hub.Clients.All.SendAsync(
                "GameStarted",
                new { gameId = started.GameId, buttons = started.NumberOfButtons },
                cancellationToken),

            GameClickRegisteredEvent clicked => _hub.Clients.All.SendAsync(
                "ClickRegistered",
                new
                {
                    gameId = clicked.GameId,
                    userId = clicked.UserId,
                    buttonIndex = clicked.ButtonIndex,
                    reactionTimeMs = clicked.ReactionTimeMs
                },
                cancellationToken),

            _ => Task.CompletedTask
        };
    }
}
