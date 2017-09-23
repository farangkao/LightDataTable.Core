using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Generic.LightDataTable.InterFace
{
    /// <summary>
    /// Inhert from this class and create a migration File
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IMigration
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="repository"></param>
        void ExecuteMigration(ICustomRepository repository);
    }
}
