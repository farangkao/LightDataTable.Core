using System;
using Generic.LightDataTable;
using Generic.LightDataTable.Helper;
using Generic.LightDataTable.InterFace;

namespace Test.Modules.Core
{
    public class UserRule : IDbRuleTrigger<User>
    {
        public void BeforeSave(ICustomRepository repository, User itemDbEntity)
        {

            if (string.IsNullOrEmpty(itemDbEntity.Password) || string.IsNullOrEmpty(itemDbEntity.UserName))
            {
                // this will do a transaction rollback and delete all changes that have happened to the database
                throw new Exception("Password or UserName can not be empty");
            }
        }

        public void AfterSave(ICustomRepository repository, User itemDbEntity, long objectId)
        {

            itemDbEntity.ClearPropertChanges();// clear all changes.
            // lets do some changes here, when the item have updated..
            itemDbEntity.Password = MethodHelper.EncodeStringToBase64(itemDbEntity.Password);
            // and now we want to save this change to the database 
            itemDbEntity.State = ItemState.Changed;
            // the lightdatatable will now that it need to update the database agen.
        }
    }
}
