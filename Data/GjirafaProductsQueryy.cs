using System;

public class GjirafaProductsQuery
{
    public string Lang { get; set; } = "hr";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 200;
    public bool InStockOnly { get; set; }
    public bool Flatten { get; set; } = true;
    public DateTime? UpdatedSince { get; set; }
}