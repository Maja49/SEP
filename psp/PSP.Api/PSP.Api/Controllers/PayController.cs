using Microsoft.AspNetCore.Mvc;
using PSP.Api.Infrastructure;

namespace PSP.Api.Controllers
{
    [ApiController]
    public class PayController : Controller
    {
        // GET https://localhost:7098/pay/{paymentId}
        [HttpGet("/pay/{paymentId}")]
        public IActionResult PayPage(string paymentId)
        {
            var html = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'/>
  <title>PSP - Payment</title>
</head>
<body>
  <h2>Payment Service Provider</h2>
  <p><b>Payment ID:</b> {paymentId}</p>

  <h3>Select payment method</h3>

  <form method='GET' action='/pay/{paymentId}/card'>
    <button type='submit'>Pay with Card</button>
  </form>

  <br/>

  <button disabled>Pay with QR (coming soon)</button>
</body>
</html>";

            return Content(html, "text/html");
        }

        // GET https://localhost:7098/pay/{paymentId}/card
        [HttpGet("/pay/{paymentId}/card")]
        public IActionResult RedirectToBank(string paymentId)
        {
            var orderId = PaymentStore.TryGetOrderId(paymentId);  
            var bankUrl = PaymentStore.TryGetBankUrl(paymentId);

            if (string.IsNullOrWhiteSpace(bankUrl))
                return Content("Bank URL not found for this payment.", "text/plain");

            if (orderId != null)
            {
                bankUrl += bankUrl.Contains("?") ? $"&orderId={orderId}" : $"?orderId={orderId}";
            }

            /*var bankPaymentUrl = orderId == null
                ? $"https://localhost:7094/pay/{paymentId}"
                : $"https://localhost:7094/pay/{paymentId}?orderId={orderId}";*/
            return Redirect(bankUrl);

        }
    }
}
