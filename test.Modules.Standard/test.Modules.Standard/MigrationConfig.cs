using System.Collections.Generic;
using Generic.LightDataTable.InterFace;
using Generic.LightDataTable.Library;
using test.Modules.Standard;

namespace test.Modules.Core
{
    public class MigrationConfig : IMigrationConfig
    {
        /// <summary>
        /// All available Migrations to be executed
        /// </summary>
        public IList<Migration> GetMigrations(ICustomRepository repository)
        {
            // return all migration that is to be executetd
            // all already executed migration that do exist in the database will be ignored
            return new List<Migration>(){new Migration_1()};
        }
    }
}
