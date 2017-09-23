using Generic.LightDataTable.Interface;
using System;
using System.Data;
using System.Data.SqlClient;

namespace Generic.LightDataTable.InterFace
{
   public interface ICustomRepository : IDisposable
    {
        ILightDataTable GetLightDataTable(SqlCommand cmd, string primaryKey = null);

        SqlCommand GetSqlCommand(string sql);

        void AddInnerParameter(SqlCommand cmd, string attrName, object value, SqlDbType dbType  = SqlDbType.NVarChar);

        SqlDbType GetSqlType(Type type);

        object ExecuteScalar(SqlCommand cmd);

        int ExecuteNonQuery(SqlCommand cmd);
        SqlTransaction CreateTransaction();
        void Rollback();

        void Commit();

    }
}
