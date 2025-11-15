using R2.ShopNet.Catalog.Domain.Entities;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Framework.CQRS.Attributes;
using R2.ShopNet.Framework.Persistence.UnitOfWork;

namespace R2.ShopNet.Catalog.Application.Commands;

/// <summary>
/// Handler for setting a product image as the primary image.
/// </summary>
[GenerateHandler]
public class SetPrimaryProductImageCommandHandler : ICommandHandler<SetPrimaryProductImageCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;

    public SetPrimaryProductImageCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(
        SetPrimaryProductImageCommand command,
        CancellationToken cancellationToken)
    {
        // Verify product exists
        var productRepository = _unitOfWork.Repository<Product>();
        var product = await productRepository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product == null)
        {
            return Result.Failure<bool>(
                Error.NotFound("Product.NotFound", "Product not found"));
        }

        // Get the image
        var imageRepository = _unitOfWork.Repository<ProductImage>();
        var image = await imageRepository.GetByIdAsync(command.ImageId, cancellationToken);
        if (image == null)
        {
            return Result.Failure<bool>(
                Error.NotFound("Image.NotFound", "Image not found"));
        }

        // Verify the image belongs to the product
        if (image.ProductId != command.ProductId)
        {
            return Result.Failure<bool>(
                Error.Validation("Image.InvalidProduct", "Image does not belong to this product"));
        }

        // Unmark any existing primary images
        var existingPrimaryImages = await imageRepository.FindAsync(
            pi => pi.ProductId == command.ProductId && pi.IsPrimary,
            cancellationToken);
        foreach (var existingImage in existingPrimaryImages)
        {
            existingImage.UnmarkAsPrimary();
        }

        // Mark the new primary image
        image.MarkAsPrimary();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
