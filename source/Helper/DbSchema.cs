using Generic.LightDataTable.SqlQuerys;
using System.Text;
using System.Reflection;
using Generic.LightDataTable.Attributes;
using System.Linq;
using System.Collections.Generic;
using Generic.LightDataTable.InterFace;
using System.Collections;
using System;
using FastDeepCloner;
using Generic.LightDataTable.Interface;

namespace Generic.LightDataTable.Helper
{
    internal static class DbSchema
    {
        private static readonly Dictionary<string, string> CachedSql = new Dictionary<string, string>();

        private static readonly Dictionary<Type, ILightDataTable> CachedObjectColumn = new Dictionary<Type, ILightDataTable>();

        private static readonly Dictionary<Type, object> CachedIDbRuleTrigger = new Dictionary<Type, object>();
        private static ILightDataTable ObjectColumns(this ICustomRepository repository, Type type)
        {
            if (CachedObjectColumn.ContainsKey(type))
                return CachedObjectColumn[type];
            var table = type.GetCustomAttribute<Table>()?.Name ?? type.Name;
            var cmd = repository.GetSqlCommand("SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = N'" + table + "'");
            var data = repository.GetLightDataTable(cmd, "COLUMN_NAME");
            CachedObjectColumn.Add(type, data);
            return CachedObjectColumn[type];
        }

