using FalconTouch.Domain.Entities;

namespace FalconTouch.Application.Games;

public interface IGameRepository
{
    Task<List<Game>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<Game?> GetActiveByIdAsync(int gameId, CancellationToken cancellationToken = default);
    Task AddAsync(Game game, CancellationToken cancellationToken = default);
    Task AddClickAsync(GameClick click, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RankingItemDto>> GetRankingAsync(int gameId, int take, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
