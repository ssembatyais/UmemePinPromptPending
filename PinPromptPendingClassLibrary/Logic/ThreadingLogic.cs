using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UssdPaymentSender.EnitiyObjects;
using System.Threading;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Net.Security;
using System.Data;
using System.Messaging;

namespace UssdPaymentSender.Logic
{
    public class ThreadingLogic
    {
        DataBaseHandler dh = new DataBaseHandler();
        private string PinPromptPendingQueue = @".\private$\parkingPinPromptPending";
        private string PinPromptProcessingQueue = @".\private$\parkingPinPromptProcessing";
        private string BinItPinPromptPendingQueue = @".\private$\QBPinPromptPending";
        private string BinItPinPromptProcessingQueue = @".\private$\QBPinPromptProcessing";
        private string caaPinPromptPendingQueue = @".\private$\CAAPinPromptPendingQueue";
        private string caaPinPromptProcessingQueue = @".\private$\CAApinpromptprocessingqueue";
        private string GenericPendingQueue = @".\private$\GenericPendingQueue";
        private DataTable returnTable;
        public double CONLOG_CHARGE, KRECS_CHARGE, PACMECS_CHARGE, KIL_CHARGE;
        private DataTable dataTable;
        private System.Messaging.Message msg;
        private MessageQueue PinPending, PinProcessing, GenericPending;

        Dictionary<string, double> VendorsBalances = new Dictionary<string, double>();
        public void ProcessTransaction(string VendorCode)
        {
            try
            {
                // dh.LogError("0", "ALL", "*272#", "1", "Entering ProcessTransactionMethod");
                List<MomoApi.Transaction> trans = dh.GetPendingUssdTransactionForProccessing(VendorCode);
                //UssdTransaction[] txns = dh.GetPendingUssdTransactionForProccessing();
                if (trans.Count > 0)
                {
                    foreach (MomoApi.Transaction txn in trans)
                    {
                        dh.UpdateTransactionToProccessing(txn);
                        CreateWorkerThread(txn);
                        // Thread.Sleep(1000);
                    }
                }
                else
                { Console.WriteLine("Nothing to process"); }
                //dh.LogError("0", "ALL", "*272#", "1", "FinishedTransacting");
            }
            catch (Exception ex)
            {
                DataBaseHandler dh = new DataBaseHandler();
                dh.LogError("0", "ALL", "*272#", "1", ex.Message + "On ProcessTransactionMethod");
                // throw ex;
            }

        }


