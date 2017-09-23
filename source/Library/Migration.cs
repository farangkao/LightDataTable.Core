using Generic.LightDataTable.InterFace;

namespace Generic.LightDataTable.Library
{
    public class Migration : IMigration
    {
        /// <summary>
        /// Default cto
        /// </summary>
        public Migration() { }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="repository"></param>
        public virtual void ExecuteMigration(ICustomRepository repository)
        {
            return;
        }
    }
}
