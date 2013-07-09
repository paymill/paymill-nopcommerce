using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.PayMill
{
    public class PayMillPaymentSettings : ISettings
    {
        public string publicKey { get; set; }
        public string privateKey { get; set; }
        public string apiUrl { get; set; }
    }
}
