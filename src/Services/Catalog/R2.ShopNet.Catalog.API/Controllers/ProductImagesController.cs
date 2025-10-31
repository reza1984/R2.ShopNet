using Microsoft.AspNetCore.Mvc;
using R2.ShopNet.Catalog.Application.Commands.DeleteProductImage;
using R2.ShopNet.Catalog.Application.Commands.UploadProductImage;
using R2.ShopNet.Catalog.Application.Queries.GetProductImages;
using R2.ShopNet.Framework.CQRS;

namespace R2.ShopNet.Catalog.API.Controllers;

/// <summary>
/// Product image management endpoints for the Catalog service.
/// Handles uploads, downloads, and deletions of product images stored in MinIO.
/// </summary>
[ApiController]
[Route("api/products/{productId}/images")]
[Produces("application/json")]
public class ProductImagesController : ControllerBase
{
    private readonly ICommandDispatcher _commandDispatcher;
    private readonly IQueryDispatcher _queryDispatcher;
    private readonly ILogger<ProductImagesController> _logger;

    public ProductImagesController(
        ICommandDispatcher commandDispatcher,
        IQueryDispatcher queryDispatcher,
        ILogger<ProductImagesController> logger)
    {
        _commandDispatcher = commandDispatcher;
        _queryDispatcher = queryDispatcher;
        _logger = logger;
    }

    /// <summary>
    /// Get all images for a product with presigned download URLs
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="expiryMinutes">URL expiry time in minutes (default: 60)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of product images with download URLs</returns>
    [HttpGet]
    [ProducesResponseType(typeof(GetProductImagesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductImages(
        [FromRoute] Guid productId,
        [FromQuery] int expiryMinutes = 60,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting images for product {ProductId}", productId);

        var query = new GetProductImagesQuery
        {
            ProductId = productId,
            ExpiryMinutes = expiryMinutes
        };

        var result = await _queryDispatcher.Dispatch(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(result.Error);
    }

    /// <summary>
    /// Upload a new image for a product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="file">Image file to upload (max 10MB, allowed: jpg, png, webp, gif)</param>
    /// <param name="altText">Alternative text for accessibility</param>
    /// <param name="displayOrder">Display order for sorting</param>
    /// <param name="isPrimary">Whether this should be the primary product image</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Upload result with image metadata</returns>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadProductImageResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB max
    public async Task<IActionResult> UploadProductImage(
        [FromRoute] Guid productId,
        [FromForm] IFormFile file,
        [FromForm] string? altText = null,
        [FromForm] int displayOrder = 0,
        [FromForm] bool isPrimary = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Uploading image for product {ProductId}: {FileName}",
            productId,
            file?.FileName);

        if (file == null || file.Length == 0)
        {
            return BadRequest("File is required");
        }

        var command = new UploadProductImageCommand
        {
            ProductId = productId,
            File = file,
            AltText = altText,
            DisplayOrder = displayOrder,
            IsPrimary = isPrimary
        };

        var result = await _commandDispatcher.Dispatch(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return CreatedAtAction(
            nameof(GetProductImages),
            new { productId },
            result.Value);
    }

    /// <summary>
    /// Delete a product image
    /// </summary>
    /// <param name="productId">Product ID (for route consistency)</param>
    /// <param name="imageId">Image ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deletion result</returns>
    [HttpDelete("{imageId}")]
    [ProducesResponseType(typeof(DeleteProductImageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteProductImage(
        [FromRoute] Guid productId,
        [FromRoute] Guid imageId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Deleting image {ImageId} for product {ProductId}",
            imageId,
            productId);

        var command = new DeleteProductImageCommand
        {
            ImageId = imageId
        };

        var result = await _commandDispatcher.Dispatch(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return result.Value.Success
            ? Ok(result.Value)
            : NotFound(result.Value);
    }
}
