﻿using System;
using Generic.LightDataTable.Attributes;
namespace Generic.LightDataTable.Library
{
    [Table("Generic_LightDataTable_DBMigration")]
    internal class DBMigration : DbEntity
    {
        public string Name { get; set; }

        public DateTime DateCreated { get; set; }

    }
}
