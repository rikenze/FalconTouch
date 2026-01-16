using FalconTouch.Domain.Entities;
using FalconTouch.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FalconTouch.Api.Controllers;

[ApiController]
[Route("api/webhooks")]
public class WebhookController : ControllerBase
{
    private readonly FalconTouchDbContext _db;

    public WebhookController(FalconTouchDbContext db)
    {
        _db = db;
    }

    [HttpPost("efi")]
    public async Task<IActionResult> EfiWebhook([FromBody] EfiWebhookPayload payload)
    {
        // TODO: validar assinatura/certificado etc.

        var providerPaymentId = payload.PaymentId; // ajuste p/ formato real

        var payment = await _db.Payments
            .SingleOrDefaultAsync(p => p.Provider == "Efi" && p.ProviderPaymentId == providerPaymentId);

        if (payment is null) return NotFound();

        if (payload.Status == "CONFIRMED")
            payment.Status = PaymentStatus.Paid;
        else if (payload.Status == "FAILED")
            payment.Status = PaymentStatus.Failed;

        await _db.SaveChangesAsync();

        return Ok();
    }
}

public class EfiWebhookPayload
{
    public string PaymentId { get; set; } = default!;
    public string Status { get; set; } = default!;
}
