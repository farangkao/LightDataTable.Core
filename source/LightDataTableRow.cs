using Generic.LightDataTable.Helper;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using Generic.LightDataTable.InterFace;
using System.Dynamic;
using System.Collections.Generic;
using System.Reflection;

namespace Generic.LightDataTable
{
    public class LightDataTableRow : LightDataTableShared
    {
        public ItemState RowState { get; set; }

        private object[] _itemArray;
        [JsonProperty(Order = 4)]
        public object[] ItemArray
        {
            get => _itemArray;
            set
            {
                if (_itemArray == null)
                    _itemArray = new object[ColumnLength];
                for (var i = 0; i <= value.Length - 1; i++)
                    this[i] = value[i];
            }
        }

        public object this[string columnName, bool loadDefaultOnError = false]
        {
            get
            {
                try
                {

                    if (loadDefaultOnError)
                        TypeValidation(ref _itemArray[Columns[columnName].ColumnIndex], Columns[columnName].DataType, loadDefaultOnError, Columns[columnName].DefaultValue);

                    return _itemArray[Columns[columnName].ColumnIndex];
                }
                catch (Exception ex)
                {
                    throw new Exception("ColumnName:" + columnName + " " + ex.Message);
                }
            }
            set
            {
                try
                {
                    var column = Columns[columnName];
                    if (loadDefaultOnError)
                        TypeValidation(ref value, column.DataType, loadDefaultOnError, column.DefaultValue);
                    _itemArray[column.ColumnIndex] = value;
                }
                catch (Exception ex)
                {
                    throw new Exception("ColumnName:" + columnName + " " + ex.Message);
                }
            }
        }

        public object this[int columnIndex, bool loadDefaultOnError = false]
        {
            get
            {
                try
                {
                    if (loadDefaultOnError)
                        TypeValidation(ref _itemArray[columnIndex], ColumnsWithIndexKey[columnIndex].DataType, loadDefaultOnError, ColumnsWithIndexKey[columnIndex].DefaultValue);
                    return _itemArray[columnIndex];
                }
                catch (Exception ex)
                {
                    throw new Exception("ColumnName:" + ColumnsWithIndexKey[columnIndex].ColumnName + " " + ex.Message);
                }
            }
            set
            {
                try
                {
                    var column = ColumnsWithIndexKey[columnIndex];
                    if (loadDefaultOnError)
                        TypeValidation(ref value, column.DataType, loadDefaultOnError, column.DefaultValue);

                    _itemArray[column.ColumnIndex] = value;
                }
                catch (Exception ex)
                {
                    throw new Exception("ColumnName:" + ColumnsWithIndexKey[columnIndex].ColumnName + " " + ex.Message);
                }
            }
        }

        public object this[LightDataTableColumn column, bool loadDefaultOnError = false]
        {
            get
            {
                if (loadDefaultOnError)
                    TypeValidation(ref _itemArray[column.ColumnIndex], column.DataType, loadDefaultOnError, column.DefaultValue);
                return _itemArray[column.ColumnIndex];
            }
            set
            {
                if (loadDefaultOnError)
                    TypeValidation(ref value, column.DataType, loadDefaultOnError, column.DefaultValue);
                _itemArray[column.ColumnIndex] = value;
            }
        }

        public LightDataTableRow() : base()
        {
        }

        internal LightDataTableRow(object[] itemArray, ColumnsCollections<string> columns, ColumnsCollections<int> columnWithIndex, CultureInfo cultureInfo = null) : base(cultureInfo)
        {
            base.Columns = columns;
            base.ColumnsWithIndexKey = columnWithIndex;
            ItemArray = itemArray;
            ColumnLength = itemArray.Length - 1;
        }

        internal LightDataTableRow(LightDataTableRow row, ColumnsCollections<string> columns, ColumnsCollections<int> columnWithIndex, CultureInfo cultureInfo = null) : base(cultureInfo)
        {
            base.Columns = columns;
            base.ColumnsWithIndexKey = columnWithIndex;
            ItemArray = row.ItemArray;
            ColumnLength = row.ColumnLength;
        }

