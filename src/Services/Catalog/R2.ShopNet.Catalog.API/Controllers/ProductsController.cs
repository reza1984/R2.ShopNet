using Microsoft.AspNetCore.Mvc;
using R2.ShopNet.Catalog.Application.Commands.CreateProduct;
using R2.ShopNet.Catalog.Application.DTOs;
using R2.ShopNet.Catalog.Application.Queries.GetProductById;
using R2.ShopNet.Catalog.Application.Queries.GetProducts;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;

namespace R2.ShopNet.Catalog.API.Controllers;

/// <summary>
/// Product management endpoints for the Catalog service.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly ICommandDispatcher _commandDispatcher;
    private readonly IQueryDispatcher _queryDispatcher;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        ICommandDispatcher commandDispatcher,
        IQueryDispatcher queryDispatcher,
        ILogger<ProductsController> logger)
    {
        _commandDispatcher = commandDispatcher;
        _queryDispatcher = queryDispatcher;
        _logger = logger;
    }

    /// <summary>
    /// Get a paginated list of products.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? status = null,
        [FromQuery] string? sortBy = "CreatedAt",
        [FromQuery] bool sortDescending = true,
        CancellationToken cancellationToken = default)
    {
        var query = new GetProductsQuery(
            pageNumber,
            pageSize,
            categoryId,
            searchTerm,
            status,
            sortBy,
            sortDescending);

        var result = await _queryDispatcher.Dispatch(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    /// <summary>
    /// Get a product by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetProductByIdQuery(id);
        var result = await _queryDispatcher.Dispatch(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(result.Error);
    }

    /// <summary>
    /// Create a new product.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProduct(
        [FromBody] CreateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _commandDispatcher.Dispatch(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return CreatedAtAction(
            nameof(GetProductById),
            new { id = result.Value.Id },
            result.Value);
    }
}
