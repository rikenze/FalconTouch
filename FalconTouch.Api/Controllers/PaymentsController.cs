using FalconTouch.Domain.Entities;
using FalconTouch.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FalconTouch.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly FalconTouchDbContext _db;

    public PaymentsController(FalconTouchDbContext db)
    {
        _db = db;
    }

    [HttpPost("create-pix")]
    public async Task<IActionResult> CreatePix([FromBody] CreatePaymentRequest request)
    {
        var userId = int.Parse(User.FindFirst("sub")!.Value);

        // TODO: chamar SDK/HTTP da Efi (Gerencianet) e criar cobrança Pix
        var providerPaymentId = Guid.NewGuid().ToString(); // mock

        var payment = new Payment
        {
            UserId = userId,
            Amount = request.Amount,
            Provider = "Efi",
            ProviderPaymentId = providerPaymentId,
            Status = PaymentStatus.Pending,
            CouponCode = request.CouponCode
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        // TODO: retornar qrcode/link vindo da Efi
        return Ok(new
        {
            paymentId = payment.Id,
            providerPaymentId,
            // pixQrCode = ...
        });
    }
}

public record CreatePaymentRequest(decimal Amount, string? CouponCode);
