using System;

namespace Generic.LightDataTable.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class Table : Attribute
    {
        public string Name { get; private set; }

        public string DisplayName { get; private set; }

        public Table(string name, string displayName= null)
        {
            Name = name;
            DisplayName = displayName ?? Name;
        }
    }
}
