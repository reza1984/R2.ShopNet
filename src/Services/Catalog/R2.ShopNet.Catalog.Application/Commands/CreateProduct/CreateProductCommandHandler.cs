using R2.ShopNet.Catalog.Application.DTOs;
using R2.ShopNet.Catalog.Domain.Entities;
using R2.ShopNet.Catalog.Domain.Events;
using R2.ShopNet.Catalog.Domain.ValueObjects;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Framework.CQRS.Attributes;
using R2.ShopNet.Framework.Events;
using R2.ShopNet.Framework.Persistence.UnitOfWork;

namespace R2.ShopNet.Catalog.Application.Commands.CreateProduct;

/// <summary>
/// Handler for creating a new product.
/// </summary>
[GenerateHandler]
public class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, Result<ProductDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;

    public CreateProductCommandHandler(
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher)
    {
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result<ProductDto>> Handle(
        CreateProductCommand command,
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

        // Check if SKU already exists
        var productRepository = _unitOfWork.Repository<Product>();
        var existingProduct = (await productRepository.FindAsync(
            p => p.Sku == command.Sku.ToUpperInvariant(),
            cancellationToken)).FirstOrDefault();

        if (existingProduct != null)
        {
            return Result.Failure<ProductDto>(
                Error.Conflict("Sku.AlreadyExists", "A product with this SKU already exists"));
        }

        // Check if category exists
        var categoryRepository = _unitOfWork.Repository<Category>();
        var category = await categoryRepository.GetByIdAsync(command.CategoryId, cancellationToken);
        if (category == null)
        {
            return Result.Failure<ProductDto>(
                Error.NotFound("Category.NotFound", "Category not found"));
        }

        // Create Money value object
        var price = new Money(command.Price, command.Currency);
        Money? discountPrice = command.DiscountPrice.HasValue
            ? new Money(command.DiscountPrice.Value, command.Currency)
            : null;

        // Create product
        var product = new Product(
            command.Name,
            command.Slug,
            command.Sku,
            price,
            command.CategoryId,
            command.Description);

        product.SetShortDescription(command.ShortDescription);
        product.SetDiscountPrice(discountPrice);
        product.UpdateStock(command.StockQuantity);
        product.SetReorderLevel(command.ReorderLevel);
        product.SetBrand(command.Brand);
        product.SetWeight(command.Weight);
        product.SetDimensions(command.Dimensions);

        // Add to repository
        await productRepository.AddAsync(product, cancellationToken);

        // Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish domain event
        await _eventPublisher.Publish(
            new ProductCreatedEvent(
                product.Id,
                product.Name,
                product.Sku,
                product.CategoryId,
                product.Price.Amount,
                product.Price.Currency),
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
            ViewCount = product.ViewCount,
            AverageRating = product.AverageRating,
            ReviewCount = product.ReviewCount,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };

        return Result.Success(productDto);
    }
}
