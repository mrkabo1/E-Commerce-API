using E_Commerce.Application.Contracts;
using E_Commerce.Application.DTOs;
using E_Commerce.Domain.Entities;
using E_Commerce.Infrastructure.DatabaseContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Stripe;

namespace E_Commerce.Infrastructure.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly E_CommerceDbContext _context;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(E_CommerceDbContext context, ILogger<PaymentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<CreatePaymentIntentResponse> CreatePaymentIntentAsync(int orderId)
        {
            _logger.LogInformation("Creating PaymentIntent for order {OrderId}.", orderId);

            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found when creating PaymentIntent.", orderId);
                throw new KeyNotFoundException("Order not found.");
            }

            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(order.TotalPrice * 100),
                Currency = "usd",
                Metadata = new Dictionary<string, string>
                {
                    { "OrderId", order.Id.ToString() }
                }
            };

            var service = new PaymentIntentService();
            PaymentIntent paymentIntent;

            try
            {
                paymentIntent = await service.CreateAsync(options);
                _logger.LogInformation("PaymentIntent created for order {OrderId}: {PaymentIntentId}, amount {Amount} {Currency}.",
                    orderId, paymentIntent.Id, paymentIntent.Amount, paymentIntent.Currency);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error while creating PaymentIntent for order {OrderId}.", orderId);
                throw; // rethrow to let controller handle
            }

            var payment = new Payment
            {
                OrderId = order.Id,
                StripePaymentIntentId = paymentIntent.Id,
                Amount = order.TotalPrice,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            await _context.Payments.AddAsync(payment);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Payment record saved for order {OrderId} with PaymentIntentId {PaymentIntentId}.", orderId, paymentIntent.Id);

            return new CreatePaymentIntentResponse { ClientSecret = paymentIntent.ClientSecret };
        }

        public async Task HandlePaymentSucceededAsync(string paymentIntentId)
        {
            _logger.LogInformation("Handling succeeded PaymentIntent {PaymentIntentId}.", paymentIntentId);

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId);
            if (payment == null)
            {
                _logger.LogWarning("Payment record not found for PaymentIntent {PaymentIntentId}.", paymentIntentId);
                return;
            }

            var order = await _context.Orders.FindAsync(payment.OrderId);
            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} (linked to PaymentIntent {PaymentIntentId}) not found.", payment.OrderId, paymentIntentId);
                return;
            }

            if (order.Status != OrderStatus.Pending)
            {
                _logger.LogWarning("Order {OrderId} status is {OrderStatus}, not Pending. Skipping update for PaymentIntent {PaymentIntentId}.",
                    order.Id, order.Status, paymentIntentId);
                return;
            }

            order.Status = OrderStatus.Paid;
            payment.Status = "Succeeded";

            await _context.SaveChangesAsync();

            _logger.LogInformation("Order {OrderId} marked as Paid after successful PaymentIntent {PaymentIntentId}.", order.Id, paymentIntentId);
        }
    }
}