using bank.Api.Models;
using Microsoft.AspNetCore.Mvc;
using bank.Api.Infrastructure;


namespace bank.Api.Controllers
{
    [ApiController]
    [Route("api/bank")]
    public class BankPaymentsController : ControllerBase
    {
        [HttpPost("init")]
        public IActionResult InitBankPayment([FromBody] BankInitRequest request)
        {
            if(request.Amount <= 0)
            {
                return BadRequest("Invalid amount");
            }

            var paymentId = Guid.NewGuid().ToString();
            BankPaymentStore.Create(paymentId, request.Amount, request.Currency);


            var paymentUrl = $"https://localhost:7094/pay/{paymentId}";

            var response = new BankInitResponse
            {
                PaymentId = paymentId,
                PaymentUrl = paymentUrl
            };

            return Ok(response);    
        }
    }
}
