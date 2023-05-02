using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UssdPaymentSender.EnitiyObjects
{
    public class Charges
    {
        private string pegasusCommisionAccount, telecomCommissionAccount, cashoutAccount, vATAccount;
        double pegasusCommission, telecomCharge, cashOutCharge, vATCharge;
        public string PegasusCommisionAccount
        {
            get
            {
                return pegasusCommisionAccount;
            }
            set
            {
                pegasusCommisionAccount = value;
            }
        }
        public string TelecomCommissionAccount
        {
            get
            {
                return telecomCommissionAccount;
            }
            set
            {
                telecomCommissionAccount = value;
            }
        }
        public string CashoutAccount
        {
            get
            {
                return cashoutAccount;
            }
            set
            {
                cashoutAccount = value;
            }
        }

        public double PegasusCommission
        {
            get
            {
                return pegasusCommission;
            }
            set
            {
                pegasusCommission = value;
            }
        }
        public double TelecomCharge
        {
            get
            {
                return telecomCharge;
            }
            set
            {
                telecomCharge = value;
            }
        }
        public double VATCharge
        {
            get
            {
                return vATCharge;
            }
            set
            {
                vATCharge = value;
            }
        }
        public double CashOutCharge
        {
            get
            {
                return cashOutCharge;
            }
            set
            {
                cashOutCharge = value;
            }
        }
        public string VATAccount
        {
            get
            {
                return vATAccount;
            }
            set
            {
                vATAccount = value;
            }
        }
    }
}

                                                                                                                                                                                           