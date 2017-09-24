using System.Collections.Generic;
using Generic.LightDataTable.Attributes;
using Generic.LightDataTable.Library;

namespace Test.Modules.Core
{
    [Table("Users")]
    [Rule(typeof(UserRule))]
    public class User : DbEntity
    {
        public string UserName { get; set; }

        public string Password { get; set; }

        public List<Address> Address { get; set; }


        [ForeignKey(type: typeof(Role))]
        public long Role_Id { get; set; }

        [IndependentData]
        public Role Role { get; set; }


    }
}
