using FalconTouch.Application.Common;

namespace FalconTouch.Application.Games;

public interface IGameService
{
    /// <summary>
    /// Inicia um novo jogo (somente Admin).
    /// </summary>
    Task<Result<GameStartResult>> StartGameAsync(
        int numberOfButtons,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registra o clique de um jogador e retorna o ranking atualizado.
    /// </summary>
    Task<Result<IReadOnlyList<RankingItemDto>>> RegisterClickAsync(
        int gameId,
        int userId,
        int buttonIndex,
        CancellationToken cancellationToken = default);
}
