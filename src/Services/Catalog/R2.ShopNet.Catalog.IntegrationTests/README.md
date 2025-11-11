# R2.ShopNet Catalog Service - Integration Tests

Comprehensive integration tests for the Catalog microservice using xUnit.net.

## Overview

Integration tests verify the complete request/response cycle of the Catalog API, including:
- HTTP endpoint behavior
- Database operations (with real PostgreSQL via TestContainers)
- Business logic validation
- Error handling

## Test Infrastructure

### Test Factories

**CatalogApiFactory** - Full integration testing with PostgreSQL TestContainer
- Uses real PostgreSQL database in Docker container
- Provides highest confidence for database interactions
- Automatically handles container lifecycle
- Slower execution but production-like environment

**InMemoryCatalogApiFactory** - Lightweight testing with in-memory database
- Faster execution for unit-like integration tests
- Good for testing business logic
- Use when database-specific features aren't critical

### Base Classes

**IntegrationTestBase** - Base class providing:
- Database reset functionality between tests (using Respawn)
- Helper methods for accessing services and DbContext
- Test lifecycle management with IAsyncLifetime
- Scoped service provider access

### Test Helpers

**TestDataBuilder** - Generates realistic test data using Bogus:
- Consistent test data across tests
- Easy creation of complex object graphs
- Reduces test setup boilerplate

## Prerequisites

- .NET 9 SDK
- Docker Desktop (for PostgreSQL TestContainer)
- **Docker must be running** for integration tests using CatalogApiFactory

## Running Tests

### Run all tests
```bash
cd src/Services/Catalog/R2.ShopNet.Catalog.IntegrationTests
dotnet test
```

### Run with verbose output
```bash
dotnet test --logger "console;verbosity=detailed"
```

### Run specific test class
```bash
dotnet test --filter "FullyQualifiedName~CategoryEndpointsTests"
```

### Run specific test method
```bash
dotnet test --filter "FullyQualifiedName~GetCategories_WithData_ReturnsPaginatedResults"
```

### Run with code coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Test Structure

```
R2.ShopNet.Catalog.IntegrationTests/
├── Endpoints/
│   └── CategoryEndpointsTests.cs    # Integration tests for Category endpoints
├── Infrastructure/
│   ├── CatalogApiFactory.cs         # WebApplicationFactory with PostgreSQL
│   ├── InMemoryCatalogApiFactory.cs # WebApplicationFactory with in-memory DB
│   └── IntegrationTestBase.cs       # Base class for tests
└── Helpers/
    └── TestDataBuilder.cs            # Test data generation utilities
```

## Key NuGet Packages

- **xUnit.net 2.9.2** - Modern test framework
- **FluentAssertions 7.0.0** - Readable, fluent assertions
- **Microsoft.AspNetCore.Mvc.Testing 9.0.0** - WebApplicationFactory support
- **Testcontainers.PostgreSql 4.1.0** - Docker containers for integration tests
- **Bogus 35.6.1** - Fake data generation
- **Respawn 6.2.1** - Database cleanup between tests
- **Microsoft.EntityFrameworkCore.InMemory 9.0.0** - In-memory database provider

## Category Endpoint Tests

### Covered Scenarios

**GET /api/Categories** - List categories with pagination
- ✅ Empty result set
- ✅ Paginated results (15 items, page size 10)
- ✅ Search/filtering by term
- ✅ Filter by parent category ID
- ✅ Sorting (ascending/descending)

**GET /api/Categories/{id}** - Get by ID
- ✅ Valid ID returns category
- ✅ Invalid ID returns 404

**GET /api/Categories/hierarchy** - Category tree
- ✅ Nested categories with children
- ✅ Empty hierarchy

**POST /api/Categories** - Create category
- ✅ Valid data creates category
- ✅ Duplicate slug returns 409 Conflict
- ✅ With parent category creates child
- ✅ Non-existent parent returns 404

**PUT /api/Categories/{id}** - Update category
- ✅ Valid data updates category
- ✅ Non-existent ID returns 404
- ✅ Duplicate slug returns 409 Conflict

**DELETE /api/Categories/{id}** - Delete category
- ✅ Valid ID deletes category
- ✅ Non-existent ID returns 404
- ✅ Category with children returns 409 Conflict

## Writing New Tests

### Example Test

```csharp
public class MyNewTests : IntegrationTestBase
{
    public MyNewTests(CatalogApiFactory factory) : base(factory) { }

    [Fact]
    public async Task MyTest_WithCondition_ReturnsExpectedResult()
    {
        // Arrange - Reset database and seed data
        await ResetDatabaseAsync();
        var category = TestDataBuilder.GenerateCategory();
        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(category);
            await db.SaveChangesAsync();
        });

        // Act - Make HTTP request
        var response = await Client.GetAsync($"/api/Categories/{category.Id}");

        // Assert - Verify results using FluentAssertions
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CategoryDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be(category.Name);
    }
}
```

## Best Practices

1. **Always reset database** - Call `ResetDatabaseAsync()` at the start of each test
2. **Use FluentAssertions** - More readable than Assert.Equal()
3. **Use TestDataBuilder** - Consistent, realistic test data
4. **Test one behavior** - Each test verifies one specific scenario
5. **Follow AAA pattern** - Arrange, Act, Assert with clear sections
6. **Descriptive names** - Format: `MethodName_Condition_ExpectedResult`
7. **Isolate tests** - No dependencies between tests
8. **Test both happy and error paths** - Success cases and failure cases

## CI/CD Integration

### GitHub Actions Example

```yaml
- name: Start Docker
  run: |
    docker info

- name: Run Integration Tests
  run: |
    dotnet test src/Services/Catalog/R2.ShopNet.Catalog.IntegrationTests \
      --logger "trx;LogFileName=test-results.trx"

- name: Publish Test Results
  if: always()
  uses: EnricoMi/publish-unit-test-result-action@v2
  with:
    files: '**/test-results.trx'
```

### Requirements
- Docker available in CI environment
- Sufficient resources (2GB RAM minimum)
- Tests can run in parallel safely

## Troubleshooting

### Docker not running
```
Error: Cannot connect to Docker daemon
Solution: Start Docker Desktop before running tests
```

### Port conflicts
```
Error: Port already in use
Solution: TestContainers uses random ports. Check no orphaned containers:
docker ps -a | grep postgres
docker rm -f <container_id>
```

### Tests hanging
```
Solution: Check Docker has enough resources (Settings > Resources)
Recommended: 4GB RAM, 2 CPUs
```

### Connection timeout
```
Error: Timeout waiting for PostgreSQL container
Solution:
- Check Docker is running
- Check internet connection (pulls postgres:17-alpine if not cached)
- Increase timeout in CatalogApiFactory if needed
```

## Performance Tips

1. **Use InMemoryCatalogApiFactory** for faster tests when appropriate
2. **Run tests in parallel** - xUnit does this by default
3. **Cache Docker images** - Keep postgres:17-alpine image cached
4. **Use test collections** to share fixtures across test classes

## Future Enhancements

- [ ] Add tests for Product endpoints
- [ ] Add tests for Brand endpoints
- [ ] Add performance/load tests
- [ ] Add authentication/authorization tests
- [ ] Add tests for image upload functionality
- [ ] Add API contract tests (Pact/Dredd)
- [ ] Add integration tests with message bus
- [ ] Add tests for caching behavior