        internal LightDataTableRow(int columnLength, ColumnsCollections<string> columns, ColumnsCollections<int> columnWithIndex, CultureInfo cultureInfo = null) : base(cultureInfo)
        {
            base.Columns = columns;
            base.ColumnsWithIndexKey = columnWithIndex;
            _itemArray = new object[columnLength];
            ColumnLength = columnLength;
        }

        /// <summary>
        /// Column ContainKey By Exepression
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TP"></typeparam>
        /// <param name="action"></param>
        /// <returns></returns>
        public bool ContainKey<T, TP>(Expression<Func<T, TP>> action) where T : class
        {
            var member = (MemberExpression)action.Body;
            var propertyName = member.Member.Name;
            return Columns.ContainsKey(propertyName);

        }

        /// <summary>
        /// Set Value By Expressions
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TP"></typeparam>
        /// <param name="action"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public TP SetTValue<T, TP>(Expression<Func<T, TP>> action, TP value) where T : class
        {
            var member = action.Body is UnaryExpression ? ((MemberExpression)((UnaryExpression)action.Body).Operand) : (action.Body is MethodCallExpression ? ((MemberExpression)((MethodCallExpression)action.Body).Object) : (MemberExpression)action.Body);
            var key = member?.Member.Name;
            return (TP)(this[key] = value);

        }


        /// <summary>
        /// return already converted value by T eg row<string>(0) its alot faster when reading values, but the retuned values is wont be a shered one
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T TValue<T>(int key)
        {
            return (T)this[key];
        }

        /// <summary>
        /// return already converted value by T eg row<string>(0) its alot faster when reading values, but the retuned values is wont be a shered one
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T TValue<T>(string key)
        {
            return (T)this[key];
        }

        /// <summary>
        /// Get Propert By expression;
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TP"></typeparam>
        /// <param name="action"></param>
        /// <returns></returns>
        public TP TValue<T, TP>(Expression<Func<T, TP>> action) where T : class
        {
            var member = action.Body is UnaryExpression ? ((MemberExpression)((UnaryExpression)action.Body).Operand) : (action.Body is MethodCallExpression ? ((MemberExpression)((MethodCallExpression)action.Body).Object) : (MemberExpression)action.Body);
            var propertyName = member?.Member.Name;
            var v = this[propertyName];
            TypeValidation(ref v, typeof(TP), true);
            return (TP)v;
        }

        /// <summary>
        /// return already converted value by T eg row<string>(0) its alot faster when reading values, but the retuned values is wont be a shered one
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T TValue<T>(LightDataTableColumn key)
        {
            return (T)this[key];
        }

        public T TValue<T>(Enum key)
        {
            return (T)this[key.ToString()];
        }

        public T TValueAndConvert<T>(Enum key)
        {
            return this.TValueAndConvert<T>(key.ToString());
        }

        public T TryValueAndConvert<T>(Enum key, bool loadDefault = false)
        {
            return this.TryValueAndConvert<T>(key.ToString(), loadDefault);
        }


        public T TValueAndConvert<T>(int key, bool loadDefault = false)
        {
            var column = this.ColumnsWithIndexKey[key];
            return this.TValueAndConvert<T>(column.ColumnName, loadDefault);
        }

