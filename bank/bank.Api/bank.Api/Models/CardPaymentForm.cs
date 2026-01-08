namespace bank.Api.Models
{
    public class CardPaymentForm
    {
        public string Pan {  get; set; }
        public string Expiry { get; set; } // MM/YY
        public string Cvv { get; set; } //123
        public string CardHolderName { get; set; } // ime prezime
    }
}
