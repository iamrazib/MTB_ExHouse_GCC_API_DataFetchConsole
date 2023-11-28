using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCCDataAutoFetchConsole.DBUtility
{
    public class MTBDBManager
    {
        public enum DatabaseType
        {
            Oracle,
            SqlServer
        }

        private IDBManager _dataBaseManager;

        public MTBDBManager(DatabaseType databaseType)
        {
            if (databaseType == DatabaseType.SqlServer)
            {
                _dataBaseManager = new SQLDBManager();
            }
            else if (databaseType == DatabaseType.Oracle)
            {
                // _dataBaseManager = new OracleDBManager();
            }
        }

        public MTBDBManager(DatabaseType databaseType, string connectionString)
        {
            if (databaseType == DatabaseType.SqlServer)
            {
                _dataBaseManager = new SQLDBManager();
            }
            else if (databaseType == DatabaseType.Oracle)
            {
                //_dataBaseManager = new OracleDBManager();
            }

            _dataBaseManager.SetConnectionString = connectionString;
        }

        public string SetConnectionString
        {
            set { _dataBaseManager.SetConnectionString = value; }
        }

        public string UserName
        {
            get { return _dataBaseManager.UserName; }
            set { _dataBaseManager.UserName = value; }
        }

        public string DatabaseName
        {
            get { return _dataBaseManager.DatabaseName; }
            set { _dataBaseManager.DatabaseName = value; }
        }

        public string ServerName
        {
            get { return _dataBaseManager.ServerName; }
            set { _dataBaseManager.ServerName = value; }
        }

        public string Password
        {
            set { _dataBaseManager.Password = value; }
        }

        public void BuildConnectionString()
        {
            try
            {
                _dataBaseManager.BuildConnectionString();
            }
            catch (Exception exception)
            {
                throw exception;
            }

        }

        public void OpenDatabaseConnection()
        {
            try
            {
                _dataBaseManager.OpenDatabaseConnection();
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        public void CloseDatabaseConnection()
        {
            try
            {
                _dataBaseManager.CloseDatabaseConnection();
            }
            catch (Exception exception)
            {
                throw exception;
            }

        }

        public System.Data.DataTable GetDataTable(string query)
        {
            try
            {
                return _dataBaseManager.GetDataTable(query);
            }
            catch (Exception exception)
            {
                throw exception;
            }

        }

        public string GetSingleString(string query)
        {
            try
            {
                return _dataBaseManager.GetSingleString(query);
            }
            catch (Exception exception)
            {
                throw exception;
            }

        }

        public bool ExcecuteCommand(string commandText)
        {
            try
            {
                return _dataBaseManager.ExcecuteCommand(commandText);
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        public void BeginTransaction()
        {
            try
            {
                _dataBaseManager.BeginTransaction();
            }
            catch (Exception exception)
            {
                throw exception;
            }

        }

        public void Commit()
        {
            try
            {
                _dataBaseManager.Commit();
            }
            catch (Exception exception)
            {
                throw exception;
            }

        }

        public void RollBack()
        {
            try
            {
                _dataBaseManager.RollBack();
            }
            catch (Exception exception)
            {
                throw exception;
            }

        }

        public string GetResponse()
        {
            return _dataBaseManager.GetResponse();
        }

    }
}
