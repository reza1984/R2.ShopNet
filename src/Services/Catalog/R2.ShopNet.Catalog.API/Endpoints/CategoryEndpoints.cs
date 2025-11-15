using Microsoft.AspNetCore.Mvc;
using R2.ShopNet.Catalog.Application.Commands;
using R2.ShopNet.Catalog.Application.Queries;
using R2.ShopNet.Catalog.Application.DTOs;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;


namespace R2.ShopNet.Catalog.API.Endpoints;

/// <summary>
/// Category management endpoints for the Catalog service.
/// </summary>

public static class Categories
{
    public static void RegisterCategoryEndpoints(this IEndpointRouteBuilder routes)
    {
        var categories = routes.MapGroup($"/api/{nameof(Categories)}")
            .WithTags(nameof(Categories));

        categories.MapGet("", async (
            [FromServices] IQueryDispatcher queryDispatcher,
            [FromQuery] int pageNumber,
            [FromQuery] int pageSize,
            CancellationToken cancellationToken,
            [FromQuery] Guid? parentCategoryId = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool sortDescending = false) =>
        {
            var query = new GetCategoriesQuery(
                pageNumber,
                pageSize,
                parentCategoryId,
                searchTerm,
                sortBy,
                sortDescending);
            var result = await queryDispatcher.Dispatch(query, cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        categories.MapGet("/{id}", async (
            [FromServices] IQueryDispatcher queryDispatcher,
            Guid id,
            CancellationToken cancellationToken) =>
        {
            var query = new GetCategoryByIdQuery(id);
            var result = await queryDispatcher.Dispatch(query, cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });

        categories.MapGet("/hierarchy", async (
            [FromServices] IQueryDispatcher queryDispatcher,
            CancellationToken cancellationToken) =>
        {
            var query = new GetCategoryHierarchyQuery();
            var result = await queryDispatcher.Dispatch(query, cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        categories.MapPost("", async (
            [FromServices] ICommandDispatcher commandDispatcher,
            [FromForm] string name,
            [FromForm] string slug,
            [FromForm] string? description,
            [FromForm] Guid? parentCategoryId,
            [FromForm] int displayOrder,
            [FromForm] IFormFile? image,
            CancellationToken cancellationToken) =>
        {
            ImageUploadDto? imageUploadDto = null;
            if (image != null && image.Length > 0)
            {
                using var memoryStream = new MemoryStream();
                await image.CopyToAsync(memoryStream, cancellationToken);
                imageUploadDto = new ImageUploadDto(
                    image.FileName,
                    memoryStream.ToArray(),
                    image.ContentType,
                    image.Length);
            }

            var command = new CreateCategoryCommand(name, slug, description, parentCategoryId, displayOrder, imageUploadDto);
            var result = await commandDispatcher.Dispatch(command, cancellationToken);
            if (!result.IsSuccess)
            {
                return result.Error.Type switch
                {
                    ErrorType.Conflict => Results.Conflict(result.Error),
                    ErrorType.NotFound => Results.NotFound(result.Error),
                    _ => Results.BadRequest(result.Error)
                };
            }
            return Results.Created($"/api/categories/{result.Value.Id}", result.Value);
        })
        .DisableAntiforgery();

        categories.MapPut("/{id}", async (
            [FromServices] ICommandDispatcher commandDispatcher,
            Guid id,
            [FromForm] string name,
            [FromForm] string slug,
            [FromForm] string? description,
            [FromForm] Guid? parentCategoryId,
            [FromForm] int displayOrder,
            [FromForm] IFormFile? image,
            CancellationToken cancellationToken) =>
        {
            ImageUploadDto? imageUploadDto = null;
            if (image != null && image.Length > 0)
            {
                using var memoryStream = new MemoryStream();
                await image.CopyToAsync(memoryStream, cancellationToken);
                imageUploadDto = new ImageUploadDto(
                    image.FileName,
                    memoryStream.ToArray(),
                    image.ContentType,
                    image.Length);
            }

            var command = new UpdateCategoryCommand(id, name, slug, description, parentCategoryId, displayOrder, imageUploadDto);
            var result = await commandDispatcher.Dispatch(command, cancellationToken);
            if (!result.IsSuccess)
            {
                return result.Error.Type switch
                {
                    ErrorType.NotFound => Results.NotFound(result.Error),
                    ErrorType.Conflict => Results.Conflict(result.Error),
                    _ => Results.BadRequest(result.Error)
                };
            }
            return Results.Ok(result.Value);
        })
        .DisableAntiforgery();

        categories.MapDelete("/{id}", async (
            [FromServices] ICommandDispatcher commandDispatcher,
            Guid id,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteCategoryCommand(id);
            var result = await commandDispatcher.Dispatch(command, cancellationToken);
            if (!result.IsSuccess)
            {
                return result.Error.Type switch
                {
                    ErrorType.NotFound => Results.NotFound(result.Error),
                    ErrorType.Conflict => Results.Conflict(result.Error),
                    _ => Results.BadRequest(result.Error)
                };
            }
            return Results.NoContent();
        });

        #region Category Images

        var categoryImages = routes.MapGroup($"/api/{nameof(Categories)}/{{categoryId:guid}}/images")
            .WithTags($"{nameof(Categories)} Images");

        categoryImages.MapPost("", async (
            [FromServices] ICommandDispatcher commandDispatcher,
            Guid categoryId,
            [FromForm] IFormFile file,
            [FromForm] string? altText,
            CancellationToken cancellationToken) =>
        {
            if (file == null || file.Length == 0)
            {
                return Results.BadRequest("File is required");
            }
            var command = new UploadCategoryImageCommand
            {
                CategoryId = categoryId,
                File = file,
                AltText = altText
            };
            var result = await commandDispatcher.Dispatch(command, cancellationToken);
            if (!result.IsSuccess)
            {
                return result.Error.Type switch
                {
                    ErrorType.NotFound => Results.NotFound(result.Error),
                    _ => Results.BadRequest(result.Error)
                };
            }
            return Results.Created($"/api/categories/{categoryId}/images", result.Value);
        })
        .DisableAntiforgery();

        categoryImages.MapDelete("", async (
            [FromServices] ICommandDispatcher commandDispatcher,
            Guid categoryId,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteCategoryImageCommand
            {
                CategoryId = categoryId
            };
            var result = await commandDispatcher.Dispatch(command, cancellationToken);
            if (!result.IsSuccess)
            {
                return result.Error.Type switch
                {
                    ErrorType.NotFound => Results.NotFound(result.Error),
                    _ => Results.BadRequest(result.Error)
                };
            }
            return result.Value.Success ? Results.Ok(result.Value) : Results.NotFound(result.Value);
        });

        #endregion
    }
}
