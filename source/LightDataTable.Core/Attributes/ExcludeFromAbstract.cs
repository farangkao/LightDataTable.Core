using System;

namespace Generic.LightDataTable.Attributes
{

    /// <inheritdoc />
    /// <summary>
    /// this indeicate that the prop will not be saved to the db
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ExcludeFromAbstract : Attribute
    {


    }
}
