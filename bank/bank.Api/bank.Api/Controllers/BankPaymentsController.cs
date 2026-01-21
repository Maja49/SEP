using bank.Api.Models;
using Microsoft.AspNetCore.Mvc;
using bank.Api.Infrastructure;
using bank.Api.DbContext;
using Microsoft.EntityFrameworkCore;


namespace bank.Api.Controllers
{
    [ApiController]
    [Route("api/bank")]
    public class BankPaymentsController : ControllerBase
    {
        private readonly BankDbContext _db;
        public BankPaymentsController(BankDbContext db) => _db = db;

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

        [HttpPost("card/charge")]
        public async Task<ActionResult<CardChargeResponse>> Charge([FromBody] CardChargeRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Pan) || req.Amount <= 0)
                return BadRequest(new CardChargeResponse { Status = "FAILED", Message = "Invalid request" });

            using var tx = await _db.Database.BeginTransactionAsync();

            var card = await _db.Cards.FirstOrDefaultAsync(c => c.Pan == req.Pan);
            if (card == null)
                return NotFound(new CardChargeResponse { Status = "FAILED", Message = "Card not found" });

            if (card.Balance < req.Amount)
                return BadRequest(new CardChargeResponse { Status = "FAILED", Message = "Insufficient funds", NewBalance = card.Balance });

            card.Balance -= req.Amount;
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new CardChargeResponse { Status = "SUCCESS", Message = "Payment successful", NewBalance = card.Balance });
        }

    }
}
