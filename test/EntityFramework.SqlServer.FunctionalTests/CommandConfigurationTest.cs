// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Microsoft.Data.Entity.FunctionalTests;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Query;
using Microsoft.Data.Entity.Query.Expressions;
using Microsoft.Data.Entity.Query.Internal;
using Microsoft.Data.Entity.Query.Sql;
using Microsoft.Data.Entity.Storage;
using Microsoft.Data.Entity.Update;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Xunit;

namespace Microsoft.Data.Entity.SqlServer.FunctionalTests
{
    public class CommandConfigurationTest : IClassFixture<CommandConfigurationTest.CommandConfigurationTestFixture>
    {
        private const string DatabaseName = "NotKettleChips";

        private readonly CommandConfigurationTestFixture _fixture;

        public CommandConfigurationTest(CommandConfigurationTestFixture fixture)
        {
            _fixture = fixture;
            _fixture.CreateDatabase();
        }

        public class CommandConfigurationTestFixture : IDisposable
        {
            public IServiceProvider ServiceProvider { get; } = new ServiceCollection()
                .AddEntityFramework()
                .AddSqlServer()
                .ServiceCollection()
                .BuildServiceProvider();

            public virtual void CreateDatabase()
            {
                SqlServerTestStore.GetOrCreateShared(DatabaseName, () =>
                    {
                        using (var context = new ChipsContext(ServiceProvider))
                        {
                            context.Database.EnsureDeleted();
                            context.Database.EnsureCreated();
                        }
                    });
            }

            public void Dispose()
            {
                using (var context = new ChipsContext(ServiceProvider))
                {
                    context.Database.EnsureDeleted();
                }
            }
        }

        [Fact]
        public void Constructed_select_query_uses_default_when_commandTimeout_not_configured_and_can_be_changed()
        {
            using (var context = new ChipsContext(_fixture.ServiceProvider))
            {
                var commandBuilder = setupCommandBuilder();

                var command = commandBuilder.Build(context.GetService<IRelationalConnection>(), new Dictionary<string, object>());

                Assert.Equal(30, command.CommandTimeout);

                context.Database.SetCommandTimeout(77);
                var command2 = commandBuilder.Build(context.GetService<IRelationalConnection>(), new Dictionary<string, object>());

                Assert.Equal(77, command2.CommandTimeout);
            }
        }

        [Fact]
        public void Constructed_select_query_honors_configured_commandTimeout_configured_in_context()
        {
            using (var context = new ConfiguredChipsContext(_fixture.ServiceProvider))
            {
                var commandBuilder = setupCommandBuilder();

                var command = commandBuilder.Build(context.GetService<IRelationalConnection>(), new Dictionary<string, object>());

                Assert.Equal(77, command.CommandTimeout);
            }
        }

        [Fact]
        public void Constructed_select_query_honors_latest_configured_commandTimeout_configured_in_context()
        {
            using (var context = new ConfiguredChipsContext(_fixture.ServiceProvider))
            {
                var commandBuilder = setupCommandBuilder();

                context.Database.SetCommandTimeout(88);
                var command = commandBuilder.Build(context.GetService<IRelationalConnection>(), new Dictionary<string, object>());

                Assert.Equal(88, command.CommandTimeout);

                context.Database.SetCommandTimeout(99);
                var command2 = commandBuilder.Build(context.GetService<IRelationalConnection>(), new Dictionary<string, object>());

                Assert.Equal(99, command2.CommandTimeout);
            }
        }

        [Fact]
        public void Constructed_select_query_CommandBuilder_throws_when_negative_CommandTimeout_is_used()
        {
            using (var context = new ConfiguredChipsContext(_fixture.ServiceProvider))
            {
                Assert.Throws<ArgumentException>(() => context.Database.SetCommandTimeout(-5));
            }
        }

        [Fact]
        public void Constructed_select_query_CommandBuilder_uses_default_when_null()
        {
            using (var context = new ConfiguredChipsContext(_fixture.ServiceProvider))
            {
                var commandBuilder = setupCommandBuilder();

                context.Database.SetCommandTimeout(null);
                var command = commandBuilder.Build(context.GetService<IRelationalConnection>(), new Dictionary<string, object>());

                Assert.Equal(30, command.CommandTimeout);
            }
        }

        private CommandBuilder setupCommandBuilder()
            => new CommandBuilder(
                new UntypedRelationalValueBufferFactoryFactory(),
                new SqlServerTypeMapper(),
                new SelectExpression(
                    new SqlServerQuerySqlGeneratorFactory(
                        new ParameterNameGeneratorFactory()))
                    .CreateGenerator);

