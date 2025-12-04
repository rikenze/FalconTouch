using FalconTouch.Application.Common;
using FalconTouch.Application.Games;
using FalconTouch.Domain.Entities;
using FalconTouch.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FalconTouch.Infrastructure.Games;

public class GameService : IGameService
{
    private readonly FalconTouchDbContext _db;

    public GameService(FalconTouchDbContext db)
    {
        _db = db;
    }

    public async Task<Result<GameStartResult>> StartGameAsync(
        int numberOfButtons,
        CancellationToken cancellationToken = default)
    {
        if (numberOfButtons <= 0)
            return Result<GameStartResult>.Fail("Número de botões inválido.");

        var game = new Game
        {
            StartedAt = DateTime.UtcNow,
            IsActive = true
        };

        _db.Games.Add(game);
        await _db.SaveChangesAsync(cancellationToken);

        var result = new GameStartResult(
            game.Id,
            numberOfButtons,
            game.StartedAt
        );

        return Result<GameStartResult>.Ok(result);
    }

    public async Task<Result<IReadOnlyList<RankingItemDto>>> RegisterClickAsync(
        int gameId,
        int userId,
        int buttonIndex,
        CancellationToken cancellationToken = default)
    {
        var game = await _db.Games.FirstOrDefaultAsync(
            g => g.Id == gameId && g.IsActive,
            cancellationToken);

        if (game is null)
            return Result<IReadOnlyList<RankingItemDto>>.Fail("Jogo não encontrado ou já finalizado.");

        var now = DateTime.UtcNow;
        var reactionMs = (int)(now - game.StartedAt).TotalMilliseconds;

        var click = new GameClick
        {
            GameId = game.Id,
            UserId = userId,
            ButtonIndex = buttonIndex,
            ClickedAt = now,
            ReactionTimeMs = reactionMs
        };

        _db.GameClicks.Add(click);
        await _db.SaveChangesAsync(cancellationToken);

        var ranking = await _db.GameClicks
            .Where(gc => gc.GameId == gameId)
            .OrderBy(gc => gc.ReactionTimeMs)
            .Take(10)
            .Select(gc => new RankingItemDto(
                gc.UserId,
                gc.User.Email,
                gc.ReactionTimeMs
            ))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<RankingItemDto>>.Ok(ranking);
    }
}
