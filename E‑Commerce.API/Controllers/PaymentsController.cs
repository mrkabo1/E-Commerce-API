using E_Commerce.Application.Contracts;
using E_Commerce.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;


namespace E_Commerce.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;

        public PaymentsController(IPaymentService paymentService, IConfiguration configuration, ILogger logger)
        {
            _paymentService = paymentService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("create-payment-intent")]
        public async Task<IActionResult> CreatePaymentIntent([FromBody] CreatePaymentIntentRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _paymentService.CreatePaymentIntentAsync(request.OrderId);
                return Ok(result);
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (Exception) { return BadRequest("Payment intent creation failed."); }
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            _logger.LogInformation("Webhook received. Raw payload: {Payload}", json);
            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    _configuration["Stripe:WebhookSecret"]
                );
                _logger.LogInformation("Event constructed successfully: {EventType}", stripeEvent.Type);

                if (stripeEvent.Type == "payment_intent.succeeded")
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    _logger.LogInformation("Processing succeeded payment intent: {PaymentIntentId}", paymentIntent.Id);
                    await _paymentService.HandlePaymentSucceededAsync(paymentIntent.Id);
                }
                return Ok();
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe webhook signature verification failed.");
                return BadRequest();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while processing the webhook.");
                return StatusCode(500, "Internal server error");
            }
        }

    }
}
