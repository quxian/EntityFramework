﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Migrations.Internal;
using Microsoft.Data.Entity.Tests;
using Microsoft.Framework.Logging;
using Xunit;

namespace Microsoft.Data.Entity.Migrations.Design
{
    public class MigrationScaffolderTest
    {
        [Fact]
        public void ScaffoldMigration_reuses_model_snapshot()
        {
            var scaffolder = CreateMigrationScaffolder<ContextWithSnapshot>();

            var migration = scaffolder.ScaffoldMigration("EmptyMigration", "WebApplication1");

            Assert.Equal(nameof(ContextWithSnapshotModelSnapshot), migration.SnapshotName);
            Assert.Equal(typeof(ContextWithSnapshotModelSnapshot).Namespace, migration.SnapshotSubnamespace);
        }

        private MigrationsScaffolder CreateMigrationScaffolder<TContext>()
            where TContext : DbContext, new()
        {
            var context = new TContext();
            var idGenerator = new MigrationsIdGenerator();
            var code = new CSharpHelper();

            return new MigrationsScaffolder(
                context,
                new Model(),
                new MigrationsAssembly(
                    context,
                    new DbContextOptions<TContext>().WithExtension(new MockRelationalOptionsExtension()),
                    idGenerator),
                new MigrationsModelDiffer(
                    new TestMetadataExtensionProvider(),
                    new MigrationsAnnotationProvider()),
                idGenerator,
                new CSharpMigrationsGenerator(code, new CSharpMigrationOperationGenerator(code), new CSharpSnapshotGenerator(code)),
                new MockHistoryRepository(),
                new LoggerFactory());
        }

        private class ContextWithSnapshot : DbContext
        {
        }

        [DbContext(typeof(ContextWithSnapshot))]
        private class ContextWithSnapshotModelSnapshot : ModelSnapshot
        {
            protected override void BuildModel(ModelBuilder modelBuilder)
            {
            }
        }

        private class MockRelationalOptionsExtension : RelationalOptionsExtension
        {
            public override void ApplyServices(EntityFrameworkServicesBuilder builder)
            {
            }
        }

        private class MockHistoryRepository : IHistoryRepository
        {
            public string GetBeginIfExistsScript(string migrationId) => null;
            public string GetBeginIfNotExistsScript(string migrationId) => null;
            public string GetCreateScript() => null;
            public string GetCreateIfNotExistsScript() => null;
            public string GetEndIfScript() => null;
            public bool Exists() => false;
            public Task<bool> ExistsAsync(CancellationToken cancellationToken) => Task.FromResult(false);
            public IReadOnlyList<HistoryRow> GetAppliedMigrations() => null;
            public Task<IReadOnlyList<HistoryRow>> GetAppliedMigrationsAsync(CancellationToken cancellationToken)
                => Task.FromResult<IReadOnlyList<HistoryRow>>(null);
            public string GetDeleteScript(string migrationId) => null;
            public string GetInsertScript(HistoryRow row) => null;
        }
    }
}
