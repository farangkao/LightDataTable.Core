using System;

namespace Generic.LightDataTable.Attributes
{
    /// <inheritdoc />
    /// <summary>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ForeignKey : Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        public Type Type { get; private set; }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public ForeignKey() { }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="type"></param>
        public ForeignKey(Type type)
        {
            Type = type;
        }


    }
}
