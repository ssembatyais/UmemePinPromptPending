using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UssdPaymentSender.EnitiyObjects
{
    public class Result
    {
        public Result() { }

        public string RowsAffected { get; set; }
        public string StatusCode { get; set; }
        public string StatusDesc { get; set; }
    }
}
