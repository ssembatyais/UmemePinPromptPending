using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data;
using Microsoft.Practices.EnterpriseLibrary.Data;
using Microsoft.Practices.EnterpriseLibrary.Common;
//using Microsoft.Practices.ObjectBuilder;
using System.Threading;
using System.Net;
using System.Collections;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using UssdPaymentSender.MomoApi;
using System.Net.Security;



namespace UssdPaymentSender.EnitiyObjects
{

    public class DataBaseHandler
    {
        private Database PegasusUssddb, MMoneyDb;

        private DataTable returnTable, dataTable;
        private DbCommand command;
        public string queue;
        public string sessionTrackqueue;

        public DataBaseHandler()
        {
            try
            {
                PegasusUssddb = DatabaseFactory.CreateDatabase("LivePegasusUssddbConnection");
                MMoneyDb = DatabaseFactory.CreateDatabase("TestMMoneyDb");
            }
            catch (Exception ex)
            {
                LogError("0", "ALL", "*272#", "1", "Inititalizing DBHandler");
                // throw ex;
            }
            //PegasusUssddb = DatabaseFactory.CreateDatabase("LivePegasusUssddbConnection");
            //queue = GetQueuePath("LiveMSQName");
            //sessionTrackqueue = GetSessionTrackQueuePath("SessionTrackMSQName");
        }

