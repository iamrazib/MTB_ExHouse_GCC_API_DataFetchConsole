﻿using GCCDataAutoFetchConsole.GCCServiceClient;
using GCCDataAutoFetchConsole.Global;
using GCCDataAutoFetchConsole.MTBCoreMiddleware;
using GCCDataAutoFetchConsole.MTBRemittanceService;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace GCCDataAutoFetchConsole
{
    /*
     * Author: Sk. Razibul Islam (Dt: 16-Sep-2021)
     * 11-Jan-2022 :  Live API added
     * 11-Jan-2022 :  Balance check added
     * 20-Jan-2022 : Live Service URL configured
     * 05-Feb-2022 : EFT Balance Check added
     */

    class Program
    {
        static IGCCService gccclient = new GCCServiceClient.GCCServiceClient();

        //API UAT
        //static RemitServiceSoapClient remitServiceClient = new RemitServiceSoapClient();

        //API LIVE
        static RemitServiceSoapClient remitServiceClient = new RemitServiceSoapClient();


        static Manager mg = new Manager();
        static string passwd = "";
        static string downloadBranch = Definitions.Values.downloadBranch;
        static string downloadUser = Definitions.Values.downloadUser;
        static string userId = Definitions.Values.GCCUserId;
        //static int partyId = Definitions.Values.GCCPartyID;
        static bool IS_INSERT_TO_LOG_TABLE = true;

        //----- for bKash
        static string CallingReferanceNO = "";
        static string ConversationID = "";
        //static string respCode = "";
        static float BKASH_TXN_LIMIT = 122500;
        static int statusCheckCount = 3;
        //----------



        static void Main(string[] args)
        {
            //ProcessReceivedTxn();

            //special case
            //UpdateBEFTNStatusToGCCAlreadySent();


            while (true)
            {
                //----- DOWNLOAD
                Console.WriteLine("Starting DownloadAccountTxn... -->" + DateTime.Now);

                DownloadAccountTxn();

                if (IS_INSERT_TO_LOG_TABLE)
                { mg.InsertAutoFetchLog(userId, "DownloadAccountTxn", "End DownloadAccountTxn..."); }
                Console.WriteLine("End DownloadAccountTxn... -->" + DateTime.Now);
                Thread.Sleep(5000); //wait 5 sec

                ProcessOwnAccountCreditTxn();

                if (IS_INSERT_TO_LOG_TABLE)
                { mg.InsertAutoFetchLog(userId, "ProcessOwnAccountCreditTxn", "End ProcessOwnAccountCreditTxn..."); }
                Console.WriteLine("End ProcessOwnAccountCreditTxn... -->" + DateTime.Now);

                Thread.Sleep(5000); //wait 5 sec

                //----- BEFTN
                Console.WriteLine();
                Console.WriteLine("Starting ProcessBEFTNTxn... -->" + DateTime.Now);
                if (IS_INSERT_TO_LOG_TABLE)
                { mg.InsertAutoFetchLog(userId, "ProcessBEFTNTxn", "Starting ProcessBEFTNTxn..."); }

                UploadBEFTNTxnIntoSystem();

                if (IS_INSERT_TO_LOG_TABLE)
                { mg.InsertAutoFetchLog(userId, "ProcessBEFTNTxn", "End ProcessBEFTNTxn..."); }
                Console.WriteLine("End ProcessBEFTNTxn... -->" + DateTime.Now);
                
                Thread.Sleep(5000); //wait 5 sec

                //----- BKASH
                Console.WriteLine();
                Console.WriteLine("Starting ProcessBkashTxn... -->" + DateTime.Now);
                if (IS_INSERT_TO_LOG_TABLE)
                { mg.InsertAutoFetchLog(userId, "ProcessBkashTxn", "Starting ProcessBkashTxn..."); }

                ProcessBkashTxn();

                int sleepTime = 300000;

                Console.WriteLine();
                Console.WriteLine("Going to wait " + (sleepTime / 1000) + " seconds..." + DateTime.Now);
                Thread.Sleep(sleepTime); // wait 5 min
                               
                  
            }
        }

        private static void ProcessReceivedTxn()
        {
            try
            {
                // received but not processed due to system issue, make forcefully status change.

                //string txnNo = "429921891426";
                //string txnNo = "429968265320";
                //string txnNo = "429992457119";
                //string txnNo = "429945146845";
                string txnNo = "429986763743";
                

                ProcessTransResponse procTranResp = gccclient.ProcessBankDepositTxn(Definitions.Values.GCCSecurityCode, txnNo);

                //if (procTranResp.ResponseCode.Equals("001") && procTranResp.Successful.ToLower().Equals("true"))
                {
                    mg.UpdateTxnStatusIntoTable(txnNo, "Processed", downloadUser, "");
                    Console.WriteLine("GCC_Number -> " + txnNo + "  Processed OK.");
                }
            }
            catch (Exception ex)
            {
                if (IS_INSERT_TO_LOG_TABLE)
                { mg.InsertAutoFetchLog(userId, "ProcessBEFTNTxn", "Error: ProcessBEFTNTxn, " + ex.ToString()); }
            }
        }

        private static void UpdateBEFTNStatusToGCCAlreadySent()
        {
            try
            {
                string refNo = "429930650661";

                if (IS_INSERT_TO_LOG_TABLE)
                { mg.InsertAutoFetchLog(userId, "ProcessBEFTNTxn", "Before GCC UpdateProcess Status: " + " refNo=" + refNo); }

                UpdateBankDepositResponse updateProcsStatusResp = gccclient.UpdateBankDepositTxnToPaid(Definitions.Values.GCCSecurityCode, refNo);
                if (updateProcsStatusResp.ResponseCode.Equals("001") && updateProcsStatusResp.Successful.ToLower().Equals("true"))
                {
                    if (IS_INSERT_TO_LOG_TABLE)
                    {
                        mg.InsertAutoFetchLog(userId, "ProcessBEFTNTxn", refNo + ", GCC UpdateProcessStatusToPaid, RespCode="
                          + updateProcsStatusResp.ResponseCode + ", Status=" + updateProcsStatusResp.Status + ", Message=" + updateProcsStatusResp.ResponseMessage + ", Successful=" + updateProcsStatusResp.Successful);
                    }

                    mg.UpdateTxnStatusIntoTable(refNo, "Paid", downloadUser, "");
                    Console.WriteLine("GCC_Number -> " + refNo + " , BEFTN Txn PAID OK.");

                    if (IS_INSERT_TO_LOG_TABLE)
                    { mg.InsertAutoFetchLog(userId, "ProcessBEFTNTxn", "RefNo=" + refNo + ", BEFTN Txn Update at DB Complete.."); }
                }
                else
                {
                    if (IS_INSERT_TO_LOG_TABLE)
                    {
                        mg.InsertAutoFetchLog(userId, "ProcessBEFTNTxn", refNo + ", GCC UpdateProcess Status ERROR!!! , RespCode="
                          + updateProcsStatusResp.ResponseCode + ", Message=" + updateProcsStatusResp.ResponseMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                if (IS_INSERT_TO_LOG_TABLE)
                { mg.InsertAutoFetchLog(userId, "ProcessBEFTNTxn", "Error: ProcessBEFTNTxn, " + ex.ToString()); }
            }
        }

        private static void ProcessBkashTxn()
        {
            string exhUserId, exhAccountNo, BeneficiaryAccountNo, BeneficiaryName, msgCodeValue = "", msgValue = "";
            string originateCountry = "", senderNationality = "", txnStatus, autoIDgcc, refrnNo;
            int partyId;
            string passwd = "";

            XmlDocument xDoc = new XmlDocument();
            XmlNodeList msgCode;

            DataTable dtRemitTransferInfo = new DataTable();
            string remitTransferResponseCode = "", remitTransferRemitStatus = "", remitTransferCBResponseCode = "", remitTransferRespMsg = "";

            string frmDate = DateTime.Now.ToString("dd-MMM-yyyy");
            DataTable dtBkash = mg.GetGccBkashData(userId, frmDate, frmDate);

            if (IS_INSERT_TO_LOG_TABLE)
            { mg.InsertAutoFetchLog(userId, "ProcessBkashTxn", "Date: " + frmDate + ", GccBkashData Row Count=" + dtBkash.Rows.Count); }
            Console.WriteLine("ProcessBkashTxn -> Date: " + frmDate + ", GccBkashData Row Count=" + dtBkash.Rows.Count);

            

            if (dtBkash.Rows.Count > 0)
            {
                for (int ii = 0; ii < dtBkash.Rows.Count; ii++)
                {
                    DataRow drow = dtBkash.Rows[ii];
                    txnStatus = drow["TxnStatus"].ToString();
                    autoIDgcc = drow["AutoId"].ToString();

                    if (!txnStatus.Equals("") && txnStatus.Equals("PROCESSED"))
                    {
                        refrnNo = drow["TransactionNo"].ToString();
                        DataTable dtRemitWalletInfo = mg.GetMobileWalletRemitTransferInfo(userId, refrnNo);

                        if (dtRemitWalletInfo.Rows.Count > 0)
                        {
                            if (IS_INSERT_TO_LOG_TABLE)
                            { mg.InsertAutoFetchLog(userId, "ProcessBkashTxn", "Error: RefNo=" + refrnNo + ", ALready exists in Wallet table, Need to Update GCC table status"); }


                            remitTransferResponseCode = dtRemitWalletInfo.Rows[0]["ResponseCode"].ToString();
                            remitTransferRemitStatus = dtRemitWalletInfo.Rows[0]["RemitStatus"].ToString();

                            try
                            {
                                remitTransferCBResponseCode = dtRemitWalletInfo.Rows[0]["CBresponseCode"] == null ? "" : (string)dtRemitWalletInfo.Rows[0]["CBresponseCode"];
                            }
                            catch (Exception ecc)
                            {
                                remitTransferCBResponseCode = "";
                            }

                            if ((!remitTransferResponseCode.Equals("") && remitTransferResponseCode.Equals("3050"))
                                  && (!remitTransferRemitStatus.Equals("") && remitTransferRemitStatus.Equals("5"))
                                  && (!remitTransferCBResponseCode.Equals("") && remitTransferCBResponseCode.Equals("3000")))
                            {
                                UpdateBkashDataAtGCCEndAndBkashTable(refrnNo, autoIDgcc);
                            }
                        }
                        else
                        {
                            /*
                            [AutoId],[TransactionNo],[AmountToPay],[TransactionDate],[ReceiveCountryCode],[ReceiveCountryName],[ReceiveCurrencyCode],[PurposeName],
                            [ReceiverName],[ReceiverNationality],[ReceiverAddress],[ReceiverCity],[ReceiverContactNo],[SenderAddress],[SendCountryCode],[SendCountryName],
                            [SenderCity],[SenderContactNo],[SenderName],[SenderNationality],[SenderIncomeSource],[SenderOccupation] 
                            [SenderIDExpiryDate],[SenderIDNumber],[SenderIDPlaceOfIssue],[SenderIDTypeName],[SentDate],[ValueDate],[PayinCurrencyCode],
                            [PayinAmount],[ExchangeRate],[TxnReceiveDate],[TxnStatus],[PaymentMode] 
                             */

                            msgCodeValue = "";
                            partyId = Definitions.Values.GCCPartyID;
                            passwd = Definitions.Values.GCCPassword;
                            DataTable dtExchAccInfo = mg.GetExchangeHouseAccountNo(userId, partyId.ToString());

                            exhUserId = dtExchAccInfo.Rows[0]["UserId"].ToString();
                            exhAccountNo = dtExchAccInfo.Rows[0]["AccountNo"].ToString();

                            BeneficiaryAccountNo = drow["ReceiverContactNo"].ToString();
                            BeneficiaryName = drow["ReceiverName"].ToString();

                            if (IS_INSERT_TO_LOG_TABLE)
                            { mg.InsertAutoFetchLog(userId, "ProcessBkashTxn", "partyId=" + partyId + ", " + exhUserId + ", " + BeneficiaryAccountNo + ", " + BeneficiaryName); }

                            string nodeValidationRequestStr = "";
                            string paymntResp;

                            // step-1: request
                            try
                            {
                                var nodeValidationRequest = remitServiceClient.MobileWalletBeneficiaryValidationRequest(partyId, exhUserId, passwd, BeneficiaryAccountNo, BeneficiaryName, BeneficiaryName, BeneficiaryName);
                                nodeValidationRequestStr = Convert.ToString(nodeValidationRequest);

                                //paymntResp = nodeValidationRequest.InnerXml;  // for UAT sys
                                paymntResp = nodeValidationRequest.ToString();

                                if (!paymntResp.Contains("<MobileWalletBeneficiaryValidationResponse"))
                                {
                                    paymntResp = "<MobileWalletBeneficiaryValidationResponse>" + paymntResp + "</MobileWalletBeneficiaryValidationResponse>";
                                }

                                xDoc.LoadXml(paymntResp);
                                //xDoc.LoadXml(nodeValidationRequest.ToString());

                                if (IS_INSERT_TO_LOG_TABLE)
                                { mg.InsertAutoFetchLog(userId, "ProcessBkashTxn", "RefNo=" + drow["TransactionNo"].ToString() + ", STEP-1: ValidationRequest=" + nodeValidationRequest.ToString()); }

                                Console.WriteLine("ProcessBkashTxn -> ValidationRequest: " + paymntResp);


                                msgCode = xDoc.GetElementsByTagName("MessageCode");
                                msgCodeValue = msgCode[0].InnerText;
                                msgValue = xDoc.GetElementsByTagName("Message")[0].InnerText;
                            }
                            catch (Exception excValReq)
                            {
                                if (IS_INSERT_TO_LOG_TABLE)
                                { mg.InsertAutoFetchLog(userId, "ProcessBkashTxn", "ERROR! MobileWalletBeneficiaryValidationRequest, STEP-1: RefNo=" + drow["TransactionNo"].ToString() + ", " + excValReq.ToString()); }
                            }

                            if (!msgCodeValue.Equals("") && msgCodeValue.Equals("0000"))
                            {
                                CallingReferanceNO = xDoc.GetElementsByTagName("MobileWalletBeneficiaryValidationResponseCallingReferanceNO")[0].InnerText;

                                if (IS_INSERT_TO_LOG_TABLE)
                                { mg.InsertAutoFetchLog(userId, "ProcessBkashTxn", "RefNo=" + drow["TransactionNo"].ToString() + ", STEP-1: CallingReferanceNO= " + CallingReferanceNO); }
                            }
                            else
                            {
                                CallingReferanceNO = "";

                                if (IS_INSERT_TO_LOG_TABLE)
                                { mg.InsertAutoFetchLog(userId, "ProcessBkashTxn", "ERROR -- STEP-1: RefNo=" + drow["TransactionNo"].ToString() + ", Validation Request Failed, Message=" + msgValue + ", " + nodeValidationRequestStr); }
                            }

                            //validation request END  


                            // ValidationResponse START
                            if (!CallingReferanceNO.Equals("")) // request success
                            {
                                Console.WriteLine();
                                Console.WriteLine("Sleep for 15 Seconds --> " + DateTime.Now);
                                msgCodeValue = "";
                                Thread.Sleep(15000); // wait for 15 sec

                                // step-2: response
                                try
                                {
                                    paymntResp = "";
                                    var nodeValidationResps = remitServiceClient.MobileWalletBeneficiaryValidationResponse(partyId, exhUserId, passwd, CallingReferanceNO);

                                    //paymntResp = nodeValidationResps.InnerXml;  // for UAT sys
                                    paymntResp = nodeValidationResps.ToString();

                                    if (!paymntResp.Contains("<BeneficiaryValidationResponse"))
                                    {
                                        paymntResp = "<BeneficiaryValidationResponse>" + paymntResp + "</BeneficiaryValidationResponse>";
                                    }

                                    xDoc.LoadXml(paymntResp);
                                    //xDoc.LoadXml(nodeValidationResps.ToString());

                                    if (IS_INSERT_TO_LOG_TABLE)
                                    { mg.InsertAutoFetchLog(userId, "ProcessBkashTxn", "RefNo=" + drow["TransactionNo"].ToString() + ", STEP-2: ValidationResps=" + nodeValidationResps.ToString()); }

                                    Console.WriteLine("ProcessBkashTxn -> ValidationResps: " + paymntResp);

                                    msgCode = xDoc.GetElementsByTagName("MessageCode");
                                    msgCodeValue = msgCode[0].InnerText;
                                    msgValue = xDoc.GetElementsByTagName("Message")[0].InnerText;
                                }
                                catch (Exception excValResp)
                                {
                                    if (IS_INSERT_TO_LOG_TABLE)
                                    { mg.InsertAutoFetchLog(userId, "ProcessBkashTxn", "ERROR! MobileWalletBeneficiaryValidationResponse, STEP-2: RefNo=" + drow["TransactionNo"].ToString() + ", " + excValResp.ToString()); }
                                }

                                if (!msgCodeValue.Equals("") && msgCodeValue.Equals("6000")) // ValidationResponse success
                                {
                                    ConversationID = xDoc.GetElementsByTagName("ConversationID")[0].InnerText;

                                    if (IS_INSERT_TO_LOG_TABLE)
                                    { mg.InsertAutoFetchLog(userId, "ProcessBkashTxn", "RefNo=" + drow["TransactionNo"].ToString() + ", STEP-2: ConversationID= " + ConversationID); }
                                }
                                else
                                {
                                    ConversationID = "";

                                    if (IS_INSERT_TO_LOG_TABLE)
                                    { mg.InsertAutoFetchLog(userId, "ProcessBkashTxn", "ERROR  -- STEP-2: RefNo=" + drow["TransactionNo"].ToString() + ", Validation Response Failed, Message=" + msgValue); }

                                    if (msgValue.ToLower().Contains("try"))
                                    {
                                        //do nothing, system will try again to process
                                    }
                                    else
                                    {
                                        mg.MarkBkashTxnCancelled(userId, autoIDgcc, drow["TransactionNo"].ToString(), msgValue);
                                    }
                                }


                                // WalletPayment START
                                if (!ConversationID.Equals(""))
                                {
                                    Console.WriteLine();
                                    Console.WriteLine("Sleep for 10 Seconds --> " + DateTime.Now);

                                    Thread.Sleep(10000); // wait for 10 sec                                

                                    string refNo = drow["TransactionNo"].ToString();
                                    BeneficiaryAccountNo = drow["ReceiverContactNo"].ToString();
                                    BeneficiaryName = drow["ReceiverName"].ToString();
                                    string originateCurrency = drow["PayinCurrencyCode"].ToString();
                                    string receivingAmount = drow["AmountToPay"].ToString();
                                    string SenderName = drow["SenderName"].ToString();
                                    string SenderMsIsdn = drow["SenderContactNo"].ToString();
                                    originateCountry = drow["SendCountryCode"].ToString();
                                    senderNationality = drow["SenderNationality"].ToString();
                                    decimal sendingAmount = decimal.Round(Convert.ToDecimal(drow["PayinAmount"]), 2);

                                    if (Convert.ToSingle(receivingAmount) <= BKASH_TXN_LIMIT)
                                    {

                                        string availBal = "0";
                                        availBal = GetExhNRTAccountBalance(exhAccountNo); 
                                                                             

                                        decimal exhouseAccountBalance = decimal.Round(Convert.ToDecimal(availBal), 2);
                                        if (IS_INSERT_TO_LOG_TABLE)
                                        { mg.InsertAutoFetchLog(userId, "ProcessBkashTxn", "RefNo=" + refNo + ", ExH Balance=" + exhouseAccountBalance); }


                                        if (exhouseAccountBalance < decimal.Round(Convert.ToDecimal(receivingAmount), 2))
                                        {
                                            if (IS_INSERT_TO_LOG_TABLE)
                                            { mg.InsertAutoFetchLog(userId, "ProcessBkashTxn", "ERROR! Balance LOW, RefNo=" + refNo + ", exhAccountBalance=" + exhouseAccountBalance); }
                                            Console.WriteLine("ProcessBkashTxn -> ERROR! Balance LOW, RefNo=" + refNo + ", exhAccountBalance=" + exhouseAccountBalance);
                                        }
                                        else
                                        {
                                            msgCodeValue = "";

                                            // step-3: payment
                                            try
                                            {
                                                paymntResp = "";
                                                var nodeWalletPaymentResp = remitServiceClient.MobileWalletPayment(partyId, exhUserId, passwd, refNo, ConversationID, originateCountry,
                                                originateCurrency, BeneficiaryAccountNo, sendingAmount, "", receivingAmount, SenderName, SenderName, SenderMsIsdn, "", "", "", "", "", "",
                                                "", senderNationality, "1", "1", "", "", exhUserId, "", "", "", "", "", "");


                                                //paymntResp = nodeWalletPaymentResp.InnerXml;  // for UAT system
                                                paymntResp = nodeWalletPaymentResp.ToString();

                                                if (!paymntResp.Contains("<MobileWalletPaymentResponse"))
                                                {
                                                    paymntResp = "<MobileWalletPaymentResponse>" + paymntResp + "</MobileWalletPaymentResponse>";
                                                }

                                                xDoc.LoadXml(paymntResp);
                                                //xDoc.LoadXml(nodeWalletPaymentResp.ToString());


                                                if (IS_INSERT_TO_LOG_TABLE)
                                                { mg.InsertAutoFetchLog(userId, "ProcessBkashTxn", "RefNo=" + refNo + ", STEP-3: WalletPaymentResp=" + nodeWalletPaymentResp.ToString()); }

                                                Console.WriteLine("ProcessBkashTxn -> MobileWalletPayment: " + paymntResp);
                                                Console.WriteLine();

                                                msgCodeValue = xDoc.GetElementsByTagName("MessageCode")[0].InnerText;
                                            }
                                            catch (Exception excWalPay)
                                            {
                                                if (IS_INSERT_TO_LOG_TABLE)
                                                { mg.InsertAutoFetchLog(userId, "ProcessBkashTxn", "ERROR! MobileWalletBeneficiaryValidationResponse, STEP-3: RefNo=" + refNo + ", " + excWalPay.ToString()); }
                                            }

                                            if (!msgCodeValue.Equals("") && msgCodeValue.Equals("0000")) // WalletPayment success
                                            {
                                                int waitForStatusSuccess;

                                                for (waitForStatusSuccess = 1; waitForStatusSuccess <= statusCheckCount; waitForStatusSuccess++)
                                                {
                                                    Thread.Sleep(12000); // wait for 12 sec 
                                                    dtRemitTransferInfo = mg.GetMobileWalletRemitTransferInfo(userId, refNo);
                                                    if (dtRemitTransferInfo.Rows.Count > 0)
                                                    {
                                                        remitTransferResponseCode = dtRemitTransferInfo.Rows[0]["ResponseCode"].ToString();
                                                        remitTransferRemitStatus = dtRemitTransferInfo.Rows[0]["RemitStatus"].ToString();
                                                        remitTransferRespMsg = dtRemitTransferInfo.Rows[0]["responseMessage"].ToString();

                                                        try
                                                        {
                                                            remitTransferCBResponseCode = dtRemitTransferInfo.Rows[0]["CBresponseCode"] == null ? "" : (string)dtRemitTransferInfo.Rows[0]["CBresponseCode"];
                                                        }
                                                        catch (Exception ecc)
                                                        {
                                                            remitTransferCBResponseCode = "";
                                                        }
                                                    }

                                                    if ((!remitTransferResponseCode.Equals("") && remitTransferResponseCode.Equals("3050"))
                                                            && (!remitTransferRemitStatus.Equals("") && remitTransferRemitStatus.Equals("5"))
                                                            && (!remitTransferCBResponseCode.Equals("") && remitTransferCBResponseCode.Equals("3000")))
                                                    {
                                                        //call & update db
                                                        try
                                                        {
                                                            UpdateBkashDataAtGCCEndAndBkashTable(refNo, autoIDgcc);
                                                        }
                                                        catch (Exception exc)
                                                        {
                                                            if (IS_INSERT_TO_LOG_TABLE)
                                                            { mg.InsertAutoFetchLog(userId, "ProcessBkashTxn", "Error: RefNo=" + refNo + ", " + exc.ToString()); }
                                                        }

                                                        break; // if remit transfer table status OK then no need to iterate again

                                                    } //response check if

                                                } //for loop end

                                                if (waitForStatusSuccess == statusCheckCount)
                                                {
                                                    if (!remitTransferResponseCode.Equals("3050") && !remitTransferRespMsg.Contains("Acknowledged"))
                                                    {
                                                        mg.UpdateFailedStatusBkashTxn(userId, refNo, autoIDgcc, remitTransferRespMsg);
                                                    }
                                                }


                                            } //WalletPayment success If
                                            else
                                            {
                                                if (IS_INSERT_TO_LOG_TABLE)
                                                { mg.InsertAutoFetchLog(userId, "ProcessBkashTxn", "ERROR !!! WalletPayment STEP-3: RefNo=" + drow["TransactionNo"].ToString()); }

                                                if (!msgCodeValue.Equals("") && msgCodeValue.Equals("2008"))  // Not Enough Fund
                                                {
                                                    if (IS_INSERT_TO_LOG_TABLE)
                                                    { mg.InsertAutoFetchLog(userId, "ProcessBkashTxn", "ERROR !!! WalletPayment FUND Shortage: RefNo=" + drow["TransactionNo"].ToString()); }

                                                    mg.MarkBkashTxnHOLD(userId, autoIDgcc, refNo);
                                                }
                                            }


                                        } // Balance Check


                                    } //bkash limit check
                                    else
                                    {
                                        if (IS_INSERT_TO_LOG_TABLE)
                                        { mg.InsertAutoFetchLog(userId, "ProcessBkashTxn", "ERROR: RefNo=" + drow["TransactionNo"].ToString() + ", Amount Per Transaction Exceed, current amount=" + receivingAmount); }

                                        mg.MarkBkashTxnCancelled(userId, autoIDgcc, refNo, "Amount Per Transaction Exceed");
                                    }

                                } // WalletPayment END

                            } // ValidationResponse END
                            
                        }

                    }

                } //for
            }// if
        }

        private static void UpdateBkashDataAtGCCEndAndBkashTable(string refrnNo, string autoIDgcc)
        {
            try
            {
                if (IS_INSERT_TO_LOG_TABLE)
                { mg.InsertAutoFetchLog(userId, "UpdateBkashDataAtGCC", "Before GCC UpdateProcess Status: " + " refNo=" + refrnNo); }

                UpdateBankDepositResponse updateProcsStatusResp = gccclient.UpdateBankDepositTxnToPaid(Definitions.Values.GCCSecurityCode, refrnNo);

                if (updateProcsStatusResp.ResponseCode.Equals("001") && updateProcsStatusResp.Successful.ToLower().Equals("true"))
                {
                    mg.UpdateTxnStatusIntoTable(refrnNo, "Paid", downloadUser, "");
                    Console.WriteLine("GCC_Number -> " + refrnNo + " , WALLET Txn PAID OK.");

                    if (IS_INSERT_TO_LOG_TABLE)
                    { mg.InsertAutoFetchLog(userId, "UpdateBkashDataAtGCC", "RefNo=" + refrnNo + ", WALLET Update at DB Complete.."); }
                }
                else
                {
                    if (IS_INSERT_TO_LOG_TABLE)
                    {
                        mg.InsertAutoFetchLog(userId, "UpdateBkashDataAtGCC", refrnNo + ", GCC UpdateProcess Status ERROR!!! , RespCode="
                          + updateProcsStatusResp.ResponseCode + ", Message=" + updateProcsStatusResp.ResponseMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                if (IS_INSERT_TO_LOG_TABLE)
                { mg.InsertAutoFetchLog(userId, "UpdateBkashDataAtGCC", "RefNo=" + refrnNo + ", ConfirmAccountCreditPayment Error: " + ex); }
            }
        }

        private static void ProcessOwnAccountCreditTxn()
        {
            string exhUserId, exhAccountNo, beneficiaryAccountNo, beneficiaryName, msgCodeValue, refrnNo;
            string autoIDgcc, txnStatus = "", status = "", respCode = "", refNo, SenderName, senderPhoneNo = "", senderAddress, senderCountry, bankId, branchId, transferCurrency, msgToBenfcry;
            int partyId;
            string remitPaymentStatus = "";
            XmlDocument xDoc = new XmlDocument();
            XmlNodeList msgCode;

            string frmDate = DateTime.Now.ToString("dd-MMM-yyyy");
            DataTable dtGccMtbAc = GetGCCOwnAccData(frmDate, frmDate);

            if (IS_INSERT_TO_LOG_TABLE)
            { mg.InsertAutoFetchLog(userId, "ProcessOwnAccountCreditTxn", "Date: " + frmDate + ", GCCOwnAccData Row Count=" + dtGccMtbAc.Rows.Count); }
            Console.WriteLine("ProcessOwnAccountCreditTxn -> Date: " + frmDate + ", GCCOwnAccData Row Count=" + dtGccMtbAc.Rows.Count);

            if (dtGccMtbAc.Rows.Count > 0)
            {
                for (int ii = 0; ii < dtGccMtbAc.Rows.Count; ii++)
                {
                    DataRow drow = dtGccMtbAc.Rows[ii];

                    txnStatus = drow["TxnStatus"].ToString();
                    status = drow["Status"].ToString();
                    respCode = drow["ResponseCode"].ToString();

                    if (!txnStatus.Equals("") && txnStatus.Equals("PROCESSED") && status.Equals("Processed") && respCode.Equals("001"))
                    {
                        refrnNo = drow["TransactionNo"].ToString();
                        DataTable dtRemitFundTransferInfo = mg.GetOwnAccountRemitTransferInfo(userId, refrnNo);

                        if (dtRemitFundTransferInfo.Rows.Count > 0)
                        {
                            if (IS_INSERT_TO_LOG_TABLE)
                            { mg.InsertAutoFetchLog(userId, "ProcessOwnAccountCreditTxn", "Error: RefNo=" + refrnNo + ", Already exists in FundTransfer table, Need to Update GCC table status"); }

                            remitPaymentStatus = dtRemitFundTransferInfo.Rows[0]["PaymentStatus"].ToString();

                            if (!remitPaymentStatus.Equals("") && remitPaymentStatus.Equals("5"))
                            {
                                autoIDgcc = drow["AutoId"].ToString();
                                UpdateOwnAccountRemitAtGCCEndAndDBTable(refrnNo, autoIDgcc);
                            }
                            else
                            {
                                autoIDgcc = drow["AutoId"].ToString();
                                UpdateFailedOwnAccountRemitAtGCCTable(refrnNo, autoIDgcc);
                            }
                        }
                        else
                        {
                            autoIDgcc = drow["AutoId"].ToString();
                            DataTable dtGccOwnBank = mg.GetGCCOwnBankRemitData(userId, autoIDgcc);

                            partyId = Definitions.Values.GCCPartyID;
                            passwd = Definitions.Values.GCCPassword;

                            DataTable dtExchAccInfo = mg.GetExchangeHouseAccountNo(userId, partyId.ToString());
                            exhUserId = dtExchAccInfo.Rows[0]["UserId"].ToString();
                            exhAccountNo = dtExchAccInfo.Rows[0]["AccountNo"].ToString();

                            beneficiaryAccountNo = dtGccOwnBank.Rows[0]["BankAccountNo"].ToString();
                            beneficiaryName = dtGccOwnBank.Rows[0]["ReceiverName"].ToString();

                            try
                            {
                                refNo = dtGccOwnBank.Rows[0]["TransactionNo"].ToString();
                                beneficiaryAccountNo = dtGccOwnBank.Rows[0]["BankAccountNo"].ToString();
                                beneficiaryName = dtGccOwnBank.Rows[0]["ReceiverName"].ToString();
                                decimal receivingAmount = decimal.Round(Convert.ToDecimal(dtGccOwnBank.Rows[0]["AmountToPay"].ToString()), 2);

                                SenderName = dtGccOwnBank.Rows[0]["SenderName"].ToString();
                                senderAddress = dtGccOwnBank.Rows[0]["SenderAddress"].ToString();
                                senderCountry = dtGccOwnBank.Rows[0]["SendCountryName"].ToString();
                                bankId = "001";
                                branchId = "";
                                DateTime paymentDate = DateTime.Now;
                                transferCurrency = "053";
                                msgToBenfcry = dtGccOwnBank.Rows[0]["PurposeName"].ToString();

                                string availBal = "0";
                                availBal = GetExhNRTAccountBalance(exhAccountNo);                                                              

                                decimal exhouseAccountBalance = decimal.Round(Convert.ToDecimal(availBal), 2);
                                if (IS_INSERT_TO_LOG_TABLE)
                                { mg.InsertAutoFetchLog(userId, "ProcessOwnAccountCreditTxn", "RefNo=" + refNo + ", ExH Balance=" + exhouseAccountBalance); }
                                Console.WriteLine("ProcessOwnAccountCreditTxn -> RefNo=" + refNo + ", ExH Balance=" + exhouseAccountBalance);


                                if (exhouseAccountBalance < decimal.Round(Convert.ToDecimal(receivingAmount), 2))
                                {
                                    if (IS_INSERT_TO_LOG_TABLE)
                                    { mg.InsertAutoFetchLog(userId, "ProcessOwnAccountCreditTxn", "ERROR! Balance LOW, RefNo=" + refNo + ", exhAccountBalance=" + exhouseAccountBalance); }
                                    Console.WriteLine("ProcessOwnAccountCreditTxn -> ERROR! Balance LOW, RefNo=" + refNo + ", exhAccountBalance=" + exhouseAccountBalance);
                                }
                                else
                                {
                                    string paymntResp = "";
                                    msgCodeValue = "";

                                    try
                                    {
                                        var nodePaymentRequest = remitServiceClient.Payment("1", partyId, exhUserId, passwd, refNo, beneficiaryAccountNo, beneficiaryName, SenderName,
                                            senderPhoneNo, senderAddress, senderCountry, bankId, branchId, paymentDate, transferCurrency, receivingAmount, "", msgToBenfcry, "");

                                        //paymntResp = nodePaymentRequest.InnerXml;

                                        paymntResp = nodePaymentRequest.ToString();
                                        if (!paymntResp.Contains("PaymentResponse"))
                                        {
                                            paymntResp = "<PaymentResponse>" + paymntResp + "</PaymentResponse>";
                                        }

                                        //xDoc.LoadXml(nodePaymentRequest.ToString());
                                        xDoc.LoadXml(paymntResp);

                                        if (IS_INSERT_TO_LOG_TABLE)
                                        { mg.InsertAutoFetchLog(userId, "ProcessOwnAccountCreditTxn", "RefNo=" + refNo + ", PaymentRequest=" + paymntResp); }

                                        msgCode = xDoc.GetElementsByTagName("MessageCode");
                                        msgCodeValue = msgCode[0].InnerText;
                                    }
                                    catch (Exception ex)
                                    {
                                        if (IS_INSERT_TO_LOG_TABLE)
                                        { mg.InsertAutoFetchLog(userId, "ProcessOwnAccountCreditTxn", "ERROR! Payment " + ex.ToString()); }
                                    }


                                    if (!msgCodeValue.Equals("") && msgCodeValue.Equals("1009"))    // Fund Transfer Success
                                    {
                                        try
                                        {
                                            if (IS_INSERT_TO_LOG_TABLE)
                                            { mg.InsertAutoFetchLog(userId, "ProcessOwnAccountCreditTxn", "Before GCC ConfirmTransaction: RefNo=" + refNo); }

                                            UpdateBankDepositResponse updateProcsStatusResp = gccclient.UpdateBankDepositTxnToPaid(Definitions.Values.GCCSecurityCode, refrnNo);

                                            if (updateProcsStatusResp.ResponseCode.Equals("001") && updateProcsStatusResp.Successful.ToLower().Equals("true"))
                                            {
                                                if (IS_INSERT_TO_LOG_TABLE)
                                                {
                                                    mg.InsertAutoFetchLog(userId, "ProcessOwnAccountCreditTxn", refrnNo + ", GCC UpdateProcessStatusToPaid, RespCode="
                                                      + updateProcsStatusResp.ResponseCode + ", Status=" + updateProcsStatusResp.Status + ", Message=" + updateProcsStatusResp.ResponseMessage + ", Successful=" + updateProcsStatusResp.Successful);
                                                }

                                                mg.UpdateTxnStatusIntoTable(refrnNo, "Paid", downloadUser, "");
                                                Console.WriteLine("GCC_Number -> " + refrnNo + " , MTB Ac Txn PAID OK.");

                                                if (IS_INSERT_TO_LOG_TABLE)
                                                { mg.InsertAutoFetchLog(userId, "UpdateOwnAccountRemitAtGCCEndAndDBTable", "RefNo=" + refrnNo + ", MTB Ac Update at DB Complete.."); }
                                            }
                                            else
                                            {
                                                if (IS_INSERT_TO_LOG_TABLE)
                                                {
                                                    mg.InsertAutoFetchLog(userId, "UpdateOwnAccountRemitAtGCCEndAndDBTable", refrnNo + ", GCC UpdateProcess Status ERROR!!! , RespCode="
                                                      + updateProcsStatusResp.ResponseCode + ", Message=" + updateProcsStatusResp.ResponseMessage);
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            if (IS_INSERT_TO_LOG_TABLE)
                                            { mg.InsertAutoFetchLog(userId, "ProcessOwnAccountCreditTxn", "Error: ConfirmAccountCreditPayment, " + ex.ToString()); }
                                        }
                                    }
                                    else
                                    {
                                        try
                                        {
                                            string msgValue = xDoc.GetElementsByTagName("Message")[0].InnerText;
                                            mg.MarkOwnBankTxnCancelled(userId, autoIDgcc, refNo, msgValue);
                                        }
                                        catch (Exception expmt)
                                        {
                                            if (IS_INSERT_TO_LOG_TABLE)
                                            { mg.InsertAutoFetchLog(userId, "ProcessOwnAccountCreditTxn", "ERROR: cancelNBLOwnAccountData, " + expmt.ToString()); }
                                        }
                                    }

                                } //------- balance check ELSE

                            }
                            catch (Exception ex)
                            {
                                if (IS_INSERT_TO_LOG_TABLE)
                                { mg.InsertAutoFetchLog(userId, "ProcessOwnAccountCreditTxn", "RefNo=" + dtGccOwnBank.Rows[0]["TransactionNo"].ToString() + ", AccountNo=" + beneficiaryAccountNo + ", AccountEnquiry ERROR: " + ex.ToString()); }
                            }

                        } //else END

                    } //if PROCESSED END

                }// for END

            } // if (dtGccMtbAc.Rows.Count > 0)

        }

        private static string GetExhNRTAccountBalance(string exhAccountNo)
        {
            string availBal = "0";
            try
            {
                CurrentBalanceByAccountNoRequest currBalReq = new CurrentBalanceByAccountNoRequest();
                currBalReq.accNo = exhAccountNo;
                currBalReq.userName = Definitions.Values.CORE_SERVICE_USERNAME;
                currBalReq.password = Definitions.Values.CORE_SERVICE_PASSWORD;

                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                       | SecurityProtocolType.Tls11
                       | SecurityProtocolType.Tls12
                       | SecurityProtocolType.Ssl3;

                ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;


                var resCurrBalance = new MTBCoreMiddleware.MTBWebServicePortTypeClient().serCurrentBalance(currBalReq);
                string respCodeBl = resCurrBalance.resCode.Trim();
                if (respCodeBl.Equals("000"))
                {
                    availBal = resCurrBalance.accInfo.availableBalance;

                    if (IS_INSERT_TO_LOG_TABLE)
                    { mg.InsertAutoFetchLog(userId, "NRT Balance", "ExH Balance=" + availBal); }
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
                if (IS_INSERT_TO_LOG_TABLE)
                { mg.InsertAutoFetchLog(userId, "NRT Balance", "ERROR! Balance Check Problem"); }
            }

            return availBal;
        }

        private static void UpdateFailedOwnAccountRemitAtGCCTable(string refrnNo, string autoIDgcc)
        {
            try
            {
                mg.UpdateTxnStatusIntoTable(refrnNo, "Error", downloadUser, "Failed");
                Console.WriteLine("GCC_Number -> " + refrnNo + ", " + "Failed");

                if (IS_INSERT_TO_LOG_TABLE)
                { mg.InsertAutoFetchLog(userId, "RejectBEFTNDueToDuplicateICNumber", "RefNo=" + refrnNo + ", BEFTN Txn Update at DB Complete.."); }
            }
            catch (Exception ex)
            {
                if (IS_INSERT_TO_LOG_TABLE)
                { mg.InsertAutoFetchLog(userId, "RejectBEFTNDueToDuplicateICNumber", "RefNo=" + refrnNo + ", RejectBEFTNDueToDuplicateICNumber Error: " + ex); }
            }
        }

        private static void UpdateOwnAccountRemitAtGCCEndAndDBTable(string refrnNo, string autoIDgcc)
        {
            try
            {
                if (IS_INSERT_TO_LOG_TABLE)
                { mg.InsertAutoFetchLog(userId, "UpdateOwnAccountRemitAtGCCEndAndDBTable", "Before GCC UpdateProcess Status: " + " refNo=" + refrnNo); }

                UpdateBankDepositResponse updateProcsStatusResp = gccclient.UpdateBankDepositTxnToPaid(Definitions.Values.GCCSecurityCode, refrnNo);

                if (updateProcsStatusResp.ResponseCode.Equals("001") && updateProcsStatusResp.Successful.ToLower().Equals("true"))
                {
                    mg.UpdateTxnStatusIntoTable(refrnNo, "Paid", downloadUser, "");
                    Console.WriteLine("GCC_Number -> " + refrnNo + " , MTB Ac Txn PAID OK.");

                    if (IS_INSERT_TO_LOG_TABLE)
                    { mg.InsertAutoFetchLog(userId, "UpdateOwnAccountRemitAtGCCEndAndDBTable", "RefNo=" + refrnNo + ", MTB Ac Update at DB Complete.."); }
                }
                else
                {
                    if (IS_INSERT_TO_LOG_TABLE)
                    {
                        mg.InsertAutoFetchLog(userId, "UpdateOwnAccountRemitAtGCCEndAndDBTable", refrnNo + ", GCC UpdateProcess Status ERROR!!! , RespCode="
                          + updateProcsStatusResp.ResponseCode + ", Message=" + updateProcsStatusResp.ResponseMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                if (IS_INSERT_TO_LOG_TABLE)
                { mg.InsertAutoFetchLog(userId, "UpdateOwnAccountRemitAtGCCEndAndDBTable", "RefNo=" + refrnNo + ", ConfirmAccountCreditPayment Error: " + ex); }
            }
        }

        private static DataTable GetGCCOwnAccData(string frmDate1, string frmDate2)
        {
            DataTable remittanceData = new DataTable();
            try
            {
                remittanceData = mg.GetGccMTBRemittanceDetailsByDate(userId, frmDate1, frmDate2);
            }
            catch (Exception exc)
            {
                if (IS_INSERT_TO_LOG_TABLE)
                { mg.InsertAutoFetchLog(userId, "ProcessOwnAccountCreditTxn", "ERROR: GetGCCOwnAccData, " + exc.ToString()); }
            }
            return remittanceData;
        }

        private static void UploadBEFTNTxnIntoSystem()
        {
            string refNo, autoIDgcc, msgCodeValue = "", txnStatus = "", status = "", respCode = "";
            string exhUserId, beneficiaryAccountNo, beneficiaryName, bankName, branchName, routingNumber, beneficiaryAddress, senderName, senderAddress, transferCurrency, paymentDescription;
            decimal receivingAmount;
            int partyId;

            XmlDocument xDoc = new XmlDocument();
            XmlNodeList msgCode;
            XmlNodeList msgVal;

            string frmDate = DateTime.Now.ToString("dd-MMM-yyyy");
            DataTable dtGccBeftn = GetGCCBEFTNAccData(frmDate, frmDate);

            if (IS_INSERT_TO_LOG_TABLE)
            { mg.InsertAutoFetchLog(userId, "ProcessBEFTNTxn", "Date: " + frmDate + ", BEFTNTxn Row Count=" + dtGccBeftn.Rows.Count); }
            Console.WriteLine("ProcessBEFTNTxn -> Date: " + frmDate + ", BEFTNTxn Row Count=" + dtGccBeftn.Rows.Count);

            if (dtGccBeftn.Rows.Count > 0)
            {
                for (int rCnt = 0; rCnt < dtGccBeftn.Rows.Count; rCnt++)
                {
                    txnStatus = dtGccBeftn.Rows[rCnt]["TxnStatus"].ToString();
                    status = dtGccBeftn.Rows[rCnt]["Status"].ToString();
                    respCode = dtGccBeftn.Rows[rCnt]["ResponseCode"].ToString();

                    if (!txnStatus.Equals("") && txnStatus.Equals("PROCESSED") && status.Equals("Processed") && respCode.Equals("001"))
                    {
                        refNo = dtGccBeftn.Rows[rCnt]["TransactionNo"].ToString();
                        DataTable dtBeftnRemitInfo = mg.GetBeftnRemitInfo(userId, refNo);

                        if (dtBeftnRemitInfo.Rows.Count > 0)
                        {
                            if (IS_INSERT_TO_LOG_TABLE)
                            { mg.InsertAutoFetchLog(userId, "ProcessBEFTNTxn", "Error: RefNo=" + refNo + ", Already Exists in BEFTNRequest table, Need to Update GCC table status"); }
                        }
                        else
                        {
                            msgCodeValue = "";
                            autoIDgcc = dtGccBeftn.Rows[rCnt]["AutoId"].ToString();
                            partyId = Definitions.Values.GCCPartyID;

                            DataTable dtExchAccInfo = mg.GetExchangeHouseAccountNo(userId, partyId.ToString());
                            exhUserId = dtExchAccInfo.Rows[0]["UserId"].ToString();
                            passwd = Definitions.Values.GCCPassword;
                            string exhAccountNo = dtExchAccInfo.Rows[0]["AccountNo"].ToString();

                            refNo = dtGccBeftn.Rows[rCnt]["TransactionNo"].ToString();
                            beneficiaryAccountNo = dtGccBeftn.Rows[rCnt]["BankAccountNo"].ToString();
                            beneficiaryName = dtGccBeftn.Rows[rCnt]["ReceiverName"].ToString();
                            bankName = dtGccBeftn.Rows[rCnt]["BankName"].ToString();
                            routingNumber = dtGccBeftn.Rows[rCnt]["BankBranchCode"].ToString();
                            branchName = mg.GetBranchNameByRoutingCode(userId, routingNumber);

                            if (!branchName.Equals(""))
                            {
                                beneficiaryAddress = dtGccBeftn.Rows[rCnt]["ReceiverAddress"].ToString();
                                senderName = dtGccBeftn.Rows[rCnt]["SenderName"].ToString();
                                senderAddress = dtGccBeftn.Rows[rCnt]["SenderAddress"].ToString();
                                transferCurrency = "053";
                                receivingAmount = decimal.Round(Convert.ToDecimal(dtGccBeftn.Rows[rCnt]["AmountToPay"].ToString()), 2);
                                paymentDescription = dtGccBeftn.Rows[rCnt]["PurposeName"].ToString();
                                string paymntResp = "";

                                string availBal = "0";
                                availBal = GetExhNRTAccountBalance(exhAccountNo);

                                decimal exhouseAccountBalance = decimal.Round(Convert.ToDecimal(availBal), 2);
                                
                                if (IS_INSERT_TO_LOG_TABLE)
                                { mg.InsertAutoFetchLog(userId, "ProcessBEFTNTxn", "RefNo=" + refNo + ", ExH Balance=" + exhouseAccountBalance); }
                                Console.WriteLine("ProcessBEFTNTxn -> RefNo=" + refNo + ", ExH Balance=" + exhouseAccountBalance);

                                /*
                                if (exhouseAccountBalance < decimal.Round(Convert.ToDecimal(receivingAmount), 2))
                                {
                                    if (IS_INSERT_TO_LOG_TABLE)
                                    { mg.InsertAutoFetchLog(userId, "ProcessBEFTNTxn", "ERROR! Balance LOW, RefNo=" + refNo + ", exhAccountBalance=" + exhouseAccountBalance); }
                                    Console.WriteLine("ProcessBEFTNTxn -> ERROR! Balance LOW, RefNo=" + refNo + ", exhAccountBalance=" + exhouseAccountBalance);
                                }
                                else
                                {
                                */
                                    try
                                    {
                                        var nodeBeftnPaymentRequest = remitServiceClient.BEFTNPayment(partyId, exhUserId, passwd, refNo, beneficiaryAccountNo, "SB", beneficiaryName, bankName,
                                            branchName, routingNumber, beneficiaryAddress, senderName, senderAddress, transferCurrency, receivingAmount, paymentDescription);

                                        //paymntResp = nodeBeftnPaymentRequest.InnerXml;

                                        paymntResp = nodeBeftnPaymentRequest.ToString();
                                        if (!paymntResp.Contains("BEFTNPayment"))
                                        {
                                            paymntResp = "<BEFTNPaymentResponse>" + paymntResp + "</BEFTNPaymentResponse>";
                                        }

                                        xDoc.LoadXml(paymntResp);
                                        //xDoc.LoadXml(nodeBeftnPaymentRequest.ToString());

                                        if (IS_INSERT_TO_LOG_TABLE)
                                        { mg.InsertAutoFetchLog(userId, "ProcessBEFTNTxn", "RefNo=" + refNo + ", BEFTNPaymentResponse=" + paymntResp); }
                                        Console.WriteLine("ProcessBEFTNTxn -> RefNo=" + refNo + ", BEFTNPaymentResponse=" + paymntResp);

                                        msgCode = xDoc.GetElementsByTagName("MessageCode");
                                        msgCodeValue = msgCode[0].InnerText;
                                    }
                                    catch (Exception ex)
                                    {
                                        if (IS_INSERT_TO_LOG_TABLE)
                                        { mg.InsertAutoFetchLog(userId, "ProcessBEFTNTxn", "BEFTNPayment Error: " + ex); }
                                    }

                                    if (!msgCodeValue.Equals("") && msgCodeValue.Equals("1020"))    //BEFTN success
                                    {
                                        try
                                        {
                                            if (IS_INSERT_TO_LOG_TABLE)
                                            { mg.InsertAutoFetchLog(userId, "ProcessBEFTNTxn", "Before GCC UpdateProcess Status: " + " refNo=" + refNo); }

                                            UpdateBankDepositResponse updateProcsStatusResp = gccclient.UpdateBankDepositTxnToPaid(Definitions.Values.GCCSecurityCode, refNo);
                                            if (updateProcsStatusResp.ResponseCode.Equals("001") && updateProcsStatusResp.Successful.ToLower().Equals("true"))
                                            {
                                                if (IS_INSERT_TO_LOG_TABLE)
                                                {
                                                    mg.InsertAutoFetchLog(userId, "ProcessBEFTNTxn", refNo + ", GCC UpdateProcessStatusToPaid, RespCode="
                                                      + updateProcsStatusResp.ResponseCode + ", Status=" + updateProcsStatusResp.Status + ", Message=" + updateProcsStatusResp.ResponseMessage + ", Successful=" + updateProcsStatusResp.Successful);
                                                }

                                                mg.UpdateTxnStatusIntoTable(refNo, "Paid", downloadUser, "");
                                                Console.WriteLine("GCC_Number -> " + refNo + " , BEFTN Txn PAID OK.");

                                                if (IS_INSERT_TO_LOG_TABLE)
                                                { mg.InsertAutoFetchLog(userId, "ProcessBEFTNTxn", "RefNo=" + refNo + ", BEFTN Txn Update at DB Complete.."); }
                                            }
                                            else
                                            {
                                                if (IS_INSERT_TO_LOG_TABLE)
                                                {
                                                    mg.InsertAutoFetchLog(userId, "ProcessBEFTNTxn", refNo + ", GCC UpdateProcess Status ERROR!!! , RespCode="
                                                      + updateProcsStatusResp.ResponseCode + ", Message=" + updateProcsStatusResp.ResponseMessage);
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            if (IS_INSERT_TO_LOG_TABLE)
                                            { mg.InsertAutoFetchLog(userId, "ProcessBEFTNTxn", "Error: ProcessBEFTNTxn, " + ex.ToString()); }
                                        }
                                    }
                                    else
                                    {
                                        msgVal = xDoc.GetElementsByTagName("Message");
                                        string msgValue = msgVal[0].InnerText;

                                        if (msgCodeValue.Equals("1017")) // Duplicate Reference Number
                                        {
                                            RejectBEFTNDueToDuplicateICNumber(userId, refNo, msgValue);
                                        }

                                        if (IS_INSERT_TO_LOG_TABLE)
                                        { mg.InsertAutoFetchLog(userId, "ProcessBEFTNTxn", "ERROR!, BEFTN Fund transfer Failed.."); }
                                    }

                                    
                                //} // balance check ELSE

                            }
                            else
                            {
                                RejectBEFTNDueToInvalidRoutingNumber(userId, refNo, routingNumber);
                            }

                        } //else

                    } //if
                }//for

            } //if END

        }

        private static void RejectBEFTNDueToDuplicateICNumber(string userId, string refNo, string msgValue)
        {
            try
            {
                mg.UpdateTxnStatusIntoTable(refNo, "Error", downloadUser, msgValue);
                Console.WriteLine("GCC_Number -> " + refNo + ", " + msgValue);

                if (IS_INSERT_TO_LOG_TABLE)
                { mg.InsertAutoFetchLog(userId, "RejectBEFTNDueToDuplicateICNumber", "RefNo=" + refNo + ", BEFTN Txn Update at DB Complete.."); }
            }
            catch (Exception ex)
            {
                if (IS_INSERT_TO_LOG_TABLE)
                { mg.InsertAutoFetchLog(userId, "RejectBEFTNDueToDuplicateICNumber", "RefNo=" + refNo + ", RejectBEFTNDueToDuplicateICNumber Error: " + ex); }
            }
        }

        private static void RejectBEFTNDueToInvalidRoutingNumber(string userId, string refNo, string routingNumber)
        {
            try
            {
                mg.UpdateTxnStatusIntoTable(refNo, "Error", downloadUser, "Invalid BankBranch Code");
                Console.WriteLine("GCC_Number -> " + refNo + "  Invalid BankBranch Code");

                if (IS_INSERT_TO_LOG_TABLE)
                { mg.InsertAutoFetchLog(userId, "RejectBEFTNDueToInvalidRoutingNumber", "RefNo=" + refNo + ", BEFTN Txn Update at DB Complete.."); }
            }
            catch (Exception ex)
            {
                if (IS_INSERT_TO_LOG_TABLE)
                { mg.InsertAutoFetchLog(userId, "RejectBEFTNDueToInvalidRoutingNumber", "RefNo=" + refNo + ", RejectBEFTNDueToInvalidRoutingNumber Error: " + ex); }
            }
        }

        private static DataTable GetGCCBEFTNAccData(string frmDate1, string frmDate2)
        {
            DataTable remittanceData = new DataTable();
            try
            {
                remittanceData = mg.GetGccBEFTNRemittanceDetailsByDate(userId, frmDate1, frmDate2);
            }
            catch (Exception exc)
            {
                if (IS_INSERT_TO_LOG_TABLE)
                { mg.InsertAutoFetchLog(userId, "ProcessBEFTNTxn", "ERROR: GetGCCBEFTNAccData, " + exc.ToString()); }
            }
            return remittanceData;
        }

        private static void DownloadAccountTxn()
        {
            int recordCount = 0;
            int confirmCount = 0;
            ProcessTransResponse procTranResp = new ProcessTransResponse();

            try
            {
                BankDepositResponse acDownloadResponse = gccclient.BankDepositTxn(Definitions.Values.GCCSecurityCode);

                if (acDownloadResponse.ResponseCode.Equals("001"))
                {
                    Console.WriteLine("No of Txn: "+acDownloadResponse.dsTransactionList.Length);
                    if (IS_INSERT_TO_LOG_TABLE)
                    { mg.InsertAutoFetchLog(userId, "DownloadAccountTxn", "No of Txn: " + acDownloadResponse.dsTransactionList.Length + ""); }

                    foreach (DtResultSet acctxn in acDownloadResponse.dsTransactionList)
                    {
                        try
                        {
                            bool isSaved = mg.InsertIntoGCCDataTable(acctxn, acDownloadResponse.ResponseCode, acDownloadResponse.ResponseMessage, acDownloadResponse.Successful, downloadBranch, downloadUser);
                            Console.WriteLine(acctxn.TransactionNo + " -> saved into DB: " + isSaved);

                            if (isSaved)
                            {
                                recordCount++;
                                procTranResp = gccclient.ProcessBankDepositTxn(Definitions.Values.GCCSecurityCode, acctxn.TransactionNo);

                                if (procTranResp.ResponseCode.Equals("001") && procTranResp.Successful.ToLower().Equals("true"))
                                {
                                    confirmCount++;
                                    mg.UpdateTxnStatusIntoTable(acctxn.TransactionNo, "Processed", downloadUser, "");
                                    Console.WriteLine("GCC_Number -> " + acctxn.TransactionNo + "  Processed OK. >> " + DateTime.Now);

                                    if (IS_INSERT_TO_LOG_TABLE)
                                    { mg.InsertAutoFetchLog(userId, "DownloadAccountTxn", "GCC_Number -> " + acctxn.TransactionNo + " Processed OK."); }
                                }
                                else
                                {
                                    if (IS_INSERT_TO_LOG_TABLE)
                                    { mg.InsertAutoFetchLog(userId, "DownloadAccountTxn", acctxn.TransactionNo + ", ERROR! Process Mark.. into GCC End."); }
                                }
                            }
                            else
                            {
                                if (IS_INSERT_TO_LOG_TABLE)
                                { mg.InsertAutoFetchLog(userId, "DownloadAccountTxn", acctxn.TransactionNo + ", ERROR! SAVING.. InsertIntoGCCDataTable."); }
                            }
                        }
                        catch (Exception excp)
                        {
                            if (IS_INSERT_TO_LOG_TABLE)
                            { mg.InsertAutoFetchLog(userId, "DownloadAccountTxn", "ERROR! InsertIntoGCCDataTable " + excp.ToString()); }
                        }
                    } // foreach END

                    if (recordCount > 0 && confirmCount > 0)
                    {
                        if (IS_INSERT_TO_LOG_TABLE)
                        { mg.InsertAutoFetchLog(userId, "DownloadAccountTxn", "Download RecordCount=" + recordCount + " , Download Mark=" + confirmCount); }
                    }
                    else
                    {
                        if (IS_INSERT_TO_LOG_TABLE)
                        { mg.InsertAutoFetchLog(userId, "DownloadAccountTxn", "procTranResp=" + procTranResp.ResponseCode + ", " + procTranResp.ResponseMessage); }
                    }

                } //acDownloadResponse.ResponseCode.Equals("001") END
                else
                {
                    string errCd = acDownloadResponse.ResponseCode;
                    string errMs = acDownloadResponse.ResponseMessage;

                    Console.WriteLine("DownloadAccountTxn -> " + errCd + " : " + errMs);
                    if (IS_INSERT_TO_LOG_TABLE)
                    { mg.InsertAutoFetchLog(userId, "DownloadAccountTxn", "Outstanding Remittance RESPONSE=" + errCd + " , " + errMs); }
                }

            }
            catch (Exception exc)
            {
                Console.WriteLine("DownloadAccountTxn -> ERROR! gccclient Download: " + exc.ToString());
                if (IS_INSERT_TO_LOG_TABLE)
                { mg.InsertAutoFetchLog(userId, "DownloadAccountTxn", "ERROR! gccclient Download " + exc.ToString()); }
            }

        }//DownloadAccountTxn()


    }
}
