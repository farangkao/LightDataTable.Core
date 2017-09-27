﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using SQLitePCL;

namespace Microsoft.Data.Sqlite
{
    internal class SqliteDataRecord : SqliteValueReader
    {
        private readonly sqlite3_stmt _stmt;

        public SqliteDataRecord(sqlite3_stmt stmt)
        {
            _stmt = stmt;
        }

        public virtual object this[string name]
            => GetValue(GetOrdinal(name));

        public virtual object this[int ordinal]
            => GetValue(ordinal);

        public override int FieldCount
            => raw.sqlite3_column_count(_stmt);

        protected override double GetDoubleCore(int ordinal)
            => raw.sqlite3_column_double(_stmt, ordinal);

        protected override long GetInt64Core(int ordinal)
            => raw.sqlite3_column_int64(_stmt, ordinal);

        protected override string GetStringCore(int ordinal)
            => raw.sqlite3_column_text(_stmt, ordinal);

        protected override byte[] GetBlobCore(int ordinal)
            => raw.sqlite3_column_blob(_stmt, ordinal);

        protected override int GetSqliteType(int ordinal)
        {
            var type = raw.sqlite3_column_type(_stmt, ordinal);
            if (type == raw.SQLITE_NULL
                && (ordinal < 0 || ordinal >= FieldCount))
            {
                // NB: Message is provided by the framework
                throw new ArgumentOutOfRangeException(nameof(ordinal), ordinal, message: null);
            }

            return type;
        }

        protected override T GetNull<T>()
            => typeof(T) == typeof(DBNull) || typeof(T) == typeof(object)
                ? (T)(object)DBNull.Value
                : throw new InvalidCastException();

        public virtual string GetName(int ordinal)
        {
            var name = raw.sqlite3_column_name(_stmt, ordinal);
            if (name == null
                && (ordinal < 0 || ordinal >= FieldCount))
            {
                // NB: Message is provided by the framework
                throw new ArgumentOutOfRangeException(nameof(ordinal), ordinal, message: null);
            }

            return name;
        }

        public virtual int GetOrdinal(string name)
        {
            for (var i = 0; i < FieldCount; i++)
            {
                if (GetName(i) == name)
                {
                    return i;
                }
            }

            // NB: Message is provided by framework
            throw new ArgumentOutOfRangeException(nameof(name), name, message: null);
        }

        public virtual string GetDataTypeName(int ordinal)
        {
            var typeName = raw.sqlite3_column_decltype(_stmt, ordinal);
            if (typeName != null)
            {
                var i = typeName.IndexOf('(');

                return i == -1
                    ? typeName
                    : typeName.Substring(0, i);
            }

            var sqliteType = GetSqliteType(ordinal);
            switch (sqliteType)
            {
                case raw.SQLITE_INTEGER:
                    return "INTEGER";

                case raw.SQLITE_FLOAT:
                    return "REAL";

                case raw.SQLITE_TEXT:
                    return "TEXT";

                case raw.SQLITE_BLOB:
                    return "BLOB";

                case raw.SQLITE_NULL:
                    return "INTEGER";

                default:
                    Debug.Assert(false, "Unexpected column type: " + sqliteType);
                    return "INTEGER";
            }
        }

        public virtual Type GetFieldType(int ordinal)
        {
            var sqliteType = GetSqliteType(ordinal);
            switch (sqliteType)
            {
                case raw.SQLITE_INTEGER:
                    return typeof(long);

                case raw.SQLITE_FLOAT:
                    return typeof(double);

                case raw.SQLITE_TEXT:
                    return typeof(string);

                case raw.SQLITE_BLOB:
                    return typeof(byte[]);

                case raw.SQLITE_NULL:
                    return typeof(int);

                default:
                    Debug.Assert(false, "Unexpected column type: " + sqliteType);
                    return typeof(int);
            }
        }

        public virtual long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
            => throw new NotSupportedException();

        public virtual long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
            => throw new NotSupportedException();
    }
}
