using Generic.LightDataTable.InterFace;

namespace Generic.LightDataTable.Library
{
    /// <inheritdoc />
    public class Migration : IMigration
    {
        /// <summary>
        /// Default cto
        /// </summary>
        public Migration() { }

        /// <summary>
        ///  Make sure that the key dose not exist in the database, if its a new Migration
        /// </summary>
        public string MigrationIdentifier { get; set; }

        /// <inheritdoc />
        /// <summary>
        /// Do your db Changes here
        /// </summary>
        /// <param name="repository"></param>
        public virtual void ExecuteMigration(ICustomRepository repository)
        {
            return;
        }
    }
}
