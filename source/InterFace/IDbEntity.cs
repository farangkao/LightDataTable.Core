using System.Collections.Generic;
using FastDeepCloner;
using Generic.LightDataTable.Attributes;
using Generic.LightDataTable.Helper;

namespace Generic.LightDataTable.InterFace
{
    /// <summary>
    /// 
    /// </summary>
    public interface IDbEntity
    {
        /// <summary>
        /// Primary Id, when overrided you have to implement PropertyName Attribute
        /// </summary>
        [PrimaryKey]
        long Id { get; set; }
        /// <summary>
        /// Changed properties
        /// </summary>
        Dictionary<string, object> PropertyChanges { get; }
        /// <summary>
        ///  Merge tow objects, only unupdated data will be merged
        /// </summary>
        /// <param name="data"></param>

        void Merge(InterFace.IDbEntity data);

        ItemState State { get; set; }
        /// <summary>
        /// Clear all the changed property and begin new validations
        /// </summary>
        /// <returns></returns>
        InterFace.IDbEntity ClearPropertChanges();

        /// <summary>
        /// Clone the whole object
        /// </summary>
        /// <param name="fieldType"></param>
        /// <returns></returns>
        IDbEntity Clone(FieldType fieldType = FieldType.PropertyInfo);
        /// <summary>
        /// This method is added incase we want JsonConverter to serilize only new data, 
        /// be sure to ClearPropertChanges before begning to change the data
        /// </summary>
        /// <returns></returns>
        string GetJsonForPropertyChangesOnly();


    }
}
