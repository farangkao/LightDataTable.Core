using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using Generic.LightDataTable.Helper;
using Generic.LightDataTable.Interface;
using Generic.LightDataTable.InterFace;
using Generic.LightDataTable.Library;


namespace Generic.LightDataTable.Transaction
{
    public class TransactionData : ICustomRepository
    {
        /// <summary>
        /// 
        /// </summary>
        public readonly string SqlConnectionStringString;
        /// <summary>
        /// Created sqlConnection
        /// </summary>
        protected SqlConnection SqlConnection { get; private set; }
        internal SqlTransaction Trans { get; private set; }
        private static bool _assLoaded;
        private static bool _tableMigrationCheck;
        private static IList<Migration> Migrations { get; set; }

        /// <summary>
        /// When enabled, LightDataTable will execute all avaiable Migration
        /// </summary>
        protected bool EnableMigration { get; private set; }



        private static void LoadPropertyChangedAss()
        {
            if (_assLoaded)
                return;
            const string assemblyName = "ProcessedByFody";
            foreach (var a in Assembly.GetEntryAssembly().DefinedTypes)
            {
                if (a.Name.Contains(assemblyName))
                {

                    _assLoaded = true;
                    return;
                }
            }
            throw new Exception("PropertyChanged.dll and Fody could not be found please install PropertyChanged.Fody and Fody. FodyWeavers.XML should look like <?xml version=\"1.0\" encoding=\"utf - 8\" ?>" +
                Environment.NewLine + "<Weavers>" +
                Environment.NewLine + "<PropertyChanged />" +
                Environment.NewLine + "</Weavers> ");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="appSettingsOrSqlConnectionString">
        /// AppSettingsName that containe the connectionstringName,
        /// OR ConnectionStringName,
        /// OR Full ConnectionString
        /// Default is Dbconnection
        /// </param>
        /// <param name="enableMigration">enable and disable Migrations</param>
        public TransactionData(string appSettingsOrSqlConnectionString = "Dbconnection", bool enableMigration = false)
        {
            EnableMigration = enableMigration;
            LoadPropertyChangedAss();
            if (string.IsNullOrEmpty(appSettingsOrSqlConnectionString))
                if (string.IsNullOrEmpty(SqlConnectionStringString))
                    throw new Exception("appSettingsOrSqlConnectionString cant be empty");

            if (string.IsNullOrEmpty(appSettingsOrSqlConnectionString)) return;

            if (appSettingsOrSqlConnectionString.Contains(";"))
                SqlConnectionStringString = appSettingsOrSqlConnectionString; // its full connectionString

            // set connectionString by appsettings
            if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings[appSettingsOrSqlConnectionString]))
            {
                SqlConnectionStringString = ConfigurationManager
                    .ConnectionStrings[ConfigurationManager.AppSettings[appSettingsOrSqlConnectionString]]
                    .ConnectionString;
            }
            else
            {
                SqlConnectionStringString = ConfigurationManager.ConnectionStrings[appSettingsOrSqlConnectionString].ConnectionString;
            }

            if (!_tableMigrationCheck && EnableMigration)
                DbSchema.CreateTable<DBMigration>(this);
            _tableMigrationCheck = true;
        }

