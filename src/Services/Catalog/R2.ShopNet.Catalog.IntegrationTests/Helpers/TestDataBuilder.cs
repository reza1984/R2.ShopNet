using Bogus;
using R2.ShopNet.Catalog.Domain.Entities;
using R2.ShopNet.Catalog.Domain.ValueObjects;

namespace R2.ShopNet.Catalog.IntegrationTests.Helpers;

/// <summary>
/// Test data builder using Bogus library to generate realistic test data
/// </summary>
public static class TestDataBuilder
{
    private static readonly Faker Faker = new();

    /// <summary>
    /// Generate a category with realistic data
    /// </summary>
    public static Category GenerateCategory(
        string? name = null,
        string? slug = null,
        string? description = null,
        Guid? parentCategoryId = null,
        int? displayOrder = null)
    {
        var categoryName = name ?? Faker.Commerce.Categories(1)[0];
        var category = new Category(
            categoryName,
            slug ?? categoryName.ToLowerInvariant().Replace(" ", "-"),
            description ?? Faker.Lorem.Sentence(),
            parentCategoryId
        );

        if (displayOrder.HasValue)
        {
            category.SetDisplayOrder(displayOrder.Value);
        }
        else
        {
            category.SetDisplayOrder(Faker.Random.Int(1, 100));
        }

        return category;
    }

    /// <summary>
    /// Generate multiple categories with unique slugs
    /// </summary>
    public static List<Category> GenerateCategories(int count)
    {
        var categories = new List<Category>();
        var usedSlugs = new HashSet<string>();

        for (int i = 0; i < count; i++)
        {
            string slug;
            string name;
            do
            {
                name = $"{Faker.Commerce.Categories(1)[0]}-{Guid.NewGuid().ToString("N").Substring(0, 8)}";
                slug = name.ToLowerInvariant().Replace(" ", "-");
            } while (usedSlugs.Contains(slug));

            usedSlugs.Add(slug);
            categories.Add(GenerateCategory(name: name, slug: slug, displayOrder: i + 1));
        }
        return categories;
    }

    /// <summary>
    /// Generate a category hierarchy (parent with children)
    /// </summary>
    public static (Category Parent, List<Category> Children) GenerateCategoryHierarchy(int childCount = 3)
    {
        var parent = GenerateCategory();
        var children = new List<Category>();

        for (int i = 0; i < childCount; i++)
        {
            var child = GenerateCategory(
                parentCategoryId: parent.Id,
                displayOrder: i + 1,
                slug: $"{parent.Slug}-child-{i + 1}"    
            );
            children.Add(child);
        }

        return (parent, children);
    }

    /// <summary>
    /// Generate random email
    /// </summary>
    public static string GenerateEmail() => Faker.Internet.Email();

    /// <summary>
    /// Generate random string
    /// </summary>
    public static string GenerateString(int length = 10) => Faker.Random.String2(length);

    /// <summary>
    /// Generate random positive integer
    /// </summary>
    public static int GenerateInt(int min = 1, int max = 100) => Faker.Random.Int(min, max);

    /// <summary>
    /// Generate a product with realistic data
    /// </summary>
    public static Product GenerateProduct(
        Guid? categoryId = null,
        string? name = null,
        string? slug = null,
        string? sku = null,
        decimal? price = null)
    {
        var productName = name ?? Faker.Commerce.ProductName();
        var productSlug = slug ?? productName.ToLowerInvariant().Replace(" ", "-");
        var productSku = sku ?? Faker.Commerce.Ean13();
        var productPrice = price ?? decimal.Parse(Faker.Commerce.Price());
        var category = categoryId ?? Guid.NewGuid();

        var product = new Product(
            productName,
            productSlug,
            productSku,
            new Money(productPrice, "USD"),
            category,
            Faker.Commerce.ProductDescription()
        );

        return product;
    }
}
