using FalconTouch.Application.Common;

namespace FalconTouch.Application.Payments;

public interface IPaymentService
{
    /// <summary>
    /// Cria um pagamento via Pix/Cartão e retorna dados pra front (ex: QRCode/link).
    /// A integração real com Efi/Stripe fica na implementação.
    /// </summary>
    Task<Result<PaymentDto>> CreatePaymentAsync(
        CreatePaymentCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza o status de um pagamento a partir de um webhook de provedor externo.
    /// </summary>
    Task<Result> UpdatePaymentStatusAsync(
        string provider,
        string providerPaymentId,
        string newStatus,
        CancellationToken cancellationToken = default);
}