        internal static long SaveObject(ICustomRepository repository, InterFace.IDbEntity o, bool isIndependentData = false, bool commit = false)
        {
            repository.CreateTransaction();
            try
            {

                var updateOnly = o.State == ItemState.Changed;
                o.State = ItemState.Added;// reset State
                var props = FastDeepCloner.DeepCloner.GetFastDeepClonerProperties(o.GetType());
                var primaryKey = MethodHelper.GetPrimaryKey(o);
                var availableColumns = repository.ObjectColumns(o.GetType());
                var objectRules = o.GetType().GetCustomAttribute<Rule>();
                var tableName = o.GetType().GetCustomAttribute<Table>()?.Name ?? o.GetType().Name;

                object dbTrigger = null;
                if (objectRules != null && !CachedIDbRuleTrigger.ContainsKey(o.GetType()))
                {
     
                    dbTrigger = Activator.CreateInstance(objectRules.RuleType) as object;
                    CachedIDbRuleTrigger.Add(o.GetType(), dbTrigger);
                }
                else if (objectRules != null)
                    dbTrigger = CachedIDbRuleTrigger[o.GetType()];

                if (primaryKey == null)
                    return 0;
                var value = primaryKey.GetValue(o).ConvertValue<long?>();
                if (value <= 0)
                    value = null;
                else if (value.HasValue && !updateOnly)
                {
                    var data = Select(repository, o.GetType(), Querys.Where.Column(primaryKey.GetPropertyName()).Equal(value.Value).ToQueryItem).Rows.FirstOrDefault();
                    if (data != null)
                    {
                        o.Merge(data.ToObject(o.GetType()) as InterFace.IDbEntity);
                    }
                }


                if (!updateOnly)
                    dbTrigger?.GetType().GetMethod("BeforeSave").Invoke(dbTrigger,new List<object>(){ repository, o }.ToArray()); // Check the Rule before save

                o.State = ItemState.Added;// reset State

                var sql = "UPDATE [" + (o.GetType().GetCustomAttribute<Table>()?.Name ?? o.GetType().Name) + "] SET ";
                var cols = props.Where(x => availableColumns.FindByPrimaryKey<bool>(x.GetPropertyName()) && x.IsInternalType && x.GetCustomAttribute<ExcludeFromAbstract>() == null && x.GetCustomAttribute<PrimaryKey>() == null);

                if (!value.HasValue)
                {
                    sql = "INSERT INTO [" + tableName + "](" + string.Join(",", cols.Select(x => "[" + x.GetPropertyName() + "]")) + ") Values(";
                    sql += string.Join(",", cols.Select(x => "@" + x.GetPropertyName())) + "); SELECT IDENT_CURRENT('" + tableName + "');";
                }
                else
                {
                    sql += string.Join(",", cols.Select(x => "[" + x.GetPropertyName() + "]" + " = @" + MethodHelper.GetPropertyName(x)));
                    sql += Querys.Where.Column(primaryKey.GetPropertyName()).Equal(value).Execute();
                }

                var cmd = repository.GetSqlCommand(sql);

                foreach (var col in cols)
                {
                    var v = col.GetValue(o);
                    if (col.ContainAttribute<ForeignKey>() && MethodHelper.ConvertValue<long?>(v) == 0)
                    {
                        var ob = props.FirstOrDefault(x => x.PropertyType == col.GetCustomAttribute<ForeignKey>().Type);
                        var obValue = ob?.GetValue(o) as InterFace.IDbEntity;
                        var independentData = ob?.GetCustomAttribute<IndependentData>() != null;
                        if (obValue != null)
                        {
                            v = MethodHelper.ConvertValue<long>(MethodHelper.GetPrimaryKey(obValue).GetValue(obValue)) <= 0 ?
                                SaveObject(repository, obValue, independentData) :
                                MethodHelper.ConvertValue<long>(MethodHelper.GetPrimaryKey(obValue).GetValue(obValue));
                        }
                    }

                    repository.AddInnerParameter(cmd, col.GetPropertyName(), v, repository.GetSqlType(col.PropertyType));
                }

                if (!value.HasValue)
                    value = MethodHelper.ConvertValue<long>(repository.ExecuteScalar(cmd));
                else repository.ExecuteNonQuery(cmd);

                if (updateOnly)
                    return value.Value;
                dbTrigger?.GetType().GetMethod("AfterSave").Invoke(dbTrigger, new List<object>() { repository, o, value.Value }.ToArray()); // Check the Rule before save

                primaryKey.SetValue(o, value);
                foreach (var prop in props.Where(x => !x.IsInternalType && x.GetCustomAttribute<ExcludeFromAbstract>() == null))
                {
                    var independentData = prop.GetCustomAttribute<IndependentData>() != null;
                    var type = prop.PropertyType.GetActualType();
                    var oValue = prop.GetValue(o);
                    if (oValue == null)
                        continue;

                    if (oValue is IList)
                    {
                        foreach (var item in (IList)oValue)
                        {
                            if (DeepCloner.GetFastDeepClonerProperties(item.GetType()).Any(x => x.GetCustomAttribute<ForeignKey>()?.Type == o.GetType()))
                            {
                                DeepCloner.GetFastDeepClonerProperties(item.GetType()).First(x => x.GetCustomAttribute<ForeignKey>()?.Type == o.GetType()).SetValue(item, value);
                            }
                            var res = SaveObject(repository, item as InterFace.IDbEntity, independentData);
                            var foreignKey = FastDeepCloner.DeepCloner.GetFastDeepClonerProperties(o.GetType()).FirstOrDefault(x => x.GetCustomAttribute<ForeignKey>()?.Type == type);
                            if (foreignKey == null || MethodHelper.ConvertValue<long>(foreignKey.GetValue(o)) > 0) continue;
                            foreignKey.SetValue(o, res);
                            o.State = ItemState.Changed;
                        }
                    }
                    else
                    {
                        if (DeepCloner.GetFastDeepClonerProperties(oValue.GetType()).Any(x => x.GetCustomAttribute<ForeignKey>()?.Type == o.GetType()))
                        {
                            DeepCloner.GetFastDeepClonerProperties(oValue.GetType()).First(x => x.GetCustomAttribute<ForeignKey>()?.Type == o.GetType()).SetValue(oValue, value);
                        }

                        var res = SaveObject(repository, oValue as InterFace.IDbEntity, independentData);
                        var foreignKey = FastDeepCloner.DeepCloner.GetFastDeepClonerProperties(o.GetType()).FirstOrDefault(x => x.GetCustomAttribute<ForeignKey>()?.Type == type);
                        if (foreignKey == null || MethodHelper.ConvertValue<long>(foreignKey.GetValue(o)) > 0) continue;
                        foreignKey.SetValue(o, res);
                        o.State = ItemState.Changed;
                    }
                }

                if (o.State == ItemState.Changed) // a change has been made outside the function Save
                    SaveObject(repository, o, false);
                if (!commit) return value.Value;
                repository.Commit();
                return value.Value;
            }
            catch (Exception e)
            {
                repository.Rollback();
                throw e;
            }
        }



