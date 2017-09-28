using Generic.LightDataTable.Transaction;

namespace test.Modules.Standard.Data
{
    /// <summary>
    /// this is a repository for lite database
    /// </summary>
    public class LiteRepository : TransactionLiteData
    {
        public LiteRepository(string appSettingsOrSqlConnectionString = "Dbconnection") : base(appSettingsOrSqlConnectionString, true)
        {
        }

    }
}
