using SV22T1020330.Models.Catalog;

namespace SV22T1020330.Shop.Models;

public class ProductIndexViewModel
{
    public ProductSearchInput Search { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
}
