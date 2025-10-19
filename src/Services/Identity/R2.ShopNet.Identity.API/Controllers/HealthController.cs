using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using R2.ShopNet.Identity.Infrastructure.Persistence;

namespace R2.ShopNet.Identity.API.Controllers;

/// <summary>
/// Health check endpoint for service monitoring.
/// </summary>
[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly IdentityDbContext _dbContext;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        IdentityDbContext dbContext,
        ILogger<HealthController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Check service health status.
    /// </summary>
    /// <returns>Health status with dependency checks</returns>
    /// <response code="200">Service is healthy</response>
    /// <response code="503">Service is unhealthy</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get()
    {
        try
        {
            // Check database connectivity
            var canConnect = await _dbContext.Database.CanConnectAsync();

            if (!canConnect)
            {
                _logger.LogError("Health check failed: Cannot connect to database");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    status = "Unhealthy",
                    service = "identity-service",
                    timestamp = DateTime.UtcNow,
                    checks = new
                    {
                        database = "Failed"
                    }
                });
            }

            return Ok(new
            {
                status = "Healthy",
                service = "identity-service",
                timestamp = DateTime.UtcNow,
                checks = new
                {
                    database = "OK"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed with exception");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "Unhealthy",
                service = "identity-service",
                timestamp = DateTime.UtcNow,
                error = ex.Message
            });
        }
    }
}
