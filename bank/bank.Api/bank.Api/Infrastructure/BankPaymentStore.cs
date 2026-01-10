using System.Collections.Concurrent;

namespace bank.Api.Infrastructure
{
    public class BankPaymentState
    {
        public DateTime CreatedUtc { get; init; }
        public bool Used { get; set; }

        public decimal Amount { get; init; }
        public string Currency { get; init; } = "RSD";
    }

    public static class BankPaymentStore
    {
        private static readonly ConcurrentDictionary<string, BankPaymentState> _payments = new();

        public static void Create(string paymentId, decimal amount, string currency)
        {
            _payments[paymentId] = new BankPaymentState
            {
                CreatedUtc = DateTime.UtcNow,
                Used = false,
                Amount = amount,
                Currency = currency
            };
        }

        public static BankPaymentState? Get(string paymentId)
        {
            return _payments.TryGetValue(paymentId, out var s) ? s : null;
        }

        public static void MarkUsed(string paymentId)
        {
            if (_payments.TryGetValue(paymentId, out var s))
            {
                s.Used = true;
            }
        }
    }
}
