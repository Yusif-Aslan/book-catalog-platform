namespace BookCatalog.IntegrationTests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<BookCatalogApiFactory>
{
    public const string Name = "Integration tests";
}