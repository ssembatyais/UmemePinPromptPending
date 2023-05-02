using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UssdPaymentSender.EnitiyObjects
{
    public class Transaction
    {
        private string narration, fromTelecom, sessionId, toTelecom,reconCode, customerRef, customerName, vendorTranId, tranAmount, tranCharge, telecom, tranType, toAccount, fromAccount, paymentDate, password, vendorCode, digitalSignature, paymentCode;
        internal string StatusCode, StatusDesc, PegPayId, TranId;
      
        public string Narration
        {
            get
            {
                return narration;
            }
            set
            {
                narration = value;
            }
        }
        public string FromTelecom
        {
            get
            {
                return fromTelecom;
            }
            set
            {
                fromTelecom = value;
            }
        }
        public string ReconCode
        {
            get
            {
                return reconCode;
            }
            set
            {
                reconCode = value;
            }
        }
        public string ToTelecom
        {
            get
            {
                return toTelecom;
            }
            set
            {
                toTelecom = value;
            }
        }
        public string PaymentCode
        {
            get
            {
                return paymentCode;
            }
            set
            {
                paymentCode = value;
            }
        }
        public string SessionId
        {
            get
            {
                return sessionId;
            }
            set
            {
                sessionId = value;
            }
        }
        public string VendorCode
        {
            get
            {
                return vendorCode;
            }
            set
            {
                vendorCode = value;
            }
        }
        public string Password
        {
            get
            {
                return password;
            }
            set
            {
                password = value;
            }
        }
        public string PaymentDate
        {
            get
            {
                return paymentDate;
            }
            set
            {
                paymentDate = value;
            }
        }
        public string Telecom
        {
            get
            {
                return telecom;
            }
            set
            {
                telecom = value;
            }
        }
        public string CustomerRef
        {
            get
            {
                return customerRef;
            }
            set
            {
                customerRef = value;
            }
        }
        public string CustomerName
        {
            get
            {
                return customerName;
            }
            set
            {
                customerName = value;
            }
        }
        public string TranAmount
        {
            get
            {
                return tranAmount;
            }
            set
            {
                tranAmount = value;
            }
        }
        public string TranCharge
        {
            get
            {
                return tranCharge;
            }
            set
            {
                tranCharge = value;
            }
        }
        public string VendorTranId
        {
            get
            {
                return vendorTranId;
            }
            set
            {
                vendorTranId = value;
            }
        }
        public string ToAccount
        {
            get
            {
                return toAccount;
            }
            set
            {
                toAccount = value;
            }
        }
        public string FromAccount
        {
            get
            {
                return fromAccount;
            }
            set
            {
                fromAccount = value;
            }
        }
        public string TranType
        {
            get
            {
                return tranType;
            }
            set
            {
                tranType = value;
            }
        }

    }
}