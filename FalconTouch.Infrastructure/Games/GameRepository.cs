using FalconTouch.Application.Games;
using FalconTouch.Domain.Entities;
using FalconTouch.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FalconTouch.Infrastructure.Games;

public class GameRepository : IGameRepository
{
    private readonly FalconTouchDbContext _db;

    public GameRepository(FalconTouchDbContext db)
    {
        _db = db;
    }

    public Task<List<Game>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return _db.Games.Where(g => g.IsActive).ToListAsync(cancellationToken);
    }

    public Task<Game?> GetActiveByIdAsync(int gameId, CancellationToken cancellationToken = default)
    {
        return _db.Games.FirstOrDefaultAsync(
            g => g.Id == gameId && g.IsActive,
            cancellationToken);
    }

    public Task AddAsync(Game game, CancellationToken cancellationToken = default)
    {
        _db.Games.Add(game);
        return Task.CompletedTask;
    }

    public Task AddClickAsync(GameClick click, CancellationToken cancellationToken = default)
    {
        _db.GameClicks.Add(click);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<RankingItemDto>> GetRankingAsync(
        int gameId,
        int take,
        CancellationToken cancellationToken = default)
    {
        return await _db.GameClicks
            .Where(gc => gc.GameId == gameId)
            .OrderBy(gc => gc.ReactionTimeMs)
            .Take(take)
            .Select(gc => new RankingItemDto(
                gc.UserId,
                gc.User.Email,
                gc.ReactionTimeMs
            ))
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
