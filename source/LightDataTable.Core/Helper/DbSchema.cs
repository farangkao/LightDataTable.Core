﻿using Generic.LightDataTable.SqlQuerys;
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
using Generic.LightDataTable.Transaction;
using System.Data.Common;

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
            var cmd = repository.GetSqlCommand(repository.GetDataBaseType() == DataBaseTypes.Mssql ?
                "SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = N'" + table + "'"
                : "SELECT name as COLUMN_NAME, type as DATA_TYPE  FROM pragma_table_info('" + table + "');");
            var data = repository.GetLightDataTable(cmd, "COLUMN_NAME");
            if (data.Rows.Any())
                CachedObjectColumn.Add(type, data);
            else return data;
            return CachedObjectColumn[type];
        }

        internal static long SaveObject(ICustomRepository repository, InterFace.IDbEntity o, bool isIndependentData = false, bool commit = false)
        {
            repository.CreateTransaction();
            try
            {

                var updateOnly = o.State == ItemState.Changed;
                o.State = ItemState.Added;// reset State
                var props = DeepCloner.GetFastDeepClonerProperties(o.GetType());
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
                    var data = Select(repository, o.GetType(), Querys.Where(repository.GetDataBaseType() == DataBaseTypes.Sqllight).Column(primaryKey.GetPropertyName()).Equal(value.Value).ToQueryItem).Rows.FirstOrDefault();
                    if (data != null)
                    {
                        o.Merge(data.ToObject(o.GetType()) as InterFace.IDbEntity);
                    }
                }


                if (!updateOnly)
                    dbTrigger?.GetType().GetMethod("BeforeSave").Invoke(dbTrigger, new List<object>() { repository, o }.ToArray()); // Check the Rule before save

                o.State = ItemState.Added;// reset State

                var sql = "UPDATE [" + (o.GetType().GetCustomAttribute<Table>()?.Name ?? o.GetType().Name) + "] SET ";
                var cols = props.Where(x => availableColumns.FindByPrimaryKey<bool>(x.GetPropertyName()) && x.IsInternalType && x.GetCustomAttribute<ExcludeFromAbstract>() == null && x.GetCustomAttribute<PrimaryKey>() == null);

                if (!value.HasValue)
                {
                    sql = "INSERT INTO [" + tableName + "](" + string.Join(",", cols.Select(x => "[" + x.GetPropertyName() + "]")) + ") Values(";
                    sql += string.Join(",", cols.Select(x => "@" + x.GetPropertyName())) + ");";
                    sql += repository.GetDataBaseType() == DataBaseTypes.Sqllight ? " select last_insert_rowid();" : " SELECT IDENT_CURRENT('" + tableName + "');";
                }
                else
                {
                    sql += string.Join(",", cols.Select(x => "[" + x.GetPropertyName() + "]" + " = @" + MethodHelper.GetPropertyName(x)));
                    sql += Querys.Where(repository.GetDataBaseType() == DataBaseTypes.Sqllight).Column(primaryKey.GetPropertyName()).Equal(value).Execute();
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
                CachedSql.Add(sqlKey, Querys.Select(type.GetActualType(), repository.GetDataBaseType() == DataBaseTypes.Sqllight).Where.Column<long>(key).Equal("@ID", true).Execute());
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
            sql.Append(Querys.Select(type, repository.GetDataBaseType() == DataBaseTypes.Sqllight).Execute());
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
            sql.Append(Querys.Select(type, repository.GetDataBaseType() == DataBaseTypes.Sqllight).Execute());
            if (!string.IsNullOrEmpty(quary))
                sql = new StringBuilder(quary);

            return repository.GetLightDataTable(repository.GetSqlCommand(sql.ToString())).Rows.ToObject<T>();
        }


        internal static ILightDataTable Select(ICustomRepository repository, Type type, QueryItem quary = null)
        {
            var sql = new StringBuilder();
            sql.Append(Querys.Select(type, repository.GetDataBaseType() == DataBaseTypes.Sqllight).Execute());
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
                CachedSql.Add(sqlKey, Querys.Select(type, repository.GetDataBaseType() == DataBaseTypes.Sqllight).Execute());
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
                CachedSql.Add(sqlKey, Querys.Select(type, repository.GetDataBaseType() == DataBaseTypes.Sqllight).Where.Column<long>(column).Equal("@ID", true).Execute());

            var cmd = repository.GetSqlCommand(CachedSql[sqlKey]);
            repository.AddInnerParameter(cmd, "@ID", id, System.Data.SqlDbType.BigInt);
            if (type.IsGenericType && type.GetGenericTypeDefinition()?.Name == "List`1" && type.GenericTypeArguments.Length > 0)
            {
                return repository.GetLightDataTable(cmd).Rows.ToObject(type);
            }

            return repository.GetLightDataTable(cmd).Rows.FirstOrDefault()?.ToObject(type);
        }


        internal static void RemoveTable(ICustomRepository repository, Type tableType, bool commit = false, List<Type> tableRemoved = null, bool remove = true)
        {
            if (commit)
                repository.CreateTransaction();
            if (tableRemoved == null)
                tableRemoved = new List<Type>();
            if (tableRemoved.Any(x => x == tableType))
                return;
            tableRemoved.Insert(0, tableType);
            var props = DeepCloner.GetFastDeepClonerProperties(tableType);

            foreach (var prop in props.Where(x => (!x.IsInternalType || x.ContainAttribute<ForeignKey>()) && !x.ContainAttribute<ExcludeFromAbstract>()))
            {
                var key = prop.GetCustomAttribute<ForeignKey>();
                if (key != null && tableRemoved.Any(x => x == key.Type))
                    continue;
                if (key != null)
                    RemoveTable(repository, key.Type, commit, tableRemoved, false);
                else RemoveTable(repository, prop.PropertyType.GetActualType(), commit, tableRemoved, false);

            }

            if (!remove)
                return;

            var tableData = ObjectColumns(repository, tableType);
            if (!tableData.Rows.Any())
                return;
            var c = tableRemoved.Count;
            while (c > 0)
            {

                for (var i = tableRemoved.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        var tType = tableRemoved[i];
                        CachedObjectColumn.Remove(tType);
                        var tableName = tType.GetCustomAttribute<Table>()?.Name ?? tType.Name;
                        var cmd = repository.GetSqlCommand("DROP TABLE [" + tableName + "];");
                        repository.ExecuteNonQuery(cmd);
                        c--;
                    }
                    catch { }
                }
            }
            if (commit)
                repository.Commit();

        }


        internal static string CreateTable(ICustomRepository repository, Type tableType, List<Type> createdTables = null, bool commit = true, bool force = false, Dictionary<string, Tuple<string, ForeignKey>> keys = null)
        {
            if (createdTables == null)
                createdTables = new List<Type>();
            var tableData = ObjectColumns(repository, tableType);
            if (createdTables.Any(x => x == tableType))
                return null;
            if (!force && tableData.Rows.Any())
                return null;

            repository.CreateTransaction();
            RemoveTable(repository, tableType);
            createdTables.Add(tableType);
            if (keys == null)
                keys = new Dictionary<string, Tuple<string, ForeignKey>>();

            List<string> sqlList = new List<string>();
            try
            {
                var isSqllite = repository.GetDataBaseType() == DataBaseTypes.Sqllight;
                var props = DeepCloner.GetFastDeepClonerProperties(tableType);
                var tableName = tableType.GetCustomAttribute<Table>()?.Name ?? tableType.Name;
                var sql = new StringBuilder("CREATE TABLE " + (!isSqllite ? "[dbo]." : "") + "[" + tableName + "](");
                var isPrimaryKey = "";
                foreach (var prop in props.Where(x => x.PropertyType.GetDbTypeByType() != null && !x.ContainAttribute<ExcludeFromAbstract>() && x.IsInternalType).GroupBy(x => x.Name).Select(x => x.First()))
                {

                    isPrimaryKey = prop.ContainAttribute<PrimaryKey>() ? prop.GetPropertyName() : isPrimaryKey;
                    var forgenKey = prop.GetCustomAttribute<ForeignKey>();
                    var dbType = prop.PropertyType.GetDbTypeByType();
                    var propName = prop.GetPropertyName();
                    sql.Append(propName + " ");
                    if (!prop.ContainAttribute<PrimaryKey>() || !isSqllite)
                        sql.Append(dbType + " ");

                    if (forgenKey != null)
                        sqlList.Add(CreateTable(repository, forgenKey.Type, createdTables, false, force, keys));

                    if (prop.ContainAttribute<PrimaryKey>())
                    {
                        if (!isSqllite)
                            sql.Append("IDENTITY(1,1) NOT NULL,");
                        else sql.Append(" Integer PRIMARY KEY AUTOINCREMENT,");
                        continue;
                    }
                    if (forgenKey != null)
                    {
                        keys.Add(propName, new Tuple<string, ForeignKey>(tableName, forgenKey));
                    }

                    sql.Append(Nullable.GetUnderlyingType(prop.PropertyType) != null ? " NULL," : " NOT NULL,");
                }

                if (keys.Any() && isSqllite)
                {
                    foreach (var key in keys)
                    {
                        var type = key.Value.Item2.Type.GetActualType();
                        var keyPrimary = type.GetPrimaryKey().GetPropertyName();
                        var tb = type.GetCustomAttribute<Table>().Name ?? type.Name;
                        sql.Append("FOREIGN KEY(" + key.Key + ") REFERENCES " + tb + "(" + keyPrimary + "),");

                    }
                    keys.Clear();
                }

                if (!string.IsNullOrEmpty(isPrimaryKey) && !isSqllite)
                {
                    sql.Append(" CONSTRAINT [PK_" + tableName + "] PRIMARY KEY CLUSTERED");
                    sql.Append(" ([" + isPrimaryKey + "] ASC");
                    sql.Append(")");
                    sql.Append(
                        "WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]");
                    sql.Append(") ON [PRIMARY]");
                }
                else
                {
                    if (isSqllite)
                        sql = new StringBuilder(sql.ToString().TrimEnd(','));
                    sql.Append(")");
                }

                if (!commit)
                    return sql.ToString();

                foreach (var prop in props.Where(x => !x.IsInternalType && !x.ContainAttribute<ExcludeFromAbstract>()).GroupBy(x => x.Name).Select(x => x.First()))
                {
                    var type = prop.PropertyType.GetActualType();
                    sqlList.Add(CreateTable(repository, type, createdTables, false, force, keys));
                }



                sqlList.Insert(0, sql.ToString());
                sqlList.RemoveAll(x => string.IsNullOrEmpty(x));
                var c = sqlList.Count;
                while (c > 0)
                {
                    for (var i = sqlList.Count - 1; i >= 0; i--)
                    {
                        try
                        {
                            var s = sqlList[i];
                            var cmd = repository.GetSqlCommand(s);
                            repository.ExecuteNonQuery(cmd);
                            c--;
                        }
                        catch (Exception ex)
                        {
                            var test = ex;
                        }
                    }
                }
                sql = new StringBuilder();

                if (keys.Any() && !isSqllite)
                {
                    foreach (var key in keys)
                    {
                        var type = key.Value.Item2.Type.GetActualType();
                        var keyPrimary = type.GetPrimaryKey().GetPropertyName();
                        var tb = type.GetCustomAttribute<Table>()?.Name ?? type.Name;
                        sql.Append("ALTER TABLE [" + key.Value.Item1 + "] ADD FOREIGN KEY (" + key.Key + ") REFERENCES [" + tb + "](" + keyPrimary + ");");

                    }
                    var s = sql.ToString();
                    var cmd = repository.GetSqlCommand(s);
                    repository.ExecuteNonQuery(cmd);

                }
                repository.Commit();
            }
            catch (Exception ex)
            {
                repository.Rollback();
                throw ex;
            }
            return string.Empty;

        }


        internal static List<string> DeleteAbstract(ICustomRepository repository, object o, bool save = false)
        {
            var type = o.GetType().GetActualType();
            var props = FastDeepCloner.DeepCloner.GetFastDeepClonerProperties(type);
            var table ="[" + type.GetCustomAttribute<Table>()?.Name ?? type.Name + "]";
            var primaryKey = MethodHelper.GetPrimaryKey(o as IDbEntity);
            var primaryKeyValue = MethodHelper.ConvertValue<long>(primaryKey.GetValue(o));
            if (primaryKeyValue <= 0)
                return new List<string>();
            var sql = new List<string>() { "DELETE " + table + Querys.Where(repository.GetDataBaseType() == DataBaseTypes.Sqllight).Column(primaryKey.GetPropertyName()).Equal(primaryKeyValue).Execute() };

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
