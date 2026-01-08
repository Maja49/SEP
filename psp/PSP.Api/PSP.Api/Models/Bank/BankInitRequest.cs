namespace PSP.Api.Models.Bank
{
    public class BankInitRequest
    {
        public string MerchantId { get; set; }  
        public decimal Amount { get; set; } 
        public string Currency {  get; set; }   

        public string Stan { get; set; }
        public DateTime PspTimestamp { get; set; }  
    }
}
