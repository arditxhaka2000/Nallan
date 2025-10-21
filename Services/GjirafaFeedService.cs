using Data;
using Services.ProductServ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public interface IGjirafaFeedService
{
    Task<GjirafaFeedResponse> GetProductsAsync(GjirafaProductsQuery query);
    Task<GjirafaProductDto> GetProductByCodeAsync(string productCode, bool flatten);
}

public class GjirafaFeedService : IGjirafaFeedService
{
    private readonly IApiServices _api;

    public GjirafaFeedService(IApiServices api)
    {
        _api = api;
    }

    public async Task<GjirafaFeedResponse> GetProductsAsync(GjirafaProductsQuery query)
    {
        var products = await _api.GetAllAsync();

        products = ApplyFilters(products, query);
        var mapped = products.Select(p => MapToGjirafaProduct(p, query.Flatten)).ToList();
        var paged = ApplyPaging(mapped, query.Page, query.PageSize);

        return new GjirafaFeedResponse
        {
            Items = paged,
            TotalCount = mapped.Count()
        };
    }

    public async Task<GjirafaProductDto> GetProductByCodeAsync(string productCode, bool flatten)
    {
        var product = await _api.GetByIdAsync(productCode);
        return MapToGjirafaProduct(product, flatten);
    }

    private List<ApiData> ApplyFilters(List<ApiData> products, GjirafaProductsQuery query)
    {
        if (query.InStockOnly)
        {
            products = products.Where(p => (p.StoreStockQuantity + p.StoreSupplierQuantity) > 0).ToList();
        }

        return products;
    }

    private List<GjirafaProductDto> ApplyPaging(List<GjirafaProductDto> items, int page, int pageSize)
    {
        return items.Skip((page - 1) * pageSize).Take(pageSize).ToList();
    }

    private GjirafaProductDto MapToGjirafaProduct(ApiData product, bool flatten)
    {
        var images = NormalizeImages(product.ImageUrls);
        var variants = flatten ? null : MapVariants(product.Variants);

        return new GjirafaProductDto
        {
            ProductCode = product.ProductCode,
            GTIN = NormalizeGtin(product.GTIN),
            Title = product.Title,
            Description = product.Description,
            Brand = product.Brand,
            ProductUrl = product.ProductUrl,
            ImageUrls = images,
            Categories = product.Categories ?? new List<string>(),
            Price = product.Price,
            OldPrice = product.OldPrice,
            StoreStockQuantity = product.StoreStockQuantity,
            StoreSupplierQuantity = product.StoreSupplierQuantity,
            Specifications = MapSpecifications(product.Specifications),
            Variants = variants,
            VAT = 18
        };
    }

    private List<string> NormalizeImages(List<string> imageUrls)
    {
        var images = imageUrls ?? new List<string>();
        return images.Count == 0 ? new List<string> { "/no-image.png" } : images;
    }

    private string NormalizeGtin(string gtin)
    {
        return string.IsNullOrWhiteSpace(gtin) ? "PARENT" : gtin;
    }

    private List<GjirafaVariantDto> MapVariants(List<VariantApi> variants)
    {
        return (variants?.Any() == true)
            ? variants.Select(v => new GjirafaVariantDto
            {
                ProductCode = v.ProductCode,
                GTIN = NormalizeGtin(v.GTIN),
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
                Specifications = MapSpecifications(v.Specifications),
                Variants = null,
                VAT = 18
            }).ToList()
            : null;
    }

    private List<SpecificationDto> MapSpecifications(List<Specification> specs)
    {
        return (specs ?? new List<Specification>())
            .Select(s => new SpecificationDto { Name = s.Name, Value = s.Value })
            .ToList();
    }
}