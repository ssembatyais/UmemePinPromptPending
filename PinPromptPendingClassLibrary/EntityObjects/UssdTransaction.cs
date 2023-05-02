using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UssdPaymentSender.EnitiyObjects
{
    public class UssdTransaction
    {
        public string PaymentReference { get; internal set; }
        public string TransAmount { get; internal set; }
        public string TransactionId { get; internal set; }
        public string Balance { get; internal set; }
        public string Phone { get; internal set; }
        public string VendorCode { get; internal set; }
        public string CustomerName { get; internal set; }
        public string Utility { get; internal set; }
        public string UmbrellaCode { get; internal set; }
        public string PaymentDate { get; internal set; }

        public string VendorTranId { get; set; }

        public string PegPayTranId { get; set; }

        public string RecordDate { get; set; }

        public string Status { get; set; }

        public string TransCategory { get; set; }

        public string Password { get; set; }

        public string Paymentcode { get; set; }

        public string ToTelecom { get; set; }

        public string FromTelecom { get; set; }

        public object ToAccount { get; set; }

        public string FromAccount { get; set; }

        public string Network { get; set; }

        public string Naration { get; set; }
    }
}