        [Fact]
        public void Constructed_update_statement_uses_default_when_commandTimeout_not_configured()
        {
            var serviceProvider = new ServiceCollection()
                .AddEntityFramework()
                .AddSqlServer()
                .ServiceCollection()
                .AddSingleton<IModificationCommandBatchFactory, TestSqlServerModificationCommandBatchFactory>()
                .BuildServiceProvider();

            using (var context = new ChipsContext(serviceProvider))
            {
                context.Database.EnsureCreated();

                context.Chips.Add(new KettleChips { BestBuyDate = DateTime.Now, Name = "Doritos Locos Tacos" });
                context.SaveChanges();
                Assert.Null(GlobalCommandTimeout);
            }
        }

        [Fact]
        public void Constructed_update_statement_uses_commandTimeout_configured_in_Context()
        {
            var serviceProvider = new ServiceCollection()
                .AddEntityFramework()
                .AddSqlServer()
                .ServiceCollection()
                .AddSingleton<IModificationCommandBatchFactory, TestSqlServerModificationCommandBatchFactory>()
                .BuildServiceProvider();

            using (var context = new ConfiguredChipsContext(serviceProvider))
            {
                context.Database.EnsureCreated();

                context.Chips.Add(new KettleChips { BestBuyDate = DateTime.Now, Name = "Doritos Locos Tacos" });
                context.SaveChanges();
                Assert.Equal(77, GlobalCommandTimeout);
            }
        }

        [Fact]
        public void Constructed_update_statement_uses_commandTimeout_not_configured_in_context()
        {
            var serviceProvider = new ServiceCollection()
                .AddEntityFramework()
                .AddSqlServer()
                .ServiceCollection()
                .AddSingleton<IModificationCommandBatchFactory, TestSqlServerModificationCommandBatchFactory>()
                .BuildServiceProvider();

            using (var context = new ChipsContext(serviceProvider))
            {
                context.Database.EnsureCreated();

                context.Database.SetCommandTimeout(88);
                context.Chips.Add(new KettleChips { BestBuyDate = DateTime.Now, Name = "Doritos Locos Tacos" });
                context.SaveChanges();
                Assert.Equal(88, GlobalCommandTimeout);
            }
        }

        [Fact]
        public void Constructed_update_statement_uses_commandTimeout_overriding_configured_in_context()
        {
            var serviceProvider = new ServiceCollection()
                .AddEntityFramework()
                .AddSqlServer()
                .ServiceCollection()
                .AddSingleton<IModificationCommandBatchFactory, TestSqlServerModificationCommandBatchFactory>()
                .BuildServiceProvider();

            using (var context = new ConfiguredChipsContext(serviceProvider))
            {
                context.Database.EnsureCreated();

                context.Database.SetCommandTimeout(88);
                context.Chips.Add(new KettleChips { BestBuyDate = DateTime.Now, Name = "Doritos Locos Tacos" });
                context.SaveChanges();
                Assert.Equal(88, GlobalCommandTimeout);
            }
        }

        [Fact]
        public async void Constructed_update_statement_uses_default_commandTimeout_can_override_not_configured_in_context_async()
        {
            var serviceProvider = new ServiceCollection()
                .AddEntityFramework()
                .AddSqlServer()
                .ServiceCollection()
                .AddSingleton<IModificationCommandBatchFactory, TestSqlServerModificationCommandBatchFactory>()
                .BuildServiceProvider();

            using (var context = new ChipsContext(serviceProvider))
            {
                context.Database.EnsureCreated();

                context.Chips.Add(new KettleChips { BestBuyDate = DateTime.Now, Name = "Doritos Locos Tacos" });
                await context.SaveChangesAsync();
                Assert.Null(GlobalCommandTimeout);

                context.Database.SetCommandTimeout(88);

                context.Chips.Add(new KettleChips { BestBuyDate = DateTime.Now, Name = "Doritos Locos Tacos" });
                await context.SaveChangesAsync();
                Assert.Equal(88, GlobalCommandTimeout);
            }
        }

        [Fact]
        public async void Constructed_update_statement_uses_default_commandTimeout_can_override_configured_in_context_async()
        {
            var serviceProvider = new ServiceCollection()
                .AddEntityFramework()
                .AddSqlServer()
                .ServiceCollection()
                .AddSingleton<IModificationCommandBatchFactory, TestSqlServerModificationCommandBatchFactory>()
                .BuildServiceProvider();

            using (var context = new ConfiguredChipsContext(serviceProvider))
            {
                context.Database.EnsureCreated();

                context.Chips.Add(new KettleChips { BestBuyDate = DateTime.Now, Name = "Doritos Locos Tacos" });
                await context.SaveChangesAsync();
                Assert.Equal(77, GlobalCommandTimeout);

                context.Database.SetCommandTimeout(88);

                context.Chips.Add(new KettleChips { BestBuyDate = DateTime.Now, Name = "Doritos Locos Tacos" });
                await context.SaveChangesAsync();
                Assert.Equal(88, GlobalCommandTimeout);
            }
        }

