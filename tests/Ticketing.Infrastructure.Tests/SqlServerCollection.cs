using Ticketing.TestSupport;

namespace Ticketing.Infrastructure.Tests;

/// <summary>
/// Binds all SQL-backed test classes to a single shared SQL Server 2019 container.
/// </summary>
[CollectionDefinition("sqlserver")]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>;
