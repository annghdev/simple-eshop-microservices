namespace Catalog.API;

public class ProductDraft
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public decimal Price { get; set; }
    public string MainImage { get; set; } = string.Empty;
    public string SecondaryImage { get; set; } = string.Empty;
    public ProductDimensions Dimensions { get; set; } = new ProductDimensions(1, 1, 1, 1);
    public Guid? CategoryId { get; set; }
    public List<string> Images { get; set; } = [];
    public List<ProductAttribute> Attributes { get; set; } = [];
    public List<Variant> Variants { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

    public bool Validate()
    {
        // simulate
        return true;
    }
}
