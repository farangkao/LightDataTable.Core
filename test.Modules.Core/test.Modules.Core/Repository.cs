using Generic.LightDataTable.Transaction;

namespace Test.Modules.Core
{
    public class Repository : TransactionData
    {
        public Repository(string appSettingsOrSqlConnectionString = "Dbconnection") : base(appSettingsOrSqlConnectionString, true)
        {
        }

    }
}
