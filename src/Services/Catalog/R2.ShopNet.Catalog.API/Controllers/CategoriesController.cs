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

namespace R2.ShopNet.Catalog.API.Controllers;

/// <summary>
/// Category management endpoints for the Catalog service.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly ICommandDispatcher _commandDispatcher;
    private readonly IQueryDispatcher _queryDispatcher;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(
        ICommandDispatcher commandDispatcher,
        IQueryDispatcher queryDispatcher,
        ILogger<CategoriesController> logger)
    {
        _commandDispatcher = commandDispatcher;
        _queryDispatcher = queryDispatcher;
        _logger = logger;
    }

    /// <summary>
    /// Get a paginated list of categories with optional filtering.
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <param name="parentCategoryId">Filter by parent category ID (use Guid.Empty for root categories)</param>
    /// <param name="searchTerm">Search term for name, description, or slug</param>
    /// <param name="sortBy">Sort field (DisplayOrder, Name, CreatedAt)</param>
    /// <param name="sortDescending">Sort descending (default: false)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCategories(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? parentCategoryId = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? sortBy = "DisplayOrder",
        [FromQuery] bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        var query = new GetCategoriesQuery(
            pageNumber,
            pageSize,
            parentCategoryId,
            searchTerm,
            sortBy,
            sortDescending);

        var result = await _queryDispatcher.Dispatch(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    /// <summary>
    /// Get a category by ID.
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetCategoryByIdQuery(id);
        var result = await _queryDispatcher.Dispatch(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(result.Error);
    }

    /// <summary>
    /// Get the full category hierarchy (tree structure).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("hierarchy")]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryHierarchyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCategoryHierarchy(
        CancellationToken cancellationToken = default)
    {
        var query = new GetCategoryHierarchyQuery();
        var result = await _queryDispatcher.Dispatch(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    /// <summary>
    /// Create a new category.
    /// </summary>
    /// <param name="command">Category creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPost]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCategory(
        [FromBody] CreateCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _commandDispatcher.Dispatch(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error.Type switch
            {
                ErrorType.Conflict => Conflict(result.Error),
                ErrorType.NotFound => NotFound(result.Error),
                _ => BadRequest(result.Error)
            };
        }

        return CreatedAtAction(
            nameof(GetCategoryById),
            new { id = result.Value.Id },
            result.Value);
    }

    /// <summary>
    /// Update an existing category.
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="command">Category update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCategory(
        Guid id,
        [FromBody] UpdateCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        // Ensure the ID in the route matches the command
        if (id != command.CategoryId)
        {
            return BadRequest(new { Error = "Category ID in route does not match command" });
        }

        var result = await _commandDispatcher.Dispatch(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error.Type switch
            {
                ErrorType.NotFound => NotFound(result.Error),
                ErrorType.Conflict => Conflict(result.Error),
                _ => BadRequest(result.Error)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Delete a category (soft delete).
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var command = new DeleteCategoryCommand(id);
        var result = await _commandDispatcher.Dispatch(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error.Type switch
            {
                ErrorType.NotFound => NotFound(result.Error),
                ErrorType.Conflict => Conflict(result.Error),
                _ => BadRequest(result.Error)
            };
        }

        return NoContent();
    }
}
