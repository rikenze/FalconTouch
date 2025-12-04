using FalconTouch.Application.Games;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FalconTouch.Api.Hubs;

[Authorize]
public class GameHub : Hub
{
    private readonly IGameService _gameService;

    private static readonly object _lock = new();
    private static int? _winnerButtonIndex = null;
    private static bool _gameStarted = false;
    private static int _currentGameId;

    public GameHub(IGameService gameService)
    {
        _gameService = gameService;
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        await Clients.All.SendAsync("PlayerConnected", Context.UserIdentifier);
    }

    [Authorize(Roles = "Admin")]
    public async Task StartGame(int numberOfButtons)
    {
        // chama a camada de aplicação pra criar o Game no banco
        var result = await _gameService.StartGameAsync(numberOfButtons);

        if (!result.Success || result.Value is null)
        {
            await Clients.Caller.SendAsync("GameStartError", result.Error ?? "Erro ao iniciar jogo.");
            return;
        }

        lock (_lock)
        {
            _winnerButtonIndex = Random.Shared.Next(0, numberOfButtons);
            _gameStarted = true;
            _currentGameId = result.Value.GameId;
        }

        await Clients.All.SendAsync("GameStarted", new
        {
            gameId = result.Value.GameId,
            buttons = result.Value.NumberOfButtons
        });
    }

    public async Task ClickButton(int buttonIndex)
    {
        if (!_gameStarted || _winnerButtonIndex is null)
        {
            await Clients.Caller.SendAsync("ClickRejected", "Game not started.");
            return;
        }

        int gameId;
        int? winnerButtonIndex;
        lock (_lock)
        {
            gameId = _currentGameId;
            winnerButtonIndex = _winnerButtonIndex;
        }

        var userId = int.Parse(Context.User!.FindFirst("sub")!.Value);

        var result = await _gameService.RegisterClickAsync(gameId, userId, buttonIndex);

        if (!result.Success || result.Value is null)
        {
            await Clients.Caller.SendAsync("ClickRejected", result.Error ?? "Erro ao registrar clique.");
            return;
        }

        // Atualiza ranking
        await Clients.All.SendAsync("RankingUpdated", result.Value);

        // Verifica vencedor (apenas a lógica do botão premiado fica no Hub)
        if (buttonIndex == winnerButtonIndex)
        {
            await Clients.All.SendAsync("WinnerConfirmed", new
            {
                gameId,
                winnerId = userId
            });

            lock (_lock)
            {
                _gameStarted = false;
                _winnerButtonIndex = null;
            }

            // Quando quiser, você pode criar um método no GameService pra fechar o jogo e gravar WinnerId.
            // Ex: await _gameService.FinishGameAsync(gameId, userId);
        }
    }
}
