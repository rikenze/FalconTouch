using FalconTouch.Domain.Entities;
using FalconTouch.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FalconTouch.Api.Hubs;

[Authorize]
public class GameHub : Hub
{
    private readonly FalconTouchDbContext _db;
    private static readonly object _lock = new();
    private static int? _winnerButtonIndex = null;
    private static bool _gameStarted = false;

    public GameHub(FalconTouchDbContext db)
    {
        _db = db;
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        await Clients.All.SendAsync("PlayerConnected", Context.UserIdentifier);
    }

    [Authorize(Roles = "Admin")]
    public async Task StartGame(int numberOfButtons)
    {
        lock (_lock)
        {
            _winnerButtonIndex = Random.Shared.Next(0, numberOfButtons);
            _gameStarted = true;
        }

        var game = new Game
        {
            StartedAt = DateTime.UtcNow,
            IsActive = true
        };
        _db.Games.Add(game);
        await _db.SaveChangesAsync();

        await Clients.All.SendAsync("GameStarted", new
        {
            gameId = game.Id,
            buttons = numberOfButtons
        });
    }

    public async Task ClickButton(int gameId, int buttonIndex)
    {
        if (!_gameStarted || _winnerButtonIndex is null)
        {
            await Clients.Caller.SendAsync("ClickRejected", "Game not started.");
            return;
        }

        var userId = int.Parse(Context.User!.FindFirst("sub")!.Value);

        Game? game;
        lock (_lock)
        {
            game = _db.Games.FirstOrDefault(g => g.Id == gameId && g.IsActive);
            if (game is null) return;

            // registra click
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
            _db.SaveChanges();

            // se acertou e ainda não tinha vencedor
            if (buttonIndex == _winnerButtonIndex && game.WinnerId is null)
            {
                game.WinnerId = userId;
                game.IsActive = false;
                game.FinishedAt = now;
                _db.SaveChanges();
            }
        }

        // Atualiza ranking top 10
        var ranking = await _db.GameClicks
            .Where(gc => gc.GameId == gameId)
            .OrderBy(gc => gc.ReactionTimeMs)
            .Take(10)
            .Select(gc => new
            {
                gc.UserId,
                gc.ReactionTimeMs
            })
            .ToListAsync();

        await Clients.All.SendAsync("RankingUpdated", ranking);

        if (game!.WinnerId is not null && buttonIndex == _winnerButtonIndex)
        {
            await Clients.All.SendAsync("WinnerConfirmed", new
            {
                gameId,
                winnerId = game.WinnerId,
            });

            lock (_lock)
            {
                _gameStarted = false;
                _winnerButtonIndex = null;
            }
        }
    }
}
