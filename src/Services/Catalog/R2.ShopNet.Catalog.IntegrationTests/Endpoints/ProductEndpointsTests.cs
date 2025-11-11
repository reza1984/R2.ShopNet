using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using R2.ShopNet.Catalog.Application.DTOs;
using R2.ShopNet.Catalog.Domain.Entities;
using R2.ShopNet.Catalog.Domain.Enums;
using R2.ShopNet.Catalog.IntegrationTests.Helpers;
using R2.ShopNet.Catalog.IntegrationTests.Infrastructure;
using R2.ShopNet.Framework.Common;

namespace R2.ShopNet.Catalog.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for Product API endpoints.
/// These tests verify the complete request/response cycle including database operations.
/// Each test class has its own isolated PostgreSQL and MinIO containers for parallel execution.
/// </summary>
public class ProductEndpointsTests : IntegrationTestBase
{
    public ProductEndpointsTests(CatalogApiFactory factory) : base(factory)
    {
    }

    #region GET /api/Products - Get all products with pagination

    [Fact]
    public async Task GetProducts_WithoutData_ReturnsEmptyList()
    {
        // Arrange
        await ResetDatabaseAsync();

        // Act
        var response = await Client.GetAsync("/api/Products?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetProducts_WithData_ReturnsPaginatedResults()
    {
        // Arrange
        await ResetDatabaseAsync();
        var category = TestDataBuilder.GenerateCategory();
        var products = Enumerable.Range(1, 15)
            .Select(_ => TestDataBuilder.GenerateProduct(categoryId: category.Id))
            .ToList();

        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(category);
            await db.Products.AddRangeAsync(products);
            await db.SaveChangesAsync();
        });

        // Act
        var response = await Client.GetAsync("/api/Products?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(10);
        result.TotalCount.Should().Be(15);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task GetProducts_WithSearchTerm_ReturnsFilteredResults()
    {
        // Arrange
        await ResetDatabaseAsync();
        var category = TestDataBuilder.GenerateCategory();
        var laptop = TestDataBuilder.GenerateProduct(categoryId: category.Id, name: "Laptop Computer", slug: "laptop-computer");
        var phone = TestDataBuilder.GenerateProduct(categoryId: category.Id, name: "Mobile Phone", slug: "mobile-phone");

        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(category);
            await db.Products.AddRangeAsync(laptop, phone);
            await db.SaveChangesAsync();
        });

        // Act
        var response = await Client.GetAsync("/api/Products?pageNumber=1&pageSize=10&searchTerm=Laptop");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items.First().Name.Should().Be("Laptop Computer");
    }

