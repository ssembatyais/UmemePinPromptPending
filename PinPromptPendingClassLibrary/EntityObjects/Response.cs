using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UssdPaymentSender.EnitiyObjects
{
    public class Response
    {
        private string statusCode, statusDescription, pegPayId, telecomId;

        public string TelecomId
        {
            get
            {
                return telecomId;
            }
            set
            {
                telecomId = value;
            }
        }
        public string PegPayId
        {
            get
            {
                return pegPayId;
            }
            set
            {
                pegPayId = value;
            }
        }
        public string StatusDescription
        {
            get
            {
                return statusDescription;
            }
            set
            {
                statusDescription = value;
            }
        }
        public string StatusCode
        {
            get
            {
                return statusCode;
            }
            set
            {
                statusCode = value;
            }
        }
    }
}
