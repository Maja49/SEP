using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using PSP.Api.Models;
using PSP.Api.Models.Bank;

namespace PSP.Api.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentsController : Controller
    {
        [HttpPost("init")]
        public async Task<IActionResult> InitPayment([FromBody] InitPaymentRequest request, [FromServices] IHttpClientFactory httpClientFactory)
        {
            var paymentId = Guid.NewGuid().ToString();

            var bankRequest = new BankInitRequest
            {
                MerchantId = "psp-merchant-001",
                Amount = request.Amount,
                Currency = request.Currency,
                Stan = "123456",
                PspTimestamp = DateTime.UtcNow
            };

            var client = httpClientFactory.CreateClient("BankClient");

            var response = await client.PostAsJsonAsync(
                "https://localhost:7094/api/bank/init",
        bankRequest
                );

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode(502, "Bank error");
            }

            var bankResponse = await response.Content
        .ReadFromJsonAsync<BankInitResponse>();

            return Ok(new
            {
                paymentId = bankResponse.PaymentId,
                paymentUrl = bankResponse.PaymentUrl,
            });
        }
    }
}
