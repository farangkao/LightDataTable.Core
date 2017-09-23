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

        public static QueryWhere Select<T>()
        {
            var type = MethodHelper.GetActualType(typeof(T));
            return new QueryWhere("Select * from " + (type.GetCustomAttribute<Table>()?.Name ?? typeof(T).Name) + " ");
        }

        public static QueryWhere Select(Type type)
        {
            type = MethodHelper.GetActualType(type);
            return new QueryWhere("Select * from " + (type.GetCustomAttribute<Table>()?.Name ?? type.Name) + " ");
        }

        public static QueryWhere Select(string tableName)
        {
            return new QueryWhere("Select * from " + tableName + " ");
        }

        public static QueryItem Where
        {
            get
            {
                var item = new QueryItem(" Where ");
                return item;
            }
        }
    }



    public class QueryWhere
    {
        private readonly string _sql;

        public QueryWhere(string sql)
        {
            _sql = sql;
        }
        public QueryItem Where => new QueryItem(_sql + " Where ");

        public string Execute()
        {
            return _sql;
        }
    }

    public sealed class QueryItem
    {
        private string _sql;
        public QueryItem(string sql)
        {
            _sql = sql;
        }

        public bool HasValue()
        {
            return _sql.Trim() != "Where";
        }

        public QueryConditions Column(string columnName)
        {
            _sql = _sql + " " + columnName;
            return new QueryConditions(_sql);
        }

        public QueryConditions Column<T>(string columnName)
        {
            var col = columnName;
            if (col.ToLower().Contains(" as "))
                col = col.Remove(col.ToLower().IndexOf(" as ", StringComparison.Ordinal));
            var type = typeof(T);
            if (type == typeof(decimal) || type == typeof(double) || type == typeof(float))
                _sql += " " + "CONVERT(decimal(18,5)," + col + ") ";
            else if (type == typeof(int) || type == typeof(long))
                _sql += " " + "CONVERT(bigint," + col + ") ";
            else _sql += " " + "CONVERT(nvarchar(max)," + col + ") ";
            return new QueryConditions(_sql);
        }

        public QueryConditions Column<T, P>(Expression<Func<T, P>> action) where T : class
        {
            var member = action.Body is UnaryExpression ? ((MemberExpression)((UnaryExpression)action.Body).Operand) : (action.Body is MethodCallExpression ? ((MemberExpression)((MethodCallExpression)action.Body).Object) : (MemberExpression)action.Body);
            if (member == null) return new QueryConditions(_sql);
            var key = member.Member.Name;
            var type = typeof(P);
            var col = key;
            if (col.ToLower().Contains(" as "))
                col = col.Remove(col.ToLower().IndexOf(" as ", StringComparison.Ordinal));
            if (type == typeof(decimal) || type == typeof(double) || type == typeof(float))
                _sql += " " + "CONVERT(decimal(18,5)," + col + ") ";
            else if (type == typeof(int) || type == typeof(long))
                _sql += " " + "CONVERT(bigint," + col + ") ";
            else _sql += " " + "CONVERT(nvarchar(max)," + col + ") ";
            return new QueryConditions(_sql);
        }


        public QueryConditions ToQueryConditions => new QueryConditions(_sql);

        public string Execute()
        {
            return _sql;
        }
    }

    public sealed class QueryConditions
    {

        private string _sql;
        public QueryConditions(string sql)
        {
            _sql = sql;
        }

        public QueryItem ToQueryItem => new QueryItem(_sql);

        public QueryItem And
        {
            get
            {
                _sql += " AND";
                return new QueryItem(_sql);
            }
        }

        public QueryItem Or
        {
            get
            {
                _sql += " OR";
                return new QueryItem(_sql);
            }
        }

        public QueryConditions Comment(string text)
        {
            _sql += string.Format("\n--{0}--\n", text);
            return new QueryConditions(_sql);
        }

        public QueryConditions Like(string value, bool isColumn = false)
        {
            if (!isColumn)
                _sql += string.Format(" like '%{0}%'", value);
            else _sql += string.Format(" like '%'+{0}+'%'", value);
            return new QueryConditions(_sql);
        }

        public QueryConditions BeginWith(string value, bool isColumn = false)
        {
            if (!isColumn)
                _sql += string.Format(" like '{0}%'", value);
            else _sql += string.Format(" like {0}+'%'", value);
            return new QueryConditions(_sql);
        }


        public QueryConditions EndWith(string value, bool isColumn = false)
        {
            if (!isColumn)
                _sql += string.Format(" like '%{0}'", value);
            else _sql += string.Format(" like '%'+{0}", value);
            return new QueryConditions(_sql);
        }

        public QueryConditions Equal(object value, bool isColumn = false)
        {
            if (!isColumn)
                _sql += string.Format(" = {0}", Querys.GetValueByType(value));
            else
                _sql += string.Format(" = {0}", value);
            return new QueryConditions(_sql);
        }

        public QueryConditions NotEqual(object value, bool isColumn = false)
        {
            if (!isColumn)
                _sql += string.Format(" != {0}", Querys.GetValueByType(value));
            else _sql += string.Format(" != {0}", value);
            return new QueryConditions(_sql);
        }

        public QueryConditions GreaterThan(object value, bool isColumn = false)
        {
            if (!isColumn)
                _sql += string.Format(" > {0}", Querys.GetValueByType(value));
            else _sql += string.Format(" > {0}", value);
            return new QueryConditions(_sql);
        }

        public QueryConditions SmallerThan(object value, bool isColumn = false)
        {
            if (!isColumn)
                _sql += string.Format(" < {0}", Querys.GetValueByType(value));
            else _sql += string.Format(" < {0}", value);
            return new QueryConditions(_sql);
        }

        public QueryConditions IsNull
        {
            get
            {
                _sql += " is null";
                return new QueryConditions(_sql);
            }
        }

        public QueryConditions IsNotNull
        {
            get
            {
                _sql += " is not null";
                return new QueryConditions(_sql);
            }
        }

        public QueryItem StartBracket
        {
            get
            {
                _sql += "(";
                return new QueryItem(_sql);
            }
        }

        public QueryItem EndBracket
        {
            get
            {
                _sql += ")";
                return new QueryItem(_sql);
            }
        }

        public string Execute()
        {
            return _sql;
        }

    }
}
