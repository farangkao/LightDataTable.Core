using System;
using System.Collections.Generic;
using System.Text;
using Generic.LightDataTable.InterFace;
using Generic.LightDataTable.Library;

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
            // here do you migration
          //  var cmd = repository.GetSqlCommand("");
            base.ExecuteMigration(repository);
        }
    }
}
