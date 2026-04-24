using E_Commerce.Application.DTOs;


namespace E_Commerce.Application.Contracts
{
    public interface IPaymentService
    {
        Task<CreatePaymentIntentResponse> CreatePaymentIntentAsync(int orderId);
        Task HandlePaymentSucceededAsync(string paymentIntentId);
    }
}
