using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UssdPaymentSender.EnitiyObjects
{
    public class UssdTransactions
    {

        public string PaymentReference { get; set; }
        public string TransAmount { get; set; }
        public string TransactionId { get; set; }
        public string Balance { get; set; }
        public string Phone { get; set; }
        public string VendorCode { get; set; }
        public string CustomerName { get; set; }
        public string Utility { get; set; }
        public string UmbrellaCode { get; set; }
        public object PaymentDate { get; set; }
        public string Network { get; set; }
        public string Naration { get; set; }
        public string TelecomId { get; set; }

        public string QueueID { get; set; }

        public string SchoolCode { get; set; }
    }
}
