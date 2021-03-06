# Introduction to LightDataTable.Core
## Nuget
https://www.nuget.org/packages/Generic.LightDataTable/
## .NET FRAMEWORK SUPPORT 
1- .NETCoreApp 2.0

2- .NETFramework 4.5.1

3- .NETFramework 4.6

4- .NETFramework 4.6.1

5- .NETStandard 2.0
## What is LightDataTable
LightDataTable is an object-relation mappar that enable .NET developers to work with relations data using objects.
LightDataTable is an alternative to entityframwork. is more flexible and much faster than entity framework.
## Can i use it to an existing database
Yes you could easily implement your existing modules and use attributes to map all your Primary Keys and Foreign Key without even
touching the database.
## Expression
LightDataTable has its own provider called SqlQueriable, which could handle almost every expression like Startwith,
EndWith Containe and so on
Se Code Example for more info.
## Code Example
let's start by creating the dbContext, lets call it Repository
```
    // Here we inherit from Transaction Data which contains the database logic for handling the transaction.
    // well thats all we need right now.
    // Inhert from TransactionLiteData for SQLite OR TransactionData FOR MSSQL 
    public class Repository : TransactionData
    {
        /// <param name="appSettingsOrSqlConnectionString">
        /// AppSettingsName that containe the connectionstringName,
        /// OR ConnectionStringName,
        /// OR Full ConnectionString
        /// Default is Dbconnection
        // <enableMigration> se Migration section for more information
        // when enabled it will create a table Generic_LightDataTable_DBMigration so it keep an 
        // eye on all exected Migrations
        // </enableMigration>
        /// </param>
        public Repository(string appSettingsOrSqlConnectionString = "Dbconnection", bool enableMigration = false) :
        base(appSettingsOrSqlConnectionString,enableMigration)
        {
        }

    }
```
let's start building our models, lets build a simple models User
```
    // Table attribute indicate that the object Name differ from the database table Name
    [Table("Users")]
    [Rule(typeof(UserRule))]
    public class User : DbEntity
    {
        public string UserName { get; set; }

        public string Password { get; set; }
        
        // Here we indicate that this attribute its a ForeignKey to object Role.
        [ForeignKey(type: typeof(Role))]
        public long Role_Id { get; set; }
        
        // when deleting an object the light database will try and delete all object that are connected to 
        // by adding IndependentData we let the lightdatatable to know that this object should not be automaticlly deleted
        [IndependentData]
        public Role Role { get; set; }

        public List<Address> Address { get; set; }
        
        //[ExcludeFromAbstract] mean that it should not be included in the DataBase Update or insert.
        // It aslo mean that it dose not exist in the Table User.
        // use this attribute to include other property that you only want to use in the code and it should not be 
        // saved to the database
        [ExcludeFromAbstract]
        public string test { get; set; }
    }
    
    [Table("Roles")]
    public class Role : DbEntity
    {
        public string Name { get; set; }

        public List<User> Users { get; set; }
    }
    
    public class Address : DbEntity
    {
        public string AddressName { get; set; }
        // in the User class we have a list of adresses, lightDataTable will do an inner join and load the address 
        // if its included in the quarry
        [ForeignKey(typeof(User))]
        public long User_Id { get; set; }
    }
    
    // LightDataTable has its own way to validate the data.
    // lets create an object and call it UserRule
    // above the User class we have specified this class to be executed before save and after.
    // by adding [Rule(typeof(UserRule))] to the user class
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
            // the lightdatatable will now know that it need to update the database agen.
        }
    }

```
## Quarry and Expression
Lets build some expression here and se how it works
```
   //To the header
   using Generic.LightDataTable;
   
   using (var rep = new Repository())
   {
        // LoadChildren indicate to load all children herarkie.
        // It has no problem handling circular references.
        // The quarry dose not call to the database before we invoke Execute or ExecuteAsync
        var users = rep.Get<User>().Where(x => 
                (x.Role.Name.EndsWith("SuperAdmin") &&
                 x.UserName.Contains("alen")) ||
                 x.Address.Any(a=> a.AddressName.StartsWith("st"))
                ).LoadChildren().Execute(); 
                
        // lets say that we need only to load some children and ignore some other, then our select will be like this instead
          var users = rep.Get<User>().Where(x => 
                (x.Role.Name.EndsWith("SuperAdmin") &&
                 x.UserName.Contains("alen")) ||
                 x.Address.Any(a=> a.AddressName.StartsWith("st"))
                ).LoadChildren(x=> x.Role.Users.Select(a=> a.Address), x=> x.Address)
                .IgnoreChildren(x=> x.Role.Users.Select(a=> a.Role)).OrderBy(x=> x.UserName).Skip(20).Take(100).Execute();        
        Console.WriteLine(users.ToJson());
        Console.ReadLine();
   }

```
## Edit, delete and insert
LightDataTable have only one method for insert and update.
It depends on primarykey, Id>0 to update and Id<=0 to insert.
```
   using (var rep = new Repository())
   {
        var users = rep.Get<User>().Where(x => 
                (x.Role.Name.EndsWith("SuperAdmin") &&
                 x.UserName.Contains("alen")) ||
                 x.Address.Any(a=> a.AddressName.StartsWith("st"))
                ).LoadChildren();
         foreach (User user in users.Execute())
         {
             user.UserName = "test 1";
             user.Role.Name = "Administrator";
             user.Address.First().AddressName = "Changed";
             // now we could do rep.Save(user); but will choose to save the whole list late
         }
        users.Save();    
        // how to delete 
        // remove all the data offcource Roles will be ignored here 
        users.Remove();
        // Remove with expression
        users.RemoveAll(x=> x.UserName.Contains("test"));
        // we could also clone the whole object and insert it as new to the database like this
        var clonedUser = users.Execute().Clone().ClearAllIdsHierarchy();
        foreach (var user in clonedUser)
        {
            // Now this will clone the object to the database, of course all Foreign Key will be automatically assigned.
            // Role wont be cloned here. cource it has IndependedData attr we could choose to clone it too, by invoking
            // .ClearAllIdsHierarchy(true);
            rep.Save(user);
        }
        Console.WriteLine(users.ToJson());
        Console.ReadLine();
   }

```
## LinqToSql Result Example
lets test and se how lightDataTable LinqToSql generator looks like.
will do a very painful quarry and se how it gets parsed.
```
            using (var rep = new Repository())
            {
               ISqlQueriable<User> users = rep.Get<User>().Where(x =>
               (x.Role.Name.EndsWith("SuperAdmin") &&
                x.UserName.Contains("alen")) ||
                x.Address.Any(a => (a.AddressName.StartsWith("st") || a.AddressName.Contains("mt")) && a.Id > 0).
                Skip(20).Take(100).Execute();  
                );
                
                List<User> userList = users.Execute();
                var sql = users.ParsedLinqToSql;
            }
            // And here is the generated Sql Quarry
             SELECT distinct Users.* FROM Users 
             left join [Roles] CEjB on CEjB.[Id] = Users.[Role_Id]
             WHERE (([CEjB].[Name] like String[%SuperAdmin] AND [Users].[UserName] like String[%alen%]) 
             OR  EXISTS (SELECT 1 FROM [Address] 
             INNER JOIN [Address] MJRhcYK on Users.[Id] = MJRhcYK.[User_Id]
             WHERE (([Address].[AddressName] like String[st%] OR [Address].[AddressName] like String[%mt%]) AND ([Address].[Id] > 0))))
             ORDER BY Id
             OFFSET 20
             ROWS FETCH NEXT 100 ROWS ONLY;
             // All String[] and Date[] will be translated to Parameters later on.   
```

