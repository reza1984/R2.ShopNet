using Microsoft.AspNetCore.Mvc;

namespace R2.ShopNet.Catalog.API.Controllers;

/// <summary>
/// Health check endpoints for the Catalog service.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Basic health check endpoint.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok(new
        {
            Service = "Catalog API",
            Status = "Healthy",
            Timestamp = DateTime.UtcNow
        });
    }
}
