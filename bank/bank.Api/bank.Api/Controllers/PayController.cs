using bank.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace bank.Api.Controllers
{
    [ApiController]
    public class PayController : Controller
    {
        // GET https://localhost:7094/pay/{paymentId}
        [HttpGet("/pay/{paymentId}")]
        public IActionResult PayPage([FromRoute] string paymentId)
        {
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

  <form method='POST' action='/pay/{paymentId}/confirm'>
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
            if(string.IsNullOrWhiteSpace(form.Pan) ||
               string.IsNullOrWhiteSpace(form.Expiry) ||
               string.IsNullOrWhiteSpace(form.Cvv) ||
               string.IsNullOrWhiteSpace(form.CardHolderName))
            {
                return Content(ResultHtml("FAILED", paymentId, "All fields are required."), "text/html");
            }

            //pan validation 
            var panDigits = new string(form.Pan.Where(char.IsDigit).ToArray());
            if(panDigits.Length < 13 || panDigits.Length > 19 || !IsLuhnValid(panDigits))
            {
                return Content(ResultHtml("FAILED", paymentId, "Invalid card number (PAN)."), "text/html");
            }

            //cvv
            if (form.Cvv.Length != 3 || !form.Cvv.All(char.IsDigit))
            {
                return Content(ResultHtml("FAILED", paymentId, "Invalid CVV."), "text/html");
            }

            var month = DateTime.UtcNow.Month;

            //mm/yy 
            if (!TryParseExpiry(form.Expiry, out var expLastDayUtc) || expLastDayUtc < DateTime.UtcNow.Date || month > 12)
            {

                return Content(ResultHtml("FAILED", paymentId, "Card expired or invalid expiry format."), "text/html");
            }


            
            var globalTransactionId = Guid.NewGuid().ToString();
            return Content(ResultHtml("SUCCESS", paymentId, $"Approved. GlobalTransactionId: {globalTransactionId}"), "text/html");
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

        private static string ResultHtml(string status, string paymentId, string message)
        {
            return $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8' /><title>Payment Result</title></head>
<body>
  <h2>Payment result: {status}</h2>
  <p><b>PaymentId:</b> {paymentId}</p>
  <p>{message}</p>
  <a href='/swagger'>Back to Swagger</a>
</body>
</html>";
        }
    }
}
