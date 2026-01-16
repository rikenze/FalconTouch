using FalconTouch.Application.Games;
using FalconTouch.Domain.Entities;
using FalconTouch.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FalconTouch.Api.Hubs;

[Authorize]
public class GameHub : Hub
{
    private readonly IGameService _gameService;
    private readonly FalconTouchDbContext _db;
    private readonly ILogger<GameHub> _logger;

    // In-memory state for the current process; not shared across instances.
    private static readonly object _lock = new();
    private static int? _winnerButtonIndex = null;
    private static bool _gameStarted = false;
    private static int _currentGameId;

    public GameHub(IGameService gameService, FalconTouchDbContext db, ILogger<GameHub> logger)
    {
        _gameService = gameService;
        _db = db;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        _logger.LogInformation("SignalR connected. ConnectionId={ConnectionId}", Context.ConnectionId);
        await Clients.All.SendAsync("PlayerConnected", Context.UserIdentifier);
    }

    [Authorize(Roles = "Admin")]
    public async Task StartGame(int numberOfButtons)
    {
        _logger.LogInformation("StartGame requested. Buttons={Buttons}, User={UserId}", numberOfButtons, Context.UserIdentifier);
        try
        {
            var result = await _gameService.StartGameAsync(numberOfButtons);

            if (!result.Success || result.Value is null)
            {
                _logger.LogWarning("StartGame failed: {Error}", result.Error);
                await Clients.Caller.SendAsync("GameStartError", result.Error ?? "Erro ao iniciar jogo.");
                return;
            }

            // Winner is chosen in-memory; the GameStarted event is broadcast via domain events.
            lock (_lock)
            {
                _winnerButtonIndex = Random.Shared.Next(0, numberOfButtons);
                _gameStarted = true;
                _currentGameId = result.Value.GameId;
            }

            // Evento GameStarted e enviado via Domain Events
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "StartGame failed unexpectedly.");
            throw;
        }
    }

    public async Task<GameStatusDto> GetGameStatus()
    {
        _logger.LogInformation("GetGameStatus requested.");
        var game = await GetOrCreateCurrentGameAsync();
        return new GameStatusDto(game.IsActive);
    }

    public async Task<PublicGameDto> GetPublicGame()
    {
        _logger.LogInformation("GetPublicGame requested.");
        try
        {
            var game = await GetOrCreateCurrentGameAsync();
            var prize = await EnsurePrizeAsync(game.Id);

            var images = prize.Images
                .Select(img => new PrizeImageDto(
                    img.Id,
                    $"data:image/jpeg;base64,{Convert.ToBase64String(img.Image)}"))
                .ToList();

            return new PublicGameDto(prize.Description, images);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetPublicGame failed.");
            throw;
        }
    }

    public async Task<CurrentGameDto> GetCurrentGame()
    {
        _logger.LogInformation("GetCurrentGame requested.");
        try
        {
            var game = await GetOrCreateCurrentGameAsync();
            var paidCount = await _db.Payments
                .CountAsync(p => p.GameId == game.Id && p.Status == PaymentStatus.Paid);

            return new CurrentGameDto(
                game.Id,
                game.IsActive,
                game.Price,
                game.MinPlayers,
                paidCount,
                game.NumberOfButtons
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetCurrentGame failed.");
            throw;
        }
    }

    public async Task<DeliveryInfoDto?> GetDeliveryInfo()
    {
        try
        {
            var userIdClaim = Context.User?.FindFirst("sub")?.Value ?? Context.User?.FindFirst("id")?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("GetDeliveryInfo: missing user claim.");
                return null;
            }
            var game = await GetOrCreateCurrentGameAsync();

            var delivery = await _db.DeliveryInfos
                .FirstOrDefaultAsync(d => d.UserId == userId && d.GameId == game.Id);

            if (delivery is null) return null;

            return new DeliveryInfoDto(
                delivery.Street,
                delivery.Number,
                delivery.Neighborhood,
                delivery.City,
                delivery.State,
                delivery.ZipCode
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetDeliveryInfo failed.");
            throw;
        }
    }

    public async Task<CouponValidationDto> ValidateCoupon(string code)
    {
        _logger.LogInformation("ValidateCoupon requested. Code={Code}", code);
        try
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return new CouponValidationDto(false, "Cupom invalido.", null, 0, 0, 0);
            }

            var influencer = await _db.Influencers
                .FirstOrDefaultAsync(i => i.Code == code.ToLower() && i.Active);

            if (influencer is null)
                return new CouponValidationDto(false, "Cupom invalido ou inativo.", null, 0, 0, 0);

            var game = await GetOrCreateCurrentGameAsync();
            var discount = influencer.DiscountPercent;
            var original = game.Price;
            var discounted = Math.Round(original * (1 - discount / 100), 2);

            var commission = influencer.CommissionType == "per_player"
                ? influencer.CommissionValue
                : 0;

            return new CouponValidationDto(true, null, influencer.Id, discounted, discount, commission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ValidateCoupon failed.");
            throw;
        }
    }

    [Authorize(Roles = "Admin")]
    public async Task<IReadOnlyList<InfluencerDto>> GetInfluencers()
    {
        _logger.LogInformation("GetInfluencers requested.");
        var game = await GetOrCreateCurrentGameAsync();

        var influencers = await _db.Influencers
            .OrderByDescending(i => i.Id)
            .ToListAsync();

        var results = new List<InfluencerDto>();

        foreach (var influencer in influencers)
        {
            var paidCount = await _db.Payments.CountAsync(p =>
                p.GameId == game.Id &&
                p.InfluencerId == influencer.Id &&
                p.Status == PaymentStatus.Paid);

            var conversionGoal = (int)Math.Round(
                influencer.FollowerCount * (double)influencer.MinimumFollowerPercentage / 100);

            results.Add(new InfluencerDto(
                influencer.Id,
                influencer.Name,
                influencer.Code,
                influencer.DiscountPercent,
                influencer.CommissionType,
                influencer.CommissionValue,
                influencer.FollowerCount,
                influencer.MinimumFollowerPercentage,
                influencer.Active,
                paidCount,
                $"{paidCount} / {conversionGoal}",
                paidCount >= conversionGoal ? "Meta Atingida" : "Abaixo da Meta",
                influencer.FollowerCount > 0 ? (decimal)paidCount / influencer.FollowerCount * 100 : 0,
                conversionGoal
            ));
        }

        return results;
    }

    [Authorize(Roles = "Admin")]
    public async Task<InfluencerDto> CreateInfluencer(InfluencerInputDto input)
    {
        _logger.LogInformation("CreateInfluencer requested. Code={Code}", input.Code);
        var influencer = new Influencer
        {
            Name = input.Name,
            Code = input.Code.ToLower(),
            DiscountPercent = input.DiscountPercent,
            CommissionType = input.CommissionType,
            CommissionValue = input.CommissionValue,
            FollowerCount = input.FollowerCount,
            MinimumFollowerPercentage = input.MinimumFollowerPercentage,
            Active = input.Active
        };

        _db.Influencers.Add(influencer);
        await _db.SaveChangesAsync();

        return await BuildInfluencerDtoAsync(influencer);
    }

    [Authorize(Roles = "Admin")]
    public async Task<InfluencerDto> UpdateInfluencer(int id, InfluencerInputDto input)
    {
        _logger.LogInformation("UpdateInfluencer requested. Id={Id}", id);
        var influencer = await _db.Influencers.FindAsync(id);
        if (influencer is null)
            throw new HubException("Influenciador nao encontrado.");

        influencer.Name = input.Name;
        influencer.Code = input.Code.ToLower();
        influencer.DiscountPercent = input.DiscountPercent;
        influencer.CommissionType = input.CommissionType;
        influencer.CommissionValue = input.CommissionValue;
        influencer.FollowerCount = input.FollowerCount;
        influencer.MinimumFollowerPercentage = input.MinimumFollowerPercentage;
        influencer.Active = input.Active;

        await _db.SaveChangesAsync();

        return await BuildInfluencerDtoAsync(influencer);
    }

    [Authorize(Roles = "Admin")]
    public async Task<bool> DeleteInfluencer(int id)
    {
        _logger.LogInformation("DeleteInfluencer requested. Id={Id}", id);
        var influencer = await _db.Influencers.FindAsync(id);
        if (influencer is null) return false;

        _db.Influencers.Remove(influencer);
        await _db.SaveChangesAsync();

        return true;
    }

    [Authorize(Roles = "Admin")]
    public async Task<AdminConfigDto> GetAdminConfig()
    {
        _logger.LogInformation("GetAdminConfig requested.");
        var game = await GetOrCreateCurrentGameAsync();
        var prize = await EnsurePrizeAsync(game.Id);

        return new AdminConfigDto(
            game.MinPlayers,
            game.Price,
            prize.Description,
            game.IsActive
        );
    }

    [Authorize(Roles = "Admin")]
    public async Task<AdminConfigDto> UpdateAdminConfig(AdminConfigInputDto input)
    {
        _logger.LogInformation("UpdateAdminConfig requested. MinPlayers={MinPlayers}", input.MinPlayers);
        var game = await GetOrCreateCurrentGameAsync();
        game.MinPlayers = input.MinPlayers;
        game.Price = input.Price;

        var prize = await EnsurePrizeAsync(game.Id);
        prize.Description = input.PrizeDescription;

        await _db.SaveChangesAsync();

        await Clients.All.SendAsync("PrizeUpdated", new PrizeUpdatedDto(prize.Description));
        await Clients.All.SendAsync("PriceUpdated", new PriceUpdatedDto(game.Price));

        return new AdminConfigDto(
            game.MinPlayers,
            game.Price,
            prize.Description,
            game.IsActive
        );
    }

    [Authorize(Roles = "Admin")]
    public async Task<bool> SetGameActive(bool isActive)
    {
        _logger.LogInformation("SetGameActive requested. IsActive={IsActive}", isActive);
        var game = await GetOrCreateCurrentGameAsync();
        game.IsActive = isActive;
        await _db.SaveChangesAsync();

        await Clients.All.SendAsync("GameStatusUpdated", new GameStatusDto(isActive));
        return true;
    }

    [Authorize(Roles = "Admin")]
    public async Task<PrizeImageDto> UploadPrizeImage(int gameId, string base64Image)
    {
        _logger.LogInformation("UploadPrizeImage requested. GameId={GameId}", gameId);
        var prize = await EnsurePrizeAsync(gameId);

        var raw = base64Image;
        var commaIndex = base64Image.IndexOf(',');
        if (commaIndex >= 0)
            raw = base64Image[(commaIndex + 1)..];

        var bytes = Convert.FromBase64String(raw);
        var image = new PrizeImage
        {
            PrizeId = prize.Id,
            Image = bytes
        };

        _db.PrizeImages.Add(image);
        await _db.SaveChangesAsync();

        var dto = new PrizeImageDto(
            image.Id,
            $"data:image/jpeg;base64,{Convert.ToBase64String(image.Image)}");

        await Clients.All.SendAsync("PrizeImagesUpdated", new PrizeImagesUpdatedDto(
            await GetPrizeImagesAsync(prize.Id)));

        return dto;
    }

    [Authorize(Roles = "Admin")]
    public async Task<bool> DeletePrizeImage(int imageId, int gameId)
    {
        _logger.LogInformation("DeletePrizeImage requested. GameId={GameId}, ImageId={ImageId}", gameId, imageId);
        var prize = await EnsurePrizeAsync(gameId);
        var image = await _db.PrizeImages.FindAsync(imageId);
        if (image is null) return false;

        _db.PrizeImages.Remove(image);
        await _db.SaveChangesAsync();

        await Clients.All.SendAsync("PrizeImagesUpdated", new PrizeImagesUpdatedDto(
            await GetPrizeImagesAsync(prize.Id)));

        return true;
    }

    public async Task ClickButton(int buttonIndex)
    {
        _logger.LogInformation("ClickButton requested. ButtonIndex={ButtonIndex}", buttonIndex);
        try
        {
            if (!_gameStarted || _winnerButtonIndex is null)
            {
                await Clients.Caller.SendAsync("ClickRejected", "Game not started.");
                return;
            }

            int gameId;
            int? winnerButtonIndex;
            // Snapshot shared state under a lock to avoid races.
            lock (_lock)
            {
                gameId = _currentGameId;
                winnerButtonIndex = _winnerButtonIndex;
            }

            var userId = int.Parse(Context.User!.FindFirst("sub")!.Value);

            var result = await _gameService.RegisterClickAsync(gameId, userId, buttonIndex);

            if (!result.Success || result.Value is null)
            {
                _logger.LogWarning("ClickButton rejected: {Error}", result.Error);
                await Clients.Caller.SendAsync("ClickRejected", result.Error ?? "Erro ao registrar clique.");
                return;
            }

            await Clients.All.SendAsync("RankingUpdated", result.Value);

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

                // Persist winner and close the game once the winning button is found.
                var game = await _db.Games.FindAsync(gameId);
                if (game is not null)
                {
                    game.IsActive = false;
                    game.FinishedAt = DateTime.UtcNow;
                    game.WinnerId = userId;
                    await _db.SaveChangesAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ClickButton failed.");
            throw;
        }
    }

    private async Task<Game> GetOrCreateCurrentGameAsync()
    {
        var game = await _db.Games
            .OrderByDescending(g => g.Id)
            .FirstOrDefaultAsync();

        if (game is not null) return game;

        // Seed a default game when none exists yet.
        var created = new Game
        {
            StartedAt = DateTime.UtcNow,
            IsActive = false,
            MinPlayers = 1000,
            Price = 12.00m
        };

        _db.Games.Add(created);
        await _db.SaveChangesAsync();

        return created;
    }

    private async Task<Prize> EnsurePrizeAsync(int gameId)
    {
        var prize = await _db.Prizes
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.GameId == gameId);

        if (prize is not null) return prize;

        // Seed a placeholder prize so the UI can render something.
        var created = new Prize
        {
            GameId = gameId,
            Description = "IPhone 13 Pro"
        };

        _db.Prizes.Add(created);
        await _db.SaveChangesAsync();

        return created;
    }

    private async Task<List<PrizeImageDto>> GetPrizeImagesAsync(int prizeId)
    {
        return await _db.PrizeImages
            .Where(i => i.PrizeId == prizeId)
            .Select(i => new PrizeImageDto(
                i.Id,
                $"data:image/jpeg;base64,{Convert.ToBase64String(i.Image)}"))
            .ToListAsync();
    }

    private async Task<InfluencerDto> BuildInfluencerDtoAsync(Influencer influencer)
    {
        var game = await GetOrCreateCurrentGameAsync();
        var paidCount = await _db.Payments.CountAsync(p =>
            p.GameId == game.Id &&
            p.InfluencerId == influencer.Id &&
            p.Status == PaymentStatus.Paid);

        var conversionGoal = (int)Math.Round(
            influencer.FollowerCount * (double)influencer.MinimumFollowerPercentage / 100);

        return new InfluencerDto(
            influencer.Id,
            influencer.Name,
            influencer.Code,
            influencer.DiscountPercent,
            influencer.CommissionType,
            influencer.CommissionValue,
            influencer.FollowerCount,
            influencer.MinimumFollowerPercentage,
            influencer.Active,
            paidCount,
            $"{paidCount} / {conversionGoal}",
            paidCount >= conversionGoal ? "Meta Atingida" : "Abaixo da Meta",
            influencer.FollowerCount > 0 ? (decimal)paidCount / influencer.FollowerCount * 100 : 0,
            conversionGoal
        );
    }
}

