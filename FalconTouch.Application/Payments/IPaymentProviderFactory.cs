namespace FalconTouch.Application.Payments;

public interface IPaymentProviderFactory
{
    IPaymentProvider Get(PaymentProviderType type);
}