    [Fact]
    public async Task GetProducts_WithCategoryIdFilter_ReturnsProductsInCategory()
    {
        // Arrange
        await ResetDatabaseAsync();
        var category1 = TestDataBuilder.GenerateCategory(name: "Electronics");
        var category2 = TestDataBuilder.GenerateCategory(name: "Books");
        var product1 = TestDataBuilder.GenerateProduct(categoryId: category1.Id, name: "Laptop");
        var product2 = TestDataBuilder.GenerateProduct(categoryId: category1.Id, name: "Phone");
        var product3 = TestDataBuilder.GenerateProduct(categoryId: category2.Id, name: "Novel");

        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddRangeAsync(category1, category2);
            await db.Products.AddRangeAsync(product1, product2, product3);
            await db.SaveChangesAsync();
        });

        // Act
        var response = await Client.GetAsync($"/api/Products?pageNumber=1&pageSize=10&categoryId={category1.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(p => p.CategoryId.Should().Be(category1.Id));
    }

    [Fact]
    public async Task GetProducts_WithStatusFilter_ReturnsFilteredProducts()
    {
        // Arrange
        await ResetDatabaseAsync();
        var category = TestDataBuilder.GenerateCategory();
        var activeProduct = TestDataBuilder.GenerateProduct(categoryId: category.Id, name: "Active Product");
        activeProduct.Activate();
        activeProduct.UpdateStock(10);

        var draftProduct = TestDataBuilder.GenerateProduct(categoryId: category.Id, name: "Draft Product");
        // draftProduct stays in Draft status (default)

        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(category);
            await db.Products.AddRangeAsync(activeProduct, draftProduct);
            await db.SaveChangesAsync();
        });

        // Act
        var response = await Client.GetAsync($"/api/Products?pageNumber=1&pageSize=10&status={ProductStatus.Active}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items.First().Name.Should().Be("Active Product");
        result.Items.First().Status.Should().Be(ProductStatus.Active.ToString());
    }

    [Fact]
    public async Task GetProducts_SortByName_ReturnsSortedResults()
    {
        // Arrange
        await ResetDatabaseAsync();
        var category = TestDataBuilder.GenerateCategory();
        var products = new[]
        {
            TestDataBuilder.GenerateProduct(categoryId: category.Id, name: "Zebra Product"),
            TestDataBuilder.GenerateProduct(categoryId: category.Id, name: "Apple Product"),
            TestDataBuilder.GenerateProduct(categoryId: category.Id, name: "Mango Product")
        };

        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(category);
            await db.Products.AddRangeAsync(products);
            await db.SaveChangesAsync();
        });

        // Act - Sort by Name ascending
        var response = await Client.GetAsync("/api/Products?pageNumber=1&pageSize=10&sortBy=Name&sortDescending=false");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(3);
        result.Items[0].Name.Should().Be("Apple Product");
        result.Items[1].Name.Should().Be("Mango Product");
        result.Items[2].Name.Should().Be("Zebra Product");
    }

    [Fact]
    public async Task GetProducts_SortByPrice_ReturnsSortedResults()
    {
        // Arrange
        await ResetDatabaseAsync();
        var category = TestDataBuilder.GenerateCategory();
        var products = new[]
        {
            TestDataBuilder.GenerateProduct(categoryId: category.Id, name: "Expensive", price: 999.99m),
            TestDataBuilder.GenerateProduct(categoryId: category.Id, name: "Cheap", price: 9.99m),
            TestDataBuilder.GenerateProduct(categoryId: category.Id, name: "Medium", price: 49.99m)
        };

        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(category);
            await db.Products.AddRangeAsync(products);
            await db.SaveChangesAsync();
        });

        // Act - Sort by Price ascending
        var response = await Client.GetAsync("/api/Products?pageNumber=1&pageSize=10&sortBy=Price&sortDescending=false");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(3);
        result.Items[0].Price.Should().Be(9.99m);
        result.Items[1].Price.Should().Be(49.99m);
        result.Items[2].Price.Should().Be(999.99m);
    }

    #endregion

    #region GET /api/Products/{id} - Get product by ID

    [Fact]
    public async Task GetProductById_WithValidId_ReturnsProduct()
    {
        // Arrange
        await ResetDatabaseAsync();
        var category = TestDataBuilder.GenerateCategory();
        var product = TestDataBuilder.GenerateProduct(categoryId: category.Id, name: "Test Product");

        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(category);
            await db.Products.AddAsync(product);
            await db.SaveChangesAsync();
        });

        // Act
        var response = await Client.GetAsync($"/api/Products/{product.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ProductDto>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(product.Id);
        result.Name.Should().Be("Test Product");
        result.CategoryId.Should().Be(category.Id);
    }

    [Fact]
    public async Task GetProductById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        await ResetDatabaseAsync();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/Products/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/Products - Create product

    [Fact]
    public async Task CreateProduct_WithValidData_ReturnsCreatedProduct()
    {
        // Arrange
        await ResetDatabaseAsync();
        var category = TestDataBuilder.GenerateCategory();
        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(category);
            await db.SaveChangesAsync();
        });

        var command = new
        {
            name = "New Laptop",
            slug = "new-laptop",
            sku = "LAP-001",
            description = "A high-performance laptop",
            shortDescription = "Performance laptop",
            price = 1299.99m,
            currency = "USD",
            categoryId = category.Id,
            stockQuantity = 50,
            reorderLevel = 10,
            brand = "TechBrand",
            weight = 2000m
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Products", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ProductDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("New Laptop");
        result.Slug.Should().Be("new-laptop");
        result.Sku.Should().Be("LAP-001");
        result.Price.Should().Be(1299.99m);
        result.StockQuantity.Should().Be(50);

        // Verify in database
        var dbProduct = await WithDbContextAsync(async db =>
            await db.Products.FirstOrDefaultAsync(p => p.Id == result.Id));
        dbProduct.Should().NotBeNull();
        dbProduct!.Name.Should().Be("New Laptop");
    }

    [Fact]
    public async Task CreateProduct_WithDuplicateSku_ReturnsConflict()
    {
        // Arrange
        await ResetDatabaseAsync();
        var category = TestDataBuilder.GenerateCategory();
        var existingProduct = TestDataBuilder.GenerateProduct(categoryId: category.Id, sku: "DUPLICATE-SKU");

        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(category);
            await db.Products.AddAsync(existingProduct);
            await db.SaveChangesAsync();
        });

        var command = new
        {
            name = "Another Product",
            slug = "another-product",
            sku = "DUPLICATE-SKU",
            price = 99.99m,
            currency = "USD",
            categoryId = category.Id,
            stockQuantity = 10
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Products", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProduct_WithDuplicateSlug_ReturnsConflict()
    {
        // Arrange
        await ResetDatabaseAsync();
        var category = TestDataBuilder.GenerateCategory();
        var existingProduct = TestDataBuilder.GenerateProduct(categoryId: category.Id, slug: "duplicate-slug");

        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(category);
            await db.Products.AddAsync(existingProduct);
            await db.SaveChangesAsync();
        });

        var command = new
        {
            name = "Another Product",
            slug = "duplicate-slug",
            sku = "UNIQUE-SKU",
            price = 99.99m,
            currency = "USD",
            categoryId = category.Id,
            stockQuantity = 10
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Products", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProduct_WithNonExistentCategory_ReturnsNotFound()
    {
        // Arrange
        await ResetDatabaseAsync();
        var nonExistentCategoryId = Guid.NewGuid();

        var command = new
        {
            name = "Product",
            slug = "product",
            sku = "SKU-001",
            price = 99.99m,
            currency = "USD",
            categoryId = nonExistentCategoryId,
            stockQuantity = 10
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Products", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProduct_WithInvalidPrice_ReturnsBadRequest()
    {
        // Arrange
        await ResetDatabaseAsync();
        var category = TestDataBuilder.GenerateCategory();
        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(category);
            await db.SaveChangesAsync();
        });

        var command = new
        {
            name = "Product",
            slug = "product",
            sku = "SKU-001",
            price = -10.00m, // Invalid negative price
            currency = "USD",
            categoryId = category.Id,
            stockQuantity = 10
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Products", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region PUT /api/Products/{id} - Update product

    [Fact]
    public async Task UpdateProduct_WithValidData_ReturnsUpdatedProduct()
    {
        // Arrange
        await ResetDatabaseAsync();
        var category = TestDataBuilder.GenerateCategory();
        var product = TestDataBuilder.GenerateProduct(categoryId: category.Id, name: "Old Name", slug: "old-slug");

        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(category);
            await db.Products.AddAsync(product);
            await db.SaveChangesAsync();
        });

        var command = new
        {
            productId = product.Id,
            name = "Updated Name",
            slug = "updated-slug",
            sku = product.Sku,
            description = "Updated description",
            price = 199.99m,
            currency = "USD",
            categoryId = category.Id,
            stockQuantity = 100
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/Products/{product.Id}", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ProductDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Name");
        result.Slug.Should().Be("updated-slug");
        result.Price.Should().Be(199.99m);

        // Verify in database
        var dbProduct = await WithDbContextAsync(async db =>
            await db.Products.FirstOrDefaultAsync(p => p.Id == product.Id));
        dbProduct.Should().NotBeNull();
        dbProduct!.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task UpdateProduct_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        await ResetDatabaseAsync();
        var nonExistentId = Guid.NewGuid();
        var category = TestDataBuilder.GenerateCategory();
        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(category);
            await db.SaveChangesAsync();
        });

        var command = new
        {
            productId = nonExistentId,
            name = "Product",
            slug = "product",
            sku = "SKU-001",
            price = 99.99m,
            currency = "USD",
            categoryId = category.Id,
            stockQuantity = 10
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/Products/{nonExistentId}", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateProduct_WithDuplicateSlug_ReturnsConflict()
    {
        // Arrange
        await ResetDatabaseAsync();
        var category = TestDataBuilder.GenerateCategory();
        var product1 = TestDataBuilder.GenerateProduct(categoryId: category.Id, slug: "existing-slug");
        var product2 = TestDataBuilder.GenerateProduct(categoryId: category.Id, slug: "another-slug");

        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(category);
            await db.Products.AddRangeAsync(product1, product2);
            await db.SaveChangesAsync();
        });

        var command = new
        {
            productId = product2.Id,
            name = "Updated",
            slug = "existing-slug", // Trying to use existing slug
            sku = product2.Sku,
            price = 99.99m,
            currency = "USD",
            categoryId = category.Id,
            stockQuantity = 10
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/Products/{product2.Id}", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateProduct_ChangingCategory_UpdatesSuccessfully()
    {
        // Arrange
        await ResetDatabaseAsync();
        var category1 = TestDataBuilder.GenerateCategory(name: "Electronics");
        var category2 = TestDataBuilder.GenerateCategory(name: "Books");
        var product = TestDataBuilder.GenerateProduct(categoryId: category1.Id);

        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddRangeAsync(category1, category2);
            await db.Products.AddAsync(product);
            await db.SaveChangesAsync();
        });

        var command = new
        {
            productId = product.Id,
            name = product.Name,
            slug = product.Slug,
            sku = product.Sku,
            price = 99.99m,
            currency = "USD",
            categoryId = category2.Id, // Change to category2
            stockQuantity = 10
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/Products/{product.Id}", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ProductDto>();
        result.Should().NotBeNull();
        result!.CategoryId.Should().Be(category2.Id);

        // Verify in database
        var dbProduct = await WithDbContextAsync(async db =>
            await db.Products.FirstOrDefaultAsync(p => p.Id == product.Id));
        dbProduct.Should().NotBeNull();
        dbProduct!.CategoryId.Should().Be(category2.Id);
    }

    [Fact]
    public async Task UpdateProduct_WithDiscountPrice_CalculatesDiscountPercentage()
    {
        // Arrange
        await ResetDatabaseAsync();
        var category = TestDataBuilder.GenerateCategory();
        var product = TestDataBuilder.GenerateProduct(categoryId: category.Id, price: 100m);

        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(category);
            await db.Products.AddAsync(product);
            await db.SaveChangesAsync();
        });

        var command = new
        {
            productId = product.Id,
            name = product.Name,
            slug = product.Slug,
            sku = product.Sku,
            price = 100m,
            discountPrice = 80m, // 20% discount
            currency = "USD",
            categoryId = category.Id,
            stockQuantity = 10
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/Products/{product.Id}", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ProductDto>();
        result.Should().NotBeNull();
        result!.DiscountPrice.Should().Be(80m);
        result.DiscountPercentage.Should().Be(20m);
    }

    #endregion

    #region DELETE /api/Products/{id} - Delete product

    [Fact]
    public async Task DeleteProduct_WithValidId_ReturnsNoContent()
    {
        // Arrange
        await ResetDatabaseAsync();
        var category = TestDataBuilder.GenerateCategory();
        var product = TestDataBuilder.GenerateProduct(categoryId: category.Id);

        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(category);
            await db.Products.AddAsync(product);
            await db.SaveChangesAsync();
        });

        // Act
        var response = await Client.DeleteAsync($"/api/Products/{product.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify product is soft-deleted in database
        var dbProduct = await WithDbContextAsync(async db =>
            await db.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == product.Id));
        dbProduct.Should().NotBeNull();
        dbProduct!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteProduct_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        await ResetDatabaseAsync();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"/api/Products/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProduct_AlreadyDeleted_ReturnsNotFound()
    {
        // Arrange
        await ResetDatabaseAsync();
        var category = TestDataBuilder.GenerateCategory();
        var product = TestDataBuilder.GenerateProduct(categoryId: category.Id);

        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(category);
            await db.Products.AddAsync(product);
            await db.SaveChangesAsync();
        });

        // First deletion
        await Client.DeleteAsync($"/api/Products/{product.Id}");

        // Act - Try to delete again
        var response = await Client.DeleteAsync($"/api/Products/{product.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Product Stock Management

    [Fact]
    public async Task UpdateProduct_WithZeroStock_SetsStatusToOutOfStock()
    {
        // Arrange
        await ResetDatabaseAsync();
        var category = TestDataBuilder.GenerateCategory();
        var product = TestDataBuilder.GenerateProduct(categoryId: category.Id);
        product.Activate();
        product.UpdateStock(10);

        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(category);
            await db.Products.AddAsync(product);
            await db.SaveChangesAsync();
        });

        var command = new
        {
            productId = product.Id,
            name = product.Name,
            slug = product.Slug,
            sku = product.Sku,
            price = 99.99m,
            currency = "USD",
            categoryId = category.Id,
            stockQuantity = 0 // Set to zero
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/Products/{product.Id}", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ProductDto>();
        result.Should().NotBeNull();
        result!.StockQuantity.Should().Be(0);
        result.Status.Should().Be(ProductStatus.OutOfStock.ToString());
    }

    [Fact]
    public async Task UpdateProduct_FromZeroToPositiveStock_SetsStatusToActive()
    {
        // Arrange
        await ResetDatabaseAsync();
        var category = TestDataBuilder.GenerateCategory();
        var product = TestDataBuilder.GenerateProduct(categoryId: category.Id);
        product.Activate();
        product.UpdateStock(0); // Out of stock

        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(category);
            await db.Products.AddAsync(product);
            await db.SaveChangesAsync();
        });

        var command = new
        {
            productId = product.Id,
            name = product.Name,
            slug = product.Slug,
            sku = product.Sku,
            price = 99.99m,
            currency = "USD",
            categoryId = category.Id,
            stockQuantity = 50 // Restock
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/Products/{product.Id}", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ProductDto>();
        result.Should().NotBeNull();
        result!.StockQuantity.Should().Be(50);
        result.Status.Should().Be(ProductStatus.Active.ToString());
    }

    #endregion

    #region Product Search and Filtering

    [Fact]
    public async Task GetProducts_SearchByDescription_ReturnsMatchingProducts()
    {
        // Arrange
        await ResetDatabaseAsync();
        var category = TestDataBuilder.GenerateCategory();
        var product1 = TestDataBuilder.GenerateProduct(categoryId: category.Id, name: "Laptop");
        product1.SetDescription("High-performance gaming laptop with RGB keyboard");

        var product2 = TestDataBuilder.GenerateProduct(categoryId: category.Id, name: "Mouse");
        product2.SetDescription("Wireless office mouse");

        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(category);
            await db.Products.AddRangeAsync(product1, product2);
            await db.SaveChangesAsync();
        });

        // Act
        var response = await Client.GetAsync("/api/Products?pageNumber=1&pageSize=10&searchTerm=gaming");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items.First().Name.Should().Be("Laptop");
    }

    [Fact]
    public async Task GetProducts_SearchBySku_ReturnsMatchingProduct()
    {
        // Arrange
        await ResetDatabaseAsync();
        var category = TestDataBuilder.GenerateCategory();
        var product1 = TestDataBuilder.GenerateProduct(categoryId: category.Id, sku: "LAP-001");
        var product2 = TestDataBuilder.GenerateProduct(categoryId: category.Id, sku: "MOU-002");

        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(category);
            await db.Products.AddRangeAsync(product1, product2);
            await db.SaveChangesAsync();
        });

        // Act
        var response = await Client.GetAsync("/api/Products?pageNumber=1&pageSize=10&searchTerm=LAP-001");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items.First().Sku.Should().Be("LAP-001");
    }

    #endregion
}
