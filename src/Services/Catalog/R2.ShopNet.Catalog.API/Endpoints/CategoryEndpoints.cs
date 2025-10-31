using Microsoft.AspNetCore.Mvc;
using R2.ShopNet.Catalog.Application.Commands.CreateCategory;
using R2.ShopNet.Catalog.Application.Commands.DeleteCategory;
using R2.ShopNet.Catalog.Application.Commands.UpdateCategory;
using R2.ShopNet.Catalog.Application.DTOs;
using R2.ShopNet.Catalog.Application.Queries.GetCategories;
using R2.ShopNet.Catalog.Application.Queries.GetCategoryById;
using R2.ShopNet.Catalog.Application.Queries.GetCategoryHierarchy;
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
            .RequireAuthorization();

        categories.MapGet("", async (
            [FromServices] IQueryDispatcher queryDispatcher,
            [FromQuery] int pageNumber,
            [FromQuery] int pageSize,
            [FromQuery] Guid? parentCategoryId,
            [FromQuery] string? searchTerm,
            [FromQuery] string? sortBy,
            [FromQuery] bool sortDescending,
            CancellationToken cancellationToken) =>
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
            [FromBody] CreateCategoryCommand command,
            CancellationToken cancellationToken) =>
        {
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
            return Results.Created($"/api/v1/categories/{result.Value.Id}", result.Value);
        });

        categories.MapPut("/{id}", async (
            [FromServices] ICommandDispatcher commandDispatcher,
            Guid id,
            [FromBody] UpdateCategoryCommand command,
            CancellationToken cancellationToken) =>
        {
            if (id != command.CategoryId)
            {
                return Results.BadRequest(new { Error = "Category ID in route does not match command" });
            }
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
        });

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
    }
}