        /// <summary>
        /// This will try to load the selected value and convert it to the selected type when it fails
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="loadDefault"></param>
        /// <returns></returns>
        public T TValueAndConvert<T>(string key, bool loadDefault = false)
        {
            var v = this[key, loadDefault];
            var type = typeof(T);
            if (v != DBNull.Value && v != null && v.GetType() != Columns[key].DataType && !(type.GetTypeInfo().IsGenericType && type.GetTypeInfo().GetGenericTypeDefinition() == typeof(Nullable<>)) && v != "")
                return (T)Convert.ChangeType(this[key], type);
            if (v == null || v == DBNull.Value || string.IsNullOrEmpty(v.ToString()))
            {
                TypeValidation(ref v, type, true);

                if (v == null && type.GetTypeInfo().IsGenericType && type.GetTypeInfo().GetGenericTypeDefinition() == typeof(Nullable<>))
                    return default(T);
            }
            else
                TypeValidation(ref v, type, false);

            if (v?.GetType() != type && (!type.GetTypeInfo().IsGenericType || type.GetTypeInfo().GetGenericTypeDefinition() != typeof(Nullable<>)))
                return (T)Convert.ChangeType(v, type);

            if (v?.GetType() != type)
                return (T)Convert.ChangeType(v, type.GetGenericArguments()[0]);
            return (T)v;

        }

        public T TryValueAndConvert<T>(string key, bool loadDefault = false)
        {
            try
            {
                return TValueAndConvert<T>(key, loadDefault);
            }
            catch
            {
                return (T)ValueByType(typeof(T));
            }
        }


        public T TryValueAndConvert<T>(int index, bool loadDefault = false)
        {
            try
            {
                return TValueAndConvert<T>(index, loadDefault);
            }
            catch
            {
                return (T)ValueByType(typeof(T));
            }
        }

        public TP TValueAndConvert<T, TP>(Expression<Func<T, TP>> action) where T : class
        {
            var member = action.Body is UnaryExpression ? ((MemberExpression)((UnaryExpression)action.Body).Operand) : (action.Body is MethodCallExpression ? ((MemberExpression)((MethodCallExpression)action.Body).Object) : (MemberExpression)action.Body);
            var propertyName = member?.Member.Name;
            return (TP)TValueAndConvert<TP>(propertyName);
        }


        /// <summary>
        /// Convert LightDataRow to DataRow
        /// </summary>
        /// <param name="parentTable"></param>
        /// <returns></returns>
        public DataRow ToDataRow(DataTable parentTable)
        {
            var row = parentTable.NewRow();
            foreach (var item in Columns.Values)
                if (parentTable.Columns.Contains(item.ColumnName))
                {
                    var v = this[item.ColumnName];
                    TypeValidation(ref v, parentTable.Columns[item.ColumnName].DataType, true);
                    if (row[item.ColumnName] != v)
                        row[item.ColumnName] = v;

                }
            return row;
        }

        /// <summary>
        /// Merge two rows together.
        /// </summary>
        /// <param name="row"></param>
        public LightDataTableRow Merge(LightDataTableRow row)
        {
            foreach (var item in row.Columns)
                if (Columns.ContainsKey(item.Key))
                    if (this[item.Key] != row[item.Key])
                        this[item.Key, true] = row[item.Key, true];
            return this;
        }

        /// <summary>
        /// Merge a class to the selected LightDataRow
        /// </summary>
        /// <param name="objectToBeMerged"></param>
        /// <returns></returns>
        public LightDataTableRow MergeUnKnownObject(object objectToBeMerged)
        {
            if (!(objectToBeMerged is ExpandoObject))
            {
                foreach (var property in FastDeepCloner.DeepCloner.GetFastDeepClonerProperties(objectToBeMerged.GetType()))
                {
                    var name = Columns.ContainsKey(property.Name) ? property.Name : property.GetPropertyName();
                    if (!Columns.ContainsKey(name)) continue;
                    try
                    {
                        var v = property.GetValue(objectToBeMerged);
                        TypeValidation(ref v, Columns[name].DataType, true);
                        if (name != null && this[name] != v)
                            this[name] = v;
                    }
                    catch
                    {
                        // Ignore
                    }
                }
            }
            else
            {
                var dictionary = (IDictionary<string, object>)objectToBeMerged;
                foreach (var key in dictionary.Keys)
                {
                    var name = Columns.ContainsKey(key) ? key : null;
                    if (name == null)
                        continue;
                    var v = dictionary[key];
                    TypeValidation(ref v, Columns[name].DataType, true);
                    if (name != null && this[name] != v)
                        this[name] = v;
                }
            }
            return this;
        }
        /// <summary>
        /// Merge LightDataRow to an object
        /// </summary>
        /// <param name="selectedObject"></param>
        public void MergeToAnObject(object selectedObject)
        {
            foreach (var prop in FastDeepCloner.DeepCloner.GetFastDeepClonerProperties(selectedObject.GetType()))
            {
                var name = Columns.ContainsKey(prop.Name) ? prop.Name : prop.GetPropertyName();
                if (!Columns.ContainsKey(name) || !prop.CanRead) continue;
                try
                {
                    var v = this[name];
                    TypeValidation(ref v, prop.PropertyType, true);
                    prop.SetValue(selectedObject, v);
                }
                catch
                {
                    // Ignore
                }
            }
        }

