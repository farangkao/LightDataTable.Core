using System.Collections.Generic;
using Generic.LightDataTable.Attributes;
using Generic.LightDataTable.Library;

namespace Test.Modules.Core
{
    [Table("Roles")]
    public class Role : DbEntity
    {
        public string Name { get; set; }

        public List<User> Users { get; set; }
    }
}