        /// <summary>
        /// Get object by ID
        /// Primary Key attribute must be set
        /// </summary>
        /// <param name="id"></param>
        /// <param name="repository"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static object GetSqlById(long id, ICustomRepository repository, Type type)
        {

            var sqlKey = type.GetActualType().FullName + "GetById";
            if (!CachedSql.ContainsKey(sqlKey))
            {
                var key = type.GetActualType().GetPrimaryKey().GetPropertyName();
                CachedSql.Add(sqlKey, Querys.Select(type.GetActualType()).Where.Column<long>(key).Equal("@ID", true).Execute());
            }
            var cmd = repository.GetSqlCommand(CachedSql[sqlKey]);
            repository.AddInnerParameter(cmd, "@ID", id, System.Data.SqlDbType.BigInt);
            if (type.IsGenericType && type.GetGenericTypeDefinition()?.Name == "List`1" && type.GenericTypeArguments.Length > 0)
            {
                return repository.GetLightDataTable(cmd).Rows.ToObject(type);
            }
            return repository.GetLightDataTable(cmd).Rows.FirstOrDefault()?.ToObject(type);
        }

        /// <summary>
        /// Get all by object 
        /// PrimaryKey attr must be set ins Where
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="repository"></param>
        /// <param name="quary"></param>
        /// <returns></returns>

        internal static List<T> Select<T>(ICustomRepository repository, QueryItem quary = null) where T : class
        {
            var type = typeof(T);
            var sql = new StringBuilder();
            sql.Append(Querys.Select(type).Execute());
            if (quary != null && quary.HasValue())
                sql.Append(quary.Execute());

            return repository.GetLightDataTable(repository.GetSqlCommand(sql.ToString())).Rows.ToObject<T>();
        }


        /// <summary>
        /// Get all by object 
        /// PrimaryKey attr must be set ins Where
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="repository"></param>
        /// <param name="quary"></param>
        /// <returns></returns>

        internal static List<T> Where<T>(ICustomRepository repository, string quary = null) where T : class
        {
            var type = typeof(T);
            var sql = new StringBuilder();
            sql.Append(Querys.Select(type).Execute());
            if (!string.IsNullOrEmpty(quary))
                sql = new StringBuilder(quary);

            return repository.GetLightDataTable(repository.GetSqlCommand(sql.ToString())).Rows.ToObject<T>();
        }


        internal static ILightDataTable Select(ICustomRepository repository, Type type, QueryItem quary = null)
        {
            var sql = new StringBuilder();
            sql.Append(Querys.Select(type).Execute());
            if (quary != null && quary.HasValue())
                sql.Append(quary.Execute());

            return repository.GetLightDataTable(repository.GetSqlCommand(sql.ToString()));
        }


        /// <summary>
        /// Get all by object 
        /// PrimaryKey attr must be set
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="repository"></param>
        /// <param name="quary"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static IList GetSqlAll(ICustomRepository repository, Type type)
        {
            var sqlKey = type.FullName + "GetSqlAll";
            if (!CachedSql.ContainsKey(sqlKey))
                CachedSql.Add(sqlKey, Querys.Select(type).Execute());
            return repository.GetLightDataTable(repository.GetSqlCommand(CachedSql[sqlKey])).Rows.ToObject(type);
        }


        /// <summary>
        /// Get all by object 
        /// Get object by column, as fogenKey
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="column"></param>
        /// <param name="repository"></param>
        /// <param name="quary"></param>
        /// <param name="id"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private static object GetByColumn(long id, string column, ICustomRepository repository, Type type)
        {
            var sqlKey = type.FullName + "GetByColumn" + column;
            if (!CachedSql.ContainsKey(sqlKey))
                CachedSql.Add(sqlKey, Querys.Select(type).Where.Column<long>(column).Equal("@ID", true).Execute());

            var cmd = repository.GetSqlCommand(CachedSql[sqlKey]);
            repository.AddInnerParameter(cmd, "@ID", id, System.Data.SqlDbType.BigInt);
            if (type.IsGenericType && type.GetGenericTypeDefinition()?.Name == "List`1" && type.GenericTypeArguments.Length > 0)
            {
                return repository.GetLightDataTable(cmd).Rows.ToObject(type);
            }

            return repository.GetLightDataTable(cmd).Rows.FirstOrDefault()?.ToObject(type);
        }


