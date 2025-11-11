using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using R2.ShopNet.Catalog.Application.DTOs;
using R2.ShopNet.Catalog.Infrastructure.Persistence;
using R2.ShopNet.Catalog.Domain.Entities;
using R2.ShopNet.Catalog.IntegrationTests.Helpers;
using R2.ShopNet.Catalog.IntegrationTests.Infrastructure;
using R2.ShopNet.Framework.Common;

namespace R2.ShopNet.Catalog.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for Category API endpoints.
/// These tests verify the complete request/response cycle including database operations.
/// Each test class has its own isolated PostgreSQL and MinIO containers for parallel execution.
/// </summary>
public class CategoryEndpointsTests : IntegrationTestBase
{
    public CategoryEndpointsTests(CatalogApiFactory factory) : base(factory)
    {
    }
    #region GET /api/Categories - Get all categories with pagination

    [Fact]
    public async Task GetCategories_WithoutData_ReturnsEmptyList()
    {
        // Arrange
        await ResetDatabaseAsync();

        // Act
        var response = await Client.GetAsync("/api/Categories?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<CategoryDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetCategories_WithData_ReturnsPaginatedResults()
    {
        // Arrange
        await ResetDatabaseAsync();
        var categories = TestDataBuilder.GenerateCategories(15);
        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddRangeAsync(categories);
            await db.SaveChangesAsync();
        });

        // Act
        var response = await Client.GetAsync("/api/Categories?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<CategoryDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(10);
        result.TotalCount.Should().Be(15);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task GetCategories_WithSearchTerm_ReturnsFilteredResults()
    {
        // Arrange
        await ResetDatabaseAsync();
        var electronicCategory = TestDataBuilder.GenerateCategory(name: "Electronics", slug: "electronics");
        var clothingCategory = TestDataBuilder.GenerateCategory(name: "Clothing", slug: "clothing");
        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddRangeAsync(electronicCategory, clothingCategory);
            await db.SaveChangesAsync();
        });

        // Act
        var response = await Client.GetAsync("/api/Categories?pageNumber=1&pageSize=10&searchTerm=Electronic");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<CategoryDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items.First().Name.Should().Be("Electronics");
    }

