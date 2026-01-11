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
  <style>
    body {{
      margin: 0;
      font-family: Arial, sans-serif;
      background: linear-gradient(135deg, #645CBB, #A084DC);
      height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
    }}

    .card {{
      background: white;
      padding: 30px 40px;
      border-radius: 12px;
      width: 420px;
      box-shadow: 0 10px 25px rgba(0,0,0,0.2);
      text-align: center;
    }}

    h2 {{
      color: #645CBB;
      margin-bottom: 10px;
    }}

    .pid {{
      font-size: 0.85em;
      color: #666;
      margin-bottom: 25px;
    }}

    button {{
      width: 100%;
      padding: 12px;
      margin-top: 10px;
      border-radius: 8px;
      border: none;
      font-size: 1em;
      cursor: pointer;
    }}

    .primary {{
      background-color: #BFACE2;
      color: #2a2a2a;
    }}

    .primary:hover {{
      background-color: #A084DC;
      color: white;
    }}

    .disabled {{
      background-color: #e0e0e0;
      color: #999;
      cursor: not-allowed;
    }}
  </style>
</head>
<body>

  <div class='card'>
    <h2>Payment Service Provider</h2>
    <div class='pid'>Payment ID: {paymentId}</div>

    <form method='GET' action='/pay/{paymentId}/card'>
      <button class='primary' type='submit'>Pay with Card</button>
    </form>

    <button class='disabled' disabled>Pay with QR (coming soon)</button>
  </div>

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