        /// <summary>
        /// Convert the current created row to an object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T ToObject<T>()
        {
            var o = FormatterServices.GetUninitializedObject(typeof(T));
            var obj = o is IList
                ? 
                    o.GetType().GetActualType().CreateInstance()
                : typeof(T).CreateInstance();
            foreach (var pr in FastDeepCloner.DeepCloner.GetFastDeepClonerProperties(obj.GetType()))
            {
                var name = pr.GetPropertyName();
                if (!Columns.ContainsKey(name) || !pr.CanRead)
                    continue;
                var value = this[name, true];
                TypeValidation(ref value, pr.PropertyType, true);
                try
                {
                    pr.SetValue(obj, value);
                }
                catch
                {
                    // ignored
                }
            }
            (obj as IDbEntity)?.ClearPropertChanges();

            if (o is IList)
            {
                ((IList)o).Add(obj);
                return (T)o;
            }
            else
                return (T)obj;

        }

        /// <summary>
        /// Convert the current created row to an object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public object ToObject(object t)
        {
            var type = t is Type ? (Type)t : t.GetType();
            var o = type.CreateInstance();
            var obj = type.GetActualType().CreateInstance();

            foreach (var pr in FastDeepCloner.DeepCloner.GetFastDeepClonerProperties(obj?.GetType()))
            {
                var name = pr.GetPropertyName();
                if (!Columns.ContainsKey(name) || !pr.CanRead)
                    continue;
                var value = this[name, true];
                TypeValidation(ref value, pr.PropertyType, true);
                try
                {
                    pr.SetValue(obj, value);
                }
                catch
                {
                    // ignored
                }
            }
            (obj as IDbEntity)?.ClearPropertChanges();
            if (o is IList)
            {
                ((IList)o).Add(obj);
                return o;
            }
            else
                return obj;

        }


        /// <summary>
        /// This Method should only be called from the lightdatatable object
        /// </summary>
        /// <param name="col"></param>
        /// <param name="value"></param>
        /// <param name="cols"></param>
        /// <param name="colsIndex"></param>
        internal void AddValue(LightDataTableColumn col, object value, ColumnsCollections<string> cols, ColumnsCollections<int> colsIndex)
        {
            ColumnsWithIndexKey = colsIndex;
            Columns = cols;
            ColumnLength = colsIndex.Count;
            var newList = _itemArray.ToList();
            newList.Add(value ?? ValueByType(col.DataType));
            _itemArray = newList.ToArray();
        }


        /// <summary>
        /// This Method should only be called from the lightdatatable object
        /// </summary>
        /// <param name="columnIndex"></param>
        /// <param name="cols"></param>
        /// <param name="colsIndex"></param>
        internal void Remove(int columnIndex, ColumnsCollections<string> cols, ColumnsCollections<int> colsIndex)
        {
            ColumnsWithIndexKey = colsIndex;
            Columns = cols;
            var newList = _itemArray.ToList();
            newList.RemoveAt(columnIndex);
            _itemArray = newList.ToArray();
        }
    }
}
