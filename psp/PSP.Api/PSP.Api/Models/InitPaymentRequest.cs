namespace PSP.Api.Models
{
    public class InitPaymentRequest
    {
        public string MerchantId { get; set; }
        public string MerchantPassword { get; set; }

        public decimal Amount { get; set; } 
        public string Currency { get; set; }

        public string MerchantOrderId { get; set; }
        public DateTime MerchantTimeStamp { get; set; }

        public string SuccessUrl { get; set; }
        public string FailedUrl { get; set; }
        public string ErrorUrl { get; set; }    
    }
}
