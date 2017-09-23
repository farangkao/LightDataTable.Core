using System.Collections.Generic;
using System.Linq;

namespace Generic.LightDataTable
{
    internal class PrimaryKeyIndex
    {
        private Dictionary<object, LightDataTableRow> SavedIndexes = new Dictionary<object, LightDataTableRow>();

        internal LightDataTableRow this[object key]
        {
            get
            {
                return SavedIndexes[key];
            }
        }

        internal bool ContainValue(object value)
        {
            return SavedIndexes.ContainsKey(value);
        }

        internal void AddValue(object oldValue, object newValue, LightDataTableRow index)
        {
            if (oldValue != null && ContainValue(oldValue))
            {
                if (!ContainValue(newValue))
                {
                    SavedIndexes.Remove(oldValue);
                    SavedIndexes.Add(newValue, index);
                }
            }
            else if (newValue != null && !ContainValue(newValue))
                SavedIndexes.Add(newValue, index);
        }

        /// <summary>
        /// For faster search we index all values for primaryKey.
        /// </summary>
        /// <param name="rows"></param>
        /// <param name="key"></param>
        internal void ClearAndRenderValues(List<LightDataTableRow> rows, string key)
        {
            SavedIndexes.Clear();
            if (!string.IsNullOrEmpty(key))
            {
                if (rows.Any())
                    SavedIndexes = rows.GroupBy(x => x[key]).Select(x => x.First()).ToList().FindAll(x => x[key] != null).ToDictionary(x => x[key], x => x);

            }
        }

    }
}
