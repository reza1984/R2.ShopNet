using Microsoft.AspNetCore.Mvc;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Identity.Application.Commands.LoginUser;
using R2.ShopNet.Identity.Application.Commands.RegisterUser;
using R2.ShopNet.Identity.Application.Commands.ForgotPassword;
using R2.ShopNet.Identity.Application.Commands.ResetPassword;

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

    /// <summary>
    /// Request password reset link via email.
    /// </summary>
    /// <param name="command">Forgot password request with email</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success message (always returns success to prevent email enumeration)</returns>
    /// <response code="200">Password reset request processed</response>
    /// <response code="400">Invalid email format</response>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ForgotPasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Password reset requested for email: {Email}", command.Email);

        var result = await _commandDispatcher.Dispatch(command, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Password reset request failed for {Email}: {Error}",
                command.Email, result.Error.Message);
            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });
        }

        _logger.LogInformation("Password reset request processed for email: {Email}", command.Email);
        return Ok(result.Value);
    }

    /// <summary>
    /// Reset user password using reset token.
    /// </summary>
    /// <param name="command">Reset password request with token and new password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success message if password reset successful</returns>
    /// <response code="200">Password reset successful</response>
    /// <response code="400">Invalid token, password validation failed, or user not found</response>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ResetPasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Password reset attempt for email: {Email}", command.Email);

        var result = await _commandDispatcher.Dispatch(command, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Password reset failed for {Email}: {Error}",
                command.Email, result.Error.Message);
            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });
        }

        _logger.LogInformation("Password reset successful for email: {Email}", command.Email);
        return Ok(result.Value);
    }
}
