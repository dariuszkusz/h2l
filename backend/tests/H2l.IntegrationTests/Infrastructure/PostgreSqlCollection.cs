using Xunit;

namespace H2l.IntegrationTests.Infrastructure;

[CollectionDefinition("PostgreSql")]
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlContainerFixture>
{
}