        public DataSet ExecuteDataSetMomo(string procedure, params object[] parameters)
        {
            try
            {
                //DataAccess data = new DataAccess();
                //return data.ExecuteDataSet("5", "PegasusUssd", procedure, parameters);
                command = MMoneyDb.GetStoredProcCommand(procedure, parameters);
                return MMoneyDb.ExecuteDataSet(command);
            }
            catch (Exception ex)
            {
                LogError("0", "ALL", "*272#", "1", "Execute Dataset");
                throw ex;
            }
        }
        public Result ExecuteNonQueryMomo(string procedure, params object[] parameters)
        {
            try
            {
                //DataAccess data = new DataAccess();
                //return data.ExecuteNonQuery("5", "PegasusUssd", procedure, parameters);
                command = MMoneyDb.GetStoredProcCommand(procedure, parameters);
                MMoneyDb.ExecuteNonQuery(command);
                return new Result();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public DataSet ExecuteDataSet(string procedure, params object[] parameters)
        {
            try
            {
                //DataAccess data = new DataAccess();
                //return data.ExecuteDataSet("5", "PegasusUssd", procedure, parameters);
                command = PegasusUssddb.GetStoredProcCommand(procedure, parameters);
                return PegasusUssddb.ExecuteDataSet(command);
            }
            catch (Exception ex)
            {
                LogError("0", "ALL", "*272#", "1", "Execute Dataset");
                throw ex;
            }
        }
        public Result ExecuteNonQuery(string procedure, params object[] parameters)
        {
            try
            {
                //DataAccess data = new DataAccess();
                //return data.ExecuteNonQuery("5", "PegasusUssd", procedure, parameters);
                command = PegasusUssddb.GetStoredProcCommand(procedure, parameters);
                PegasusUssddb.ExecuteNonQuery(command);
                return new Result();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public void UpdateTransactionInTestMomo(string procedure, params object[] parameters)
        {
            try
            {
                //DataAccess data = new DataAccess();
                //return data.ExecuteNonQuery("5", "PegasusUssd", procedure, parameters);
                command = MMoneyDb.GetStoredProcCommand(procedure, parameters);
                MMoneyDb.ExecuteNonQuery(command);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public string InsertReceivedTransactionPUSH4(MomoApi.Transaction trans, int tranTypeId, Charges charge)
        {
            string PegPayId = "";
            try
            {
                command = MMoneyDb.GetStoredProcCommand("InsertReceivedTransactionPUSHException_2", new object[]{
                 trans.FromAccount,
                trans.ToAccount,
                trans.CustomerName,
                trans.VendorTranId, 
               // trans.SessionId,
                trans.TranAmount,
                charge.PegasusCommission,
                trans.FromTelecom.Trim(),
                trans.ToTelecom.Trim(),
                trans.PaymentDate,
                trans.PaymentCode,
                tranTypeId,
                trans.VendorCode,
                trans.Telecom,
                charge.TelecomCharge,
                charge.CashOutCharge, "0", "0",
                charge.PegasusCommisionAccount,
                charge.TelecomCommissionAccount,
                charge.CashoutAccount,
                trans.Narration
            });

                dataTable = MMoneyDb.ExecuteDataSet(command).Tables[0];
                if (dataTable.Rows.Count > 0)
                {
                    PegPayId = dataTable.Rows[0]["PegPayTranId"].ToString();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return PegPayId;
        }

        public List<MomoApi.Transaction> GetPendingsAndFailed()
        {
            List<MomoApi.Transaction> ussdtxn = new List<MomoApi.Transaction>();
            try
            {
                DataBaseHandler dh = new DataBaseHandler();
                // dh.LogError("0", "ALL", "*272#", "1", "Getting txns to send");
                DataSet ds = ExecuteDataSet("GetPendingsToProccessInstitutionCollections");
                DataTable dt = ds.Tables[0];
                DataTable dtcred = ds.Tables[1];

                foreach (DataRow dr in dt.Rows)
                {
                    MomoApi.Transaction txn = new MomoApi.Transaction();
                    txn.VendorTranId = dr["VendorTranId"].ToString();
                    txn.VendorCode = "TESTFLEXIPAY";
                    txn.Password = "17D14VD828";
                    txn.SessionId = dr["PegPayTranId"].ToString();
                    //txn.Password = GetVendorPassword(dtcred, txn.VendorCode);
                    ussdtxn.Add(txn);
                }
            }
            catch (Exception ex)
            {
                ExecuteNonQuery("SystemErrorLogs_Insert",
                       "",
                       "",
                       "",
                       "GetPendingsAndFailedForProccessing",
                       ex.Message,
                       ex.StackTrace
                       );
            }


            return ussdtxn;

        }

        public string GetTelecomAccount(string VendorCode, string ToTelecom, string tranType)
        {
            DataTable datatable = new DataTable();
            string fromAccount = "";
            try
            {
                string Type = "";
                if (tranType.ToUpper().Equals("AIRTIME"))
                {
                }
                else
                {
                    Type = "ESCROW";
                    //if (!tranType.ToUpper().Equals("PULL"))
                    //{
                    //    VendorCode = "PEGASUS";
                    //}

                    datatable = GetTelecomAccount1(VendorCode, ToTelecom, Type);
                    if (datatable.Rows.Count > 0)
                    {
                        fromAccount = datatable.Rows[0]["AccountNumber"].ToString();
                    }
                    else
                    {
                        VendorCode = "PEGASUS";
                        datatable = GetTelecomAccount1(VendorCode, ToTelecom, Type);
                        fromAccount = datatable.Rows[0]["AccountNumber"].ToString();
                    }
                }


            }
            catch (Exception ex)
            {
                throw ex;
            }
            return fromAccount;
        }
        public string InsertReceivedTransactionPULLException(MomoApi.Transaction trans, int tranTypeId, Charges charge)
        {
            string PegPayId = "";
            DataTable datatable = null;
            try
            {

                command = MMoneyDb.GetStoredProcCommand("InsertReceivedTransactionPULLException1New", trans.FromAccount, trans.ToAccount, trans.CustomerName, trans.VendorTranId, trans.TranAmount, charge.PegasusCommission, trans.FromTelecom.Trim(), trans.ToTelecom.Trim(), trans.PaymentDate, trans.PaymentCode
                    , tranTypeId, trans.VendorCode, trans.Telecom, "", charge.TelecomCharge, charge.CashOutCharge, charge.VATCharge, "0", "0", charge.PegasusCommisionAccount, charge.TelecomCommissionAccount, charge.CashoutAccount, charge.VATAccount);
                command.CommandTimeout = 90000;
                datatable = MMoneyDb.ExecuteDataSet(command).Tables[0];
                if (datatable.Rows.Count > 0)
                {
                    PegPayId = datatable.Rows[0]["PegPayTranId"].ToString();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return PegPayId;
        }
        internal DataTable GetPegasusCharge(string VendorCode)
        {
            try
            {
                command = MMoneyDb.GetStoredProcCommand("GetPegasusCharge", VendorCode);
                DataTable dt = MMoneyDb.ExecuteDataSet(command).Tables[0];
                return dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        internal DataTable CheckForMerchantAccount(string VendorCode, string network)
        {
            try
            {
                command = MMoneyDb.GetStoredProcCommand("CheckForMerchantAccount", VendorCode, network);
                DataTable dt = MMoneyDb.ExecuteDataSet(command).Tables[0];
                return dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        internal void UpdateBalanceOkayFlag(string VendorCode, string status)
        {
            try
            {
                command = MMoneyDb.GetStoredProcCommand("UpdateBalanceFlag", VendorCode, status);
                MMoneyDb.ExecuteNonQuery(command);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        internal DataTable GetPegasusAccountBalance(string VendorCode)
        {
            try
            {
                command = MMoneyDb.GetStoredProcCommand("GetPegasusAccountBalance", VendorCode);
                DataTable dt = MMoneyDb.ExecuteDataSet(command).Tables[0];
                return dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public DataTable GetTelecomAccount1(string CustomerCode, string ToTelecom, string AccountType)
        {
            try
            {
                command = MMoneyDb.GetStoredProcCommand("GetFromAccount", CustomerCode, ToTelecom, AccountType);
                DataTable dt = MMoneyDb.ExecuteDataSet(command).Tables[0];
                return dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public List<MomoApi.Transaction> GetPendingUssdTransactionForProccessing(string vendorCode)
        {
            List<MomoApi.Transaction> ussdtxn = new List<MomoApi.Transaction>();
            try
            {
                DataBaseHandler dh = new DataBaseHandler();
                // dh.LogError("0", "ALL", "*272#", "1", "Getting txns to send");
                DataSet ds = ExecuteDataSet("GetPendingUssdTransactionForProccessingCONLOG", vendorCode);
                DataTable dt = ds.Tables[0];
                DataTable dtcred = ds.Tables[1];

                foreach (DataRow dr in dt.Rows)
                {
                    MomoApi.Transaction txn = new MomoApi.Transaction();

                    Decimal amount = Decimal.Parse(dr["TranAmount"].ToString());
                    Double amount2 = Double.Parse(amount.ToString()) + 0.00;
                    Decimal result = Math.Truncate(Decimal.Parse(amount2.ToString()));
                    txn.CustomerName = dr["CustName"].ToString();
                    txn.VendorTranId = dr["VendorTranId"].ToString();
                    txn.SessionId = dr["RecordId"].ToString();
                    txn.CustomerRef = dr["PaymentReference"].ToString();
                    txn.TranAmount = result.ToString();
                    txn.PaymentDate = dr["PaymentDate"].ToString();
                    //txn.VendorCode = dr["VendorCode"].ToString();
                    txn.VendorCode = "TESTFLEXIPAY";
                    txn.TranType = dr["TransCategory"].ToString();
                    txn.TranCharge = "0";
                    txn.PaymentCode = "1";
                    txn.PaymentDate = DateTime.Parse(txn.PaymentDate).ToString("MM/dd/yyyy");
                    txn.FromTelecom = dr["FromNetwork"].ToString();
                    txn.ToTelecom = dr["ToNetwork"].ToString();
                    txn.Narration = dr["Narration"].ToString();
                    txn.Password = "17D14VD828";//
                    //txn.Password = GetVendorPassword(dtcred, txn.VendorCode);

                    txn.FromAccount = dr["Phone"].ToString();
                    txn.ToAccount = dr["Phone"].ToString();
                    txn.AddendumData = dr["VendorCode"].ToString();
                    string dataToSign =
                    txn.CustomerRef + txn.CustomerName + txn.FromTelecom + txn.ToTelecom +
                    txn.VendorTranId + txn.VendorCode + txn.Password + txn.PaymentDate + txn.TranType
                    + txn.PaymentCode + txn.TranAmount + txn.FromAccount + txn.ToAccount;
                    txn.DigitalSignature = GetDigitalSignature(dataToSign);

                    ussdtxn.Add(txn);
                }
            }
            catch (Exception ex)
            {
                ExecuteNonQuery("SystemErrorLogs_Insert",
                       "",
                       "",
                       "",
                       "GetPendingUssdTransactionForProccessing",
                       ex.Message,
                       ex.StackTrace
                       );
            }


            return ussdtxn;

        }


        public List<MomoApi.Transaction> GetPendingMomoEquityTransactionForProccessing(string vendorCode)
        {
            List<MomoApi.Transaction> ussdtxn = new List<MomoApi.Transaction>();
            try
            {
                DataBaseHandler dh = new DataBaseHandler();
                // dh.LogError("0", "ALL", "*272#", "1", "Getting txns to send");
                DataSet ds = ExecuteDataSetMomo("GetPendingMomoEquityTransactionForProccessing", vendorCode);
                DataTable dt = ds.Tables[0];

                foreach (DataRow dr in dt.Rows)
                {
                    MomoApi.Transaction txn = new MomoApi.Transaction();

                    Decimal amount = Decimal.Parse(dr["TranAmount"].ToString());
                    Double amount2 = Double.Parse(amount.ToString()) + 0.00;
                    Decimal result = Math.Truncate(Decimal.Parse(amount2.ToString()));
                    txn.CustomerName = dr["CustName"].ToString();
                    txn.VendorTranId = dr["VendorTranId"].ToString();
                    txn.SessionId = dr["RecordId"].ToString();
                    //txn.CustomerRef = dr["PaymentReference"].ToString();
                    txn.TranAmount = result.ToString();
                    txn.PaymentDate = dr["PaymentDate"].ToString();
                    //txn.VendorCode = dr["VendorCode"].ToString();
                    txn.VendorCode = "TESTFLEXIPAY";
                    txn.TranType = "PULL"; //dr["TranType"].ToString();
                    txn.TranCharge = "0";
                    txn.PaymentCode = "1";
                    txn.PaymentDate = DateTime.Parse(txn.PaymentDate).ToString("MM/dd/yyyy");
                    txn.FromTelecom = dr["FromNetwork"].ToString();
                    txn.ToTelecom = dr["ToNetwork"].ToString();
                    txn.Narration = dr["Narration"].ToString();
                    txn.Password = "17D14VD828";//
                    //txn.Password = GetVendorPassword(dtcred, txn.VendorCode);

                    //txn.FromAccount = dr["Phone"].ToString().Replace('+',' ').Trim();
                    //txn.ToAccount = dr["Phone"].ToString().Replace('+', ' ').Trim();

                    txn.FromAccount = dr["Phone"].ToString();
                    txn.ToAccount = dr["Phone"].ToString();
                    txn.AddendumData = dr["VendorCode"].ToString();
                    string dataToSign =
                    txn.CustomerRef + txn.CustomerName + txn.FromTelecom + txn.ToTelecom +
                    txn.VendorTranId + txn.VendorCode + txn.Password + txn.PaymentDate + txn.TranType
                    + txn.PaymentCode + txn.TranAmount + txn.FromAccount + txn.ToAccount;
                    txn.DigitalSignature = GetDigitalSignature(dataToSign);

                    ussdtxn.Add(txn);
                }
            }
            catch (Exception ex)
            {
                ExecuteNonQuery("SystemErrorLogs_Insert",
                       "",
                       "",
                       "",
                       "GetPendingUssdTransactionForProccessing",
                       ex.Message,
                       ex.StackTrace
                       );
            }


            return ussdtxn;

        }
        public List<MomoApi.Transaction> GetPendingMomoOssnTransactionForProccessing(string vendorCode)
        {
            List<MomoApi.Transaction> ussdtxn = new List<MomoApi.Transaction>();
            try
            {
                DataBaseHandler dh = new DataBaseHandler();
                // dh.LogError("0", "ALL", "*272#", "1", "Getting txns to send");
                DataSet ds = ExecuteDataSetMomo("GetPendingMomoEquityTransactionForProccessingOSSN", vendorCode);
                DataTable dt = ds.Tables[0];

                foreach (DataRow dr in dt.Rows)
                {
                    MomoApi.Transaction txn = new MomoApi.Transaction();

                    Decimal amount = Decimal.Parse(dr["TranAmount"].ToString());
                    Double amount2 = Double.Parse(amount.ToString()) + 0.00;
                    Decimal result = Math.Truncate(Decimal.Parse(amount2.ToString()));
                    txn.CustomerName = dr["CustName"].ToString();
                    txn.VendorTranId = dr["VendorTranId"].ToString();
                    txn.SessionId = dr["RecordId"].ToString();
                    //txn.CustomerRef = dr["PaymentReference"].ToString();
                    txn.TranAmount = result.ToString();
                    txn.PaymentDate = dr["PaymentDate"].ToString();
                    //txn.VendorCode = dr["VendorCode"].ToString();
                    txn.VendorCode = "TESTFLEXIPAY";
                    txn.TranType = "PULL"; //dr["TranType"].ToString();
                    txn.TranCharge = "0";
                    txn.PaymentCode = "1";
                    txn.PaymentDate = DateTime.Parse(txn.PaymentDate).ToString("MM/dd/yyyy");
                    txn.FromTelecom = dr["FromNetwork"].ToString();
                    txn.ToTelecom = dr["ToNetwork"].ToString();
                    txn.Narration = dr["Narration"].ToString();
                    txn.Password = "17D14VD828";//
                    //txn.Password = GetVendorPassword(dtcred, txn.VendorCode);

                    txn.FromAccount = dr["Phone"].ToString();
                    txn.ToAccount = dr["Phone"].ToString();
                    txn.AddendumData = dr["VendorCode"].ToString();
                    string dataToSign =
                    txn.CustomerRef + txn.CustomerName + txn.FromTelecom + txn.ToTelecom +
                    txn.VendorTranId + txn.VendorCode + txn.Password + txn.PaymentDate + txn.TranType
                    + txn.PaymentCode + txn.TranAmount + txn.FromAccount + txn.ToAccount;
                    txn.DigitalSignature = GetDigitalSignature(dataToSign);

                    ussdtxn.Add(txn);
                }
            }
            catch (Exception ex)
            {
                ExecuteNonQuery("SystemErrorLogs_Insert",
                       "",
                       "",
                       "",
                       "GetPendingUssdTransactionForProccessing",
                       ex.Message,
                       ex.StackTrace
                       );
            }


            return ussdtxn;

        }


        public List<MomoApi.Transaction> GetPendingMomoINFOCOMTransactionForProccessing()
        {
            List<MomoApi.Transaction> ussdtxn = new List<MomoApi.Transaction>();
            try
            {
                DataBaseHandler dh = new DataBaseHandler();
                // dh.LogError("0", "ALL", "*272#", "1", "Getting txns to send");
                DataSet ds = ExecuteDataSetMomo("GetInfoComTransactions");
                DataTable dt = ds.Tables[0];

                foreach (DataRow dr in dt.Rows)
                {
                    MomoApi.Transaction txn = new MomoApi.Transaction();

                    Decimal amount = Decimal.Parse(dr["TranAmount"].ToString());
                    Double amount2 = Double.Parse(amount.ToString()) + 0.00;
                    Decimal result = Math.Truncate(Decimal.Parse(amount2.ToString()));
                    txn.CustomerName = dr["CustName"].ToString();
                    txn.VendorTranId = dr["VendorTranId"].ToString();
                    txn.SessionId = dr["RecordId"].ToString();
                    //txn.CustomerRef = dr["PaymentReference"].ToString();
                    txn.TranAmount = result.ToString();
                    txn.PaymentDate = dr["PaymentDate"].ToString();
                    //txn.VendorCode = dr["VendorCode"].ToString();
                    txn.VendorCode = "TESTFLEXIPAY";
                    txn.TranType = "PULL"; //dr["TranType"].ToString();
                    txn.TranCharge = "0";
                    txn.PaymentCode = "1";
                    txn.PaymentDate = DateTime.Parse(txn.PaymentDate).ToString("MM/dd/yyyy");
                    txn.FromTelecom = dr["FromNetwork"].ToString();
                    txn.ToTelecom = dr["ToNetwork"].ToString();
                    txn.Narration = dr["Narration"].ToString();
                    txn.Password = "17D14VD828";//
                    //txn.Password = GetVendorPassword(dtcred, txn.VendorCode);

                    txn.FromAccount = dr["Phone"].ToString();
                    txn.ToAccount = dr["Phone"].ToString();
                    txn.AddendumData = dr["VendorCode"].ToString();
                    string dataToSign =
                    txn.CustomerRef + txn.CustomerName + txn.FromTelecom + txn.ToTelecom +
                    txn.VendorTranId + txn.VendorCode + txn.Password + txn.PaymentDate + txn.TranType
                    + txn.PaymentCode + txn.TranAmount + txn.FromAccount + txn.ToAccount;
                    txn.DigitalSignature = GetDigitalSignature(dataToSign);

                    ussdtxn.Add(txn);
                }
            }
            catch (Exception ex)
            {
                ExecuteNonQuery("SystemErrorLogs_Insert",
                       "",
                       "",
                       "",
                       "GetPendingUssdTransactionForProccessing",
                       ex.Message,
                       ex.StackTrace
                       );
            }


            return ussdtxn;

        }


        public static string GetDigitalSignature(string Tosign)
        {
            string certificate = @"E:\PEGTESTPFX.pfx";
            //string certificate = @"C:\AirtelMoneyCerts\terrapayCert.pfx";   //Tingate710 test1234
            //       string certificate = @"C:\AirtelMoneyCerts\rea.pfx";
            X509Certificate2 cert = new X509Certificate2(certificate, "pegasus@2020");

            //X509Certificate2 cert = new X509Certificate2(certificate, "Pegasus@2020", X509KeyStorageFlags.UserKeySet);
            RSACryptoServiceProvider rsa = (RSACryptoServiceProvider)cert.PrivateKey;
            // Hash the data
            SHA1Managed sha1 = new SHA1Managed();
            ASCIIEncoding encoding = new ASCIIEncoding();
            byte[] data = encoding.GetBytes(Tosign);
            byte[] hash = sha1.ComputeHash(data);
            // Sign the hash
            byte[] digitalCert = rsa.SignHash(hash, CryptoConfig.MapNameToOID("SHA1"));
            string strDigCert = Convert.ToBase64String(digitalCert);
            return strDigCert;
        }

        private string GetVendorPassword(DataTable dt, string vendorcode)
        {
            string password = "";
            //there is a better implement of dt.select(vendorcode)
            foreach (DataRow dr in dt.Rows)
            {
                if (dr["Username"].ToString().Equals(vendorcode))
                {
                    password = DecryptString(dr["Password"].ToString());
                }
            }

            return password;
        }
        private string DecryptString(string Encrypted)
        {
            string ret = "";
            ret = Encryption.encrypt.DecryptString(Encrypted, "Umeme2501PegPay");
            return ret;
        }

        internal Hashtable GetNetworkCodes()
        {
            Hashtable networkCodes = new Hashtable();
            try
            {
                command = MMoneyDb.GetStoredProcCommand("GetNetworkCodes");
                DataSet ds = MMoneyDb.ExecuteDataSet(command);
                int recordCount = ds.Tables[0].Rows.Count;
                if (recordCount != 0)
                {
                    for (int i = 0; i < recordCount; i++)
                    {
                        DataRow dr = ds.Tables[0].Rows[i];
                        string network = dr["Network"].ToString();
                        string code = dr["Code"].ToString();
                        networkCodes.Add(code, network);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return networkCodes;
        }

        public void LogError(string vendorCode, string fromAccount, string toAccount, string vendorTranId, string errorMessage, string SatusCode)
        {
            try
            {
                command = MMoneyDb.GetStoredProcCommand("LogError", vendorCode, fromAccount, toAccount, vendorTranId, errorMessage, SatusCode);
                MMoneyDb.ExecuteNonQuery(command);
            }
            catch (Exception ex)
            {
            }
        }
        public void LogError(string MSISDN, string Network, string ShortCode, string SessionId, string Message)
        {
            try
            {
                command = PegasusUssddb.GetStoredProcCommand("LogUssdError", MSISDN, SessionId, Message, Network, ShortCode);
                PegasusUssddb.ExecuteNonQuery(command);
            }
            catch (Exception ex)
            {
                ArrayList a = new ArrayList();
                a.Add(MSISDN + "," + SessionId + "," + ShortCode + "," + Message + "," + Network);
                //DataFile df = new DataFile();
                //df.writeToFile(@"E:\USSDLogs\ErrorLog\" + SessionId + "-" + DateTime.Now.ToString("ddMMyyyyhhmmss") + ".txt", a);
            }
        }

        public string InsertReceivedTransactionPUSH2(MomoApi.Transaction trans, int tranTypeId, Charges charge)
        {
            string PegPayId = "";

            DataTable datatable = new DataTable();
            try
            {
                command = MMoneyDb.GetStoredProcCommand("InsertReceivedTransactionPUSH_QueueOthers_NoBalanceUpdates", new object[]{
                trans.FromAccount,
                trans.ToAccount,
                trans.CustomerRef,
                trans.VendorTranId,
                trans.TranAmount,
                charge.PegasusCommission,
                trans.FromTelecom.Trim(),
                trans.ToTelecom.Trim(),
                trans.PaymentDate,
                trans.PaymentCode,
                tranTypeId,
                trans.VendorCode,
                trans.CustomerName,
                "0",
                charge.TelecomCharge,
                charge.CashOutCharge, "0", "0",
                "20200101000000267",//charge.PegasusCommisionAccount,
                charge.TelecomCommissionAccount,
                charge.CashoutAccount,
                trans.Narration
            });

                datatable = MMoneyDb.ExecuteDataSet(command).Tables[0];
                if (datatable.Rows.Count > 0)
                {
                    PegPayId = datatable.Rows[0]["PegPayTranId"].ToString();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return PegPayId;
        }
        public string GetVendorPegPayAccount(string VendorCode)
        {
            DataTable datatable = new DataTable();
            string Account = "";
            try
            {
                datatable = GetVendorPegPayAcc(VendorCode);
                if (datatable.Rows.Count > 0)
                {
                    Account = datatable.Rows[0]["AccountNumber"].ToString();
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return Account;
        }
        internal DataTable GetVendorPegPayAcc(string VendorCode)
        {
            try
            {
                command = MMoneyDb.GetStoredProcCommand("GetPegPayAccountOnly", VendorCode);
                DataTable dt = MMoneyDb.ExecuteDataSet(command).Tables[0];
                return dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        internal void UpdateTransactionStatus(string sessionid, string status, string reason, string paymentId, string Pega_PayId)
        {
            try
            {
                object[] values = { sessionid, status, reason, paymentId, Pega_PayId };
                ExecuteNonQueryMomo("UpdateEquityTransactionStatus", values);
            }
            catch (Exception ex)
            {
                ExecuteNonQuery("SystemErrorLogs_Insert",
                    sessionid,
                    paymentId,
                    Pega_PayId,
                    "UpdateTransactionStatus",
                    ex.Message,
                    ex.StackTrace
                    );
            }
        }

        internal string GetCommissionAccount(string CustomerCode, string Type, string Network)
        {
            DataTable datatable = null;
            string Account = "";
            try
            {

                command = MMoneyDb.GetStoredProcCommand("GetCommissionAccount", CustomerCode, Type, Network);
                datatable = MMoneyDb.ExecuteDataSet(command).Tables[0];
                if (datatable.Rows.Count > 0)
                {
                    Account = datatable.Rows[0]["AccountNumber"].ToString();
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            return Account;
        }

        internal void UpdateEquityTransactionToProccessing(MomoApi.Transaction txn)
        {
            try
            {
                ExecuteNonQueryMomo("UpdateEquitytxnToProccessing", txn.SessionId);
            }
            catch (Exception ex)
            {
                ExecuteNonQuery("SystemErrorLogs_Insert",
                      txn.SessionId,
                      txn.VendorTranId,
                      txn.CustomerRef,
                      "UpdateTransactionToProccessing",
                      ex.Message,
                      ex.StackTrace
                      );
            }
        }

        internal void UpdateTransactionToProccessing(MomoApi.Transaction txn)
        {
            try
            {
                ExecuteNonQuery("UpdatetxnToProccessing", txn.SessionId);
            }
            catch (Exception ex)
            {
                ExecuteNonQuery("SystemErrorLogs_Insert",
                      txn.SessionId,
                      txn.VendorTranId,
                      txn.CustomerRef,
                      "UpdateTransactionToProccessing",
                      ex.Message,
                      ex.StackTrace
                      );
            }
        }


        internal bool SaveMOMOTransaction(MomoApi.Transaction txn, string status, string reason, string telecom)
        {
            bool saved = false;
            try
            {
                string[] arrt = { txn.FromAccount,
                   telecom,
                    txn.CustomerRef,
                    status,
                    txn.VendorCode,
                   reason,
                    txn.VendorCode,
                    txn.CustomerName, txn.TranAmount, txn.PaymentDate, txn.FromTelecom, txn.Narration, txn.VendorTranId};

                DataSet set = ExecuteDataSet("SaveMOMOTransaction", arrt);

                if (set.Tables[0].Rows.Count > 0)
                {
                    DataRow dr = set.Tables[0].Rows[0];
                    string reference = dr["VendorTranId"].ToString();
                    if (!String.IsNullOrEmpty(reference))
                    {
                        saved = true;
                    }
                }

            }
            catch (Exception ex)
            {

                string[] error = { txn.VendorTranId,
                    txn.CustomerRef,
                   txn.FromAccount,
                   "SaveMOMOTransaction",
                   ex.Message,
                   ex.StackTrace};

                ExecuteNonQuery("SystemErrorLogs_Insert", error);
                ////return false;
            }
            return saved;
        }

        internal void UpdateUSSDMomoTxnStatus(MomoApi.Transaction txn, string status, string telecom)
        {
            try
            {
                string[] arrt = {
                    txn.SessionId,
                    status,
                    telecom
                    };
                ExecuteNonQuery("UpdateUSSDMomoTxnStatus", arrt);

            }
            catch (Exception ex)
            {

                string[] error = { txn.VendorTranId,
                    txn.CustomerRef,
                   txn.FromAccount,
                   "UpdateUSSDMomoTxnStatus",
                   ex.Message,
                   ex.StackTrace};

                ExecuteNonQuery("SystemErrorLogs_Insert", error);
                ////return false;
            }

        }

    }
}
