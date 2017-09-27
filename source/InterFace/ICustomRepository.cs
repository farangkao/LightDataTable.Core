using Generic.LightDataTable.Interface;
using System;
using System.Data;
using System.Data.Common;


namespace Generic.LightDataTable.InterFace
{
   public interface ICustomRepository : IDisposable
    {
        ILightDataTable GetLightDataTable(DbCommand cmd, string primaryKey = null);

        DbCommand GetSqlCommand(string sql);

        void AddInnerParameter(DbCommand cmd, string attrName, object value, SqlDbType dbType  = SqlDbType.NVarChar);

        SqlDbType GetSqlType(Type type);

        object ExecuteScalar(DbCommand cmd);

        int ExecuteNonQuery(DbCommand cmd);

        DbTransaction CreateTransaction();

        void Rollback();

        void Commit();
    }
}
