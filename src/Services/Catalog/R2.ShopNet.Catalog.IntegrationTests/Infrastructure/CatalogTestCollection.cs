using Xunit;

namespace R2.ShopNet.Catalog.IntegrationTests.Infrastructure;

/// <summary>
/// Collection definition to enable parallel test execution.
/// Each test will get its own isolated database within the shared container.
/// </summary>
[CollectionDefinition(nameof(CatalogTestCollection), DisableParallelization = false)]
public class CatalogTestCollection : ICollectionFixture<CatalogApiFactory>
{
}
