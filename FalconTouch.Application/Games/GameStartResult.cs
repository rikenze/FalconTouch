using FalconTouch.Domain.Entities;

namespace FalconTouch.Application.Games;

public record GameStartResult(
    int GameId,
    int NumberOfButtons,
    DateTime StartedAt
);
