// Models/GjirafaFeedDtos.cs
using System.Collections.Generic;

public sealed class GjirafaProductDto
{
    public string ProductCode { get; set; }
    public string GTIN { get; set; }               // "PARENT" or barcode if available
    public string Title { get; set; }
    public string Description { get; set; }
    public string Brand { get; set; }
    public string ProductUrl { get; set; }
    public List<string> ImageUrls { get; set; } = new(); // MUST be a list
    public List<string> Categories { get; set; } = new();
    public decimal Price { get; set; }
    public decimal OldPrice { get; set; }
    public int StoreStockQuantity { get; set; }
    public int StoreSupplierQuantity { get; set; }
    public List<SpecificationDto> Specifications { get; set; } = new();
    public List<GjirafaVariantDto> Variants { get; set; } = null; // optional
    public int VAT { get; set; } = 18; // requested by Gjirafa
}

public sealed class GjirafaVariantDto
{
    public string ProductCode { get; set; }
    public string GTIN { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Brand { get; set; }
    public string ProductUrl { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public decimal Price { get; set; }
    public decimal OldPrice { get; set; }
    public int StoreStockQuantity { get; set; }
    public int StoreSupplierQuantity { get; set; }
    public List<SpecificationDto> Specifications { get; set; } = new();
    public List<GjirafaVariantDto> Variants { get; set; } = null; // Gjirafa said no nested variants
    public int VAT { get; set; } = 18;
}

public sealed class SpecificationDto
{
    public string Name { get; set; }
    public string Value { get; set; }
}