        private void ValidateConnection()
        {
            if (SqlConnection == null)
                SqlConnection = new SqlConnection(SqlConnectionStringString);
            if (SqlConnection.State == ConnectionState.Broken || SqlConnection.State == ConnectionState.Closed)
                SqlConnection.Open();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public SqlTransaction CreateTransaction()
        {
            ValidateConnection();
            if (Trans?.Connection == null)
            {
                Trans = SqlConnection.BeginTransaction();
            }
            return Trans;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public IDataReader ExecuteReader(SqlCommand cmd)
        {
            ValidateConnection();
            return cmd.ExecuteReader();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public object ExecuteScalar(SqlCommand cmd)
        {
            ValidateConnection();
            return cmd.ExecuteScalar();
        }

        public int ExecuteNonQuery(SqlCommand cmd)
        {
            ValidateConnection();
            return cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Roleback transaction
        /// </summary>
        public void Rollback()
        {
            Trans?.Rollback();
            Dispose();

        }

        /// <summary>
        /// commit the transaction
        /// </summary>
        public void Commit()
        {
            Trans?.Commit();
            Dispose();
        }

        /// <summary>
        /// return a list of LightDataTable e.g. DataSet
        /// </summary>
        /// <param name="cmd">sqlCommand that are create from GetSqlCommand</param>
        /// <param name="primaryKeyId"> Table primaryKeyId, so LightDataTable.FindByPrimaryKey could be used </param>
        /// <returns></returns>
        protected List<ILightDataTable> GetLightDataTableList(SqlCommand cmd, string primaryKeyId = null)
        {
            var returnList = new List<ILightDataTable>();
            var reader = ExecuteReader(cmd);
            returnList.Add(new LightDataTable().ReadData(reader, primaryKeyId));

            while (reader.NextResult())
                returnList.Add(new LightDataTable().ReadData(reader, primaryKeyId));
            reader.Close();
            return returnList;
        }

        /// <summary>
        /// Specifie the migrationConfig which containe a list Migration to migrate
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <exception cref="Exception"></exception>
        protected void MigrationConfig<T>() where T : IMigrationConfig
        {
            if (Migrations != null) return;
            try
            {
                var config = Activator.CreateInstance<T>();
                Migrations = config.GetMigrations(this) ?? new List<Migration>();
                this.CreateTransaction();
                foreach (var migration in Migrations)
                {
                    var name = migration.GetType().FullName;
                    var dbMigration = this.Get<DBMigration>().Where(x => x.Name == name).Execute();
                    if (dbMigration.Any())
                        continue;
                    var item = new DBMigration
                    {
                        Name = name,
                        DateCreated = DateTime.Now
                    };
                    var bf = new BinaryFormatter();
                    using (var ms = new MemoryStream())
                    {
                        bf.Serialize(ms, migration);
                        item.MigrationData = ms.ToArray();
                    }
                    migration.ExecuteMigration(this);
                    this.Save(item);
                }
                Commit();
            }
            catch (Exception ex)
            {
                Rollback();
                throw ex;
            }
        }

        /// <summary>
        /// return LightDataTable e.g. DataTable
        /// </summary>
        /// <param name="cmd">sqlCommand that are create from GetSqlCommand</param>
        /// <param name="primaryKey">Table primaryKeyId, so LightDataTable.FindByPrimaryKey could be used </param>
        /// <returns></returns>
        public ILightDataTable GetLightDataTable(SqlCommand cmd, string primaryKey = null)
        {
            ValidateConnection();
            var reader = cmd.ExecuteReader();
            return new LightDataTable().ReadData(reader, primaryKey);
        }

        /// <summary>
        /// SqlDbType by system.Type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public SqlDbType GetSqlType(Type type)
        {
            if (type == typeof(string))
                return SqlDbType.NVarChar;

            if (type.GetTypeInfo().IsGenericType && type.GetTypeInfo().GetGenericTypeDefinition() == typeof(Nullable<>))
                type = Nullable.GetUnderlyingType(type);

            var param = new SqlParameter("", Activator.CreateInstance(type));
            return param.SqlDbType;
        }


        /// <summary>
        /// Add parameters to SqlCommand
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="attrName"></param>
        /// <param name="value"></param>
        /// <param name="dbType"></param>
        public void AddInnerParameter(SqlCommand cmd, string attrName, object value, SqlDbType dbType = SqlDbType.NVarChar)
        {
            if (attrName != null && attrName[0] != '@')
                attrName = "@" + attrName;

            var sqlDbTypeValue = value ?? DBNull.Value;
            var param = cmd.CreateParameter();
            param.SqlDbType = dbType;
            param.Value = sqlDbTypeValue;
            param.ParameterName = attrName;
            cmd.Parameters.Add(param);
        }

        /// <summary>
        /// Return SqlCommand that already containe SQLConnection
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public SqlCommand GetSqlCommand(string sql)
        {
            ValidateConnection();
            return Trans == null ? new SqlCommand(sql, SqlConnection) : new SqlCommand(sql, SqlConnection, Trans);
        }

        public virtual void Dispose()
        {
            Trans?.Dispose();
            SqlConnection?.Dispose();
            Trans = null;
            SqlConnection = null;
        }
    }
}