        public void ProcessRonSourcingTransactions()
        {
            try
            {
                MomoApi.Transaction momo = PickFromPendingQueue();
                CreateWorkerThread(momo);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public void ProcessCAATransactions()
        {
            try
            {
                MomoApi.Transaction momo = PickFromPendingQueueCAA();
                CreateWorkerThread(momo);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public void ProcessBinItTransactions()
        {
            try
            {
                MomoApi.Transaction momo = PickFromPendingQueue(BinItPinPromptPendingQueue, BinItPinPromptProcessingQueue);
                CreateWorkerThread(momo);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public MomoApi.Transaction PickFromPendingQueue()
        {
            UssdTransactions trans = new UssdTransactions();
            try
            {

                PinPending = new MessageQueue(PinPromptPendingQueue);
                PinProcessing = new MessageQueue(PinPromptProcessingQueue);
                msg = new System.Messaging.Message();
                PinPending.Formatter = new XmlMessageFormatter(new Type[] { typeof(UssdTransactions) });
                msg = PinPending.Receive();
                //msg = PinPending.Peek();
                trans = (UssdTransactions)msg.Body;

                MomoApi.Transaction txn = new MomoApi.Transaction();

                Decimal amount = Decimal.Parse(trans.TransAmount);
                Double amount2 = Double.Parse(amount.ToString()) + 0.00;
                Decimal result = Math.Truncate(Decimal.Parse(amount2.ToString()));
                txn.CustomerName = trans.Phone;
                txn.VendorTranId = trans.TransactionId;
                txn.SessionId = trans.TransactionId;
                txn.CustomerRef = txn.CustomerName;///trans.TransactionId;
                txn.TranAmount = result.ToString();
                txn.PaymentDate = (string)trans.PaymentDate;
                //txn.VendorCode = dr["VendorCode"].ToString();
                txn.VendorCode = "TESTFLEXIPAY";
                txn.TranType = "PULL";
                txn.TranCharge = "0";
                txn.PaymentCode = "1";
                txn.PaymentDate = DateTime.Parse(txn.PaymentDate).ToString("MM/dd/yyyy");
                txn.FromTelecom = trans.Network;
                txn.ToTelecom = trans.Network;
                txn.Narration = trans.Naration;
                txn.Telecom = trans.Network;
                txn.Password = "17D14VD828";//
                                            //txn.Password = GetVendorPassword(dtcred, txn.VendorCode);

                txn.FromAccount = trans.Phone;
                txn.ToAccount = trans.Phone;//dh.GetVendorPegPayAccount(trans.VendorCode);
                txn.AddendumData = trans.VendorCode;
                string dataToSign =
                txn.CustomerRef + txn.CustomerName + txn.FromTelecom + txn.ToTelecom +
                txn.VendorTranId + txn.VendorCode + txn.Password + txn.PaymentDate + txn.TranType
                + txn.PaymentCode + txn.TranAmount + txn.FromAccount + txn.ToAccount;
                txn.DigitalSignature = DataBaseHandler.GetDigitalSignature(dataToSign);

                txn.VendorCode = trans.VendorCode;
                //sending to processing queue

                dh.SaveMOMOTransaction(txn, "PENDING", txn.Narration, txn.Telecom);

                MessageQueue queue;
                if (MessageQueue.Exists(PinPromptProcessingQueue))
                {
                    queue = new MessageQueue(PinPromptProcessingQueue);
                }
                else
                {
                    queue = MessageQueue.Create(PinPromptProcessingQueue);
                }

                System.Messaging.Message Processing = new System.Messaging.Message();
                Processing.Body = trans;
                Processing.Label = trans.QueueID;
                Processing.Recoverable = true;
                string id = Processing.Id;
                PinProcessing.Send(Processing);

                ///VERY VERY BAD.... BUT IT'S TEST
                SendToGenericQueue(trans);

                return txn;
            }
            catch (Exception ert)
            {
                string[] error = { trans.Phone,
                  trans.QueueID,
                  trans.PaymentReference,
                  "Exception on checking  Queue",
                  ert.Message,
                  ert.StackTrace};
                //ExecuteNonQuery("SystemErrorLogs_Insert", error);
                throw ert;
            }
        }

        public MomoApi.Transaction PickFromPendingQueueCAA()
        {
            UssdTransactions trans = new UssdTransactions();
            try
            {

                PinPending = new MessageQueue(caaPinPromptPendingQueue);
                PinProcessing = new MessageQueue(caaPinPromptProcessingQueue);
                msg = new System.Messaging.Message();
                PinPending.Formatter = new XmlMessageFormatter(new Type[] { typeof(UssdTransactions) });
                msg = PinPending.Receive();
                //msg = PinPending.Peek();
                trans = (UssdTransactions)msg.Body;

                MomoApi.Transaction txn = new MomoApi.Transaction();

                Decimal amount = Decimal.Parse(trans.TransAmount);
                Double amount2 = Double.Parse(amount.ToString()) + 0.00;
                Decimal result = Math.Truncate(Decimal.Parse(amount2.ToString()));
                txn.CustomerName = trans.CustomerName;
                txn.VendorTranId = trans.TransactionId;
                txn.SessionId = trans.TransactionId;
                txn.CustomerRef = trans.TransactionId;
                txn.TranAmount = result.ToString();
                txn.PaymentDate = (string)trans.PaymentDate;
                //txn.VendorCode = dr["VendorCode"].ToString();
                txn.VendorCode = "TESTFLEXIPAY";
                txn.TranType = "PULL";
                txn.TranCharge = "0";
                txn.PaymentCode = "1";
                txn.PaymentDate = DateTime.Parse(txn.PaymentDate).ToString("MM/dd/yyyy");
                txn.FromTelecom = trans.Network;
                txn.ToTelecom = trans.Network;
                txn.Narration = trans.Naration;
                txn.Telecom = trans.Network;
                txn.Password = "17D14VD828";//
                                            //txn.Password = GetVendorPassword(dtcred, txn.VendorCode);

                txn.FromAccount = trans.Phone;
                txn.ToAccount = trans.Phone;//dh.GetVendorPegPayAccount(trans.VendorCode);
                txn.AddendumData = trans.VendorCode;
                string dataToSign =
                txn.CustomerRef + txn.CustomerName + txn.FromTelecom + txn.ToTelecom +
                txn.VendorTranId + txn.VendorCode + txn.Password + txn.PaymentDate + txn.TranType
                + txn.PaymentCode + txn.TranAmount + txn.FromAccount + txn.ToAccount;
                txn.DigitalSignature = DataBaseHandler.GetDigitalSignature(dataToSign);

                txn.VendorCode = trans.VendorCode;
                //sending to processing queue

                dh.SaveMOMOTransaction(txn, "PENDING", txn.Narration, txn.Telecom);

                MessageQueue queue;
                if (MessageQueue.Exists(caaPinPromptProcessingQueue))
                {
                    queue = new MessageQueue(caaPinPromptProcessingQueue);
                }
                else
                {
                    queue = MessageQueue.Create(caaPinPromptProcessingQueue);
                }

                System.Messaging.Message Processing = new System.Messaging.Message();
                Processing.Body = trans;
                Processing.Label = trans.QueueID;
                Processing.Recoverable = true;
                string id = Processing.Id;
                PinProcessing.Send(Processing);


                return txn;
            }
            catch (Exception ert)
            {
                string[] error = { trans.Phone,
                  trans.QueueID,
                  trans.PaymentReference,
                  "Exception on checking  Queue",
                  ert.Message,
                  ert.StackTrace};
                //ExecuteNonQuery("SystemErrorLogs_Insert", error);
                throw ert;
            }
        }
        public MomoApi.Transaction PickFromPendingQueue(string PendingQueue, string ProcessingQueue)
        {
            UssdTransactions trans = new UssdTransactions();
            try
            {
                PinPending = MessageQueue.Exists(PendingQueue) ? new MessageQueue(PendingQueue) : MessageQueue.Create(PendingQueue);
                PinProcessing = MessageQueue.Exists(ProcessingQueue) ? new MessageQueue(ProcessingQueue) : MessageQueue.Create(ProcessingQueue);

                msg = new System.Messaging.Message();
                PinPending.Formatter = new XmlMessageFormatter(new Type[] { typeof(UssdTransactions) });
                msg = PinPending.Receive();
                //msg = PinPending.Peek();

                trans = (UssdTransactions)msg.Body;

                //Creating Momo Object from Ussd_txn Object
                MomoApi.Transaction txn = new MomoApi.Transaction();

                Decimal amount = Decimal.Parse(trans.TransAmount);
                Double amount2 = Double.Parse(amount.ToString()) + 0.00;
                Decimal result = Math.Truncate(Decimal.Parse(amount2.ToString()));

                string vendorcode = "TESTFLEXIPAY";
                string password = "17D14VD828";

                txn.CustomerName = trans.CustomerName;
                txn.VendorTranId = trans.TransactionId;
                txn.SessionId = trans.TransactionId;
                txn.CustomerRef = trans.TransactionId;
                txn.TranAmount = result.ToString();
                txn.PaymentDate = (string)trans.PaymentDate;
                txn.VendorCode = vendorcode;//"TESTFLEXIPAY";
                txn.TranType = "PULL";
                txn.TranCharge = "0";
                txn.PaymentCode = "1";
                txn.PaymentDate = DateTime.Parse(txn.PaymentDate).ToString("MM/dd/yyyy");
                txn.FromTelecom = trans.Network;
                txn.ToTelecom = trans.Network;
                txn.Narration = trans.Naration;
                txn.Telecom = trans.Network;
                txn.Password = password;//"17D14VD828";//GetVendorPassword(dtcred, txn.VendorCode);
                txn.FromAccount = trans.Phone;
                txn.ToAccount = trans.Phone;
                //dh.GetVendorPegPayAccount(trans.VendorCode);
                txn.AddendumData = trans.VendorCode;

                string dataToSign = txn.CustomerRef + txn.CustomerName + txn.FromTelecom + txn.ToTelecom +txn.VendorTranId + txn.VendorCode + txn.Password + txn.PaymentDate + txn.TranType
                + txn.PaymentCode + txn.TranAmount + txn.FromAccount + txn.ToAccount;

                txn.DigitalSignature = DataBaseHandler.GetDigitalSignature(dataToSign);
                txn.VendorCode = trans.VendorCode;

                //saving to USSDReceivedTransaction
                dh.SaveMOMOTransaction(txn, "PENDING", txn.Narration, txn.Telecom);

                //sending to processing queue
                System.Messaging.Message Processing = new System.Messaging.Message();
                Processing.Body = trans;
                Processing.Label = trans.QueueID;
                Processing.Recoverable = true;
                string id = Processing.Id;
                PinProcessing.Send(Processing);

                return txn;
            }
            catch (Exception ert)
            {
                string[] error = { trans.Phone,
                  trans.QueueID,
                  trans.PaymentReference,
                  "Exception on checking  Queue",
                  ert.Message,
                  ert.StackTrace};
                //ExecuteNonQuery("SystemErrorLogs_Insert", error);
                throw ert;
            }
        }

        public void ProcessEquityTransaction(string VendorCode)
        {
            try
            {
                List<MomoApi.Transaction> trans = dh.GetPendingMomoEquityTransactionForProccessing(VendorCode);
                if (trans.Count > 0)
                {
                    foreach (MomoApi.Transaction txn in trans)
                    {
                        dh.UpdateEquityTransactionToProccessing(txn);
                        CreateEquityWorkerThread(txn);
                        // Thread.Sleep(1000);
                    }
                }
                else
                { Console.WriteLine("Nothing to process"); }
            }
            catch (Exception ex)
            {
                DataBaseHandler dh = new DataBaseHandler();
                dh.LogError("0", "ALL", "*272#", "1", ex.Message + "On ProcessTransactionMethod");
            }

        }
        public void ProcessOssnTransaction(string VendorCode)
        {
            try
            {
                List<MomoApi.Transaction> trans = dh.GetPendingMomoOssnTransactionForProccessing(VendorCode);
                if (trans.Count > 0)
                {
                    foreach (MomoApi.Transaction txn in trans)
                    {
                        //dh.UpdateEquityTransactionToProccessing(txn);
                        CreateOssnWorkerThread(txn);
                        // Thread.Sleep(1000);
                    }
                }
                else
                { Console.WriteLine("Nothing to process"); }
            }
            catch (Exception ex)
            {
                DataBaseHandler dh = new DataBaseHandler();
                dh.LogError("0", "ALL", "*272#", "1", ex.Message + "On ProcessTransactionMethod");
            }

        }

        public void ProcessInfocomTransaction()
        {
            try
            {
                List<MomoApi.Transaction> trans = dh.GetPendingMomoINFOCOMTransactionForProccessing();
                if (trans.Count > 0)
                {
                    foreach (MomoApi.Transaction txn in trans)
                    {
                        //dh.UpdateEquityTransactionToProccessing(txn);
                        CreateInfocomWorkerThread(txn);
                        // Thread.Sleep(1000);
                    }
                }
                else { Console.WriteLine("Nothing to process"); }
            }
            catch (Exception ex)
            {
                DataBaseHandler dh = new DataBaseHandler();
                dh.LogError("0", "ALL", "*272#", "1", ex.Message + "On ProcessTransactionMethod");
            }

        }

        private void CreateWorkerThread(object trans)
        {
            // Thread.Sleep(1000);
            //Thread workerThread = new Thread(new ParameterizedThreadStart(PullMoney));
            //workerThread.Start(trans);
            PullMoney(trans);
        }

        private void CreateEquityWorkerThread(object trans)
        {
            //Thread.Sleep(1000);
            //Thread workerThread1 = new Thread(new ParameterizedThreadStart(PullEquityMoney));
            //workerThread1.Start(trans);
            PullEquityMoney(trans);
        }
        private void CreateOssnWorkerThread(object trans)
        {
            //Thread.Sleep(1000);
            Thread workerThread1 = new Thread(new ParameterizedThreadStart(PullEquityMoney));
            workerThread1.Start(trans);
            //PullEquityMoney(trans);
        }

        private void CreateInfocomWorkerThread(object trans)
        {
            //Thread.Sleep(1000);
            //Thread workerThread1 = new Thread(new ParameterizedThreadStart(PullEquityMoney));
            //workerThread1.Start(trans);
            PullEquityMoney(trans);
        }

        public void GetTransactionStatus()
        {
            try
            {
                MomoApi.TranDetailResponse resp = new MomoApi.TranDetailResponse();
                List<MomoApi.Transaction> trans = dh.GetPendingsAndFailed();

                if (trans.Count > 0)
                {
                    foreach (MomoApi.Transaction txn in trans)
                    {
                        System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls | System.Net.SecurityProtocolType.Tls12;
                        //ServicePointManager.ServerCertificateValidationCallback = RemoteCertificateValidation;
                        MomoApi.PegPayTelecomsApi pegpay = new MomoApi.PegPayTelecomsApi();
                        //pegpay.Timeout = 6 * 1000 * 60;
                        pegpay.Url = "https://pegasus.co.ug:8002/LivePegPayTelecomsApi/PegPayTelecomsApi.asmx?WSDL";
                        resp = pegpay.GetTransactionDetails(txn.VendorCode, txn.Password, txn.VendorTranId);
                        txn.SessionId = txn.VendorTranId;
                        if (resp.StatusCode.Equals("0"))
                        {
                            dh.UpdateTransactionStatus(txn.SessionId, "SUCCESS", resp.StatusDescription, resp.TelecomID, resp.PegpayId);
                            if (txn.VendorCode == "BINIT_PULLS")
                            {
                                dh.UpdateUSSDMomoTxnStatus(txn, "FAILED", resp.TelecomID);
                            }
                        }
                        else if (resp.StatusDescription.Equals("PENDING"))
                        {
                            dh.UpdateTransactionStatus(txn.SessionId, "PENDING", resp.StatusDescription, resp.TelecomID, resp.PegpayId);
                        }
                        else
                        {
                            dh.UpdateTransactionStatus(txn.SessionId, "FAILED", resp.StatusDescription, resp.TelecomID, resp.PegpayId);
                            if (txn.VendorCode == "BINIT_PULLS")
                            {
                                dh.UpdateUSSDMomoTxnStatus(txn, "FAILED", resp.TelecomID);
                            }
                        }
                    }
                }
            }
            catch (Exception ee)
            {

                dh.LogError("0", "ALL", "*272#", "1", ee.Message + " Exception on Going to MomoAPI to Get Status");
            }
        }

        public void PullMoney(Object obj)
        {
            MomoApi.Transaction momoTrans = (MomoApi.Transaction)obj;

            string vendorcode = momoTrans.VendorCode;
            try
            {

                MomoApi.Response resp = new MomoApi.Response();
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls | System.Net.SecurityProtocolType.Tls;
                ServicePointManager.ServerCertificateValidationCallback = RemoteCertificateValidation;
                MomoApi.PegPayTelecomsApi pegpay = new MomoApi.PegPayTelecomsApi();

                pegpay.Url = "https://pegasus.co.ug:8002/LivePegPayTelecomsApi/PegPayTelecomsApi.asmx?WSDL";


                Console.WriteLine("Going to Momo");
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                Console.WriteLine("Transaction inserted into Test Mobile Money");

                momoTrans.VendorCode = "TESTFLEXIPAY";
                momoTrans.Password = "17D14VD828";


                //momoTrans.VendorCode = "KRECS";
                //momoTrans.Password = "82X03GA364";
                //if (!string.IsNullOrEmpty(momoTrans.Telecom)) {
                //    momoTrans.FromAccount = momoTrans.Telecom;
                //}
                resp = pegpay.PostTransaction(momoTrans);

                momoTrans.VendorCode = vendorcode;

                Response testMomoResponse = LogTransactionInDB(momoTrans);
                Console.WriteLine(resp.StatusDescription + " " + momoTrans.FromAccount);
                if (resp.StatusCode.Equals("0") || resp.StatusCode.Equals("21"))
                {
                    if (resp.StatusCode.Equals("0"))
                    {
                        dh.UpdateTransactionStatus(momoTrans.SessionId, "SUCCESS", resp.StatusDescription, resp.TelecomId, resp.PegPayId);
                        //update transaction details in TestMoMo with a
                        object[] data = { resp.TelecomId, testMomoResponse.PegPayId };
                        dh.UpdateTransactionInTestMomo("UpdateTranscactioninTestMomo", data);
                        
                        if(momoTrans.VendorCode == "BINIT_PULLS")
                        {
                            dh.UpdateUSSDMomoTxnStatus(momoTrans, "SUCCESS", resp.TelecomId);
                        }

                    }
                    else if (resp.StatusCode.Equals("21"))
                    {
                        MomoApi.TranDetailResponse response = new MomoApi.TranDetailResponse();
                        //MomoApi.PegPayTelecomsApi pegpay = new MomoApi.PegPayTelecomsApi();
                        pegpay.Url = "https://pegasus.co.ug:8002/LivePegPayTelecomsApi/PegPayTelecomsApi.asmx?WSDL";
                        try
                        {
                            response = pegpay.GetTransactionDetails(momoTrans.VendorCode, momoTrans.Password, momoTrans.VendorTranId);
                            if (response.StatusCode.Equals("0"))
                            {
                                dh.UpdateTransactionStatus(momoTrans.SessionId, "SUCCESS", response.StatusDescription, response.TelecomID, response.PegpayId);
                                if (momoTrans.VendorCode == "BINIT_PULLS")
                                {
                                    dh.UpdateUSSDMomoTxnStatus(momoTrans, "SUCCESS", resp.TelecomId);
                                }
                            }
                            else if (response.StatusDescription.Equals("PENDING"))
                            {
                                dh.UpdateTransactionStatus(momoTrans.SessionId, "PENDING", response.StatusDescription, response.TelecomID, response.PegpayId);
                            }
                            else
                            {
                                dh.UpdateTransactionStatus(momoTrans.SessionId, "FAILED", response.StatusDescription, response.TelecomID, response.PegpayId);
                                if (momoTrans.VendorCode == "BINIT_PULLS")
                                {
                                    dh.UpdateUSSDMomoTxnStatus(momoTrans, "FAILED", resp.TelecomId);
                                }
                            }
                            object[] data = { resp.StatusDescription, testMomoResponse.PegPayId };
                            dh.UpdateTransactionInTestMomo("UpdateTranscactioninTestMomo", data);
                            //dh.LogError(momoTrans.FromAccount, momoTrans.Telecom, momoTrans.VendorCode, momoTrans.VendorTranId, ex1.Message);
                        }
                        catch (Exception ex)
                        {
                            dh.UpdateTransactionStatus(momoTrans.SessionId, "PENDING", response.StatusDescription, response.TelecomID, response.PegpayId);
                            dh.LogError(momoTrans.FromAccount, momoTrans.Telecom, momoTrans.VendorCode, momoTrans.VendorTranId, ex.Message);

                        }
                    }
                }

                else
                {
                    dh.UpdateTransactionStatus(momoTrans.SessionId, "FAILED", resp.StatusDescription, resp.TelecomId, resp.PegPayId);
                }
            }
            catch (WebException ex1)
            {
                dh.LogError("0", "ALL", "*272#", "1", "Exception on Going to MomoAPI to Pull " + ex1.Message);


                MomoApi.TranDetailResponse resp = new MomoApi.TranDetailResponse();
                MomoApi.PegPayTelecomsApi pegpay = new MomoApi.PegPayTelecomsApi();
                pegpay.Url = "https://pegasus.co.ug:8002/LivePegPayTelecomsApi/PegPayTelecomsApi.asmx?WSDL";
                try
                {
                    resp = pegpay.GetTransactionDetails(momoTrans.VendorCode, momoTrans.Password, momoTrans.VendorTranId);


                    momoTrans.VendorCode = vendorcode;
                    Response testMomoResponse = LogTransactionInDB(momoTrans);

                    if (resp.StatusCode.Equals("0"))
                    {
                        dh.UpdateTransactionStatus(momoTrans.SessionId, "SUCCESS", resp.StatusDescription, resp.TelecomID, resp.PegpayId);
                        if (momoTrans.VendorCode == "BINIT_PULLS")
                        {
                            dh.UpdateUSSDMomoTxnStatus(momoTrans, "SUCCESS", resp.TelecomID);
                        }
                    }
                    else if (resp.StatusDescription.Equals("PENDING"))
                    {
                        dh.UpdateTransactionStatus(momoTrans.SessionId, "PENDING", resp.StatusDescription, resp.TelecomID, resp.PegpayId);
                    }
                    else
                    {
                        dh.UpdateTransactionStatus(momoTrans.SessionId, "FAILED", resp.StatusDescription, resp.TelecomID, resp.PegpayId);
                        if (momoTrans.VendorCode == "BINIT_PULLS")
                        {
                            dh.UpdateUSSDMomoTxnStatus(momoTrans, "FAILED", resp.TelecomID);
                        }
                    }
                }
                catch (Exception e)
                {
                    dh.UpdateTransactionStatus(momoTrans.SessionId, "PENDING", resp.StatusDescription, resp.TelecomID, resp.PegpayId);
                    dh.LogError(momoTrans.FromAccount, momoTrans.Telecom, momoTrans.VendorCode, momoTrans.VendorTranId, e.Message);
                }
                //workerThread.Abort();
                //Thread.
            }
        }

        public void PullEquityMoney(Object obj)
        {
            MomoApi.Transaction momoTrans = (MomoApi.Transaction)obj;
            try
            {
                MomoApi.Response resp = new MomoApi.Response();
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls | System.Net.SecurityProtocolType.Tls;
                ServicePointManager.ServerCertificateValidationCallback = RemoteCertificateValidation;
                MomoApi.PegPayTelecomsApi pegpay = new MomoApi.PegPayTelecomsApi();

                pegpay.Url = "https://pegasus.co.ug:8002/LivePegPayTelecomsApi/PegPayTelecomsApi.asmx?WSDL";




                Console.WriteLine("Going to Momo");
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                Console.WriteLine("Transaction inserted into Test Mobile Money");


                momoTrans.VendorCode = "TESTFLEXIPAY";
                momoTrans.Password = "17D14VD828";


                //momoTrans.VendorCode = "KRECS";
                //momoTrans.Password = "82X03GA364";
                if (!string.IsNullOrEmpty(momoTrans.Telecom))
                {
                    momoTrans.FromAccount = momoTrans.Telecom;
                }
                resp = pegpay.PostTransaction(momoTrans);
                Response testMomoResponse = LogTransactionInDB(momoTrans);
                Console.WriteLine(resp.StatusDescription + " " + momoTrans.FromAccount);
                if (resp.StatusCode.Equals("0") || resp.StatusCode.Equals("21"))
                {
                    if (resp.StatusCode.Equals("0"))
                    {
                        dh.UpdateTransactionStatus(momoTrans.SessionId, "SUCCESS", resp.StatusDescription, resp.TelecomId, resp.PegPayId);
                        //update transaction details in TestMoMo with a
                        //object[] data = { resp.TelecomId, testMomoResponse.PegPayId };
                        //dh.UpdateTransactionInTestMomo("UpdateTranscactioninTestMomo", data);
                    }
                    else if (resp.StatusCode.Equals("21"))
                    {
                        MomoApi.TranDetailResponse response = new MomoApi.TranDetailResponse();
                        //MomoApi.PegPayTelecomsApi pegpay = new MomoApi.PegPayTelecomsApi();
                        pegpay.Url = "https://pegasus.co.ug:8002/LivePegPayTelecomsApi/PegPayTelecomsApi.asmx?WSDL";
                        try
                        {
                            response = pegpay.GetTransactionDetails(momoTrans.VendorCode, momoTrans.Password, momoTrans.VendorTranId);
                            if (response.StatusCode.Equals("0"))
                            {
                                dh.UpdateTransactionStatus(momoTrans.SessionId, "SUCCESS", response.StatusDescription, response.TelecomID, response.PegpayId);
                            }
                            else if (response.StatusDescription.Equals("PENDING"))
                            {
                                dh.UpdateTransactionStatus(momoTrans.SessionId, "PENDING", response.StatusDescription, response.TelecomID, response.PegpayId);
                            }
                            else
                            {
                                dh.UpdateTransactionStatus(momoTrans.SessionId, "FAILED", response.StatusDescription, response.TelecomID, response.PegpayId);
                            }
                            //object[] data = { resp.StatusDescription, testMomoResponse.PegPayId };
                            //dh.UpdateTransactionInTestMomo("UpdateTranscactioninTestMomo", data);
                            //dh.LogError(momoTrans.FromAccount, momoTrans.Telecom, momoTrans.VendorCode, momoTrans.VendorTranId, ex1.Message);
                        }
                        catch (Exception ex)
                        {
                            dh.UpdateTransactionStatus(momoTrans.SessionId, "PENDING", response.StatusDescription, response.TelecomID, response.PegpayId);
                            dh.LogError(momoTrans.FromAccount, momoTrans.Telecom, momoTrans.VendorCode, momoTrans.VendorTranId, ex.Message);

                        }
                    }
                }

                else
                {
                    dh.UpdateTransactionStatus(momoTrans.SessionId, "FAILED", resp.StatusDescription, resp.TelecomId, resp.PegPayId);
                }
            }
            catch (WebException ex1)
            {
                dh.LogError("0", "ALL", "*272#", "1", "Exception on Going to MomoAPI to Pull " + ex1.Message);
                MomoApi.TranDetailResponse resp = new MomoApi.TranDetailResponse();
                MomoApi.PegPayTelecomsApi pegpay = new MomoApi.PegPayTelecomsApi();
                pegpay.Url = "https://pegasus.co.ug:8002/LivePegPayTelecomsApi/PegPayTelecomsApi.asmx?WSDL";
                try
                {
                    resp = pegpay.GetTransactionDetails(momoTrans.VendorCode, momoTrans.Password, momoTrans.VendorTranId);
                    if (resp.StatusCode.Equals("0"))
                    {
                        dh.UpdateTransactionStatus(momoTrans.SessionId, "SUCCESS", resp.StatusDescription, resp.TelecomID, resp.PegpayId);
                    }
                    else if (resp.StatusDescription.Equals("PENDING"))
                    {
                        dh.UpdateTransactionStatus(momoTrans.SessionId, "PENDING", resp.StatusDescription, resp.TelecomID, resp.PegpayId);
                    }
                    else
                    {
                        dh.UpdateTransactionStatus(momoTrans.SessionId, "FAILED", resp.StatusDescription, resp.TelecomID, resp.PegpayId);
                    }
                }
                catch (Exception e)
                {
                    dh.UpdateTransactionStatus(momoTrans.SessionId, "PENDING", resp.StatusDescription, resp.TelecomID, resp.PegpayId);
                    dh.LogError(momoTrans.FromAccount, momoTrans.Telecom, momoTrans.VendorCode, momoTrans.VendorTranId, e.Message);
                }
                //workerThread.Abort();
                //Thread.
            }
        }
        public Response LogTransactionInDB(MomoApi.Transaction trans)
        {
            Response resp = new Response();
            DataBaseHandler dh = new DataBaseHandler();
            string PegPayID = "";
            try
            {
                if (trans.AddendumData.Equals("KRECS"))
                {
                    trans.VendorCode = "KRECS";
                }
                if (trans.AddendumData.Equals("SAGEWOOD"))
                {
                    trans.VendorCode = "SAGEWOOD";
                }
                string ToAccount = "";
                string Phone = GetPhone(trans);
                PhoneValidator pv = new PhoneValidator();
                if (pv.PhoneNumbersOk(Phone))
                {
                    //we format only telecom numbers, bank accounts skipped
                    Phone = formatPhone(Phone);
                }

                trans.Telecom = Phone;
                if (trans.VendorCode.ToUpper() == "EZEEMONEY")
                {
                    ToAccount = trans.ToAccount;
                }
                else
                {
                    ToAccount = dh.GetTelecomAccount(trans.VendorCode, trans.ToTelecom, trans.TranType);
                }
                trans.ToAccount = ToAccount;

                string FromAccount = dh.GetVendorPegPayAccount(trans.VendorCode);
                trans.FromAccount = FromAccount;


                Charges charge = GetCharges(trans);
                List<string> umbrellas = new List<string>(new string[] { "NORTHERN", "CENTRAL", "MIDWESTERN", "SOUTHWESTERN", "KARAMOJA", "EASTERN" });

                if (umbrellas.Contains(trans.VendorCode))
                {
                    double tranamount = Double.Parse(trans.TranAmount) - (charge.PegasusCommission + charge.CashOutCharge);
                    int transamount = (int)tranamount;
                    trans.TranAmount = transamount.ToString();
                }
                if (trans.VendorCode.Equals("KRECS"))
                {
                    double tranamount = Double.Parse(trans.TranAmount);
                    int transamount = (int)tranamount;
                    trans.TranAmount = transamount.ToString();

                }
                DateTime startTimeBalCheck = DateTime.Now;
                Console.WriteLine("Balance Check Start Time:" + startTimeBalCheck);
                //check vendors balance
                //if (CheckBalanceIsOk(trans, charge))
                if (CheckBalanceIsOkSmart(trans, charge))
                {
                    DateTime endTimeBalCheck = DateTime.Now;
                    Console.WriteLine("Balance Check End Time:" + endTimeBalCheck);
                    Console.WriteLine("Difference in Time:" + endTimeBalCheck.Subtract(startTimeBalCheck).TotalSeconds);

                    DateTime startTimeInsert = DateTime.Now;
                    Console.WriteLine("Insert Start Time:" + startTimeBalCheck);
                    //sufficient funds
                    int tranTypeID = GetTranTypeID(trans.TranType);

                    if (trans.VendorCode.ToUpper() == "KARAMOJA" || trans.VendorCode.ToUpper() == "EASTERN" || trans.VendorCode.ToUpper() == "CENTRAL" || trans.VendorCode.ToUpper() == "NORTHERN"
                       || trans.VendorCode.ToUpper() == "SOUTHWESTERN" || trans.VendorCode.ToUpper() == "MIDWESTERN" || trans.VendorCode.ToUpper() == "KRECS")
                    {
                        PegPayID = dh.InsertReceivedTransactionPUSH4(trans, tranTypeID, charge);
                    }
                    else if (trans.VendorCode.Contains("SAGEWOOD"))
                    {
                        PegPayID = dh.InsertReceivedTransactionPULLException(trans, tranTypeID, charge);
                    }
                    else if (trans.VendorCode.Contains("EQUITY_BANK"))
                    {
                        //PegPayID = dh.InsertReceivedTransactionPULLException(trans, tranTypeID, charge);
                    }
                    else
                    {
                        charge.TelecomCommissionAccount = trans.FromTelecom == "MTN" ? "2013676709333" : "2013676709444";
                        charge.CashoutAccount = trans.FromTelecom == "MTN" ? "2013676709222" : "2013676709666";
                        PegPayID = dh.InsertReceivedTransactionPUSH2(trans, tranTypeID, charge);
                    }
                    //}
                    DateTime endTimeInsert = DateTime.Now;
                    Console.WriteLine("Insert End Time:" + endTimeInsert);
                    Console.WriteLine("Difference in Time:" + endTimeInsert.Subtract(startTimeInsert).TotalSeconds);
                    resp.PegPayId = PegPayID;
                    resp.StatusCode = "0";
                    resp.StatusDescription = "SUCCESS";
                    resp.TelecomId = "";
                }
                else
                {

                    ////return insufficient funds
                    //resp.StatusCode = "20";
                    //resp.StatusDescription = dh.GetStatusDescription(resp.StatusCode);
                    //string Reason = resp.StatusDescription;
                    //dh.InsertIntoDeleted(trans, Reason);
                    ////at this point, remove from VendorTranLog
                    //dh.RemoveFromVendorTransactionLog(trans);
                    //resp.TelecomId = "";
                }
            }
            catch (Exception e)
            {

                //log Error
                dh.LogError(trans.VendorCode, trans.FromAccount, trans.ToAccount,
                            trans.VendorTranId, "LiveMobileMoneyQueueProcessor.LogTransactionInDB:" + e.Message + " on Insertig into DB", "0");

                //return success
                resp.StatusCode = "100";
                resp.StatusDescription = e.Message;
                resp.TelecomId = "";
                resp.PegPayId = "";
            }

            return resp;
        }
        internal Charges GetCharges(MomoApi.Transaction trans)
        {
            try
            {
                DataTable dt = new DataTable();
                Charges charge = new Charges();
                //int tranTypeId = int.Parse(dt.Rows[0]["TypeId"].ToString());
                if (trans.TranType.Equals("PULL"))
                {
                    List<string> umbrellas = new List<string>(new string[] { "NORTHERN", "CENTRAL", "MIDWESTERN", "SOUTHWESTERN", "KARAMOJA", "EASTERN" });

                    if (umbrellas.Contains(trans.VendorCode))
                    {
                        charge.PegasusCommisionAccount = dh.GetCommissionAccount("MOWE_COMMISSION", "PEGASUS", trans.FromTelecom);
                        // charge.PegasusCommission = GetPegasusCharge(trans.VendorCode, Convert.ToDouble(trans.TranAmount.Trim()));
                        charge.TelecomCommissionAccount = "";
                        charge.CashoutAccount = dh.GetCommissionAccount("MOWE-SMS-COMMISSION", "PEGASUS", trans.FromTelecom);//"";
                        charge.TelecomCharge = 0;
                        double pegasusCharge = 0.025 * Convert.ToDouble(trans.TranAmount);
                        charge.PegasusCommission = pegasusCharge;
                        //double tranamount = Double.Parse(trans.TranAmount) - pegasusCharge;
                        //trans.TranAmount = tranamount.ToString();
                        if (string.IsNullOrEmpty(trans.Narration))
                        {
                            // charge.CashOutCharge = 0; to be uncommented
                            charge.CashOutCharge = 50;

                        }
                        else
                        {
                            charge.CashOutCharge = 50;

                        }
                    }
                    else if (trans.VendorCode.ToUpper() == "KRECS")
                    {
                        //deduct the flat fee of 1080 plus the sms charge
                        charge.PegasusCommisionAccount = dh.GetCommissionAccount("KRECS_COMMISSION", "PEGASUS", trans.FromTelecom);

                        charge.TelecomCharge = 0;
                        double pegasusCharge = 1080;
                        charge.PegasusCommission = pegasusCharge;
                        if (string.IsNullOrEmpty(trans.Narration))
                        {
                            //charge.CashOutCharge = 50;

                        }
                        else
                        {
                            //charge.CashOutCharge = 50;

                        }

                    }
                    else if (trans.VendorCode.ToUpper() == "SAGEWOOD")
                    {
                        //deduct the flat fee of 1080 plus the sms charge
                        charge.PegasusCommisionAccount = dh.GetCommissionAccount("SAGEWOOD-COMMISSION", "PEGASUS", trans.FromTelecom);
                        charge.TelecomCommissionAccount = "";//
                        charge.CashoutAccount = dh.GetCommissionAccount("SAGEWOOD-SMS-COMMISSION", "PEGASUS", trans.FromTelecom);//"";
                        charge.VATAccount = dh.GetCommissionAccount("VALUE ADDED TAX", "COMM", trans.FromTelecom);
                        charge.TelecomCharge = 0;
                        double pegasusCharge = (800 + 280);
                        charge.PegasusCommission = pegasusCharge;

                        if (string.IsNullOrEmpty(trans.Narration))
                        {
                            // charge.CashOutCharge = 0; to be uncommented
                            charge.CashOutCharge = 40;

                        }
                        else
                        {
                            charge.CashOutCharge = 40;

                        }
                        charge.VATCharge = 0.18 * pegasusCharge;
                        double tranamount = Double.Parse(trans.TranAmount);
                        trans.TranAmount = tranamount.ToString();

                    }
                    else
                    {
                        charge.PegasusCommisionAccount = "";
                        charge.TelecomCommissionAccount = "";
                        charge.CashoutAccount = "";// dh.GetCommissionAccount(trans.FromTelecom, "CASHOUT", trans.FromTelecom);
                        charge.PegasusCommission = Double.Parse(dh.GetPegasusCharge(trans.VendorCode).Rows[0]["PegasusCharge"].ToString());
                        charge.TelecomCharge = 0;
                        charge.CashOutCharge = 0;
                    }
                }
                else
                {
                    charge.PegasusCommisionAccount = "";
                    charge.TelecomCommissionAccount = "";
                    charge.CashoutAccount = "";// dh.GetCommissionAccount(trans.FromTelecom, "CASHOUT", trans.FromTelecom);
                    charge.PegasusCommission = Double.Parse(dh.GetPegasusCharge(trans.VendorCode).Rows[0]["PegasusCharge"].ToString());
                    charge.TelecomCharge = 0;
                    charge.CashOutCharge = 0;
                }
                return charge;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        internal double GetPegasusCharge(string VendorCode, double Amount)
        {
            try
            {
                double charge = 0;
                DataTable dataTable = dh.GetPegasusCharge(VendorCode);
                if (dataTable.Rows.Count > 0)
                {
                    string ChargeType = dataTable.Rows[0]["ChargeType"].ToString();
                    if (ChargeType.Equals("1"))
                    {
                        charge = Convert.ToDouble(dataTable.Rows[0]["PegasusCharge"].ToString());
                    }
                    else if (ChargeType.Equals("2"))
                    {
                        charge = (Convert.ToDouble(dataTable.Rows[0]["PegasusCharge"].ToString()) / 100) * Amount;
                    }
                    else if (ChargeType.Equals("3"))
                    {
                        //get teir charges;
                        charge = Convert.ToDouble(dataTable.Rows[0]["PegasusCharge"].ToString());
                    }
                    else
                    {
                        charge = Convert.ToDouble(dataTable.Rows[0]["PegasusCharge"].ToString());
                    }

                }
                else
                {
                    charge = 0;
                }
                return charge;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private int GetTranTypeID(string tranType)
        {
            switch (tranType)
            {
                case "PUSH":
                    return 2;
                case "PULL":
                    return 1;
                case "AIRTIME":
                    return 3;
                case "BILLPAY":
                    return 5;
                case "REVERSAL":
                    return 4;
                default:
                    return -1;
            }
        }
        public string formatPhone(string phoneString)
        {
            string output = "";
            int len = phoneString.Length;
            if (len.Equals(10))
            {
                // 0772020124
                string Sub = phoneString.Substring(1, 9);
                output = "256" + Sub;
            }
            else if (len.Equals(9))
            {
                output = "256" + phoneString;
            }
            else
            {
                output = phoneString;
            }
            return output;
        }

        internal string GetPhone(MomoApi.Transaction trans)
        {
            try
            {
                trans.Telecom = trans.ToAccount;
                string phone = "";
                if (!string.IsNullOrEmpty(trans.TranType))
                {
                    if (trans.TranType.Trim().Equals("PUSH") || trans.TranType.Trim().Equals("AIRTIME"))
                    {
                        phone = trans.Telecom;
                    }
                    else if (trans.TranType.Trim().Equals("PULL") || trans.TranType.Trim().Equals("BILLPAY"))
                    {
                        phone = trans.Telecom;
                    }
                }

                return phone;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        internal bool CheckBalanceIsOkSmart(MomoApi.Transaction trans, Charges charge)
        {
            try
            {

                Console.WriteLine("CheckBalance Start" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff"));

                //its not a push(payout/debit) request..we stop here
                if (!trans.TranType.Equals("PUSH") && !trans.TranType.Equals("AIRTIME"))
                {
                    return true;
                }

                //should we check for balances for this vendor
                DataTable dataTable = dh.CheckForMerchantAccount(trans.VendorCode, trans.FromTelecom);

                //merchant has his own account
                //we should not check balance
                if (dataTable.Rows.Count > 0)
                {
                    return true;
                }

                //set variables
                double balance = 0;
                double total = 0;
                total = Convert.ToDouble(trans.TranAmount) + charge.CashOutCharge + charge.PegasusCommission + charge.TelecomCharge;

                //do we already have the balances for this vendors
                if (VendorsBalances.ContainsKey(trans.VendorCode))
                {

                    balance = VendorsBalances[trans.VendorCode];

                    //is the balance enough to cover the transaction
                    if (total < balance)
                    {
                        //debit the balance
                        balance = balance - total;

                        //update the balance in memory as well
                        VendorsBalances[trans.VendorCode] = balance;

                        if (trans.VendorCode.ToUpper() == "EZEEMONEY")
                        {
                            dh.UpdateBalanceOkayFlag(trans.VendorCode, "OK");
                        }
                        return true;
                    }
                }

                //no balance found or its too low
                dataTable = dh.GetPegasusAccountBalance(trans.VendorCode);

                //no balance found in db
                if (dataTable.Rows.Count <= 0)
                {
                    if (trans.VendorCode.ToUpper() == "EZEEMONEY")
                    {
                        dh.UpdateBalanceOkayFlag(trans.VendorCode, "NOT_OK");
                    }
                    return false;
                }

                //ok...balance found
                balance = Convert.ToDouble(dataTable.Rows[0]["AccountBalance"].ToString().Trim());

                //is it enough
                if (total < balance)
                {
                    //debit the balance
                    balance = balance - total;

                    //save updated balance in memory
                    if (!VendorsBalances.ContainsKey(trans.VendorCode))
                        VendorsBalances.Add(trans.VendorCode, balance);
                    else
                        VendorsBalances[trans.VendorCode] = balance;

                    if (trans.VendorCode.ToUpper() == "EZEEMONEY")
                    {
                        dh.UpdateBalanceOkayFlag(trans.VendorCode, "OK");
                    }
                    //done
                    return true;
                }

                //save updated balance in memory
                if (!VendorsBalances.ContainsKey(trans.VendorCode))
                    VendorsBalances.Add(trans.VendorCode, balance);
                else
                    VendorsBalances[trans.VendorCode] = balance;

                if (trans.VendorCode.ToUpper() == "EZEEMONEY")
                {
                    dh.UpdateBalanceOkayFlag(trans.VendorCode, "NOT_OK");
                }
                //done
                return false;

            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Console.WriteLine("CheckBalance End" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff"));
            }

        }


        public bool SendToGenericQueue(UssdTransactions txn) {
            bool logged = false;
            try {
                // MessageQueue queue;
                if (MessageQueue.Exists(GenericPendingQueue)) {
                    GenericPending = new MessageQueue(GenericPendingQueue);
                } else {
                    GenericPending = MessageQueue.Create(GenericPendingQueue);
                }
                System.Messaging.Message msg = new System.Messaging.Message();
                msg.Body = txn;
                msg.Label = txn.QueueID;
                msg.Recoverable = true;
                GenericPending.Send(msg);
                logged = true;
            } catch (Exception ert) {
                throw ert;
            }
            return logged;
        }

        private static bool RemoteCertificateValidation(Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}
