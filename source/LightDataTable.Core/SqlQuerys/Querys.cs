using Generic.LightDataTable.Attributes;
using System;
using System.Linq.Expressions;
using System.Reflection;
using Generic.LightDataTable.Helper;

namespace Generic.LightDataTable.SqlQuerys
{
    public static class Querys
    {
        internal static string GetValueByType(object value)
        {
            if (value == null)
                return string.Format("'{0}'", "null");
            var type = value.GetType();
            if (type == typeof(decimal) || type == typeof(double) || type == typeof(float) || type == typeof(int) || type == typeof(long))
                return value.ToString();
            else return string.Format("'{0}'", value);

        }

        public static QueryWhere Select<T>(bool isSqllite)
        {
            var type = MethodHelper.GetActualType(typeof(T));
            return new QueryWhere("Select * from " + (type.GetCustomAttribute<Table>()?.Name ?? typeof(T).Name) + " ", isSqllite);
        }

        public static QueryWhere Select(Type type, bool isSqllite)
        {
            type = MethodHelper.GetActualType(type);
            return new QueryWhere("Select * from " + (type.GetCustomAttribute<Table>()?.Name ?? type.Name) + " ", isSqllite);
        }

        public static QueryWhere Select(string tableName , bool isSqllite)
        {
            return new QueryWhere("Select * from " + tableName + " ", isSqllite);
        }

        public static QueryItem Where(bool isSqllite = false)
        {
                var item = new QueryItem(" Where ", isSqllite);
                return item;
 
        }
    }



    public class QueryWhere
    {
        private readonly string _sql;

        private bool IsSqllite { get; set; }

        public QueryWhere(string sql, bool isSqllite = false)
        {
            _sql = sql;
            IsSqllite = isSqllite;
        }
        public QueryItem Where => new QueryItem(_sql + " Where ", IsSqllite);

        public string Execute()
        {
            return _sql;
        }
    }

    public sealed class QueryItem
    {
        private string _sql;
        private bool IsSqllite { get; set; }
        public QueryItem(string sql, bool isSqllite)
        {
            _sql = sql;
            IsSqllite = isSqllite;
        }

        public bool HasValue()
        {
            return _sql.Trim() != "Where";
        }

        public QueryConditions Column(string columnName)
        {
            _sql = _sql + " " + columnName;
            return new QueryConditions(_sql, IsSqllite);
        }

        public QueryConditions Column<T>(string columnName)
        {
            var col = columnName;
            if (col.ToLower().Contains(" as "))
                col = col.Remove(col.ToLower().IndexOf(" as ", StringComparison.Ordinal));
            var type = typeof(T);
            if (type == typeof(decimal) || type == typeof(double) || type == typeof(float))
                _sql += !IsSqllite ? " " + "CONVERT(decimal(18,5)," + col + ") " : " Cast(" + col + " AS decimal(18,5)) ";
            else if (type == typeof(int) || type == typeof(long))
                _sql += !IsSqllite ? " " + "CONVERT(decimal(18,5)," + col + ") " : " Cast(" + col + " AS bigint) ";
            else _sql += !IsSqllite ? " " + "CONVERT(decimal(18,5)," + col + ") " : " Cast(" + col + " AS nvarchar(4000)) ";

            return new QueryConditions(_sql, IsSqllite);
        }

        public QueryConditions Column<T, P>(Expression<Func<T, P>> action) where T : class
        {
            var member = action.Body is UnaryExpression ? ((MemberExpression)((UnaryExpression)action.Body).Operand) : (action.Body is MethodCallExpression ? ((MemberExpression)((MethodCallExpression)action.Body).Object) : (MemberExpression)action.Body);
            if (member == null) return new QueryConditions(_sql, IsSqllite);
            var key = member.Member.Name;
            var type = typeof(P);
            var col = key;
            if (col.ToLower().Contains(" as "))
                col = col.Remove(col.ToLower().IndexOf(" as ", StringComparison.Ordinal));
            if (type == typeof(decimal) || type == typeof(double) || type == typeof(float))
                _sql += !IsSqllite ? " " + "CONVERT(decimal(18,5)," + col + ") " : " Cast(" + col + " AS decimal(18,5)) ";
            else if (type == typeof(int) || type == typeof(long))
                _sql += !IsSqllite ? " " + "CONVERT(decimal(18,5)," + col + ") " : " Cast(" + col + " AS bigint) ";
            else _sql += !IsSqllite ? " " + "CONVERT(decimal(18,5)," + col + ") " : " Cast(" + col + " AS nvarchar(4000)) ";
            return new QueryConditions(_sql, IsSqllite);
        }


