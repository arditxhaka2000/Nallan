using Services.ProductServ;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

[ApiController]
[Route("api/gjirafa")]
[Authorize] // Requires valid JWT token
public sealed class GjirafaFeedController : ControllerBase
{
    private readonly IGjirafaFeedService _feedService;

    public GjirafaFeedController(IGjirafaFeedService feedService)
    {
        _feedService = feedService;
    }

    /// <summary>
    /// GET /api/gjirafa/products?page=1&pageSize=200&inStockOnly=true&flatten=true
    /// Requires JWT token from /api/gjirafa/login
    /// Authorization: Bearer {token}
    /// </summary>
    [HttpGet("products")]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 200,
        [FromQuery] bool inStockOnly = false,
        [FromQuery] bool flatten = true)
    {
        var query = new GjirafaProductsQuery
        {
            Page = page,
            PageSize = pageSize,
            InStockOnly = inStockOnly,
            Flatten = flatten
        };

        var response = await _feedService.GetProductsAsync(query);
        Response.Headers["X-Total-Count"] = response.TotalCount.ToString();

        return Ok(response.Items);
    }

    /// <summary>
    /// GET /api/gjirafa/products/{productCode}?flatten=true
    /// Requires JWT token from /api/gjirafa/login
    /// Authorization: Bearer {token}
    /// </summary>
    [HttpGet("products/{productCode}")]
    public async Task<IActionResult> GetProductByCode(
        [FromRoute] string productCode,
        [FromQuery] bool flatten = true)
    {
        var dto = await _feedService.GetProductByCodeAsync(productCode, flatten);
        return Ok(dto);
    }
}
