namespace FalconTouch.Application.Games;

public record RankingItemDto(
    int UserId,
    string? Email,
    int ReactionTimeMs
);
