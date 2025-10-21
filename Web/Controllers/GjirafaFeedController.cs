using Services.ProductServ;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;

[ApiController]
[Route("api/gjirafa")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public sealed class GjirafaFeedController : ControllerBase
{
    private readonly IGjirafaFeedService _feedService;

    public GjirafaFeedController(IGjirafaFeedService feedService)
    {
        _feedService = feedService;
    }

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

    [HttpGet("products/{productCode}")]
    public async Task<IActionResult> GetProductByCode(
        [FromRoute] string productCode,
        [FromQuery] bool flatten = true)
    {
        var dto = await _feedService.GetProductByCodeAsync(productCode, flatten);
        return Ok(dto);
    }
}