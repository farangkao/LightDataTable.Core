using Generic.LightDataTable.Transaction;

namespace test.Modules.Standard.Data
{
    // this is a repository for mssql
    public class Repository : TransactionData
    {
        public Repository(string appSettingsOrSqlConnectionString = "MSSQLDbconnection") : base(appSettingsOrSqlConnectionString, true)
        {
        }

    }
}
