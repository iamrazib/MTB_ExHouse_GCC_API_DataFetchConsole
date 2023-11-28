using GCCDataAutoFetchConsole.DBUtility;
using GCCDataAutoFetchConsole.Global;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace GCCDataAutoFetchConsole
{
    class Manager
    {
        public string remittanceConnectionString = Utility.DecryptString(ConfigurationManager.ConnectionStrings["RemittanceDBConnectionString"].ConnectionString.Trim());
        MTBDBManager dbManager = null;

        public void InsertAutoFetchLog(string userId, string methodName, string responseMessage)
        {
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, remittanceConnectionString);
                dbManager.OpenDatabaseConnection();

                string query = "INSERT INTO [RemittanceDB].[dbo].[APIDataAutoFetchLog]([UserId],[MethodName],[ResponseMessage]) VALUES('" + userId + "', '" + methodName + "', '" + responseMessage + "')";
                bool status = dbManager.ExcecuteCommand(query);
            }
            catch (Exception ex)
            {
                //throw ex;
            }
            finally
            {
                dbManager.CloseDatabaseConnection();
            }
        }

        internal bool InsertIntoGCCDataTable(GCCServiceClient.DtResultSet acctxn, string responseCode, string responseMessage, string successful, string downloadBranch, string downloadUser)
        {
            SqlConnection openCon = new SqlConnection(remittanceConnectionString);
            SqlCommand cmdSaveAcData = new SqlCommand();

            if (openCon.State.Equals(ConnectionState.Closed))
            {
                openCon.Open();
            }

            cmdSaveAcData.CommandType = CommandType.StoredProcedure;
            cmdSaveAcData.CommandText = "GCCSpInsertAccountAndCashTxnData";
            cmdSaveAcData.Connection = openCon;

            cmdSaveAcData.Parameters.Add("@TransactionNo", SqlDbType.VarChar).Value = acctxn.TransactionNo.Trim();
            cmdSaveAcData.Parameters.Add("@AmountToPay", SqlDbType.Float).Value = Math.Round(Convert.ToDouble(acctxn.PayoutAmount), 2);
            cmdSaveAcData.Parameters.Add("@ResponseCode", SqlDbType.VarChar).Value = responseCode == null ? "" : responseCode.Trim();
            cmdSaveAcData.Parameters.Add("@ResponseMessage", SqlDbType.VarChar).Value = responseMessage == null ? "" : responseMessage.Trim();
            cmdSaveAcData.Parameters.Add("@Status", SqlDbType.VarChar).Value = acctxn.Status == null ? "" : acctxn.Status.Trim();
            cmdSaveAcData.Parameters.Add("@Successful", SqlDbType.VarChar).Value = successful == null ? "" : successful.Trim();
            cmdSaveAcData.Parameters.Add("@TransactionDate", SqlDbType.VarChar).Value = acctxn.ValueDate == null ? "" : acctxn.ValueDate.ToString();
            cmdSaveAcData.Parameters.Add("@ReceiveCountryCode", SqlDbType.VarChar).Value = acctxn.PayoutCountryCode == null ? "" : acctxn.PayoutCountryCode.Trim();
            cmdSaveAcData.Parameters.Add("@ReceiveCountryName", SqlDbType.VarChar).Value = acctxn.PayoutCountryName == null ? "" : acctxn.PayoutCountryName.Trim();
            cmdSaveAcData.Parameters.Add("@ReceiveCurrencyCode", SqlDbType.VarChar).Value = acctxn.PayoutCurrencyCode == null ? "" : acctxn.PayoutCurrencyCode.Trim();
            cmdSaveAcData.Parameters.Add("@PurposeName", SqlDbType.VarChar).Value = acctxn.Purpose == null ? "" : acctxn.Purpose.Trim();
            cmdSaveAcData.Parameters.Add("@ReceiverName", SqlDbType.VarChar).Value = (acctxn.ReceiverFirstName.Trim() + " " + acctxn.ReceiverMiddleName.Trim() + acctxn.ReceiverLastName.Trim()).Trim();
            cmdSaveAcData.Parameters.Add("@ReceiverNationality", SqlDbType.VarChar).Value = acctxn.ReceiverNationality == null ? "" : acctxn.ReceiverNationality.Trim();
            cmdSaveAcData.Parameters.Add("@ReceiverAddress", SqlDbType.VarChar).Value = acctxn.PayoutCountryName == null ? "" : acctxn.PayoutCountryName.Trim();
            cmdSaveAcData.Parameters.Add("@ReceiverCity", SqlDbType.VarChar).Value = acctxn.PayoutCityName == null ? "" : acctxn.PayoutCityName.Trim();
            cmdSaveAcData.Parameters.Add("@ReceiverContactNo", SqlDbType.VarChar).Value = acctxn.ReceiverContactNo == null ? "" : acctxn.ReceiverContactNo.Trim();
            cmdSaveAcData.Parameters.Add("@SenderAddress", SqlDbType.VarChar).Value = acctxn.SenderAddress == null ? "" : acctxn.SenderAddress.Trim();
            cmdSaveAcData.Parameters.Add("@SendCountryCode", SqlDbType.VarChar).Value = acctxn.PayinCountryCode == null ? "" : acctxn.PayinCountryCode.Trim();
            cmdSaveAcData.Parameters.Add("@SendCountryName", SqlDbType.VarChar).Value = acctxn.PayinCountryName == null ? "" : acctxn.PayinCountryName.Trim();
            cmdSaveAcData.Parameters.Add("@SenderCity", SqlDbType.VarChar).Value = "";
            cmdSaveAcData.Parameters.Add("@SenderContactNo", SqlDbType.VarChar).Value = acctxn.SenderContactNo == null ? "" : acctxn.SenderContactNo.Trim();
            cmdSaveAcData.Parameters.Add("@SenderName", SqlDbType.VarChar).Value = (acctxn.SenderFirstName.Trim() + "" + acctxn.SenderMiddleName.Trim() + " " + acctxn.SenderLastName.Trim()).Trim();
            cmdSaveAcData.Parameters.Add("@SenderNationality", SqlDbType.VarChar).Value = acctxn.SenderNationality == null ? "" : acctxn.SenderNationality.Trim();
            cmdSaveAcData.Parameters.Add("@SenderIncomeSource", SqlDbType.VarChar).Value = acctxn.SenderIncomeSource == null ? "" : acctxn.SenderIncomeSource.Trim();
            cmdSaveAcData.Parameters.Add("@SenderOccupation", SqlDbType.VarChar).Value = acctxn.SenderOccupation == null ? "" : acctxn.SenderOccupation.Trim();
            cmdSaveAcData.Parameters.Add("@SenderIDExpiryDate", SqlDbType.VarChar).Value = acctxn.SenderIDExpiryDate == null ? "" : acctxn.SenderIDExpiryDate.Trim();
            cmdSaveAcData.Parameters.Add("@SenderIDNumber", SqlDbType.VarChar).Value = acctxn.SenderIDNumber == null ? "" : acctxn.SenderIDNumber.Trim();
            cmdSaveAcData.Parameters.Add("@SenderIDPlaceOfIssue", SqlDbType.VarChar).Value = acctxn.SenderIDPlaceOfIssue == null ? "" : acctxn.SenderIDPlaceOfIssue.Trim();
            cmdSaveAcData.Parameters.Add("@SenderIDTypeName", SqlDbType.VarChar).Value = acctxn.SenderIDType == null ? "" : acctxn.SenderIDType.Trim();
            cmdSaveAcData.Parameters.Add("@TxnReceiveDate", SqlDbType.DateTime).Value = DateTime.Now;
            cmdSaveAcData.Parameters.Add("@TxnStatus", SqlDbType.VarChar).Value = "RECEIVED";

            cmdSaveAcData.Parameters.Add("@BankAccountNo", SqlDbType.VarChar).Value = acctxn.BankAccountNo == null ? "" : acctxn.BankAccountNo.Trim();
            cmdSaveAcData.Parameters.Add("@BankName", SqlDbType.VarChar).Value = acctxn.BankName == null ? "" : acctxn.BankName.Trim();
            cmdSaveAcData.Parameters.Add("@BankBranchName", SqlDbType.VarChar).Value = acctxn.BankBranchName == null ? "" : acctxn.BankBranchName.Trim();
            cmdSaveAcData.Parameters.Add("@BankBranchCode", SqlDbType.VarChar).Value = acctxn.BankBranchCode == null ? "" : acctxn.BankBranchCode.Trim();
            cmdSaveAcData.Parameters.Add("@SentDate", SqlDbType.DateTime).Value = acctxn.SentDate;
            cmdSaveAcData.Parameters.Add("@ValueDate", SqlDbType.DateTime).Value = acctxn.ValueDate;
            cmdSaveAcData.Parameters.Add("@PayinCurrencyCode", SqlDbType.VarChar).Value = acctxn.PayinCurrencyCode == null ? "" : acctxn.PayinCurrencyCode.Trim();
            cmdSaveAcData.Parameters.Add("@PayinAmount", SqlDbType.Float).Value = acctxn.PayinAmount == null ? 0 : Math.Round(Convert.ToDouble(acctxn.PayinAmount), 2);
            cmdSaveAcData.Parameters.Add("@ExchangeRate", SqlDbType.Float).Value = acctxn.ExchangeRate == null ? 0 : Math.Round(Convert.ToDouble(acctxn.ExchangeRate), 2);
            cmdSaveAcData.Parameters.Add("@GccPayMode", SqlDbType.VarChar).Value = acctxn.PaymentMode == null ? "" : acctxn.PaymentMode.Trim();

            if (acctxn.PaymentMode.Equals("CTA"))
            {
                if (acctxn.BankName.ToUpper().Contains("MUTUAL") && acctxn.BankName.ToUpper().Contains("TRUST") && acctxn.BankBranchCode.StartsWith("145"))
                {
                    cmdSaveAcData.Parameters.Add("@PaymentMode", SqlDbType.VarChar).Value = "OWNBANK";
                }
                else if (!acctxn.BankName.ToUpper().Contains("MUTUAL") && !acctxn.BankBranchCode.StartsWith("145"))
                {
                    cmdSaveAcData.Parameters.Add("@PaymentMode", SqlDbType.VarChar).Value = "BEFTN";
                }
                else
                {
                    cmdSaveAcData.Parameters.Add("@PaymentMode", SqlDbType.VarChar).Value = "";
                }
            }
            else if (acctxn.PaymentMode.Equals("MBW"))
            {
                cmdSaveAcData.Parameters.Add("@PaymentMode", SqlDbType.VarChar).Value = "BKASH";
            }
            else
            {
                cmdSaveAcData.Parameters.Add("@PaymentMode", SqlDbType.VarChar).Value = "";
            }

            cmdSaveAcData.Parameters.Add("@DownloadBranch", SqlDbType.VarChar).Value = downloadBranch;
            cmdSaveAcData.Parameters.Add("@Remarks", SqlDbType.VarChar).Value = "";

            try
            {
                int k = cmdSaveAcData.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                InsertAutoFetchLog(Definitions.Values.GCCUserId, "InsertIntoGCCDataTable", "Error ! InsertIntoGCCDataTable" + ex.ToString());
                return false;
            }
            return true;
        }



        internal void UpdateTxnStatusIntoTable(string TransactionNo, string confirmFlag, string downloadUser, string remarks)
        {
            try
            {
                string query = "";
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, remittanceConnectionString);
                dbManager.OpenDatabaseConnection();

                if (confirmFlag.Equals("Processed"))
                {
                    query = "UPDATE [RemittanceDB].[dbo].[GCCRequestData] SET [Status]='" + confirmFlag + "', [TxnStatus]='PROCESSED'  WHERE [TransactionNo]='" + TransactionNo + "'";
                }
                else if (confirmFlag.Equals("Paid"))
                {
                    query = "UPDATE [RemittanceDB].[dbo].[GCCRequestData] SET [Status]='" + confirmFlag + "', [TxnStatus]='PAID',  [TxnPaymentDate]=getdate(), [ClearingUser]='" + downloadUser + "', [ClearingDate]=getdate()  WHERE [TransactionNo]='" + TransactionNo + "'";
                }
                else  // Error
                {
                    query = "UPDATE [RemittanceDB].[dbo].[GCCRequestData] SET [Status]='Error', [TxnStatus]='CANCELLED', [Remarks]='" + remarks + "', [CancelDate]=getdate()  WHERE [TransactionNo]='" + TransactionNo + "'";
                }

                bool status = dbManager.ExcecuteCommand(query);

            }
            catch (Exception ex)
            {
                //throw ex;
            }
            finally
            {
                dbManager.CloseDatabaseConnection();
            }
        }

        internal DataTable GetGccBEFTNRemittanceDetailsByDate(string userId, string fromDate, string toDate)
        {
            DataTable dt = new DataTable();
            string sqlQuery = string.Empty;
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, remittanceConnectionString);
                dbManager.OpenDatabaseConnection();

                sqlQuery = "SELECT [AutoId],[TransactionNo],[AmountToPay],[ResponseCode],[ResponseMessage],[Status],[Successful],[TransactionDate],[ReceiveCountryCode]"
                    +" ,[ReceiveCountryName],[ReceiveCurrencyCode],[PurposeName],[ReceiverName],[ReceiverNationality],[ReceiverAddress],[ReceiverCity],[ReceiverContactNo]"
                    +" ,[SenderAddress],[SendCountryCode],[SendCountryName],[SenderCity],[SenderContactNo],[SenderName],[SenderNationality],[SenderIncomeSource]"
                    +" ,[SenderOccupation],[BankAccountNo],[BankName],[BankBranchName],[BankBranchCode],[SentDate],[ValueDate],[PayinCurrencyCode],[PayinAmount]"
                    +" ,[ExchangeRate],[TxnReceiveDate],[TxnStatus],[PaymentMode] "
                    +" FROM [RemittanceDB].[dbo].[GCCRequestData] "
                    +" WHERE [PaymentMode]='BEFTN' AND [TxnStatus]='PROCESSED' AND [Status]='Processed' AND [ResponseCode]='001' "
                    //+ " AND convert(date,[TxnReceiveDate])>='" + fromDate + "' AND  convert(date,[TxnReceiveDate])<='" + toDate + "'"
	                +" ORDER BY [AutoId]";    

                dt = dbManager.GetDataTable(sqlQuery.Trim());
            }
            catch (Exception exception)
            {
                InsertAutoFetchLog(userId, "GetGccBEFTNRemittanceDetailsByDate", "Error ! GetGccBEFTNTxn fetch Error. " + ", " + exception.ToString());
            }
            finally
            {
                dbManager.CloseDatabaseConnection();
            }
            return dt;
        }


        internal DataTable GetBeftnRemitInfo(string userId, string refNo)
        {
            DataTable dt = new DataTable();
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, remittanceConnectionString);
                dbManager.OpenDatabaseConnection();
                string sqlQuery = "select * from [RemittanceDB].[dbo].[BEFTNRequest] where [RefNo]='" + refNo.Trim() + "' AND [PartyId]=" + Definitions.Values.GCCPartyID;
                dt = dbManager.GetDataTable(sqlQuery.Trim());
            }
            catch (Exception ex)
            {
                InsertAutoFetchLog(userId, "GetBeftnRemitInfo", "Error ! GetBeftnRemitInfo fetch Error." + ex.ToString());
            }
            finally
            {
                dbManager.CloseDatabaseConnection();
            }
            return dt;
        }

        internal DataTable GetExchangeHouseAccountNo(string userId, string ExchangeHouseCode)
        {
            DataTable dt = new DataTable();
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, remittanceConnectionString);
                dbManager.OpenDatabaseConnection();
                dt = dbManager.GetDataTable("SELECT * FROM [RemittanceDB].[dbo].[Users] WHERE PartyId='" + ExchangeHouseCode + "' ");
            }
            catch (Exception ex)
            {
                InsertAutoFetchLog(userId, "GetExchangeHouseAccountNo", "Error ! GetExchangeHouseAccountNo" + ex.ToString());
            }
            finally
            {
                dbManager.CloseDatabaseConnection();
            }
            return dt;
        }
        
        internal string GetBranchNameByRoutingCode(string userId, string routingNumber)
        {
            DataTable dt = new DataTable();
            string branchName = "";
            string sqlQuery = string.Empty;
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, remittanceConnectionString);
                dbManager.OpenDatabaseConnection();

                sqlQuery = "SELECT [MTB Sl No],[MTB Code],[Bank Code],[Agent Name],[Branch Name],[City Name],[District],[Routing Number],[Country] "
                    + " FROM [RemittanceDB].[dbo].[BANK_BRANCH] WHERE [Routing Number]='" + routingNumber + "'";
                dt = dbManager.GetDataTable(sqlQuery.Trim());

                branchName = dt.Rows[0]["Branch Name"].ToString();
            }
            catch (Exception exception)
            {
                InsertAutoFetchLog(userId, "GetBranchNameByRoutingCode", "Error ! GetBranchNameByRoutingCode fetch Error. " + ", " + exception.ToString());
            }
            finally
            {
                dbManager.CloseDatabaseConnection();
            }
            return branchName;
        }


        internal DataTable GetGccMTBRemittanceDetailsByDate(string userId, string fromDate, string toDate)
        {
            DataTable dt = new DataTable();
            string sqlQuery = string.Empty;
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, remittanceConnectionString);
                dbManager.OpenDatabaseConnection();

                sqlQuery = "SELECT [AutoId],[TransactionNo],[AmountToPay],[ResponseCode],[ResponseMessage],[Status],[Successful],[TransactionDate],[ReceiveCountryCode]"
                    + " ,[ReceiveCountryName],[ReceiveCurrencyCode],[PurposeName],[ReceiverName],[ReceiverNationality],[ReceiverAddress],[ReceiverCity],[ReceiverContactNo]"
                    + " ,[SenderAddress],[SendCountryCode],[SendCountryName],[SenderCity],[SenderContactNo],[SenderName],[SenderNationality],[SenderIncomeSource]"
                    + " ,[SenderOccupation],[BankAccountNo],[BankName],[BankBranchName],[BankBranchCode],[SentDate],[ValueDate],[PayinCurrencyCode],[PayinAmount]"
                    + " ,[ExchangeRate],[TxnReceiveDate],[TxnStatus],[PaymentMode] "
                    + " FROM [RemittanceDB].[dbo].[GCCRequestData] "
                    + " WHERE [PaymentMode]='OWNBANK' AND [TxnStatus]='PROCESSED' AND [Status]='Processed' AND [ResponseCode]='001' "
                    //+ " AND convert(date,[TxnReceiveDate])>='" + fromDate + "' AND  convert(date,[TxnReceiveDate])<='" + toDate + "'"
                    + " ORDER BY [AutoId]";

                dt = dbManager.GetDataTable(sqlQuery.Trim());
            }
            catch (Exception exception)
            {
                InsertAutoFetchLog(userId, "GetGccMTBRemittanceDetailsByDate", "Error ! GetGccMTBTxn fetch Error. " + ", " + exception.ToString());
            }
            finally
            {
                dbManager.CloseDatabaseConnection();
            }
            return dt;
        }


        internal DataTable GetOwnAccountRemitTransferInfo(string userId, string refrnNo)
        {
            DataTable dt = new DataTable();
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, remittanceConnectionString);
                dbManager.OpenDatabaseConnection();
                string sqlQuery = "select * from [RemittanceDB].[dbo].[FundTransferRequest] where [RefNo]='" + refrnNo.Trim() + "'";
                dt = dbManager.GetDataTable(sqlQuery.Trim());
            }
            catch (Exception ex)
            {
                InsertAutoFetchLog(userId, "GetOwnAccountRemitTransferInfo", "Error ! OwnAccountInfo fetch Error." + ex.ToString());
            }
            finally
            {
                dbManager.CloseDatabaseConnection();
            }
            return dt;
        }

        internal DataTable GetGCCOwnBankRemitData(string userId, string autoIDgcc)
        {
            DataTable dt = new DataTable();
            string sqlQuery = string.Empty;
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, remittanceConnectionString);
                dbManager.OpenDatabaseConnection();

                sqlQuery = "SELECT [AutoId],[TransactionNo],[AmountToPay],[ResponseCode],[ResponseMessage],[Status],[Successful],[TransactionDate],[ReceiveCountryCode]"
                    + " ,[ReceiveCountryName],[ReceiveCurrencyCode],[PurposeName],[ReceiverName],[ReceiverNationality],[ReceiverAddress],[ReceiverCity],[ReceiverContactNo]"
                    + " ,[SenderAddress],[SendCountryCode],[SendCountryName],[SenderCity],[SenderContactNo],[SenderName],[SenderNationality],[SenderIncomeSource]"
                    + " ,[SenderOccupation],[BankAccountNo],[BankName],[BankBranchName],[BankBranchCode],[SentDate],[ValueDate],[PayinCurrencyCode],[PayinAmount]"
                    + " ,[ExchangeRate],[TxnReceiveDate],[TxnStatus],[PaymentMode] "
                    + " FROM [RemittanceDB].[dbo].[GCCRequestData] "
                    + " WHERE [AutoId]='" + autoIDgcc + "' AND [PaymentMode]='OWNBANK' AND [TxnStatus]='PROCESSED' AND [Status]='Processed' AND [ResponseCode]='001' ";

                dt = dbManager.GetDataTable(sqlQuery.Trim());
            }
            catch (Exception exception)
            {
                InsertAutoFetchLog(userId, "GetGCCOwnBankRemitData", "Error ! GetGccMTBTxn fetch Error. " + ", " + exception.ToString());
            }
            finally
            {
                dbManager.CloseDatabaseConnection();
            }
            return dt;
        }

        internal void MarkOwnBankTxnCancelled(string userId, string autoIDgcc, string refNo, string msgValue)
        {
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, remittanceConnectionString);
                dbManager.OpenDatabaseConnection();
                string sqlQuery = "UPDATE [RemittanceDB].[dbo].[GCCRequestData] SET [TxnStatus]='CANCELLED', [Remarks]='" + msgValue + "', [CancelDate]=getdate() "
                + " WHERE [TransactionNo]='" + refNo + "' AND [AutoId]=" + Convert.ToInt32(autoIDgcc);

                bool status = dbManager.ExcecuteCommand(sqlQuery);
            }
            catch (Exception ex)
            {
                InsertAutoFetchLog(userId, "MarkOwnBankTxnCancelled", "Error ! MarkOwnBankTxnCancelled Error." + ex.ToString());
            }
            finally
            {
                dbManager.CloseDatabaseConnection();
            }
        }



        internal DataTable GetGccBkashData(string userId, string frmDate1, string frmDate2)
        {
            DataTable dt = new DataTable();
            string sqlQuery = string.Empty;
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, remittanceConnectionString);
                dbManager.OpenDatabaseConnection();

                sqlQuery = "SELECT [AutoId],[TransactionNo],[AmountToPay],[TransactionDate],[ReceiveCountryCode],[ReceiveCountryName],[ReceiveCurrencyCode],[PurposeName],[ReceiverName],[ReceiverNationality],[ReceiverAddress] "
                    + " ,[ReceiverCity],[ReceiverContactNo],[SenderAddress],[SendCountryCode],[SendCountryName],[SenderCity],[SenderContactNo],[SenderName],[SenderNationality],[SenderIncomeSource],[SenderOccupation] "
                    + " ,[SenderIDExpiryDate],[SenderIDNumber],[SenderIDPlaceOfIssue],[SenderIDTypeName],[SentDate],[ValueDate],[PayinCurrencyCode],[PayinAmount],[ExchangeRate],[TxnReceiveDate],[TxnStatus],[PaymentMode] "
                    + " FROM [RemittanceDB].[dbo].[GCCRequestData] WHERE PaymentMode='BKASH' and TxnStatus='PROCESSED' AND [ResponseCode]='001' ";

                dt = dbManager.GetDataTable(sqlQuery.Trim());
            }
            catch (Exception exception)
            {
                InsertAutoFetchLog(userId, "GetGccBkashData", "Error ! GetGccBkashData fetch Error. " + ", " + exception.ToString());
            }
            finally
            {
                dbManager.CloseDatabaseConnection();
            }
            return dt;
        }

        internal DataTable GetMobileWalletRemitTransferInfo(string userId, string refNo)
        {
            DataTable dt = new DataTable();
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, remittanceConnectionString);
                dbManager.OpenDatabaseConnection();
                string sqlQuery = "SELECT * FROM [RemittanceDB].[dbo].[MobileWalletRemitTransfer] where [TranTxnId] LIKE '" + refNo.Trim() + "%'";
                dt = dbManager.GetDataTable(sqlQuery.Trim());
            }
            catch (Exception ex)
            {
                InsertAutoFetchLog(userId, "GetMobileWalletRemitTransferInfo", "Error ! GetMobileWalletRemitTransferInfo fetch Error. RefNo=" + refNo.Trim() + ", " + ex.ToString());
            }
            finally
            {
                dbManager.CloseDatabaseConnection();
            }
            return dt;
        }

        internal void MarkBkashTxnCancelled(string userId, string autoIDgcc, string refNo, string msgValue)
        {
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, remittanceConnectionString);
                dbManager.OpenDatabaseConnection();
                string sqlQuery = "UPDATE [RemittanceDB].[dbo].[GCCRequestData] SET [TxnStatus]='CANCELLED', [Remarks]='" + msgValue + "' , [CancelDate]=getdate() "
                + " WHERE [TransactionNo]='" + refNo + "' AND AutoId=" + Convert.ToInt32(autoIDgcc);

                bool status = dbManager.ExcecuteCommand(sqlQuery);
            }
            catch (Exception ex)
            {
                InsertAutoFetchLog(userId, "MarkBkashTxnCancelled", "Error ! MarkBkashTxnCancelled Error." + ex.ToString());
            }
            finally
            {
                dbManager.CloseDatabaseConnection();
            }
        }

        internal void MarkBkashTxnHOLD(string userId, string autoIDgcc, string refNo)
        {
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, remittanceConnectionString);
                dbManager.OpenDatabaseConnection();
                string sqlQuery = "UPDATE [RemittanceDB].[dbo].[GCCRequestData] SET [TxnStatus]='HOLD', [Remarks]='LOW Balance' , [CancelDate]=getdate() "
                + " WHERE [TransactionNo]='" + refNo + "' AND AutoId=" + Convert.ToInt32(autoIDgcc);

                bool status = dbManager.ExcecuteCommand(sqlQuery);
            }
            catch (Exception ex)
            {
                InsertAutoFetchLog(userId, "MarkBkashTxnHOLD", "Error ! MarkBkashTxnHOLD." + ex.ToString());
            }
            finally
            {
                dbManager.CloseDatabaseConnection();
            }
        }



        internal void UpdateFailedStatusBkashTxn(string userId, string refNo, string autoIDgcc, string remitTransferRespMsg)
        {
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, remittanceConnectionString);
                dbManager.OpenDatabaseConnection();
                string sqlQuery = "UPDATE [RemittanceDB].[dbo].[GCCRequestData] SET [TxnStatus]='ERROR', [Remarks]='" + remitTransferRespMsg + "', [CancelDate]=getdate() "
                + " WHERE [TransactionNo]='" + refNo + "' AND [AutoId]=" + Convert.ToInt32(autoIDgcc);

                bool status = dbManager.ExcecuteCommand(sqlQuery);
            }
            catch (Exception ex)
            {
                InsertAutoFetchLog(userId, "UpdateFailedStatusBkashTxn", "Error ! UpdateFailedStatusBkashTxn Error." + ex.ToString());
            }
            finally
            {
                dbManager.CloseDatabaseConnection();
            }
        }
    }
}
