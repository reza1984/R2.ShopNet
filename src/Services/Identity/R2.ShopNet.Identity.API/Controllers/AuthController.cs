using Microsoft.AspNetCore.Mvc;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Identity.Application.Commands.LoginUser;
using R2.ShopNet.Identity.Application.Commands.RegisterUser;

namespace R2.ShopNet.Identity.API.Controllers;

/// <summary>
/// Authentication endpoints for user registration and login.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly ICommandDispatcher _commandDispatcher;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ICommandDispatcher commandDispatcher,
        ILogger<AuthController> logger)
    {
        _commandDispatcher = commandDispatcher;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user account.
    /// </summary>
    /// <param name="command">User registration details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Registration result with user ID</returns>
    /// <response code="200">User registered successfully</response>
    /// <response code="400">Invalid input or email already exists</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("User registration attempt for email: {Email}", command.Email);

        var result = await _commandDispatcher.Dispatch(command, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("User registration failed for {Email}: {Error}",
                command.Email, result.Error.Message);
            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });
        }

        _logger.LogInformation("User {UserId} registered successfully", result.Value.UserId);
        return Ok(result.Value);
    }

    /// <summary>
    /// Authenticate user and obtain JWT access token.
    /// </summary>
    /// <param name="command">User login credentials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Login result with access token</returns>
    /// <response code="200">Login successful, returns JWT token</response>
    /// <response code="400">Invalid credentials or account locked</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(
        [FromBody] LoginUserCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Login attempt for email: {Email}", command.Email);

        var result = await _commandDispatcher.Dispatch(command, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Login failed for {Email}: {Error}",
                command.Email, result.Error.Message);
            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });
        }

        _logger.LogInformation("User {UserId} logged in successfully", result.Value.UserId);
        return Ok(result.Value);
    }
}
