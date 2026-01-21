namespace bank.Api.Models
{
    public class CardChargeResponse
    {
        public string Status { get; set; } = "FAILED"; // SUCCESS / FAILED
        public string Message { get; set; } = string.Empty;
        public decimal? NewBalance { get; set; }
    }
}
