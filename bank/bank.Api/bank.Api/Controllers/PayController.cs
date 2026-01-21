using bank.Api.Models;
using Microsoft.AspNetCore.Mvc;
using bank.Api.Infrastructure;
using bank.Api.DbContext;
using Microsoft.EntityFrameworkCore;

namespace bank.Api.Controllers
{
    [ApiController]
    public class PayController : Controller
    {
        private readonly BankDbContext _db;

        public PayController(BankDbContext db)
        {
            _db = db;
        }

        private static readonly TimeSpan PaymentTtl = TimeSpan.FromMinutes(5);

        // GET https://localhost:7094/pay/{paymentId}

        private static string UiCss() => @"
<style>
:root{
  --primary:#645CBB;
  --secondary:#A084DC;
  --accent:#BFACE2;
  --soft:#EBC7E6;
  --bg:#F6F4FF;
  --white:#fff;
}
*{box-sizing:border-box;}
body{
  margin:0;
  font-family:Segoe UI, Arial, sans-serif;
  background:linear-gradient(135deg,var(--bg),var(--soft));
  color:#1f1f2e;
}
.container{
  max-width:780px;
  margin:40px auto;
  padding:0 16px;
}
.card{
  background:var(--white);
  border-radius:16px;
  box-shadow:0 12px 30px rgba(0,0,0,.10);
  overflow:hidden;
}
.header{
  background:linear-gradient(90deg,var(--primary),var(--secondary));
  color:white;
  padding:18px 22px;
}
.header h2{margin:0;font-size:22px;}
.sub{
  opacity:.9;
  font-size:14px;
  margin-top:6px;
}
.content{padding:22px;}
.grid{display:grid;grid-template-columns:1fr 1fr;gap:14px;}
@media (max-width:700px){ .grid{grid-template-columns:1fr;} }

.label{font-weight:600;margin-bottom:6px;display:block;color:#2c2b55;}
.input{
  width:100%;
  padding:12px 12px;
  border:1px solid #ddd;
  border-radius:12px;
  font-size:14px;
  outline:none;
}
.input:focus{
  border-color:var(--secondary);
  box-shadow:0 0 0 .2rem rgba(160,132,220,.25);
}
.readonly{
  background:#f4f2fb;
}
.row{
  display:flex; gap:12px; align-items:center; flex-wrap:wrap;
}
.pill{
  display:inline-flex;
  align-items:center;
  gap:10px;
  padding:8px 12px;
  background:rgba(191,172,226,.35);
  border:1px solid rgba(100,92,187,.25);
  border-radius:999px;
  font-size:13px;
  color:#2c2b55;
}
.btn{
  width:100%;
  border:none;
  border-radius:14px;
  padding:12px 14px;
  font-weight:700;
  font-size:15px;
  color:white;
  background:var(--primary);
  cursor:pointer;
  margin-top:14px;
}
.btn:hover{ background:var(--secondary); }
.note{
  font-size:12px; color:#5b5a7a; margin-top:10px;
}
.cards{
  display:flex; gap:10px; margin-top:10px; flex-wrap:wrap;
}
.cardlogo{
  width:56px; height:34px;
  border-radius:10px;
  border:1px solid #e6e6ef;
  background:linear-gradient(180deg,#fff,#f5f5fb);
  display:flex; align-items:center; justify-content:center;
  font-weight:800; color:#3a3570;
}
.status{
  display:inline-block;
  padding:8px 12px;
  border-radius:999px;
  font-weight:700;
  font-size:13px;
}
.success{ background:rgba(40,167,69,.12); color:#1e7e34; border:1px solid rgba(40,167,69,.25); }
.failed{ background:rgba(220,53,69,.12); color:#b02a37; border:1px solid rgba(220,53,69,.25); }

a.link{
  color:var(--primary);
  font-weight:700;
  text-decoration:none;
}
a.link:hover{ text-decoration:underline; }
</style>
";

        [HttpGet("/pay/{paymentId}")]
        public IActionResult PayPage([FromRoute] string paymentId)
        {
            var orderId = Request.Query["orderId"].ToString();
            var orderIdPart = string.IsNullOrWhiteSpace(orderId) ? "" : $"?orderId={orderId}";

            var state = BankPaymentStore.Get(paymentId);
            if (state == null)
                return Content(ResultHtml("FAILED", paymentId, "Unknown payment.", orderId), "text/html");

            if (DateTime.UtcNow - state.CreatedUtc > PaymentTtl)
                return Content(ResultHtml("FAILED", paymentId, "Payment form expired.", orderId), "text/html");

            if (state.Used)
                return Content(ResultHtml("FAILED", paymentId, "This payment was already used.", orderId), "text/html");


            var html = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8' />
  <title>Bank Payment</title>
  {UiCss()}
</head>
<body>
  <div class='container'>
    <div class='card'>
      <div class='header'>
        <h2>Acquirer Bank — Secure Payment</h2>
        <div class='sub'>One-time payment link • Expires in {PaymentTtl.TotalMinutes:0} min</div>
      </div>

      <div class='content'>
        <div class='row' style='margin-bottom:14px;'>
          <span class='pill'><b>PaymentId:</b> {paymentId}</span>
          <span class='pill'><b>OrderId:</b> {(string.IsNullOrWhiteSpace(orderId) ? "-" : orderId)}</span>
        </div>

        <div class='row' style='margin-bottom:10px;'>
          <div class='cards'>
            <div class='cardlogo'>VISA</div>
            <div class='cardlogo'>MC</div>
            <div class='cardlogo'>AMEX</div>
          </div>
        </div>

        <form method='POST' action='/pay/{paymentId}/confirm{orderIdPart}'>
          <div class='grid'>
            <div>
              <label class='label'>PAN</label>
              <input class='input' name='pan' placeholder='1234 5678 9012 3456' autocomplete='cc-number' required />
            </div>

            <div>
              <label class='label'>Amount</label>
              <input class='input readonly' value='{state.Amount} {state.Currency}' readonly />
            </div>

            <div>
              <label class='label'>Expiry (MM/YY)</label>
              <input class='input' name='expiry' placeholder='12/30' autocomplete='cc-exp' required/>
            </div>

            <div>
              <label class='label'>CVV</label>
              <input class='input' name='cvv' placeholder='123' autocomplete='cc-csc' required/>
            </div>

            <div style='grid-column:1 / -1;'>
              <label class='label'>Card holder name</label>
              <input class='input' name='cardHolderName' placeholder='Ime Prezime' autocomplete='cc-name' required/>
            </div>
          </div>

          <button class='btn' type='submit'>Pay securely</button>
          <div class='note'>Card number is validated using Luhn algorithm. This payment link is valid for one attempt only.</div>
        </form>
      </div>
    </div>
  </div>
</body>
</html>";


            return Content(html, "text/html");
        }

        // POST https://localhost:7094/pay/{paymentId}/confirm
        [HttpPost("/pay/{paymentId}/confirm")]
        public async Task<IActionResult> Confirm([FromRoute] string paymentId, [FromForm] CardPaymentForm form)
        {
            var orderId = Request.Query["orderId"].ToString();

            var state = BankPaymentStore.Get(paymentId);
            if (state == null)
                return Content(ResultHtml("FAILED", paymentId, "Unknown payment.", orderId), "text/html");

            if (DateTime.UtcNow - state.CreatedUtc > PaymentTtl)
                return Content(ResultHtml("FAILED", paymentId, "Payment form expired.", orderId), "text/html");

            if (state.Used)
                return Content(ResultHtml("FAILED", paymentId, "This payment was already used.", orderId), "text/html");

            // one-time: čim pokuša confirm, zaključaj transakciju (jedan pokušaj)
            
            if (string.IsNullOrWhiteSpace(form.Pan) ||
               string.IsNullOrWhiteSpace(form.Expiry) ||
               string.IsNullOrWhiteSpace(form.Cvv) ||
               string.IsNullOrWhiteSpace(form.CardHolderName))
            {
                return Content(ResultHtml("FAILED", paymentId, "All fields are required.", orderId), "text/html");
            }

            //pan validation 
            var panDigits = new string(form.Pan.Where(char.IsDigit).ToArray());
            if(panDigits.Length < 13 || panDigits.Length > 19 || !IsLuhnValid(panDigits))
            {
                return Content(ResultHtml("FAILED", paymentId, "Invalid card number (PAN).", orderId), "text/html");
            }

            //cvv
            if (form.Cvv.Length != 3 || !form.Cvv.All(char.IsDigit))
            {
                return Content(ResultHtml("FAILED", paymentId, "Invalid CVV.", orderId), "text/html");
            }

            var month = DateTime.UtcNow.Month;

            //mm/yy 
            if (!TryParseExpiry(form.Expiry, out var expLastDayUtc) || expLastDayUtc < DateTime.UtcNow.Date || month > 12)
            {

                return Content(ResultHtml("FAILED", paymentId, "Card expired or invalid expiry format.", orderId), "text/html");
            }

            BankPaymentStore.MarkUsed(paymentId);


            using var tx = await _db.Database.BeginTransactionAsync();

            var card = await _db.Cards.FirstOrDefaultAsync(c => c.Pan == panDigits);
            if (card == null)
            {
                await tx.RollbackAsync();
                return Content(ResultHtml("FAILED", paymentId, "Card not found.", orderId), "text/html");
            }

            if (card.Balance < state.Amount)
            {
                await tx.RollbackAsync();
                return Content(ResultHtml("FAILED", paymentId, "Insufficient funds.", orderId), "text/html");
            }

            card.Balance -= state.Amount;
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            var globalTransactionId = Guid.NewGuid().ToString();
            return Content(ResultHtml("SUCCESS", paymentId, $"Approved. GlobalTransactionId: {globalTransactionId}", orderId), "text/html");
        }

        private static bool IsLuhnValid(string pan)
        {
            int sum = 0;
            bool alt = false;
            for(int i = pan.Length - 1; i >= 0; i--)
            {
                int n = pan[i] - '0';
                if(alt)
                {
                    n *= 2;
                    if (n > 9) n -= 9;
                }
                sum += n;
                alt = !alt;
            }
            return sum % 10 == 0;
        }

        private static bool TryParseExpiry(string expiry, out DateTime expLastDayUtc)
        {
            expLastDayUtc = default;

            var parts = expiry.Split('/');
            if(parts.Length != 2) return false;
            if (!int.TryParse(parts[0], out var mm)) return false;
            if (!int.TryParse(parts[1], out var yy)) return false;
            if (mm < 1 || mm > 12) return false;

            // 26 zapravo bude 2026
            var year = 2000 + yy;
            var lastDay = DateTime.DaysInMonth(year, mm);

            expLastDayUtc = new DateTime(year, mm, lastDay, 23, 59, 59, DateTimeKind.Utc);
            return true;
        }

        private static string ResultHtml(string status, string paymentId, string message, string? orderId)
        {
            var isSuccess = status == "SUCCESS";
            var cls = isSuccess ? "success" : "failed";
            var label = isSuccess ? "SUCCESS" : "FAILED";

            string backLink = "";
            if (!string.IsNullOrWhiteSpace(orderId))
            {
                var path = isSuccess ? "success" : "failed";
                backLink = $"<p style='margin-top:14px;'><a class='link' href='http://localhost:5000/payment/{path}/{orderId}'>← Return to WebShop</a></p>";
            }

            return $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8' />
  <title>Payment Result</title>
  {UiCss()}
</head>
<body>
  <div class='container'>
    <div class='card'>
      <div class='header'>
        <h2>Payment Result</h2>
        <div class='sub'>Acquirer response • Transaction status</div>
      </div>

      <div class='content'>
        <div class='row' style='margin-bottom:14px;'>
          <span class='status {cls}'>{label}</span>
          <span class='pill'><b>PaymentId:</b> {paymentId}</span>
        </div>

        <p style='font-size:15px; margin:0;'>{message}</p>

        {backLink}
      </div>
    </div>
  </div>
</body>
</html>";
        }


    }
}