        internal static void CreateTable<T>(ICustomRepository repository, bool force= false)
        {
            var tableData = ObjectColumns(repository, typeof(T));
            if (tableData == null || tableData.Rows.Any())
                return;
            repository.CreateTransaction();
            try
            {
                var props = DeepCloner.GetFastDeepClonerProperties(typeof(T));
                var tableName = typeof(T).GetCustomAttribute<Table>()?.Name ?? typeof(T).Name;
                var sql = new StringBuilder("CREATE TABLE [dbo].[" + tableName + "](");
                var isPrimaryKey = "";
                foreach (var prop in props.Where(x => x.PropertyType.GetDbTypeByType() != null && !x.ContainAttribute<ExcludeFromAbstract>()).GroupBy(x=> x.Name).Select(x=> x.First()))
                {
                    isPrimaryKey = prop.ContainAttribute<PrimaryKey>() ? prop.GetPropertyName() : isPrimaryKey;
                    var dbType = prop.PropertyType.GetDbTypeByType();
                    var propName = prop.GetPropertyName();
                    sql.Append(propName + " ");
                    sql.Append(dbType + " ");
                    if (prop.ContainAttribute<PrimaryKey>())
                    {
                        sql.Append("IDENTITY(1,1) NOT NULL,");
                        continue;
                    }

                    sql.Append(Nullable.GetUnderlyingType(prop.PropertyType) != null ? " NULL," : " NOT NULL,");
                }

                if (!string.IsNullOrEmpty(isPrimaryKey))
                {
                    sql.Append(" CONSTRAINT [PK_"+tableName+"] PRIMARY KEY CLUSTERED");
                    sql.Append(" ([" + isPrimaryKey + "] ASC");
                    sql.Append(")");
                    sql.Append(
                        "WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]");
                    sql.Append(") ON [PRIMARY]");
                }
                else sql.Append(")");
                var cmd = repository.GetSqlCommand(sql.ToString());
                repository.ExecuteNonQuery(cmd);
                repository.Commit();
            }
            catch (Exception ex)
            {
                repository.Rollback();
                throw ex;
            }

        }


        internal static List<string> DeleteAbstract(ICustomRepository repository, object o, bool save = false)
        {
            var type = o.GetType().GetActualType();
            var props = FastDeepCloner.DeepCloner.GetFastDeepClonerProperties(type);
            var table = type.GetCustomAttribute<Table>()?.Name ?? type.Name;
            var primaryKey = MethodHelper.GetPrimaryKey(o as IDbEntity);
            var primaryKeyValue = MethodHelper.ConvertValue<long>(primaryKey.GetValue(o));
            if (primaryKeyValue <= 0)
                return new List<string>();
            var sql = new List<string>() { "DELETE " + table + Querys.Where.Column(primaryKey.GetPropertyName()).Equal(primaryKeyValue).Execute() };

            foreach (var prop in props.Where(x => !x.IsInternalType && x.GetCustomAttribute<IndependentData>() == null && x.GetCustomAttribute<ExcludeFromAbstract>() == null))
            {
                var value = prop.GetValue(o);

                if (value == null)
                    continue;
                var subSql = new List<string>();
                var propType = prop.PropertyType.GetActualType();
                var insertBefore = props.Any(x => x.GetCustomAttribute<ForeignKey>()?.Type == propType);
                if (FastDeepCloner.DeepCloner.GetFastDeepClonerProperties(propType).All(x => x.GetCustomAttribute<ForeignKey>()?.Type != type))
                    if (!insertBefore)
                        continue;
                if (value is IList)
                    foreach (var item in value as IList)
                    {
                        subSql.AddRange(DeleteAbstract(repository, item));
                    }
                else
                    subSql.AddRange(DeleteAbstract(repository, value));

                if (insertBefore)
                    sql.InsertRange(sql.Count - 1, subSql);
                else sql.AddRange(subSql);
            }

            if (!save) return sql;
            try
            {
                repository.CreateTransaction();
                // begin deleting all object that refer to the requasted object
                for (var i = sql.Count - 1; i >= 0; i--)
                {
                    var cmd = repository.GetSqlCommand(sql[i]);
                    cmd.ExecuteNonQuery();
                }
                repository.Commit();
            }
            catch (Exception e)
            {
                repository.Rollback();
                throw e;
            }
            return sql;
        }


