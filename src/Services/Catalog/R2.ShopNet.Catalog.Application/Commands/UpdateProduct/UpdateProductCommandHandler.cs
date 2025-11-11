using R2.ShopNet.Catalog.Application.DTOs;
using R2.ShopNet.Catalog.Domain.Entities;
using R2.ShopNet.Catalog.Domain.Enums;
using R2.ShopNet.Catalog.Domain.Events;
using R2.ShopNet.Catalog.Domain.ValueObjects;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Framework.CQRS.Attributes;
using R2.ShopNet.Framework.Events;
using R2.ShopNet.Framework.Persistence.UnitOfWork;

namespace R2.ShopNet.Catalog.Application.Commands.UpdateProduct;

/// <summary>
/// Handler for updating an existing product.
/// </summary>
[GenerateHandler]
public class UpdateProductCommandHandler : ICommandHandler<UpdateProductCommand, Result<ProductDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;

    public UpdateProductCommandHandler(
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher)
    {
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result<ProductDto>> Handle(
        UpdateProductCommand command,
        CancellationToken cancellationToken)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return Result.Failure<ProductDto>(
                Error.Validation("Name.Empty", "Product name cannot be empty"));
        }

        if (string.IsNullOrWhiteSpace(command.Sku))
        {
            return Result.Failure<ProductDto>(
                Error.Validation("Sku.Empty", "Product SKU cannot be empty"));
        }

        if (command.Price < 0)
        {
            return Result.Failure<ProductDto>(
                Error.Validation("Price.Negative", "Product price cannot be negative"));
        }

        var productRepository = _unitOfWork.Repository<Product>();

        // Get the product
        var product = await productRepository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product == null)
        {
            return Result.Failure<ProductDto>(
                Error.NotFound("Product.NotFound", "Product not found"));
        }

        // Check if SKU is being changed and already exists
        if (product.Sku != command.Sku.ToUpperInvariant())
        {
            var existingProduct = (await productRepository.FindAsync(
                p => p.Sku == command.Sku.ToUpperInvariant() && p.Id != command.ProductId,
                cancellationToken)).FirstOrDefault();

            if (existingProduct != null)
            {
                return Result.Failure<ProductDto>(
                    Error.Conflict("Sku.AlreadyExists", "A product with this SKU already exists"));
            }
        }

        // Check if category exists
        var categoryRepository = _unitOfWork.Repository<Category>();
        var category = await categoryRepository.GetByIdAsync(command.CategoryId, cancellationToken);
        if (category == null)
        {
            return Result.Failure<ProductDto>(
                Error.NotFound("Category.NotFound", "Category not found"));
        }

        // Update product properties
        product.SetName(command.Name);
        product.SetSlug(command.Slug);
        product.SetSku(command.Sku);
        product.SetDescription(command.Description);
        product.SetShortDescription(command.ShortDescription);

        // Update pricing
        var price = new Money(command.Price, command.Currency);
        product.SetPrice(price);

        Money? discountPrice = command.DiscountPrice.HasValue
            ? new Money(command.DiscountPrice.Value, command.Currency)
            : null;
        product.SetDiscountPrice(discountPrice);

        Money? costPrice = command.CostPrice.HasValue
            ? new Money(command.CostPrice.Value, command.Currency)
            : null;
        product.SetCostPrice(costPrice);

        // Update inventory
        product.UpdateStock(command.StockQuantity);
        product.SetReorderLevel(command.ReorderLevel);

        // Update status
        if (Enum.TryParse<ProductStatus>(command.Status, out var status))
        {
            product.SetStatus(status);
        }

        // Update category
        product.SetCategory(command.CategoryId);

        // Update details
        product.SetBrand(command.Brand);
        product.SetWeight(command.Weight);
        product.SetDimensions(command.Dimensions);

        // Update SEO
        product.SetMetaTags(command.MetaTitle, command.MetaDescription, command.MetaKeywords);

        // Save changes
        await productRepository.UpdateAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish domain event
        await _eventPublisher.Publish(
            new ProductUpdatedEvent(product.Id, product.Name, product.Sku),
            cancellationToken);

        // Map to DTO
        var productDto = new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            ShortDescription = product.ShortDescription,
            Slug = product.Slug,
            Sku = product.Sku,
            Price = product.Price.Amount,
            Currency = product.Price.Currency,
            DiscountPrice = product.DiscountPrice?.Amount,
            DiscountPercentage = product.GetDiscountPercentage(),
            StockQuantity = product.StockQuantity,
            ReorderLevel = product.ReorderLevel,
            Status = product.Status.ToString(),
            CategoryId = product.CategoryId,
            CategoryName = category.Name,
            Brand = product.Brand,
            Weight = product.Weight,
            Dimensions = product.Dimensions,
            MetaTitle = product.MetaTitle,
            MetaDescription = product.MetaDescription,
            MetaKeywords = product.MetaKeywords,
            ViewCount = product.ViewCount,
            AverageRating = product.AverageRating,
            ReviewCount = product.ReviewCount,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };

        return Result.Success(productDto);
    }
}
