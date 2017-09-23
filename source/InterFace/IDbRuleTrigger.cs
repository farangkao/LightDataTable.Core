namespace Generic.LightDataTable.InterFace
{
    public interface IDbRuleTrigger 
    {
        /// <summary>
        /// Event triggered before save an item
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="itemDbEntity"></param>
        void BeforeSave(ICustomRepository repository, IDbEntity itemDbEntity);

        /// <summary>
        /// triggered after saveing an item
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="itemDbEntity"></param>
        /// <param name="objectId"></param>
        void AfterSave(ICustomRepository repository, IDbEntity itemDbEntity, long objectId);
    }
}