    [Fact]
    public async Task GetCategories_WithParentCategoryIdFilter_ReturnsChildCategories()
    {
        // Arrange
        await ResetDatabaseAsync();
        var (parent, children) = TestDataBuilder.GenerateCategoryHierarchy(childCount: 3);
        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(parent);
            await db.Categories.AddRangeAsync(children);
            await db.SaveChangesAsync();
        });

        // Act
        var response = await Client.GetAsync($"/api/Categories?pageNumber=1&pageSize=10&parentCategoryId={parent.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<CategoryDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(3);
        result.Items.Should().AllSatisfy(c => c.ParentCategoryId.Should().Be(parent.Id));
    }

    [Fact]
    public async Task GetCategories_SortByName_ReturnsSortedResults()
    {
        // Arrange
        await ResetDatabaseAsync();
        var categories = new[]
        {
            TestDataBuilder.GenerateCategory(name: "Zebra", displayOrder: 3),
            TestDataBuilder.GenerateCategory(name: "Apple", displayOrder: 1),
            TestDataBuilder.GenerateCategory(name: "Mango", displayOrder: 2)
        };
        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddRangeAsync(categories);
            await db.SaveChangesAsync();
        });

        // Act - Sort by Name ascending
        var response = await Client.GetAsync("/api/Categories?pageNumber=1&pageSize=10&sortBy=Name&sortDescending=false");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<CategoryDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(3);
        result.Items[0].Name.Should().Be("Apple");
        result.Items[1].Name.Should().Be("Mango");
        result.Items[2].Name.Should().Be("Zebra");
    }

    [Fact]
    public async Task GetCategories_SortByNameDescending_ReturnsSortedResults()
    {
        // Arrange
        await ResetDatabaseAsync();
        var categories = new[]
        {
            TestDataBuilder.GenerateCategory(name: "Zebra", displayOrder: 3),
            TestDataBuilder.GenerateCategory(name: "Apple", displayOrder: 1),
            TestDataBuilder.GenerateCategory(name: "Mango", displayOrder: 2)
        };
        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddRangeAsync(categories);
            await db.SaveChangesAsync();
        });

        // Act - Sort by Name descending
        var response = await Client.GetAsync("/api/Categories?pageNumber=1&pageSize=10&sortBy=Name&sortDescending=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<CategoryDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(3);
        result.Items[0].Name.Should().Be("Zebra");
        result.Items[1].Name.Should().Be("Mango");
        result.Items[2].Name.Should().Be("Apple");
    }

    [Fact]
    public async Task GetCategories_SortByDisplayOrder_ReturnsSortedResults()
    {
        // Arrange
        await ResetDatabaseAsync();
        var categories = new[]
        {
            TestDataBuilder.GenerateCategory(name: "Zebra", displayOrder: 3),
            TestDataBuilder.GenerateCategory(name: "Apple", displayOrder: 1),
            TestDataBuilder.GenerateCategory(name: "Mango", displayOrder: 2)
        };
        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddRangeAsync(categories);
            await db.SaveChangesAsync();
        });

        // Act - Sort by DisplayOrder ascending
        var response = await Client.GetAsync("/api/Categories?pageNumber=1&pageSize=10&sortBy=DisplayOrder&sortDescending=false");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<CategoryDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(3);
        result.Items[0].DisplayOrder.Should().Be(1);
        result.Items[0].Name.Should().Be("Apple");
        result.Items[1].DisplayOrder.Should().Be(2);
        result.Items[1].Name.Should().Be("Mango");
        result.Items[2].DisplayOrder.Should().Be(3);
        result.Items[2].Name.Should().Be("Zebra");
    }

    [Fact]
    public async Task GetCategories_SortByDisplayOrderDescending_ReturnsSortedResults()
    {
        // Arrange
        await ResetDatabaseAsync();
        var categories = new[]
        {
            TestDataBuilder.GenerateCategory(name: "Zebra", displayOrder: 3),
            TestDataBuilder.GenerateCategory(name: "Apple", displayOrder: 1),
            TestDataBuilder.GenerateCategory(name: "Mango", displayOrder: 2)
        };
        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddRangeAsync(categories);
            await db.SaveChangesAsync();
        });

        // Act - Sort by DisplayOrder descending
        var response = await Client.GetAsync("/api/Categories?pageNumber=1&pageSize=10&sortBy=DisplayOrder&sortDescending=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<CategoryDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(3);
        result.Items[0].DisplayOrder.Should().Be(3);
        result.Items[0].Name.Should().Be("Zebra");
        result.Items[1].DisplayOrder.Should().Be(2);
        result.Items[1].Name.Should().Be("Mango");
        result.Items[2].DisplayOrder.Should().Be(1);
        result.Items[2].Name.Should().Be("Apple");
    }

    [Fact]
    public async Task GetCategories_SortByCreatedAt_ReturnsSortedResults()
    {
        // Arrange
        await ResetDatabaseAsync();

        // Create categories with slight delays to ensure different CreatedAt timestamps
        var category1 = TestDataBuilder.GenerateCategory(name: "First", displayOrder: 3);
        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(category1);
            await db.SaveChangesAsync();
        });

        await Task.Delay(10); // Small delay to ensure different timestamps

        var category2 = TestDataBuilder.GenerateCategory(name: "Second", displayOrder: 1);
        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(category2);
            await db.SaveChangesAsync();
        });

        await Task.Delay(10); // Small delay to ensure different timestamps

        var category3 = TestDataBuilder.GenerateCategory(name: "Third", displayOrder: 2);
        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(category3);
            await db.SaveChangesAsync();
        });

        // Act - Sort by CreatedAt ascending (oldest first)
        var response = await Client.GetAsync("/api/Categories?pageNumber=1&pageSize=10&sortBy=CreatedAt&sortDescending=false");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<CategoryDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(3);
        result.Items[0].Name.Should().Be("First", "it was created first");
        result.Items[1].Name.Should().Be("Second", "it was created second");
        result.Items[2].Name.Should().Be("Third", "it was created third");
    }

    [Fact]
    public async Task GetCategories_SortByCreatedAtDescending_ReturnsSortedResults()
    {
        // Arrange
        await ResetDatabaseAsync();

        // Create categories with slight delays to ensure different CreatedAt timestamps
        var category1 = TestDataBuilder.GenerateCategory(name: "First", displayOrder: 3);
        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(category1);
            await db.SaveChangesAsync();
        });

        await Task.Delay(10); // Small delay to ensure different timestamps

        var category2 = TestDataBuilder.GenerateCategory(name: "Second", displayOrder: 1);
        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(category2);
            await db.SaveChangesAsync();
        });

        await Task.Delay(10); // Small delay to ensure different timestamps

        var category3 = TestDataBuilder.GenerateCategory(name: "Third", displayOrder: 2);
        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(category3);
            await db.SaveChangesAsync();
        });

        // Act - Sort by CreatedAt descending (newest first)
        var response = await Client.GetAsync("/api/Categories?pageNumber=1&pageSize=10&sortBy=CreatedAt&sortDescending=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<CategoryDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(3);
        result.Items[0].Name.Should().Be("Third", "it was created last");
        result.Items[1].Name.Should().Be("Second", "it was created second");
        result.Items[2].Name.Should().Be("First", "it was created first");
    }

    [Fact]
    public async Task GetCategories_WithNoSortBy_DefaultsToDisplayOrder()
    {
        // Arrange
        await ResetDatabaseAsync();
        var categories = new[]
        {
            TestDataBuilder.GenerateCategory(name: "Zebra", displayOrder: 3),
            TestDataBuilder.GenerateCategory(name: "Apple", displayOrder: 1),
            TestDataBuilder.GenerateCategory(name: "Mango", displayOrder: 2)
        };
        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddRangeAsync(categories);
            await db.SaveChangesAsync();
        });

        // Act - No sortBy parameter, should default to DisplayOrder
        var response = await Client.GetAsync("/api/Categories?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<CategoryDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(3);
        result.Items[0].DisplayOrder.Should().Be(1);
        result.Items[1].DisplayOrder.Should().Be(2);
        result.Items[2].DisplayOrder.Should().Be(3);
    }

    #endregion

    #region GET /api/Categories/{id} - Get category by ID

    [Fact]
    public async Task GetCategoryById_WithValidId_ReturnsCategory()
    {
        // Arrange
        await ResetDatabaseAsync();
        var category = TestDataBuilder.GenerateCategory(name: "Test Category");
        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(category);
            await db.SaveChangesAsync();
        });

        // Act
        var response = await Client.GetAsync($"/api/Categories/{category.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CategoryDto>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(category.Id);
        result.Name.Should().Be("Test Category");
    }

    [Fact]
    public async Task GetCategoryById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        await ResetDatabaseAsync();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/Categories/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GET /api/Categories/hierarchy - Get category hierarchy

    [Fact]
    public async Task GetCategoryHierarchy_WithNestedCategories_ReturnsHierarchicalStructure()
    {
        // Arrange
        await ResetDatabaseAsync();
        var (parent, children) = TestDataBuilder.GenerateCategoryHierarchy(childCount: 2);
        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(parent);
            await db.Categories.AddRangeAsync(children);
            await db.SaveChangesAsync();
        });

        // Act
        var response = await Client.GetAsync("/api/Categories/hierarchy");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<CategoryHierarchyDto>>();
        result.Should().NotBeNull();
        result.Should().HaveCount(1); // One root category
        result!.First().SubCategories.Should().HaveCount(2); // Two child categories
    }

    [Fact]
    public async Task GetCategoryHierarchy_WithoutData_ReturnsEmptyList()
    {
        // Arrange
        await ResetDatabaseAsync();

        // Act
        var response = await Client.GetAsync("/api/Categories/hierarchy");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<CategoryHierarchyDto>>();
        result.Should().NotBeNull();
        result!.Should().BeEmpty();
    }

    #endregion

    #region POST /api/Categories - Create category

    [Fact]
    public async Task CreateCategory_WithValidData_ReturnsCreatedCategory()
    {
        // Arrange
        await ResetDatabaseAsync();
        
        // Create a simple test image (1x1 pixel PNG)
        var imageBytes = new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D,
            0x49, 0x48, 0x44, 0x52, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
            0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4, 0x89, 0x00, 0x00, 0x00,
            0x0A, 0x49, 0x44, 0x41, 0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
            0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00, 0x00, 0x00, 0x00, 0x49,
            0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82
        };
        
        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        
        var formData = new MultipartFormDataContent
        {
            { new StringContent("Electronics"), "name" },
            { new StringContent("electronics"), "slug" },
            { new StringContent("Electronic devices and accessories"), "description" },
            { new StringContent("1"), "displayOrder" },
            { imageContent, "image", "test-category.png" }
        };

        // Act
        var response = await Client.PostAsync("/api/Categories", formData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CategoryDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("Electronics");
        result.Slug.Should().Be("electronics");
        result.ImageUrl.Should().NotBeNullOrEmpty("image should be uploaded to MinIO test container");

        // Verify in database
        var dbCategory = await WithDbContextAsync(async db =>
            await db.Categories.FirstOrDefaultAsync(c => c.Id == result.Id));
        dbCategory.Should().NotBeNull();
        dbCategory!.Name.Should().Be("Electronics");
        dbCategory.ImageUrl.Should().NotBeNullOrEmpty("image URL should be persisted in database");

        // Verify file exists in MinIO
        var minioFileExists = await VerifyFileExistsInMinIO(result.Id);
        minioFileExists.Should().BeTrue("the image file should exist in MinIO storage");
        
        // Verify we can download the file from MinIO and it matches the uploaded content
        var downloadedFile = await DownloadFileFromMinIO(result.Id);
        downloadedFile.Should().NotBeNull("the file should be downloadable from MinIO");
        downloadedFile.Should().HaveCount(67, "the downloaded file should match the uploaded file size");
        downloadedFile.Should().BeEquivalentTo(imageBytes, "the downloaded file content should match the uploaded file");

        // Verify the ImageUrl is a valid presigned URL and can be used to download the image
        result.ImageUrl.Should().StartWith("http", "ImageUrl should be a valid HTTP URL");
        
        // Use a separate HttpClient to access the presigned MinIO URL (not the test API client)
        using var httpClient = new HttpClient();
        var imageResponse = await httpClient.GetAsync(result.ImageUrl);
        imageResponse.StatusCode.Should().Be(HttpStatusCode.OK, "the presigned URL should be accessible");
        
        var downloadedFromUrl = await imageResponse.Content.ReadAsByteArrayAsync();
        downloadedFromUrl.Should().HaveCount(67, "image downloaded from URL should match the uploaded file size");
        downloadedFromUrl.Should().BeEquivalentTo(imageBytes, "image downloaded from URL should match the uploaded file");
    }

    [Fact]
    public async Task CreateCategory_WithDuplicateSlug_ReturnsConflict()
    {
        // Arrange
        await ResetDatabaseAsync();
        var existingCategory = TestDataBuilder.GenerateCategory(name: "Electronics", slug: "electronics");
        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(existingCategory);
            await db.SaveChangesAsync();
        });

        var formData = new MultipartFormDataContent
        {
            { new StringContent("New Electronics"), "name" },
            { new StringContent("electronics"), "slug" },
            { new StringContent("Description"), "description" },
            { new StringContent("1"), "displayOrder" }
        };

        // Act
        var response = await Client.PostAsync("/api/Categories", formData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateCategory_WithParentCategory_CreatesChildCategory()
    {
        // Arrange
        await ResetDatabaseAsync();
        var parentCategory = TestDataBuilder.GenerateCategory();
        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(parentCategory);
            await db.SaveChangesAsync();
        });

        var formData = new MultipartFormDataContent
        {
            { new StringContent("Subcategory"), "name" },
            { new StringContent("subcategory"), "slug" },
            { new StringContent("Child category"), "description" },
            { new StringContent(parentCategory.Id.ToString()), "parentCategoryId" },
            { new StringContent("1"), "displayOrder" }
        };

        // Act
        var response = await Client.PostAsync("/api/Categories", formData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CategoryDto>();
        result.Should().NotBeNull();
        result!.ParentCategoryId.Should().Be(parentCategory.Id);
    }

    [Fact]
    public async Task CreateCategory_WithNonExistentParent_ReturnsNotFound()
    {
        // Arrange
        await ResetDatabaseAsync();
        var nonExistentParentId = Guid.NewGuid();

        var formData = new MultipartFormDataContent
        {
            { new StringContent("Subcategory"), "name" },
            { new StringContent("subcategory"), "slug" },
            { new StringContent("Description"), "description" },
            { new StringContent(nonExistentParentId.ToString()), "parentCategoryId" },
            { new StringContent("1"), "displayOrder" }
        };

        // Act
        var response = await Client.PostAsync("/api/Categories", formData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region PUT /api/Categories/{id} - Update category

    [Fact]
    public async Task UpdateCategory_WithValidData_ReturnsUpdatedCategory()
    {
        // Arrange
        await ResetDatabaseAsync();
        var category = TestDataBuilder.GenerateCategory(name: "Old Name", slug: "old-slug");
        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(category);
            await db.SaveChangesAsync();
        });

        var formData = new MultipartFormDataContent
        {
            { new StringContent("New Name"), "name" },
            { new StringContent("new-slug"), "slug" },
            { new StringContent("Updated description"), "description" },
            { new StringContent("5"), "displayOrder" }
        };

        // Act
        var response = await Client.PutAsync($"/api/Categories/{category.Id}", formData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CategoryDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("New Name");
        result.Slug.Should().Be("new-slug");

        // Verify in database
        var dbCategory = await WithDbContextAsync(async db =>
            await db.Categories.FirstOrDefaultAsync(c => c.Id == category.Id));
        dbCategory.Should().NotBeNull();
        dbCategory!.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task UpdateCategory_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        await ResetDatabaseAsync();
        var nonExistentId = Guid.NewGuid();

        var formData = new MultipartFormDataContent
        {
            { new StringContent("Name"), "name" },
            { new StringContent("slug"), "slug" },
            { new StringContent("Description"), "description" },
            { new StringContent("1"), "displayOrder" }
        };

        // Act
        var response = await Client.PutAsync($"/api/Categories/{nonExistentId}", formData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateCategory_WithDuplicateSlug_ReturnsConflict()
    {
        // Arrange
        await ResetDatabaseAsync();
        var category1 = TestDataBuilder.GenerateCategory(slug: "electronics");
        var category2 = TestDataBuilder.GenerateCategory(slug: "clothing");
        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddRangeAsync(category1, category2);
            await db.SaveChangesAsync();
        });

        var formData = new MultipartFormDataContent
        {
            { new StringContent("Updated"), "name" },
            { new StringContent("electronics"), "slug" }, // Trying to use existing slug
            { new StringContent("Description"), "description" },
            { new StringContent("1"), "displayOrder" }
        };

        // Act
        var response = await Client.PutAsync($"/api/Categories/{category2.Id}", formData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateCategory_WithEmptyName_ReturnsBadRequest()
    {
        // Arrange
        await ResetDatabaseAsync();
        var category = TestDataBuilder.GenerateCategory();
        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(category);
            await db.SaveChangesAsync();
        });

        var formData = new MultipartFormDataContent
        {
            { new StringContent(""), "name" }, // Empty name
            { new StringContent("valid-slug"), "slug" },
            { new StringContent("Description"), "description" },
            { new StringContent("1"), "displayOrder" }
        };

        // Act
        var response = await Client.PutAsync($"/api/Categories/{category.Id}", formData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateCategory_WithEmptySlug_ReturnsBadRequest()
    {
        // Arrange
        await ResetDatabaseAsync();
        var category = TestDataBuilder.GenerateCategory();
        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(category);
            await db.SaveChangesAsync();
        });

        var formData = new MultipartFormDataContent
        {
            { new StringContent("Valid Name"), "name" },
            { new StringContent(""), "slug" }, // Empty slug
            { new StringContent("Description"), "description" },
            { new StringContent("1"), "displayOrder" }
        };

        // Act
        var response = await Client.PutAsync($"/api/Categories/{category.Id}", formData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateCategory_WithSelfAsParent_ReturnsBadRequest()
    {
        // Arrange
        await ResetDatabaseAsync();
        var category = TestDataBuilder.GenerateCategory();
        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(category);
            await db.SaveChangesAsync();
        });

        var formData = new MultipartFormDataContent
        {
            { new StringContent("Updated Name"), "name" },
            { new StringContent("updated-slug"), "slug" },
            { new StringContent("Description"), "description" },
            { new StringContent(category.Id.ToString()), "parentCategoryId" }, // Self as parent
            { new StringContent("1"), "displayOrder" }
        };

        // Act
        var response = await Client.PutAsync($"/api/Categories/{category.Id}", formData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateCategory_WithNonExistentParent_ReturnsNotFound()
    {
        // Arrange
        await ResetDatabaseAsync();
        var category = TestDataBuilder.GenerateCategory();
        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(category);
            await db.SaveChangesAsync();
        });

        var nonExistentParentId = Guid.NewGuid();
        var formData = new MultipartFormDataContent
        {
            { new StringContent("Updated Name"), "name" },
            { new StringContent("updated-slug"), "slug" },
            { new StringContent("Description"), "description" },
            { new StringContent(nonExistentParentId.ToString()), "parentCategoryId" },
            { new StringContent("1"), "displayOrder" }
        };

        // Act
        var response = await Client.PutAsync($"/api/Categories/{category.Id}", formData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateCategory_WithCircularReference_ReturnsBadRequest()
    {
        // Arrange
        await ResetDatabaseAsync();

        // Create a category hierarchy: grandparent -> parent -> child
        var grandparent = TestDataBuilder.GenerateCategory(name: "Grandparent", slug: "grandparent");
        var parent = TestDataBuilder.GenerateCategory(name: "Parent", slug: "parent");
        var child = TestDataBuilder.GenerateCategory(name: "Child", slug: "child");

        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(grandparent);
            await db.SaveChangesAsync();
        });

        parent.SetParentCategory(grandparent.Id);
        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(parent);
            await db.SaveChangesAsync();
        });

        child.SetParentCategory(parent.Id);
        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(child);
            await db.SaveChangesAsync();
        });

        // Try to set grandparent's parent to child (would create circular reference)
        var formData = new MultipartFormDataContent
        {
            { new StringContent("Grandparent Updated"), "name" },
            { new StringContent("grandparent"), "slug" },
            { new StringContent("Description"), "description" },
            { new StringContent(child.Id.ToString()), "parentCategoryId" }, // Creates circular reference
            { new StringContent("1"), "displayOrder" }
        };

        // Act
        var response = await Client.PutAsync($"/api/Categories/{grandparent.Id}", formData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateCategory_WithNewImage_UpdatesImageSuccessfully()
    {
        // Arrange
        await ResetDatabaseAsync();
        var category = TestDataBuilder.GenerateCategory(name: "Electronics", slug: "electronics");
        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(category);
            await db.SaveChangesAsync();
        });

        // Create a simple test image (1x1 pixel PNG)
        var newImageBytes = new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D,
            0x49, 0x48, 0x44, 0x52, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
            0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4, 0x89, 0x00, 0x00, 0x00,
            0x0A, 0x49, 0x44, 0x41, 0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
            0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00, 0x00, 0x00, 0x00, 0x49,
            0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82
        };

        var imageContent = new ByteArrayContent(newImageBytes);
        imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");

        var formData = new MultipartFormDataContent
        {
            { new StringContent("Electronics Updated"), "name" },
            { new StringContent("electronics-updated"), "slug" },
            { new StringContent("Updated description"), "description" },
            { new StringContent("5"), "displayOrder" },
            { imageContent, "image", "updated-category.png" }
        };

        // Act
        var response = await Client.PutAsync($"/api/Categories/{category.Id}", formData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CategoryDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("Electronics Updated");
        result.ImageUrl.Should().NotBeNullOrEmpty("image should be uploaded");

        // Verify in database
        var dbCategory = await WithDbContextAsync(async db =>
            await db.Categories.FirstOrDefaultAsync(c => c.Id == category.Id));
        dbCategory.Should().NotBeNull();
        dbCategory!.ImageUrl.Should().NotBeNullOrEmpty("image URL should be persisted");
    }

    [Fact]
    public async Task UpdateCategory_ChangingParentCategory_UpdatesSuccessfully()
    {
        // Arrange
        await ResetDatabaseAsync();
        var parent1 = TestDataBuilder.GenerateCategory(name: "Parent1", slug: "parent1");
        var parent2 = TestDataBuilder.GenerateCategory(name: "Parent2", slug: "parent2");
        var child = TestDataBuilder.GenerateCategory(name: "Child", slug: "child");

        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddRangeAsync(parent1, parent2);
            await db.SaveChangesAsync();
        });

        child.SetParentCategory(parent1.Id);
        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(child);
            await db.SaveChangesAsync();
        });

        // Change parent from parent1 to parent2
        var formData = new MultipartFormDataContent
        {
            { new StringContent("Child Updated"), "name" },
            { new StringContent("child-updated"), "slug" },
            { new StringContent("Description"), "description" },
            { new StringContent(parent2.Id.ToString()), "parentCategoryId" },
            { new StringContent("1"), "displayOrder" }
        };

        // Act
        var response = await Client.PutAsync($"/api/Categories/{child.Id}", formData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CategoryDto>();
        result.Should().NotBeNull();
        result!.ParentCategoryId.Should().Be(parent2.Id, "parent should be updated to parent2");

        // Verify in database
        var dbCategory = await WithDbContextAsync(async db =>
            await db.Categories.FirstOrDefaultAsync(c => c.Id == child.Id));
        dbCategory.Should().NotBeNull();
        dbCategory!.ParentCategoryId.Should().Be(parent2.Id);
    }

    #endregion

    #region DELETE /api/Categories/{id} - Delete category

    [Fact]
    public async Task DeleteCategory_WithValidId_ReturnsNoContent()
    {
        // Arrange
        await ResetDatabaseAsync();
        var category = TestDataBuilder.GenerateCategory();
        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(category);
            await db.SaveChangesAsync();
        });

        // Act
        var response = await Client.DeleteAsync($"/api/Categories/{category.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify category is deleted from database
        var dbCategory = await WithDbContextAsync(async db =>
            await db.Categories.FirstOrDefaultAsync(c => c.Id == category.Id));
        dbCategory.Should().BeNull();
    }

    [Fact]
    public async Task DeleteCategory_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        await ResetDatabaseAsync();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"/api/Categories/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCategory_WithChildCategories_ReturnsConflict()
    {
        // Arrange
        await ResetDatabaseAsync();
        var (parent, children) = TestDataBuilder.GenerateCategoryHierarchy(childCount: 1);
        await WithDbContextAsync(async db =>
        {
            await db.Categories.AddAsync(parent);
            await db.Categories.AddRangeAsync(children);
            await db.SaveChangesAsync();
        });

        // Act
        var response = await Client.DeleteAsync($"/api/Categories/{parent.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteCategory_WithProducts_ReturnsConflict()
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
        var response = await Client.DeleteAsync($"/api/Categories/{category.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Verify category still exists in database
        var dbCategory = await WithDbContextAsync(async db =>
            await db.Categories.FirstOrDefaultAsync(c => c.Id == category.Id));
        dbCategory.Should().NotBeNull("category should not be deleted when it contains products");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Verifies that a file exists in MinIO storage for the given category
    /// </summary>
    private async Task<bool> VerifyFileExistsInMinIO(Guid categoryId)
    {
        return await ExecuteInScopeAsync(async serviceProvider =>
        {
            var storageService = serviceProvider.GetRequiredService<R2.ShopNet.Framework.Persistence.Storage.Abstractions.IObjectStorageService>();
            
            try
            {
                // List all objects with the category ID prefix
                var objects = await storageService.ListAsync($"categories/{categoryId}");
                return objects.Any();
            }
            catch
            {
                return false;
            }
        });
    }

    /// <summary>
    /// Downloads a file from MinIO and verifies its content
    /// </summary>
    private async Task<byte[]?> DownloadFileFromMinIO(Guid categoryId)
    {
        return await ExecuteInScopeAsync(async serviceProvider =>
        {
            var storageService = serviceProvider.GetRequiredService<R2.ShopNet.Framework.Persistence.Storage.Abstractions.IObjectStorageService>();
            
            try
            {
                // List objects to get the file name
                var objects = await storageService.ListAsync($"categories/{categoryId}");
                var objectKey = objects.FirstOrDefault();
                
                if (string.IsNullOrEmpty(objectKey))
                    return null;

                // Download the file
                using var stream = await storageService.DownloadAsync(objectKey);
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
            catch
            {
                return null;
            }
        });
    }

    #endregion
}
