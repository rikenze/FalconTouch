using FalconTouch.Domain.Entities;
using FalconTouch.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using FalconTouch.Api.Hubs;
using Microsoft.Extensions.Logging;

namespace FalconTouch.Api.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly FalconTouchDbContext _db;
    private readonly IHubContext<GameHub> _hub;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(FalconTouchDbContext db, IHubContext<GameHub> hub, ILogger<PaymentsController> logger)
    {
        _db = db;
        _hub = hub;
        _logger = logger;
    }

    [HttpGet("check-payment")]
    public async Task<IActionResult> CheckPayment()
    {
        _logger.LogDebug("CheckPayment requested.");
        try
        {
            var idClaim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (idClaim == null || !int.TryParse(idClaim.Value, out var userId))
            {
                return Unauthorized(new { message = "Invalid or missing user id claim." });
            }

            var user = await _db.Users.FindAsync(userId);
            if (user is null) return NotFound(new { message = "Usuario nao encontrado." });

            if (user.Role == "Admin") return Ok(new { hasPaid = true });

            var game = await GetOrCreateCurrentGameAsync();
            var hasPaid = await _db.Payments.AnyAsync(p =>
                p.UserId == userId &&
                p.GameId == game.Id &&
                p.Status == PaymentStatus.Paid);

            return Ok(new { hasPaid });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CheckPayment failed.");
            return StatusCode(500, new { message = "Erro ao verificar pagamento." });
        }
    }

    [HttpPost("pix")]
    public async Task<IActionResult> CreatePix([FromBody] PixRequest request)
    {
        _logger.LogInformation("CreatePix requested. Amount={Amount}", request.Amount);
        try
        {
            var idClaim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (idClaim == null || !int.TryParse(idClaim.Value, out var userId))
            {
                return Unauthorized(new { message = "Invalid or missing user id claim." });
            }

            var game = await GetOrCreateCurrentGameAsync();
            if (!game.IsActive)
                return BadRequest(new { message = "Nenhum jogo ativo no momento." });

            var providerPaymentId = Guid.NewGuid().ToString("N");

            var payment = new Payment
            {
                UserId = userId,
                GameId = game.Id,
                Amount = request.Amount,
                Provider = "Efi",
                ProviderPaymentId = providerPaymentId,
                Status = PaymentStatus.Pending,
                CouponCode = request.CouponCode,
                InfluencerId = request.InfluencerId,
                CommissionAmount = request.CommissionAmount,
                DiscountPercent = request.DiscountPercent
            };

            _db.Payments.Add(payment);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                txid = providerPaymentId,
                qrcode = "pix-qr-code-placeholder",
                imagemQrcode = "pix-qr-image-placeholder"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreatePix failed.");
            return StatusCode(500, new { message = "Erro ao gerar Pix." });
        }
    }

    [HttpPost("create-payment-intent")]
    public async Task<IActionResult> CreatePaymentIntent([FromBody] PaymentIntentRequest request)
    {
        _logger.LogDebug("CreatePaymentIntent requested. Amount={Amount}", request.Amount);
        try
        {
            if (request.Amount <= 0)
                return BadRequest(new { message = "Valor invalido." });

            // TODO: integrar com Stripe e retornar clientSecret real.
            var clientSecret = $"mock_{Guid.NewGuid():N}";
            return Ok(new { clientSecret });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreatePaymentIntent failed.");
            return StatusCode(500, new { message = "Erro ao criar pagamento." });
        }
    }

    [HttpPost("confirm-card")]
    public async Task<IActionResult> ConfirmCardPayment([FromBody] ConfirmCardPaymentRequest request)
    {
        _logger.LogInformation("ConfirmCardPayment requested. Amount={Amount}", request.Amount);
        try
        {
            var idClaim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (idClaim == null || !int.TryParse(idClaim.Value, out var userId))
            {
                return Unauthorized(new { message = "Invalid or missing user id claim." });
            }

            var game = await GetOrCreateCurrentGameAsync();
            if (!game.IsActive)
                return BadRequest(new { message = "Nenhum jogo ativo no momento." });

            var payment = new Payment
            {
                UserId = userId,
                GameId = game.Id,
                Amount = request.Amount,
                Provider = "Stripe",
                ProviderPaymentId = request.ProviderPaymentId ?? Guid.NewGuid().ToString("N"),
                Status = PaymentStatus.Paid,
                CouponCode = request.CouponCode,
                InfluencerId = request.InfluencerId,
                CommissionAmount = request.CommissionAmount,
                DiscountPercent = request.DiscountPercent,
                PaidAt = DateTime.UtcNow
            };

            _db.Payments.Add(payment);
            await _db.SaveChangesAsync();

            await UpsertDeliveryInfoAsync(userId, game.Id, request.Delivery);

            var paidCount = await _db.Payments.CountAsync(p =>
                p.GameId == game.Id && p.Status == PaymentStatus.Paid);

            await _hub.Clients.All.SendAsync("PlayersPaidCountUpdated", new
            {
                current = paidCount,
                min = game.MinPlayers
            });

            return Ok(new { message = "Pagamento confirmado e salvo com sucesso." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ConfirmCardPayment failed.");
            return StatusCode(500, new { message = "Erro ao confirmar pagamento." });
        }
    }

    [HttpGet("pix/status/{txid}")]
    public async Task<IActionResult> GetPixStatus([FromRoute] string txid)
    {
        _logger.LogDebug("GetPixStatus requested. TxId={TxId}", txid);
        try
        {
            var payment = await _db.Payments
                .FirstOrDefaultAsync(p => p.Provider == "Efi" && p.ProviderPaymentId == txid);

            return Ok(new { paid = payment?.Status == PaymentStatus.Paid });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetPixStatus failed.");
            return StatusCode(500, new { message = "Erro ao consultar pagamento." });
        }
    }

    private async Task<Game> GetOrCreateCurrentGameAsync()
    {
        var game = await _db.Games
            .OrderByDescending(g => g.Id)
            .FirstOrDefaultAsync();

        if (game is not null) return game;

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

    private async Task UpsertDeliveryInfoAsync(int userId, int gameId, DeliveryInfoRequest delivery)
    {
        var existing = await _db.DeliveryInfos
            .FirstOrDefaultAsync(d => d.UserId == userId && d.GameId == gameId);

        if (existing is null)
        {
            _db.DeliveryInfos.Add(new DeliveryInfo
            {
                UserId = userId,
                GameId = gameId,
                Street = delivery.Street,
                Number = delivery.Number,
                Neighborhood = delivery.Neighborhood,
                City = delivery.City,
                State = delivery.State,
                ZipCode = delivery.ZipCode
            });
        }
        else
        {
            existing.Street = delivery.Street;
            existing.Number = delivery.Number;
            existing.Neighborhood = delivery.Neighborhood;
            existing.City = delivery.City;
            existing.State = delivery.State;
            existing.ZipCode = delivery.ZipCode;
        }

        await _db.SaveChangesAsync();
    }
}

public record PixRequest(
    decimal Amount,
    string? CouponCode,
    int? InfluencerId,
    decimal? DiscountPercent,
    decimal? CommissionAmount);

public record PaymentIntentRequest(int Amount);

public record DeliveryInfoRequest(
    string Street,
    string Number,
    string Neighborhood,
    string City,
    string State,
    string ZipCode);

public record ConfirmCardPaymentRequest(
    decimal Amount,
    DeliveryInfoRequest Delivery,
    string? CouponCode,
    int? InfluencerId,
    decimal? DiscountPercent,
    decimal? CommissionAmount,
    string? ProviderPaymentId);