## Migration
LightDataTable has its own Migration methods, so lets se down here how it work.
```
   //Create Class and call it IniMigration and inhert from Migration
   public class IniMigration : Migration
        public IniMigration()
        {
           // in the database will be created a migration that contain this Identifier.
           // it's very important that its unique.
            MigrationIdentifier = "SystemFirstStart"; 
        }
        public override void ExecuteMigration(ICustomRepository repository)
        {
            // create the table User, Role, Address 
            // because we have a forgenkeys in user class that refer to address and roles, those will also be
            // created
            repository.CreateTable<User>(true);
            var user = new User()
            {
                Role = new Role() { Name = "Admin" },
                Address = new List<Address>() { new Address() { AddressName = "test" } },
                UserName = "Alen Toma",
                Password = "test"
            };
            repository.Save(user);

            base.ExecuteMigration(repository);
        }
    }
  }

    // now lets create the MigrationConfig Class
    public class MigrationConfig : IMigrationConfig
    {
        /// <summary>
        /// All available Migrations to be executed.
        // when Migration Is eneabled in TransactionLiteData or TransactionData.
        // this class will be triggered at system start.
        /// </summary>
        public IList<Migration> GetMigrations(ICustomRepository repository)
        {
            // return all migration that is to be executetd
            // all already executed migration that do exist in the database will be ignored
            return new List<Migration>(){new IniMigration()};
        }
    }

```





## Issues
This project is under developing and it's not in its final state so please report any bugs or improvement you might find

