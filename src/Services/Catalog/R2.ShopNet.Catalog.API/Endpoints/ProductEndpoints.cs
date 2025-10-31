using Microsoft.AspNetCore.Mvc;
using R2.ShopNet.Catalog.Application.Commands.CreateProduct;
using R2.ShopNet.Catalog.Application.Commands.DeleteProductImage;
using R2.ShopNet.Catalog.Application.Commands.UploadProductImage;
using R2.ShopNet.Catalog.Application.Queries.GetProductById;
using R2.ShopNet.Catalog.Application.Queries.GetProductImages;
using R2.ShopNet.Catalog.Application.Queries.GetProducts;
using R2.ShopNet.Framework.CQRS;


namespace R2.ShopNet.Catalog.API.Endpoints;

/// <summary>
/// Product management endpoints for the Catalog service.
/// </summary>

public static class Products
{
    public static void RegisterProductEndpoints(this IEndpointRouteBuilder routes)
    {
        #region Products

        var products = routes.MapGroup($"/api/{nameof(Products)}");

        products.MapGet("", async (
            [FromServices] IQueryDispatcher queryDispatcher,
            [FromQuery] int pageNumber,
            [FromQuery] int pageSize,
            [FromQuery] Guid? categoryId,
            [FromQuery] string? searchTerm,
            [FromQuery] string? status,
            [FromQuery] string? sortBy,
            [FromQuery] bool sortDescending,
            CancellationToken cancellationToken) =>
        {
            var query = new GetProductsQuery(
                pageNumber,
                pageSize,
                categoryId,
                searchTerm,
                status,
                sortBy,
                sortDescending);
            var result = await queryDispatcher.Dispatch(query, cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        products.MapGet("/{id}", async (
            [FromServices] IQueryDispatcher queryDispatcher,
            Guid id,
            CancellationToken cancellationToken) =>
        {
            var query = new GetProductByIdQuery(id);
            var result = await queryDispatcher.Dispatch(query, cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });

        products.MapPost("", async (
            [FromServices] ICommandDispatcher commandDispatcher,
            [FromBody] CreateProductCommand command,
            CancellationToken cancellationToken) =>
        {
            var result = await commandDispatcher.Dispatch(command, cancellationToken);
            if (!result.IsSuccess)
            {
                return Results.BadRequest(result.Error);
            }
            return Results.Created($"/api/{nameof(Products)}/{result.Value.Id}", result.Value);
        });
       
        #endregion

        #region Product Images

        var productImages = routes.MapGroup($"/api/{nameof(Products)}/{{productId}}/images");

        productImages.MapGet("", async (
                [FromServices] IQueryDispatcher queryDispatcher,
                Guid productId,
                [FromQuery] int expiryMinutes,
                CancellationToken cancellationToken) =>
            {
                var query = new GetProductImagesQuery
                {
                    ProductId = productId,
                    ExpiryMinutes = expiryMinutes
                };
                var result = await queryDispatcher.Dispatch(query, cancellationToken);
                return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
            });

        productImages.MapPost("", async (
            [FromServices] ICommandDispatcher commandDispatcher,
            Guid productId,
            IFormFile file,
            string? altText,
            int displayOrder,
            bool isPrimary,
            CancellationToken cancellationToken) =>
        {
            if (file == null || file.Length == 0)
            {
                return Results.BadRequest("File is required");
            }
            var command = new UploadProductImageCommand
            {
                ProductId = productId,
                File = file,
                AltText = altText,
                DisplayOrder = displayOrder,
                IsPrimary = isPrimary
            };
            var result = await commandDispatcher.Dispatch(command, cancellationToken);
            if (!result.IsSuccess)
            {
                return Results.BadRequest(result.Error);
            }
            return Results.Created($"/api/v1/products/{productId}/images", result.Value);
        });

        productImages.MapDelete("/{imageId}", async (
            [FromServices] ICommandDispatcher commandDispatcher,
            Guid productId,
            Guid imageId,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteProductImageCommand
            {
                ImageId = imageId
            };
            var result = await commandDispatcher.Dispatch(command, cancellationToken);
            if (!result.IsSuccess)
            {
                return Results.BadRequest(result.Error);
            }
            return result.Value.Success ? Results.Ok(result.Value) : Results.NotFound(result.Value);
        });

        #endregion
    }
}
