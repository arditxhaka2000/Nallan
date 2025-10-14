// Controllers/GjirafaFeedController.cs
using Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.ProductServ; // IApiServices
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[ApiController]
[Route("api/gjirafa")]
public sealed class GjirafaFeedController : ControllerBase
{
    private readonly IApiServices _api;
    private readonly IConfiguration _cfg;

    public GjirafaFeedController(IApiServices api, IConfiguration cfg)
    {
        _api = api;
        _cfg = cfg;
    }

    // GET /api/gjirafa/products?lang=hr&page=1&pageSize=200&inStockOnly=true&flatten=true&updatedSince=2025-01-01
    [HttpGet("products")]
    [AllowAnonymous] // or require auth; see below
    public async Task<IActionResult> GetProducts(
        [FromQuery] string lang = "hr",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 200,
        [FromQuery] bool inStockOnly = false,
        [FromQuery] bool flatten = true,
        [FromQuery] DateTime? updatedSince = null,
        [FromHeader(Name = "X-API-Key")] string apiKey = null)
    {
        // Simple API-key auth (optional)
        var expectedKey = _cfg.GetValue<string>("GjirafaFeed:ApiKey");
        if (!string.IsNullOrEmpty(expectedKey) && apiKey != expectedKey)
            return Unauthorized(new { error = "Invalid API key" });

        var products = await _api.GetAllAsync(lang);

        // Optional: if your ApiData exposes UpdatedAt, filter here
        if (updatedSince.HasValue)
        {
            // products = products.Where(p => p.UpdatedAt >= updatedSince.Value).ToList();
        }

        if (inStockOnly)
            products = products.Where(p => (p.StoreStockQuantity + p.StoreSupplierQuantity) > 0).ToList();

        // Map to Gjirafa DTOs
        var mapped = products.Select(MapToGjirafaProduct(flatten)).ToList();

        // Paging
        var total = mapped.Count;
        var items = mapped.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        Response.Headers["X-Total-Count"] = total.ToString();
        return Ok(items);
    }

    // GET /api/gjirafa/products/{productCode}?lang=hr&flatten=true
    [HttpGet("products/{productCode}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProductByCode(
        [FromRoute] string productCode,
        [FromQuery] string lang = "hr",
        [FromQuery] bool flatten = true,
        [FromHeader(Name = "X-API-Key")] string apiKey = null)
    {
        var expectedKey = _cfg.GetValue<string>("GjirafaFeed:ApiKey");
        if (!string.IsNullOrEmpty(expectedKey) && apiKey != expectedKey)
            return Unauthorized(new { error = "Invalid API key" });

        var p = await _api.GetByIdAsync(productCode, lang);
        var dto = MapToGjirafaProduct(flatten)(p);
        return Ok(dto);
    }

    private Func<ApiData, GjirafaProductDto> MapToGjirafaProduct(bool flatten)
        => (ApiData p) =>
        {
            // Ensure image list is not null and never a single string
            var images = p.ImageUrls ?? new List<string>();
            if (images.Count == 0) images = new List<string> { "/no-image.png" };

            // Variants: include them only if flatten==false and your data actually has them
            var variants = (!flatten && (p.Variants?.Any() == true))
                ? p.Variants.Select(v => new GjirafaVariantDto
                {
                    ProductCode = v.ProductCode,
                    GTIN = string.IsNullOrWhiteSpace(v.GTIN) ? "" : v.GTIN,
                    Title = v.Title,
                    Description = v.Description,
                    Brand = v.Brand,
                    ProductUrl = v.ProductUrl,
                    ImageUrls = v.ImageUrls ?? new List<string>(),
                    Categories = v.Categories ?? new List<string>(),
                    Price = v.Price,
                    OldPrice = v.OldPrice,
                    StoreStockQuantity = v.StoreStockQuantity,
                    StoreSupplierQuantity = v.StoreSupplierQuantity,
                    Specifications = (v.Specifications ?? new List<Specification>())
                                     .Select(s => new SpecificationDto { Name = s.Name, Value = s.Value }).ToList(),
                    Variants = null, // Gjirafa doesn't want nested variants
                    VAT = 18
                }).ToList()
                : null;

            return new GjirafaProductDto
            {
                ProductCode = p.ProductCode,
                GTIN = string.IsNullOrWhiteSpace(p.GTIN) ? "PARENT" : p.GTIN, // matches their sample
                Title = p.Title,
                Description = p.Description,
                Brand = p.Brand,
                ProductUrl = p.ProductUrl,
                ImageUrls = images,
                Categories = p.Categories ?? new List<string>(),
                Price = p.Price,
                OldPrice = p.OldPrice,
                StoreStockQuantity = p.StoreStockQuantity,
                StoreSupplierQuantity = p.StoreSupplierQuantity,
                Specifications = (p.Specifications ?? new List<Specification>())
                                 .Select(s => new SpecificationDto { Name = s.Name, Value = s.Value }).ToList(),
                Variants = variants,
                VAT = 18
            };
        };
}
