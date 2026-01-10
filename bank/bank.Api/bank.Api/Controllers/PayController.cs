using bank.Api.Models;
using Microsoft.AspNetCore.Mvc;
using bank.Api.Infrastructure;


namespace bank.Api.Controllers
{
    [ApiController]
    public class PayController : Controller
    {
        private static readonly TimeSpan PaymentTtl = TimeSpan.FromMinutes(5);

        // GET https://localhost:7094/pay/{paymentId}
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
</head>
<body>
  <h2>Bank payment page</h2>
  <p><b>PaymentId:</b> {paymentId}</p>
  <p><b>Amount:</b> {state.Amount} {state.Currency}</p>


  <form method='POST' action='/pay/{paymentId}/confirm{orderIdPart}'>
    <label>PAN:</label><br/>
    <input name='pan' placeholder='1234 5678 9012 3456' /><br/><br/>

    <label>Expiry (MM/YY):</label><br/>
    <input name='expiry' placeholder='12/30' /><br/><br/>

    <label>CVV:</label><br/>
    <input name='cvv' placeholder='123' /><br/><br/>

    <label>Card holder name:</label><br/>
    <input name='cardHolderName' placeholder='Ime Prezime' /><br/><br/>

    <button type='submit'>Pay</button>
  </form>
</body>
</html>";

            return Content(html, "text/html");
        }

        // POST https://localhost:7094/pay/{paymentId}/confirm
        [HttpPost("/pay/{paymentId}/confirm")]
        public IActionResult Confirm([FromRoute] string paymentId, [FromForm] CardPaymentForm form)
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
            BankPaymentStore.MarkUsed(paymentId);

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
            string backLink = "";
            if (!string.IsNullOrWhiteSpace(orderId))
            {
                var path = status == "SUCCESS" ? "success" : "failed";
                backLink = $"<p><a href='http://localhost:5000/payment/{path}/{orderId}'>Return to WebShop</a></p>";
            }

            return $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8' /><title>Payment Result</title></head>
<body>
  <h2>Payment result: {status}</h2>
  <p><b>PaymentId:</b> {paymentId}</p>
  <p>{message}</p>
  {backLink}
</body>
</html>";
        }

    }
}
