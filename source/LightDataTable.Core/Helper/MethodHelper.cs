using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using FastDeepCloner;
using Newtonsoft.Json;
using Generic.LightDataTable.Attributes;
using Generic.LightDataTable.InterFace;
using Generic.LightDataTable.Library;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Data.Common;
using System.Text.RegularExpressions;
using System.Data;
using System.Data.SqlClient;
#if NET461 || NET451 || NET46 
using System.Data.SQLite;
#elif NETCOREAPP2_0 || NETSTANDARD2_0
using Microsoft.Data.Sqlite;
#endif

namespace Generic.LightDataTable
{
    public static class MethodHelper
    {
        private static readonly Dictionary<IFastDeepClonerProperty, string> CachedPropertyNames = new Dictionary<IFastDeepClonerProperty, string>();
        private static readonly Dictionary<Type, IFastDeepClonerProperty> CachedPrimaryKeys = new Dictionary<Type, IFastDeepClonerProperty>();
        private static readonly Dictionary<Type, Dictionary<string, IFastDeepClonerProperty>> CachedProperties = new Dictionary<Type, Dictionary<string, IFastDeepClonerProperty>>();

        private static Dictionary<Type, String> DbMapper = new Dictionary<Type, string>()
        {
            {typeof(int), "BIGINT"},
            {typeof(long), "INT"},
            {typeof(string), "NVARCHAR(4000)"},
            {typeof(bool), "BIT"},
            {typeof(DateTime), "DATETIME"},
            {typeof(float), "FLOAT"},
            {typeof(decimal), "DECIMAL(18,0)"},
            {typeof(Guid), "UNIQUEIDENTIFIER"},
            {typeof(byte[]), "varbinary(MAX)"}
        };

        internal static DbCommand ProcessSql(this ICustomRepository repository, DbConnection connection, DbTransaction tran, string sql)
        {
            var stringExp = new Regex(@"String\[.+?\]");
            var DateExp = new Regex(@"Date\[.+?\]");
            var i = 0;
            var dicCols = new Dictionary<string, Tuple<object, SqlDbType>>();
            MatchCollection matches = null;
            while ((matches = stringExp.Matches(sql)).Count > 0)
            {
                Match exp = matches[0];
                var col = "@CO" + i + "L";
                object str = exp.Value.TrimEnd(']').Substring(@"String\[".Length - 1);
                sql = sql.Remove(exp.Index, exp.Value.Length);
                sql = sql.Insert(exp.Index, col);
                dicCols.Add(col, new Tuple<object, SqlDbType>(str.ConvertValue<string>(), SqlDbType.NVarChar));
                i++;
            }

            while ((matches = DateExp.Matches(sql)).Count > 0)
            {
                Match exp = matches[0];
                var col = "@CO" + i + "L";
                object str = exp.Value.TrimEnd(']').Substring(@"Date\[".Length - 1);
                sql = sql.Remove(exp.Index, exp.Value.Length);
                sql = sql.Insert(exp.Index, col);
                dicCols.Add(col, new Tuple<object, SqlDbType>(str.ConvertValue<DateTime>(), SqlDbType.DateTime));
                i++;
            }

            DbCommand cmd = null;
#if NET461 || NET451 || NET46
            if (repository.GetDataBaseType() == Helper.DataBaseTypes.Mssql)
                cmd = tran != null ? new SqlCommand(sql, connection as SqlConnection, tran as SqlTransaction) : new SqlCommand(sql, connection as SqlConnection);
            else cmd = tran == null ? new SQLiteCommand(sql, connection as SQLiteConnection) : new SQLiteCommand(sql, connection as SQLiteConnection, tran as SQLiteTransaction);
            foreach (var dic in dicCols)
                repository.AddInnerParameter(cmd, dic.Key, dic.Value.Item1, dic.Value.Item2);
            return cmd;
#elif NETCOREAPP2_0 || NETSTANDARD2_0
            if (repository.GetDataBaseType() == Helper.DataBaseTypes.Mssql)
                cmd = tran != null ? new SqlCommand(sql, connection as SqlConnection, tran as SqlTransaction) : new SqlCommand(sql, connection as SqlConnection);
            else cmd = tran == null ? new SqliteCommand(sql, connection as SqliteConnection) : new SqliteCommand(sql, connection as SqliteConnection, tran as SqliteTransaction);
            foreach (var dic in dicCols)
                repository.AddInnerParameter(cmd, dic.Key, dic.Value.Item1, dic.Value.Item2);
            return cmd;

#endif
        }

        public static Attribute GetAttributeType<T>() where T : Attribute
        {
            return (T)FormatterServices.GetUninitializedObject(typeof(T));
        }


        public static string GetDbTypeByType(this Type type)
        {
            if (Nullable.GetUnderlyingType(type) != null)
                type = Nullable.GetUnderlyingType(type);
            return DbMapper.ContainsKey(type) ? DbMapper[type] : null;
        }


        public static object ConvertValue(this Type toType, object value)
        {
            var data = new LightDataTable();
            data.AddColumn("value", toType, value);
            data.AddRow(new object[1] { value });
            return data.Rows.First()[0, true];
        }

