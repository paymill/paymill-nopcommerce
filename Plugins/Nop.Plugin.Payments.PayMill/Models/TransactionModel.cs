using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nop.Plugin.Payments.PayMill.Models
{
    public class TransactionModel
    {
        public string id { get; set; }
        public string amount { get; set; }
        public string origin_amount { get; set; }
        public string currency { get; set; }
        public string status { get; set; }
        public string description { get; set; }
        public string livemode { get; set; }
        public string created_at { get; set; }
        public string updated_at { get; set; }
        public string response_code { get; set; }
        public PaymentModel payment { get; set; }

    }
}
