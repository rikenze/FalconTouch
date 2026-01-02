using FalconTouch.Application.Common;
using FalconTouch.Application.Games;
using FalconTouch.Domain.Entities;

namespace FalconTouch.Infrastructure.Games;

public class GameService : IGameService
{
    private readonly IGameRepository _repository;
    private readonly IGameEventPublisher _eventPublisher;

    public GameService(IGameRepository repository, IGameEventPublisher eventPublisher)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result<GameStartResult>> StartGameAsync(
        int numberOfButtons,
        CancellationToken cancellationToken = default)
    {
        if (numberOfButtons <= 0)
            return Result<GameStartResult>.Fail("Numero de botoes invalido.");

        var activeGames = await _repository.GetActiveAsync(cancellationToken);

        foreach (var active in activeGames)
        {
            active.Deactivate(DateTime.UtcNow);
        }

        var game = Game.Create(numberOfButtons, DateTime.UtcNow);

        await _repository.AddAsync(game, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        await _eventPublisher.PublishAsync(
            new FalconTouch.Domain.Events.GameStartedEvent(game.Id, numberOfButtons),
            cancellationToken);

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
        var game = await _repository.GetActiveByIdAsync(gameId, cancellationToken);

        if (game is null)
            return Result<IReadOnlyList<RankingItemDto>>.Fail("Jogo nao encontrado ou ja finalizado.");

        var click = game.RegisterClick(userId, buttonIndex, DateTime.UtcNow);

        await _repository.AddClickAsync(click, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        foreach (var domainEvent in game.DomainEvents)
        {
            await _eventPublisher.PublishAsync(domainEvent, cancellationToken);
        }
        game.ClearDomainEvents();

        var ranking = await _repository.GetRankingAsync(gameId, 10, cancellationToken);

        return Result<IReadOnlyList<RankingItemDto>>.Ok(ranking);
    }
}