        public QueryConditions ToQueryConditions => new QueryConditions(_sql, IsSqllite);

        public string Execute()
        {
            return _sql;
        }
    }

    public sealed class QueryConditions
    {

        private string _sql;
        private bool IsSqllite { get; set; }
        public QueryConditions(string sql, bool isSqllite)
        {
            _sql = sql;
            IsSqllite = isSqllite;
        }

        public QueryItem ToQueryItem => new QueryItem(_sql, IsSqllite);

        public QueryItem And
        {
            get
            {
                _sql += " AND";
                return new QueryItem(_sql, IsSqllite);
            }
        }

        public QueryItem Or
        {
            get
            {
                _sql += " OR";
                return new QueryItem(_sql, IsSqllite);
            }
        }

        public QueryConditions Comment(string text)
        {
            _sql += string.Format("\n--{0}--\n", text);
            return new QueryConditions(_sql, IsSqllite);
        }

        public QueryConditions Like(string value, bool isColumn = false)
        {
            if (!isColumn)
                _sql += string.Format(" like '%{0}%'", value);
            else _sql += string.Format(" like '%'+{0}+'%'", value);
            return new QueryConditions(_sql, IsSqllite);
        }

        public QueryConditions BeginWith(string value, bool isColumn = false)
        {
            if (!isColumn)
                _sql += string.Format(" like '{0}%'", value);
            else _sql += string.Format(" like {0}+'%'", value);
            return new QueryConditions(_sql, IsSqllite);
        }


        public QueryConditions EndWith(string value, bool isColumn = false)
        {
            if (!isColumn)
                _sql += string.Format(" like '%{0}'", value);
            else _sql += string.Format(" like '%'+{0}", value);
            return new QueryConditions(_sql, IsSqllite);
        }

        public QueryConditions Equal(object value, bool isColumn = false)
        {
            if (!isColumn)
                _sql += string.Format(" = {0}", Querys.GetValueByType(value));
            else
                _sql += string.Format(" = {0}", value);
            return new QueryConditions(_sql, IsSqllite);
        }

        public QueryConditions NotEqual(object value, bool isColumn = false)
        {
            if (!isColumn)
                _sql += string.Format(" != {0}", Querys.GetValueByType(value));
            else _sql += string.Format(" != {0}", value);
            return new QueryConditions(_sql, IsSqllite);
        }

        public QueryConditions GreaterThan(object value, bool isColumn = false)
        {
            if (!isColumn)
                _sql += string.Format(" > {0}", Querys.GetValueByType(value));
            else _sql += string.Format(" > {0}", value);
            return new QueryConditions(_sql, IsSqllite);
        }

        public QueryConditions SmallerThan(object value, bool isColumn = false)
        {
            if (!isColumn)
                _sql += string.Format(" < {0}", Querys.GetValueByType(value));
            else _sql += string.Format(" < {0}", value);
            return new QueryConditions(_sql, IsSqllite);
        }

        public QueryConditions IsNull
        {
            get
            {
                _sql += " is null";
                return new QueryConditions(_sql, IsSqllite);
            }
        }

        public QueryConditions IsNotNull
        {
            get
            {
                _sql += " is not null";
                return new QueryConditions(_sql, IsSqllite);
            }
        }

        public QueryItem StartBracket
        {
            get
            {
                _sql += "(";
                return new QueryItem(_sql, IsSqllite);
            }
        }

        public QueryItem EndBracket
        {
            get
            {
                _sql += ")";
                return new QueryItem(_sql, IsSqllite);
            }
        }

        public string Execute()
        {
            return _sql;
        }

    }
}
