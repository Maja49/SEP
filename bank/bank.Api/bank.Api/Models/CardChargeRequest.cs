namespace bank.Api.Models
{
    public class CardChargeRequest
    {
        public string Pan { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
