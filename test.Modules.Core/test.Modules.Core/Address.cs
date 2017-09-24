using System.Collections.Generic;
using Generic.LightDataTable.Attributes;
using Generic.LightDataTable.Library;

namespace Test.Modules.Core
{
    public class Address : DbEntity
    {
        public string AddressName { get; set; }

        [ForeignKey(typeof(User))]
        public long User_Id { get; set; }

    }
}
