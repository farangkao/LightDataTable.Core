using System.Collections.Generic;
using Generic.LightDataTable.Library;

namespace Generic.LightDataTable.InterFace
{
    public interface IMigrationConfig
    {
        /// <summary>
        /// All available Migrations to be executed
        /// </summary>
        IList<Migration> GetMigrations(ICustomRepository repository);
    }
}
