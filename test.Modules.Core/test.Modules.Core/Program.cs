using System;
using System.Linq;
using Generic.LightDataTable;
using FastDeepCloner;
using test.Modules.Core.Data;

namespace Test.Modules.Core
{
    internal class Program
    {


        private static void Main(string[] args)
        {
            /// lite database with migration cant use both in the same system so mssql or lite db
            //using (var rep = new LiteRepository())
            //{
            //    var users = rep.Get<User>().Where(x =>
            //        (x.Role.Name.EndsWith("admin") &&
            //         x.UserName.Contains("alen")) ||
            //        x.Address.Any(a => (a.AddressName.StartsWith("test") || a.AddressName.Contains("mt")) && a.Id > 0)
            //    ).LoadChildren();


            //    foreach (User user in users.Execute())
            //    {
            //        user.UserName = "test 2";
            //        user.Role.Name = "Administrator 1";
            //        user.Address.First().AddressName = "Changed";
            //        // now we could do rep.Save(user); but will choose to save the whole list later insetad
            //    }
            //    var sql = users.ParsedLinqToSql;
            //    var us = users.Execute().Clone().ClearAllIdsHierarchy();
            //    users.Save();

            //    Console.WriteLine(users.ToJson());
            //    //Console.ReadLine();
            //}

            // Mssql with migration
            using (var rep = new Repository())
            {
                var users = rep.Get<User>().Where(x =>
                    (x.Role.Name.EndsWith("admin") &&
                     x.UserName.Contains("alen")) ||
                    x.Address.Any(a => (a.AddressName.StartsWith("test") || a.AddressName.Contains("mt")) && a.Id > 0)
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

                Console.WriteLine(users.ToJson());
                Console.ReadLine();
            }
        }
    }
}