        internal static T LoadChildren<T>(T item, ICustomRepository repository, bool onlyFirstLevel = false, List<string> classes = null, List<string> ignoreList = null, Dictionary<long, List<string>> pathLoaded = null, string parentProb = null, long id = 0) where T : class
        {

            if (pathLoaded == null)
                pathLoaded = new Dictionary<long, List<string>>();
            switch (item)
            {
                case null:
                    return null;
                case IList _:
                    foreach (var tItem in (IList)item)
                    {
                        var entity = tItem as IDbEntity;
                        if (entity == null)
                            continue;
                        LoadChildren(entity, repository, onlyFirstLevel, classes, ignoreList, pathLoaded, parentProb, entity.Id);
                    }
                    break;
                default:
                    if ((item as IDbEntity) == null)
                        return item;
                    (item as IDbEntity)?.ClearPropertChanges();
                    var props = FastDeepCloner.DeepCloner.GetFastDeepClonerProperties(item.GetType());

                    id = (item as IDbEntity).Id;
                    foreach (var prop in props.Where(x => !x.IsInternalType && !x.ContainAttribute<ExcludeFromAbstract>()))
                    {
                        var path = string.Format("{0}.{1}", parentProb ?? "", prop.Name).TrimEnd('.').TrimStart('.');
                        var propCorrectPathName = path?.Split('.').Length >= 2 ? string.Join(".", path.Split('.').Reverse().Take(2).Reverse()) : path;

                        if (classes != null && classes.All(x => x != propCorrectPathName))
                            continue;
                        if (ignoreList != null && ignoreList.Any(x => x == propCorrectPathName))
                            continue;

                        var propValue = prop.GetValue(item);
                        if (propValue != null)
                            continue;
                        if (pathLoaded.ContainsKey(id) && pathLoaded[id].Any(x => x == path))
                            continue;

                        if (!pathLoaded.ContainsKey(id))
                            pathLoaded.Add(id, new List<string>() { path });
                        else if (pathLoaded[id].All(x => x != path)) pathLoaded[id].Add(path);

                        var propertyName = prop.Name;
                        if (path?.Split('.').Length >= 2)
                            propertyName = string.Join(".", path.Split('.').Reverse().Take(3).Reverse()) + "." + parentProb.Split('.').Last() + "." + propertyName;

                        var ttype = prop.PropertyType.GetActualType();

                        var key = props.FirstOrDefault(x => x.ContainAttribute<ForeignKey>() && x.GetCustomAttribute<ForeignKey>().Type == ttype);
                        if (key == null)
                        {
                            var column = DeepCloner.GetFastDeepClonerProperties(ttype).FirstOrDefault(x => x.ContainAttribute<ForeignKey>() && x.GetCustomAttribute<ForeignKey>().Type == item.GetType());
                            var primaryKey = MethodHelper.GetPrimaryKey(item as IDbEntity);
                            if (column == null || primaryKey == null)
                                continue;
                            var keyValue = primaryKey.GetValue(item).ConvertValue<long?>();
                            if (!keyValue.HasValue) continue;
                            var result = GetByColumn(keyValue.Value, column.Name, repository, prop.PropertyType);
                            prop.SetValue(item, result);
                            if (result != null && !onlyFirstLevel)
                                LoadChildren(result, repository, onlyFirstLevel, classes, ignoreList, pathLoaded, propertyName, id);
                            (item as IDbEntity)?.ClearPropertChanges();
                        }
                        else
                        {
                            var keyValue = MethodHelper.ConvertValue<long?>(key.GetValue(item));
                            if (!keyValue.HasValue) continue;
                            var result = GetSqlById(keyValue.Value, repository, prop.PropertyType);
                            prop.SetValue(item, result);
                            if (result != null && !onlyFirstLevel)
                                LoadChildren(result, repository, onlyFirstLevel, classes, ignoreList, pathLoaded, propertyName, id);
                            (item as IDbEntity)?.ClearPropertChanges();
                        }
                    }

                    break;
            }

            return item;
        }

    }
}