        [Fact]
        public async void Overridden_commandTimeout_overrides_timeout_configured_in_context_async()
        {
            var serviceProvider = new ServiceCollection()
                .AddEntityFramework()
                .AddSqlServer()
                .ServiceCollection()
                .AddSingleton<IModificationCommandBatchFactory, TestSqlServerModificationCommandBatchFactory>()
                .BuildServiceProvider();

            using (var context = new ConfiguredChipsContext(serviceProvider))
            {
                context.Database.EnsureCreated();

                context.Database.SetCommandTimeout(88);

                context.Chips.Add(new KettleChips { BestBuyDate = DateTime.Now, Name = "Doritos Locos Tacos" });
                await context.SaveChangesAsync();
                Assert.Equal(88, GlobalCommandTimeout);
            }
        }

        [Theory]
        [InlineData(51, 6)]
        [InlineData(50, 5)]
        [InlineData(20, 2)]
        [InlineData(2, 1)]
        public void Keys_generated_in_batches(int count, int expected)
        {
            var loggerFactory = new TestSqlLoggerFactory();
            var serviceProvider = new ServiceCollection()
                .AddEntityFramework()
                .AddSqlServer()
                .ServiceCollection()
                .AddInstance<ILoggerFactory>(loggerFactory)
                .AddSingleton<IModificationCommandBatchFactory, TestSqlServerModificationCommandBatchFactory>()
                .BuildServiceProvider();

            using (var context = new ConfiguredChipsContext(serviceProvider))
            {
                context.Database.EnsureCreated();

                for (var i = 0; i < count; i++)
                {
                    context.Chips.Add(new KettleChips { BestBuyDate = DateTime.Now, Name = "Doritos Locos Tacos " + i });
                }
                context.SaveChanges();
            }

            Assert.Equal(expected, CountSqlLinesContaining("SELECT NEXT VALUE FOR"));
        }

        public int CountSqlLinesContaining(string searchTerm)
        {
            return CountLinesContaining(Sql, searchTerm);
        }

        public int CountLinesContaining(string source, string searchTerm)
        {
            var text = source.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            var matchQuery = from word in text
                             where word.Contains(searchTerm)
                             select word;

            return matchQuery.Count();
        }

        public static int? GlobalCommandTimeout;

        public class TestSqlServerModificationCommandBatch : SqlServerModificationCommandBatch
        {
            protected override DbCommand CreateStoreCommand(string commandText, IRelationalConnection connection, IRelationalTypeMapper typeMapper, int? commandTimeout)
            {
                GlobalCommandTimeout = commandTimeout;
                return base.CreateStoreCommand(commandText, connection, typeMapper, commandTimeout);
            }

            public TestSqlServerModificationCommandBatch(
                ISqlServerUpdateSqlGenerator sqlGenerator,
                int? maxBatchSize)
                : base(sqlGenerator, maxBatchSize)
            {
            }
        }

        public class TestSqlServerModificationCommandBatchFactory : SqlServerModificationCommandBatchFactory
        {
            public TestSqlServerModificationCommandBatchFactory(
                ISqlServerUpdateSqlGenerator sqlGenerator)
                : base(sqlGenerator)
            {
            }

            public override ModificationCommandBatch Create(
                IDbContextOptions options,
                IRelationalMetadataExtensionProvider metadataExtensionProvider)
            {
                var optionsExtension = options.Extensions.OfType<SqlServerOptionsExtension>().FirstOrDefault();

                var maxBatchSize = optionsExtension?.MaxBatchSize;

                return new TestSqlServerModificationCommandBatch(
                    (ISqlServerUpdateSqlGenerator)UpdateSqlGenerator,
                    maxBatchSize);
            }
        }

        private class ChipsContext : DbContext
        {
            public ChipsContext(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }

            public DbSet<KettleChips> Chips { get; set; }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                optionsBuilder.UseSqlServer(SqlServerTestStore.CreateConnectionString(DatabaseName));
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.UseSqlServerSequenceHiLo();
            }
        }

        private class KettleChips
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public DateTime BestBuyDate { get; set; }
        }

        private class ConfiguredChipsContext : ChipsContext
        {
            public ConfiguredChipsContext(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                optionsBuilder.UseSqlServer("Database=" + DatabaseName).CommandTimeout(77);

                base.OnConfiguring(optionsBuilder);
            }
        }

        private static string Sql => TestSqlLoggerFactory.Sql;
    }
}
