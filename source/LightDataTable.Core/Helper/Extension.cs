using Newtonsoft.Json;
using Generic.LightDataTable.Interface;
using Generic.LightDataTable.InterFace;
using Generic.LightDataTable.Library;
using Generic.LightDataTable.SqlQuerys;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using FastDeepCloner;
using Generic.LightDataTable.Attributes;
using Generic.LightDataTable.Helper;
using Generic.LightDataTable.Transaction;

namespace Generic.LightDataTable
{
    public static class Extension
    {
        public static T FromJson<T>(this string jsonData) where T : class
        {
            return JsonConvert.DeserializeObject<T>(jsonData);
        }

        public static object FromJson(this string jsonData, Type type)
        {
            return JsonConvert.DeserializeObject(jsonData, type);
        }

        public static string ToJson(this object data)
        {
            return JsonConvert.SerializeObject(data);
        }

        public static List<T> Clone<T>(this List<T> items) where T : class, IDbEntity
        {
            return DeepCloner.Clone(items, new FastDeepClonerSettings()
            {
                FieldType = FieldType.PropertyInfo,
                OnCreateInstance = new Extensions.CreateInstance(FormatterServices.GetUninitializedObject)
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="items"></param>
        /// <param name="clearIndependedData"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static List<T> ClearAllIdsHierarchy<T>(this List<T> items, bool clearIndependedData = false) where T : class, IDbEntity
        {
            foreach (var o in items)
            {
                o.ClearAllIdsHierarchy(clearIndependedData);
                o.ClearPropertChanges();
            }
            return items;
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="clearIndependedData"></param>
        public static void ClearAllIdsHierarchy(this object item, bool clearIndependedData = false)
        {
            if (item == null)
                return;
            if (item is IList)
            {
                foreach (var o in item as IList)
                {
                    var oItem = o as IDbEntity;
                    if (oItem == null)
                        continue;
                    oItem.ClearAllIdsHierarchy();
                    oItem.ClearPropertChanges();
                }
            }
            else
            {
                var entity = item as IDbEntity;
                if (entity == null)
                    return;
                var properties = DeepCloner.GetFastDeepClonerProperties(item.GetType());
                foreach (var prop in properties)
                {
                    if (!prop.CanRead || (!clearIndependedData && prop.ContainAttribute<IndependentData>()) || prop.ContainAttribute<ExcludeFromAbstract>())
                        continue;

                    var oValue = prop.GetValue(item);
                    if (oValue == null)
                        continue;
                    if (!prop.IsInternalType)
                        oValue.ClearAllIdsHierarchy();
                    else
                    {
                        if (prop.Name.EndsWith("ID", StringComparison.CurrentCultureIgnoreCase))
                            prop.SetValue(item, prop.PropertyType.ConvertValue(null));
                    }
                }
                entity.ClearPropertChanges();
            }
        }

        internal static DataBaseTypes GetDataBaseType(this ICustomRepository repository)
        {
            return repository is TransactionData ? DataBaseTypes.Mssql : DataBaseTypes.Sqllight;
        }

        internal static T ToType<T>(this object o) where T : class
        {
            object item;
            if (typeof(T) == typeof(IList))
                return (T)(item = new LightDataTable(o).Rows.Select(x => ((IList)x.ToObject<T>())[0]).ToList());
            else return new LightDataTable(o).Rows.First().ToObject<T>();
        }

        /// <summary>
        /// object must containe PrimaryKey
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="repository"></param>
        /// <param name="o"></param>
        /// <returns></returns>
        public static long Save<T>(this ICustomRepository repository, T o) where T : IDbEntity
        {
            if (o == null) return -1;
            o.State = ItemState.Added;
            return DbSchema.SaveObject(repository, o, false, true);
        }

        /// <summary>
        /// object must containe PrimaryKey
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="repository"></param>
        /// <param name="o"></param>
        /// <returns></returns>
        internal static async Task<long> SaveAsync<T>(this ICustomRepository repository, T o) where T : IDbEntity
        {
            if (o == null) return await Task.FromResult<long>(-1);
            o.State = ItemState.Added;
            return await Task.Run(() => DbSchema.SaveObject(repository, o, false, true));
        }

        /// <summary>
        /// Delete object from db
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="repository"></param>
        /// <param name="o"></param>
        internal static void DeleteAbstract<T>(this ICustomRepository repository, T o) where T : class, IDbEntity
        {
            if (o != null)
                DbSchema.DeleteAbstract(repository, o, true);
        }

        /// <summary>
        /// This will recreate the table and if it has a ForeignKey to other tables it will also recreate those table to
        /// use it wisely
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="repository"></param>
        /// <param name="force"> remove and recreate all</param>

        public static void CreateTable<T>(this ICustomRepository repository, bool force = false) where T : class, IDbEntity
        {
            DbSchema.CreateTable(repository, typeof(T), null, true, force);
        }

        /// <summary>
        /// This will remove the table and if it has a ForeignKey to other tables it will also remove those table to
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="repository"></param>
        public static void RemoveTable<T>(this ICustomRepository repository) where T : class, IDbEntity
        {
            DbSchema.RemoveTable(repository, typeof(T));
        }

        /// <summary>
        /// Delete object from db
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="repository"></param>
        /// <param name="o"></param>
        internal static async Task DeleteAbstractAsync<T>(this ICustomRepository repository, T o) where T : class, IDbEntity
        {
            if (o != null)
                await Task.Run(() => DbSchema.DeleteAbstract(repository, o, true));
        }

        /// <summary>
        /// select object by its id
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="repository"></param>
        /// <param name="objectId"></param>
        /// <returns></returns>
        public static ISqlQueriable<T> GetAbstractById<T>(this ICustomRepository repository, long? objectId) where T : class, IDbEntity
        {
            return !objectId.HasValue ? null : new SqlQueriable<T>(new List<T>() { (T)DbSchema.GetSqlById(objectId.Value, repository, typeof(T)) }, repository);
        }

        /// <summary>
        /// get all record from db
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="repository"></param>
        /// <returns></returns>
        internal static List<T> GetAbstractAll<T>(this ICustomRepository repository) where T : class, IDbEntity
        {
            return DbSchema.GetSqlAll(repository, typeof(T))?.Cast<T>().ToList();
        }

        /// <summary>
        /// get all record from db
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="repository"></param>
        /// <returns></returns>
        internal static async Task<List<T>> GetAbstractAllAsync<T>(this ICustomRepository repository) where T : class, IDbEntity
        {
            return await Task.Run(() => DbSchema.GetSqlAll(repository, typeof(T))?.Cast<T>().ToList());
        }

        /// <summary>
        /// load children
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="repository"></param>
        /// <param name="item"></param>
        /// <param name="onlyFirstLevel"></param>
        /// <param name="ignoreList"></param>
        internal static void LoadChildren<T>(this ICustomRepository repository, T item, bool onlyFirstLevel = false, List<string> ignoreList = null) where T : class, IDbEntity
        {
            DbSchema.LoadChildren<T>(item, repository, onlyFirstLevel, null, ignoreList != null && ignoreList.Any() ? ignoreList : null);
        }

        /// <summary>
        /// load children
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="repository"></param>
        /// <param name="item"></param>
        /// <param name="onlyFirstLevel"></param>
        /// <param name="ignoreList"></param>
        internal static async Task<bool> LoadChildrenAsync<T>(this ICustomRepository repository, T item, bool onlyFirstLevel = false, List<string> ignoreList = null) where T : class, IDbEntity
        {
            await Task.Run(() => DbSchema.LoadChildren<T>(item, repository, onlyFirstLevel, null, ignoreList != null && ignoreList.Any() ? ignoreList : null));
            return await Task.FromResult(true);
        }

        /// <summary>
        /// load children 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TP"></typeparam>
        /// <param name="repository"></param>
        /// <param name="item"></param>
        /// <param name="onlyFirstLevel"></param>
        /// <param name="ignoreList"></param>
        /// <param name="actions"></param>
        internal static void LoadChildren<T, TP>(this ICustomRepository repository, T item, bool onlyFirstLevel = false, List<string> ignoreList = null, params Expression<Func<T, TP>>[] actions) where T : class, IDbEntity
        {
            var parames = new List<string>();
            if (actions != null)
                parames = actions.ConvertExpressionToIncludeList();
            DbSchema.LoadChildren<T>(item, repository, onlyFirstLevel, actions != null ? parames : null, ignoreList != null && ignoreList.Any() ? ignoreList : null);
        }

        /// <summary>
        /// load children 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TP"></typeparam>
        /// <param name="repository"></param>
        /// <param name="item"></param>
        /// <param name="onlyFirstLevel"></param>
        /// <param name="ignoreList"></param>
        /// <param name="actions"></param>
        internal static async Task<bool> LoadChildrenAsync<T, TP>(this ICustomRepository repository, T item, bool onlyFirstLevel = false, List<string> ignoreList = null, params Expression<Func<T, TP>>[] actions) where T : class, IDbEntity
        {
            var parames = new List<string>();
            if (actions != null)
                parames = actions.ConvertExpressionToIncludeList();
            await Task.Run(() => DbSchema.LoadChildren<T>(item, repository, onlyFirstLevel, actions != null ? parames : null, ignoreList != null && ignoreList.Any() ? ignoreList : null));
            return await Task.FromResult<bool>(true);
        }


        /// <summary>
        /// select by quarry
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="repository"></param>
        /// <param name="quary"></param>
        /// <returns></returns>
        public static ISqlQueriable<T> SelectAbsence<T>(this ICustomRepository repository, QueryItem quary = null) where T : class, IDbEntity
        {
            return new SqlQueriable<T>(DbSchema.Select<T>(repository, quary), repository);
        }


        /// <summary>
        /// select by quarry
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="repository"></param>
        /// <param name="quary"></param>
        /// <returns></returns>
        internal static List<T> Where<T>(this ICustomRepository repository, LightDataLinqToNoSql<T> quary = null) where T : class, IDbEntity
        {
            var sql = quary?.Quary;
            return DbSchema.Where<T>(repository, sql);
        }

        /// <summary>
        /// select by quarry
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="repository"></param>
        /// <param name="quary"></param>
        /// <returns></returns>
        internal static async Task<List<T>> WhereAsync<T>(this ICustomRepository repository, LightDataLinqToNoSql<T> quary = null) where T : class, IDbEntity
        {
            var sql = quary?.Quary;
            return await Task.Run(() => DbSchema.Where<T>(repository, sql));
        }


        public static ISqlQueriable<T> Get<T>(this ICustomRepository repository) where T : class, IDbEntity
        {
            return new SqlQueriable<T>(null, repository);

        }

        internal static Type TypeByTypeAndDbIsNull(this Type propertyType, bool allowDbNull)
        {
            if (propertyType == typeof(int))
                return allowDbNull ? typeof(int?) : typeof(int);

            if (propertyType == typeof(decimal))
                return allowDbNull ? typeof(decimal?) : typeof(decimal);

            if (propertyType == typeof(double))
                return allowDbNull ? typeof(double?) : typeof(double);

            if (propertyType == typeof(float))
                return allowDbNull ? typeof(float?) : typeof(float);

            if (propertyType == typeof(DateTime))
                return allowDbNull ? typeof(DateTime?) : typeof(DateTime);

            if (propertyType == typeof(long))
                return allowDbNull ? typeof(long?) : typeof(long);

            if (propertyType == typeof(TimeSpan))
                return allowDbNull ? typeof(TimeSpan?) : typeof(TimeSpan);

            if (propertyType == typeof(bool))
                return allowDbNull ? typeof(bool?) : typeof(bool);

            return propertyType == typeof(byte[]) ? typeof(byte[]) : typeof(string);
        }

        internal static ILightDataTable ReadData(this ILightDataTable data, IDataReader reader, string primaryKey = null)
        {
            var i = 0;
            if (reader.FieldCount <= 0)
                return data;
            data.TablePrimaryKey = primaryKey;

            if (reader.FieldCount <= 0)
            {
                reader.Close();
                reader.Dispose();
                return data;
            }
            try
            {



                var dataRowCollection = reader.GetSchemaTable()?.Rows;
                if (dataRowCollection != null)
                    foreach (DataRow item in dataRowCollection)
                    {
                        //var isKey = Converter<bool>.Parse(item["IsKey"]);
                        var columnName = item["ColumnName"].ToString();
                        var dataType = TypeByTypeAndDbIsNull(item["DataType"] as Type,
                            MethodHelper.ConvertValue<bool>(item["AllowDBNull"]));
                        if (data.Columns.ContainsKey(columnName))
                            columnName = columnName + i;
                        data.AddColumn(columnName, dataType);

                        i++;
                    }
            }
            catch
            {
                for (int col = 0; col < reader.FieldCount; col++)
                {
                    var columnName = reader.GetName(col);
                    var dataType = TypeByTypeAndDbIsNull(reader.GetFieldType(col) as Type,
                        MethodHelper.ConvertValue<bool>(true));
                    if (data.Columns.ContainsKey(columnName))
                        columnName = columnName + i;
                    data.AddColumn(columnName, dataType);

                }
            }

            while (reader.Read())
            {
                var row = data.NewRow();
                reader.GetValues(row.ItemArray);
                data.AddRow(row);
            }

            reader.Close();
            reader.Dispose();
            return data;
        }
    }
}
