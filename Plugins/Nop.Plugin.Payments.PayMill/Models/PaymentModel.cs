using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nop.Plugin.Payments.PayMill.Models
{
    public class PaymentModel
    {

        public string id { get; set; }
        public string type { get; set; }
        public string client { get; set; }
        public string card_type { get; set; }
        public string country { get; set; }
        public string expire_month { get; set; }
        public string expire_year { get; set; }
        public string card_holder { get; set; }
        public string last4 { get; set; }
        public string created_at { get; set; }
        public string updated_at { get; set; }

    }
}