public record GameStatusDto(bool GameStarted);
public record PublicGameDto(string PrizeDescription, IReadOnlyList<PrizeImageDto> Images);
public record PrizeImageDto(int Id, string Image);
public record PrizeUpdatedDto(string Description);
public record PrizeImagesUpdatedDto(IReadOnlyList<PrizeImageDto> Images);
public record PriceUpdatedDto(decimal Price);
public record CurrentGameDto(
    int Id,
    bool IsActive,
    decimal Price,
    int MinPlayers,
    int PlayersPaidCount,
    int NumberOfButtons);
public record DeliveryInfoDto(
    string Street,
    string Number,
    string Neighborhood,
    string City,
    string State,
    string ZipCode);
public record CouponValidationDto(
    bool IsValid,
    string? Message,
    int? InfluencerId,
    decimal PriceWithDiscount,
    decimal DiscountPercent,
    decimal CommissionAmount);
public record AdminConfigDto(int MinPlayers, decimal Price, string PrizeDescription, bool GameActive);
public record AdminConfigInputDto(int MinPlayers, decimal Price, string PrizeDescription);
public record InfluencerInputDto(
    string Name,
    string Code,
    decimal DiscountPercent,
    string CommissionType,
    decimal CommissionValue,
    int FollowerCount,
    decimal MinimumFollowerPercentage,
    bool Active);
public record InfluencerDto(
    int Id,
    string Name,
    string Code,
    decimal DiscountPercent,
    string CommissionType,
    decimal CommissionValue,
    int FollowerCount,
    decimal MinimumFollowerPercentage,
    bool Active,
    int PaidCount,
    string ConversionDisplay,
    string ConversionStatus,
    decimal ConversionPercent,
    int ConversionGoal);
