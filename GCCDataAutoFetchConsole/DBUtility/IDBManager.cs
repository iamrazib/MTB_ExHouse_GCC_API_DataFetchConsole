using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCCDataAutoFetchConsole.DBUtility
{
    public interface IDBManager
    {
        string SetConnectionString { set; }
        string UserName { get; set; }
        string DatabaseName { get; set; }
        string ServerName { get; set; }
        string Password { set; }
        void BuildConnectionString();
        void OpenDatabaseConnection();
        void CloseDatabaseConnection();
        DataTable GetDataTable(string query);
        string GetSingleString(string query);
        bool ExcecuteCommand(string commandText);
        void BeginTransaction();
        void Commit();
        void RollBack();

        //for test purpose
        string GetResponse();
    }
}
