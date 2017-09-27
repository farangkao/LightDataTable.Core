using System;
using System.Collections.Generic;
using System.Text;
using Generic.LightDataTable.InterFace;
using Generic.LightDataTable.Library;
using Generic.LightDataTable;
using Test.Modules.Core;

namespace test.Modules.Core.Migrations
{
    public class Migration_1 : Migration
    {
        public Migration_1()
        {
            MigrationIdentifier = "Test";
        }
        public override void ExecuteMigration(ICustomRepository repository)
        {

            repository.CreateTable<User>(true); // create the table User, Role, Address 

            var user = new User()
            {
                Role = new Role() { Name = "Admin" },
                Address = new List<Address>() { new Address() { AddressName = "test" } },
                UserName = "Alen Toma",
                Password = "test"
            };
            repository.Save(user);

            base.ExecuteMigration(repository);
        }
    }
}
