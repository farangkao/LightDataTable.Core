using System;
using System.Linq;
using Generic.LightDataTable;
using FastDeepCloner;

namespace Test.Modules.Core
{
    internal class Program
    {


        private static void Main(string[] args)
        {
            using (var rep = new Repository())
            {
                var users = rep.Get<User>().Where(x =>
                    (x.Role.Name.EndsWith("SuperAdmin") &&
                     x.UserName.Contains("alen")) ||
                    x.Address.Any(a => (a.AddressName.StartsWith("st") || a.AddressName.Contains("mt")) && a.Id > 0)
                ).LoadChildren();


                foreach (User user in users.Execute())
                {
                    user.UserName = "test 2";
                    user.Role.Name = "Administrator 1";
                    user.Address.First().AddressName = "Changed";
                    // now we could do rep.Save(user); but will choose to save the whole list later insetad
                }
                var sql = users.ParsedLinqToSql;
                var us = users.Execute().Clone().ClearAllIdsHierarchy();
                users.Save();
                //// remove all the data offcource Roles will be ignored here 
                //users.Remove();
                //// Remove with expression
                //users.RemoveAll(x => x.UserName.Contains("test"));

                //// we could also clone the whole object and insert it as new to the database like this
                //var clonedUser = users.Execute().Clone().ClearAllIdsHierarki();
                //foreach (var user in clonedUser)
                //{
                //    // now this will do clone the object to the database, of course all Foreign Key will be automatically assigned
                //    rep.Save(user);
                //}

                Console.WriteLine(users.ToJson());
                Console.ReadLine();
            }
        }
    }
}
