using Generic.LightDataTable.InterFace;

namespace Generic.LightDataTable.Library
{
    public class Migration : IMigration
    {
        /// <summary>
        /// Default cto
        /// </summary>
        public Migration() { }
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