        public static T ConvertValue<T>(this object value)
        {
            var data = new LightDataTable();
            data.AddColumn("value", typeof(T), value);
            data.AddRow(new object[1] { value });
            return data.Rows.First().TryValueAndConvert<T>(0, true);
        }

        internal static List<string> ConvertExpressionToIncludeList(this Expression[] actions, bool onlyLast = false)
        {
            var result = new List<string>();
            if (actions == null) return result;
            foreach (var exp in actions)
            {
                var tempList = new List<string>();
                var expString = exp.ToString().Split('.');
                var propTree = "";
                foreach (var item in expString)
                {
                    var x = item.Trim().Replace("(", "").Replace(")", "").Replace("&&", "").Replace("||", "").Trim();
                    if (x.Any(char.IsWhiteSpace) || x.Contains("="))
                        continue;
                    propTree += ("." + x);
                    if (propTree.Split('.').Length == 4)
                        propTree = string.Join(".", propTree.Split('.').Skip(2));
                    tempList.Add(propTree.TrimStart('.'));
                }
                if (!onlyLast)
                    result.AddRange(tempList);
                else if (tempList.Any())
                    result.Add(tempList.Last());
            }
            return result;
        }

        public static string EncodeStringToBase64(string stringToEncode)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(stringToEncode));
        }

        public static string DecodeStringFromBase64(string stringToDecode)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(stringToDecode));
        }

        public static string GetPropertyName(this IFastDeepClonerProperty prop)
        {
            if (CachedPropertyNames.ContainsKey(prop))
                return CachedPropertyNames[prop];
            CachedPropertyNames.Add(prop, prop.GetCustomAttribute<PropertyName>()?.Name ?? prop.Name);
            return CachedPropertyNames[prop];
        }

        public static object GetPropertyValue(this IDbEntity item, string propertyName)
        {
            if (!CachedProperties.ContainsKey(item.GetType()))
                CachedProperties.Add(item.GetType(), DeepCloner.GetFastDeepClonerProperties(item.GetType()).GroupBy(x => x.Name).Select(x => x.First()).ToDictionary(x => x.Name, x => x));
            return CachedProperties[item.GetType()][propertyName].GetValue(item);
        }

        public static Expression<Func<TInput, object>> PropertyExpression<TInput, TOutput>(this Expression<Func<TInput, TOutput>> expression)
        {
            var memberName = ((MemberExpression)expression.Body).Member.Name;
            var param = Expression.Parameter(typeof(TInput));
            var field = Expression.Property(param, memberName);
            return Expression.Lambda<Func<TInput, object>>(field, param);
        }

        public static bool Between(DateTime input, DateTime date1, DateTime date2)
        {
            return (input > date1 && input < date2);
        }

        public static Type GetActualType(this Type type)
        {
            if (type.GetTypeInfo().IsArray)
                return type.GetElementType();
            if (type.GenericTypeArguments.Any())
                return type.GenericTypeArguments.First();
            if (type.FullName?.Contains("List`1") ?? false)
                return type.GetRuntimeProperty("Item").PropertyType;
            return type;
        }

        public static string TrimEnd(this string source, string value)
        {
            if (!source.EndsWith(value))
                return source;
            return source.Remove(source.LastIndexOf(value));
        }

        public static string InsertLast(this string str, string text, char ch)
        {
            str = str.Trim();
            if (string.IsNullOrEmpty(ch.ToString()))
                str = str.Insert(str.Length - 1, text);
            else
            {
                var i = str.Length - 1;
                for (int j = str.Length - 1; j >= 0; j--)
                {
                    if (str[j] != ch)
                    {
                        i = j + 1;
                        break;
                    }
                }
                str = str.Insert(i, text);
            }
            return str;
        }

        public static IFastDeepClonerProperty GetPrimaryKey(IDbEntity item)
        {
            if (CachedPrimaryKeys.ContainsKey(item.GetType()))
                return CachedPrimaryKeys[item.GetType()];
            CachedPrimaryKeys.Add(item.GetType(), DeepCloner.GetFastDeepClonerProperties(item.GetType()).FirstOrDefault(x => x.ContainAttribute<PrimaryKey>()));
            return CachedPrimaryKeys[item.GetType()];
        }

        public static IFastDeepClonerProperty GetPrimaryKey(this Type type)
        {
            if (CachedPrimaryKeys.ContainsKey(type))
                return CachedPrimaryKeys[type];
            CachedPrimaryKeys.Add(type, DeepCloner.GetFastDeepClonerProperties(type).FirstOrDefault(x => x.ContainAttribute<PrimaryKey>()));
            return CachedPrimaryKeys[type];
        }

        public static object CreateInstance(this Type type, bool uninitializedObject = true)
        {
            return uninitializedObject ? FormatterServices.GetUninitializedObject(type) : Activator.CreateInstance(type);
        }

        // This method is added incase we want JsonConverter to serilize only new data, 
        // be sure to ClearPropertChanges before beginning to change the data
        public static string CreateNewValuesFromObject(IDbEntity desireObject)
        {
            return JsonConvert.SerializeObject(desireObject, Formatting.Indented, new JsonSerializerSettings { ContractResolver = new ShouldSerializeContractResolver() });
        }

    }
}
