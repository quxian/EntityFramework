// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Migrations.Internal;
using Microsoft.Data.Entity.Sqlite;
using Microsoft.Data.Entity.Sqlite.Metadata;
using Microsoft.Data.Entity.Storage;
using Microsoft.Data.Entity.Update;
using Microsoft.Framework.Logging;
using Moq;
using Xunit;

namespace Microsoft.Data.Entity.Migrations
{
    public class SqliteHistoryRepositoryTest
    {
        private static string EOL => Environment.NewLine;

        [Fact]
        public void GetCreateScript_works()
        {
            var sql = CreateHistoryRepository().GetCreateScript();

            Assert.Equal(
                "CREATE TABLE \"__EFMigrationsHistory\" (" + EOL +
                "    \"MigrationId\" TEXT NOT NULL CONSTRAINT \"PK_HistoryRow\" PRIMARY KEY," + EOL +
                "    \"ProductVersion\" TEXT NOT NULL" + EOL +
                ");" + EOL,
                sql);
        }

        [Fact]
        public void GetCreateIfNotExistsScript_works()
        {
            var sql = CreateHistoryRepository().GetCreateIfNotExistsScript();

            Assert.Equal(
                "CREATE TABLE IF NOT EXISTS \"__EFMigrationsHistory\" (" + EOL +
                "    \"MigrationId\" TEXT NOT NULL CONSTRAINT \"PK_HistoryRow\" PRIMARY KEY," + EOL +
                "    \"ProductVersion\" TEXT NOT NULL" + EOL +
                ");" + EOL,
                sql);
        }

        [Fact]
        public void GetDeleteScript_works()
        {
            var sql = CreateHistoryRepository().GetDeleteScript("Migration1");

            Assert.Equal(
                "DELETE FROM \"__EFMigrationsHistory\"" + EOL +
                "WHERE \"MigrationId\" = 'Migration1';" + EOL,
                sql);
        }

        [Fact]
        public void GetInsertScript_works()
        {
            var sql = CreateHistoryRepository().GetInsertScript(
                new HistoryRow("Migration1", "7.0.0"));

            Assert.Equal(
                "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\")" + EOL +
                "VALUES ('Migration1', '7.0.0');" + EOL,
                sql);
        }

        [Fact]
        public void GetBeginIfNotExistsScript_works()
        {
            var repository = CreateHistoryRepository();
            var ex = Assert.Throws<NotSupportedException>(() => repository.GetBeginIfNotExistsScript("Migration1"));

            Assert.Equal(Strings.MigrationScriptGenerationNotSupported, ex.Message);
        }

        [Fact]
        public void GetBeginIfExistsScript_works()
        {
            var repository = CreateHistoryRepository();
            var ex = Assert.Throws<NotSupportedException>(() => repository.GetBeginIfExistsScript("Migration1"));

            Assert.Equal(Strings.MigrationScriptGenerationNotSupported, ex.Message);
        }

        [Fact]
        public void GetEndIfScript_works()
        {
            var repository = CreateHistoryRepository();
            var ex = Assert.Throws<NotSupportedException>(() => repository.GetEndIfScript());

            Assert.Equal(Strings.MigrationScriptGenerationNotSupported, ex.Message);
        }

        private static IHistoryRepository CreateHistoryRepository()
        {
            var typeMapper = new SqliteTypeMapper();
            var annotationsProvider = new SqliteMetadataExtensionProvider();
            var updateSqlGenerator = new SqliteUpdateSqlGenerator();
            var commandBuilderFactory = new RelationalCommandBuilderFactory(
                new LoggerFactory(),
                typeMapper);

            return new SqliteHistoryRepository(
                Mock.Of<IRelationalDatabaseCreator>(),
                commandBuilderFactory,
                Mock.Of<IRelationalConnection>(),
                new DbContextOptions<DbContext>(
                    new Dictionary<Type, IDbContextOptionsExtension>
                    {
                        { typeof(SqliteOptionsExtension), new SqliteOptionsExtension() }
                    }),
                new MigrationsModelDiffer(
                    annotationsProvider,
                    new SqliteMigrationsAnnotationProvider()),
                new SqliteMigrationsSqlGenerator(
                    updateSqlGenerator,
                    commandBuilderFactory,
                    typeMapper,
                    annotationsProvider),
                annotationsProvider,
                updateSqlGenerator);
        }

        private class Context : DbContext
        {
        }
    }
}
