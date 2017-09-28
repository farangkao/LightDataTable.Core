﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using Generic.LightDataTable.Interface;
using Generic.LightDataTable.InterFace;
using Generic.LightDataTable.Library;
#if NET461 || NET451 || NET46 
using System.Data.SQLite;
#elif NETCOREAPP2_0 || NETSTANDARD2_0
using Microsoft.Data.Sqlite;
#endif


namespace Generic.LightDataTable.Transaction
{
    /// <inheritdoc />
    public class TransactionLiteData : ICustomRepository
    {

        /// <summary>
        /// 
        /// </summary>
        public readonly string SqlConnectionStringString;
        /// <summary>
        /// Created sqlConnection
        /// </summary>
#if NET461 || NET451 || NET46
        protected SQLiteConnection SqlConnection { get; private set; }
        internal SQLiteTransaction Trans { get; private set; }
#elif NETCOREAPP2_0 || NETSTANDARD2_0
        protected SqliteConnection SqlConnection { get; private set; }
        internal SqliteTransaction Trans { get; private set; }
#endif
        private static bool _assLoaded;
        private static bool _tableMigrationCheck;
        private static IList<Migration> Migrations { get; set; }

        private static IMigrationConfig Config { get; set; }

        /// <summary>
        /// When enabled, Light DataTable will execute all available Migration
        /// </summary>
        protected bool EnableMigration { get; private set; }


        private static void LoadPropertyChangedAss()
        {
            if (_assLoaded)
                return;

            const string assemblyName = "ProcessedByFody";
            if (!Assembly.GetEntryAssembly().DefinedTypes.Any(a => a.Name.Contains(assemblyName)))
                throw new Exception(
                    "PropertyChanged.dll and Fody could not be found please install PropertyChanged.Fody and Fody. FodyWeavers.XML should look like <?xml version=\"1.0\" encoding=\"utf - 8\" ?>" +
                    Environment.NewLine + "<Weavers>" +
                    Environment.NewLine + "<PropertyChanged />" +
                    Environment.NewLine + "</Weavers> ");
            _assLoaded = true;
            return;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="appSettingsOrSqlConnectionString">
        /// AppSettingsName that containe the connectionstringName,
        /// OR ConnectionStringName,
        /// OR Full ConnectionString
        /// Default is Data Source=MyDatabase.sqlite;Version=3;
        /// </param>
        /// <param name="createIfNotExist">Create that database file if it dose not exist</param>
        /// <param name="enableMigration">enable and disable Migrations</param>
        public TransactionLiteData(string appSettingsOrSqlConnectionString = "Dbconnection", bool enableMigration = false)
        {


            EnableMigration = enableMigration;
            LoadPropertyChangedAss();
            if (string.IsNullOrEmpty(appSettingsOrSqlConnectionString))
                if (string.IsNullOrEmpty(SqlConnectionStringString))
                    throw new Exception("appSettingsOrSqlConnectionString cant be empty");


            if (string.IsNullOrEmpty(appSettingsOrSqlConnectionString)) return;

            if (appSettingsOrSqlConnectionString.Contains(";") || appSettingsOrSqlConnectionString.Contains(".sql"))
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
                if (!(appSettingsOrSqlConnectionString.Contains(";") || appSettingsOrSqlConnectionString.Contains(".sql")))
                    SqlConnectionStringString = ConfigurationManager.ConnectionStrings[appSettingsOrSqlConnectionString].ConnectionString;
            }

            if (!_tableMigrationCheck && EnableMigration)
            {
                this.CreateTable<DBMigration>(false);

                if (Assembly.GetEntryAssembly().DefinedTypes.Any(a => typeof(IMigrationConfig).IsAssignableFrom(a)))
                    Config = Activator.CreateInstance(Assembly.GetEntryAssembly().DefinedTypes
                        .First(a => typeof(IMigrationConfig).IsAssignableFrom(a))) as IMigrationConfig;

                MigrationConfig(Config);
            }
            _tableMigrationCheck = true;
        }



        private void ValidateConnection()
        {
#if NET461 || NET451 || NET46
             if (SqlConnection == null)
                SqlConnection = new SQLiteConnection(SqlConnectionStringString);
#elif NETCOREAPP2_0 || NETSTANDARD2_0
            if (SqlConnection == null)
                SqlConnection = new SqliteConnection(SqlConnectionStringString);
#endif
            if (SqlConnection.State == ConnectionState.Broken || SqlConnection.State == ConnectionState.Closed)
                SqlConnection.Open();
        }

        /// <summary>
        /// Create Transaction
        /// Only one Transaction will be created until it get disposed
        /// </summary>
        /// <returns></returns>
        public DbTransaction CreateTransaction()
        {
            ValidateConnection();
            if (Trans?.Connection == null)
                Trans = SqlConnection.BeginTransaction();

            return Trans;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public IDataReader ExecuteReader(DbCommand cmd)
        {
            ValidateConnection();
            return cmd.ExecuteReader();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public object ExecuteScalar(DbCommand cmd)
        {
            ValidateConnection();
            return cmd.ExecuteScalar();
        }

        /// <inheritdoc />
        public int ExecuteNonQuery(DbCommand cmd)
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
        protected List<ILightDataTable> GetLightDataTableList(DbCommand cmd, string primaryKeyId = null)
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
        /// the migration is executed automaticly but as long as you have class that inhert from IMigrationConfig
        /// or you could manully execute a migration
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <exception cref="Exception"></exception>
        protected void MigrationConfig<T>(T config) where T : IMigrationConfig
        {
            if (Migrations != null) return;
            try
            {
                Migrations = config.GetMigrations(this) ?? new List<Migration>();
                this.CreateTransaction();
                foreach (var migration in Migrations)
                {
                    var dbMigration = new List<DBMigration>();
                    var name = migration.GetType().FullName + migration.MigrationIdentifier;
                    try
                    {
                        dbMigration = this.Get<DBMigration>().Where(x => x.Name == name).Execute();
                    }
                    catch (Exception e)
                    {

                    }
                    if (dbMigration.Any())
                        continue;
                    var item = new DBMigration
                    {
                        Name = name,
                        DateCreated = DateTime.Now
                    };
                    migration.ExecuteMigration(this);
                    this.Save(item);
                }
                Commit();
            }
            catch (Exception)
            {
                Rollback();
                throw;
            }
        }

        /// <summary>
        /// return LightDataTable e.g. DataTable
        /// </summary>
        /// <param name="cmd">sqlCommand that are create from GetSqlCommand</param>
        /// <param name="primaryKey">Table primaryKeyId, so LightDataTable.FindByPrimaryKey could be used </param>
        /// <returns></returns>
        public ILightDataTable GetLightDataTable(DbCommand cmd, string primaryKey = null)
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
        public void AddInnerParameter(DbCommand cmd, string attrName, object value, SqlDbType dbType = SqlDbType.NVarChar)
        {
            if (attrName != null && attrName[0] != '@')
                attrName = "@" + attrName;

#if NET461 || NET451 || NET46
           var sqlDbTypeValue = value ?? DBNull.Value;
            (cmd as SQLiteCommand).Parameters.AddWithValue(attrName, value);
#elif NETCOREAPP2_0 || NETSTANDARD2_0
            var sqlDbTypeValue = value ?? DBNull.Value;
            (cmd as SqliteCommand).Parameters.AddWithValue(attrName, value);
#endif
        }

        /// <summary>
        /// Return SqlCommand that already containe SQLConnection
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public DbCommand GetSqlCommand(string sql)
        {
            ValidateConnection();
            return this.ProcessSql(SqlConnection, Trans, sql);
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
