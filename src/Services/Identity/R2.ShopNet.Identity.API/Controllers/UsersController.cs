using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Identity.Application.Commands.ActivateUser;
using R2.ShopNet.Identity.Application.Commands.DeactivateUser;
using R2.ShopNet.Identity.Application.Commands.DeleteUser;
using R2.ShopNet.Identity.Application.Commands.UpdateUser;
using R2.ShopNet.Identity.Application.DTOs;
using R2.ShopNet.Identity.Application.Queries.GetUserById;
using R2.ShopNet.Identity.Application.Queries.GetUsers;

namespace R2.ShopNet.Identity.API.Controllers;

/// <summary>
/// User management endpoints for admin operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
// [Authorize(Roles = "Admin")] // TODO: Enable authorization once auth is fully configured
public class UsersController : ControllerBase
{
    private readonly ICommandDispatcher _commandDispatcher;
    private readonly IQueryDispatcher _queryDispatcher;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        ICommandDispatcher commandDispatcher,
        IQueryDispatcher queryDispatcher,
        ILogger<UsersController> logger)
    {
        _commandDispatcher = commandDispatcher;
        _queryDispatcher = queryDispatcher;
        _logger = logger;
    }

    /// <summary>
    /// Get a paginated list of users.
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20)</param>
    /// <param name="searchTerm">Search term for filtering users</param>
    /// <param name="isActive">Filter by active status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of users</returns>
    /// <response code="200">Users retrieved successfully</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting users - Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);

        var query = new GetUsersQuery(pageNumber, pageSize, searchTerm, isActive);
        var result = await _queryDispatcher.Dispatch(query, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to get users: {Error}", result.Error.Message);
            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get a user by ID.
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User details</returns>
    /// <response code="200">User retrieved successfully</response>
    /// <response code="404">User not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting user by ID: {UserId}", id);

        var query = new GetUserByIdQuery(id);
        var result = await _queryDispatcher.Dispatch(query, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("User not found: {UserId}", id);
            return NotFound(new { error = result.Error.Code, message = result.Error.Message });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Update a user's information.
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="command">Update user command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    /// <response code="200">User updated successfully</response>
    /// <response code="404">User not found</response>
    /// <response code="400">Update failed</response>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateUser(
        Guid id,
        [FromBody] UpdateUserCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating user: {UserId}", id);

        // Ensure the ID from the route matches the command
        var updateCommand = command with { UserId = id };
        var result = await _commandDispatcher.Dispatch(updateCommand, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to update user {UserId}: {Error}", id, result.Error.Message);
            
            if (result.Error.Type == ErrorType.NotFound)
                return NotFound(new { error = result.Error.Code, message = result.Error.Message });
            
            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });
        }

        return Ok(new { message = "User updated successfully" });
    }

    /// <summary>
    /// Soft delete a user.
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    /// <response code="200">User deleted successfully</response>
    /// <response code="404">User not found</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting user: {UserId}", id);

        var command = new DeleteUserCommand(id);
        var result = await _commandDispatcher.Dispatch(command, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to delete user {UserId}: {Error}", id, result.Error.Message);
            
            if (result.Error.Type == ErrorType.NotFound)
                return NotFound(new { error = result.Error.Code, message = result.Error.Message });
            
            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });
        }

        return Ok(new { message = "User deleted successfully" });
    }

    /// <summary>
    /// Activate a user account.
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    /// <response code="200">User activated successfully</response>
    /// <response code="404">User not found</response>
    [HttpPost("{id}/activate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateUser(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Activating user: {UserId}", id);

        var command = new ActivateUserCommand(id);
        var result = await _commandDispatcher.Dispatch(command, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to activate user {UserId}: {Error}", id, result.Error.Message);
            
            if (result.Error.Type == ErrorType.NotFound)
                return NotFound(new { error = result.Error.Code, message = result.Error.Message });
            
            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });
        }

        return Ok(new { message = "User activated successfully" });
    }

    /// <summary>
    /// Deactivate a user account.
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    /// <response code="200">User deactivated successfully</response>
    /// <response code="404">User not found</response>
    [HttpPost("{id}/deactivate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateUser(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deactivating user: {UserId}", id);

        var command = new DeactivateUserCommand(id);
        var result = await _commandDispatcher.Dispatch(command, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to deactivate user {UserId}: {Error}", id, result.Error.Message);
            
            if (result.Error.Type == ErrorType.NotFound)
                return NotFound(new { error = result.Error.Code, message = result.Error.Message });
            
            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });
        }

        return Ok(new { message = "User deactivated successfully" });
    }
}
