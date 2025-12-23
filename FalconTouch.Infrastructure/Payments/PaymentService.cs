using FalconTouch.Application.Common;
using FalconTouch.Application.Payments;
using FalconTouch.Domain.Entities;
using FalconTouch.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FalconTouch.Infrastructure.Payments;

public class PaymentService : IPaymentService
{
    private readonly FalconTouchDbContext _db;
    private readonly IConfiguration _config;

    public PaymentService(FalconTouchDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<Result<PaymentDto>> CreatePaymentAsync(
        CreatePaymentCommand command,
        CancellationToken cancellationToken = default)
    {
        var efiBaseUrl = _config["PaymentProviders:Efi:BaseUrl"];
        var efiClientId = _config["PaymentProviders:Efi:ClientId"];
        var efiClientSecret = _config["PaymentProviders:Efi:ClientSecret"];

        // TODO: aqui você chamará Efi ou Stripe real
        var providerPaymentId = Guid.NewGuid().ToString(); // MOCK

        var payment = new Payment
        {
            UserId = command.UserId,
            Amount = command.Amount,
            Currency = command.Currency,
            Provider = command.Provider,
            ProviderPaymentId = providerPaymentId,
            Status = PaymentStatus.Pending,
            CouponCode = command.CouponCode
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<PaymentDto>.Ok(new PaymentDto(
            payment.Id,
            payment.Amount,
            payment.Currency,
            payment.Provider,
            payment.ProviderPaymentId,
            payment.Status.ToString(),
            payment.CouponCode,
            payment.CreatedAt
        ));
    }

    public async Task<Result> UpdatePaymentStatusAsync(
        string provider,
        string providerPaymentId,
        string newStatus,
        CancellationToken cancellationToken = default)
    {
        var payment = await _db.Payments
            .FirstOrDefaultAsync(p =>
                p.Provider == provider &&
                p.ProviderPaymentId == providerPaymentId,
                cancellationToken);

        if (payment is null)
            return Result.Fail("Pagamento não encontrado.");

        payment.Status = newStatus switch
        {
            "CONFIRMED" => PaymentStatus.Paid,
            "FAILED" => PaymentStatus.Failed,
            _ => payment.Status
        };

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Ok(string.Empty);
    }
}
