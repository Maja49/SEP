using System.Collections.Concurrent;

namespace PSP.Api.Infrastructure
{
    public static class PaymentStore
    {
        private static readonly ConcurrentDictionary<string, int> _paymentToOrderId = new();

        private static readonly ConcurrentDictionary<string, string> _paymentToBankUrl = new();

        public static void SetBankUrl(string paymentId, string bankUrl)
        {
            _paymentToBankUrl[paymentId] = bankUrl;
        }

        public static string? TryGetBankUrl(string paymentId)
        {
            return _paymentToBankUrl.TryGetValue(paymentId, out var url) ? url : null;
        }


        public static void SetOrderId(string paymentId, int orderId)
        {
            _paymentToOrderId[paymentId] = orderId;
        }

        public static int? TryGetOrderId(string paymentId)
        {
            return _paymentToOrderId.TryGetValue(paymentId, out var orderId) ? orderId : null;
        }
    }
}
