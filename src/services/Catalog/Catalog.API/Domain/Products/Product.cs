namespace Catalog.API;

public class Product
{
    public Product() { }

    public Product(ProductDraft draft)
    {
        Id = draft.Id;
        Name = draft.Name;
        ShortDescription = draft.ShortDescription;
        Description = draft.Description;
        Sku = draft.Sku;
        Cost = draft.Cost;
        Price = draft.Price;
        MainImage = draft.MainImage;
        SecondaryImage = draft.SecondaryImage;
        Dimensions = draft.Dimensions;
        CategoryId = draft.CategoryId;
        Images = draft.Images;
        Attributes = draft.Attributes;
        Variants = draft.Variants;
    }

    public static Product Publish(ProductDraft draft)
        => new Product(draft);

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
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;


    // Edit Text Informations

    public ProductNameEdited EditName(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Invalid product name");
        Name = name;
        return new ProductNameEdited(Id, name);
    }

    public ProductShortDescriptionEdited EditShortDescription(string shortDescription)
    {
        if (string.IsNullOrEmpty(shortDescription))
            throw new ArgumentException("Invalid short description");

        ShortDescription = shortDescription;
        return new ProductShortDescriptionEdited(Id, shortDescription);
    }

    public ProductDescriptionEdited EditDescription(string description)
    {
        if (string.IsNullOrEmpty(description))
            throw new ArgumentException("Invalid description");

        Description = description;
        return new ProductDescriptionEdited(Id, description);
    }

    public ProductDeactivated Decative()
    {
        IsActive = false;
        return new ProductDeactivated(Id);
    }

    public ProductReactivated Recative()
    {
        IsActive = true;
        return new ProductReactivated(Id);
    }

    public ProductCostChanged ChangeCost(decimal cost)
    {
        Cost = cost;
        return new ProductCostChanged(Id, cost);
    }

    public ProductPriceChanged ChangePrice(decimal price)
    {
        Price = price;
        return new ProductPriceChanged(Id, price);
    }

    public ProductMainImageChanged ChangeMainImage(string mainImage)
    {
        if (string.IsNullOrEmpty(mainImage))
            throw new ArgumentException("Invalid image url");
        MainImage = mainImage;
        return new ProductMainImageChanged(Id, mainImage);
    }
    public ProductSecondaryImageChanged ChangeSecondaryImage(string secondaryImage)
    {
        SecondaryImage = secondaryImage;
        return new ProductSecondaryImageChanged(Id, secondaryImage);
    }

    public ProductImageAdded AddImage(string image)
    {
        Images.Add(image);
        return new ProductImageAdded(Id, image);
    }
    public ProductImageRemoved RemoveImage(string image)
    {
        Images.Remove(image);
        return new ProductImageRemoved(Id, image);
    }

    public ProductAttributeAdded AddAttribute(
        Attribute attribute,
        int displayOrder,
        bool groupVariants,
        Dictionary<Guid, AttributeValue> variantAttributeValues)
    {
        Attributes.Add(new ProductAttribute
        {
            AttributeId = attribute.Id,
            DisplayOrder = displayOrder,
            GroupVariants = groupVariants
        });
        foreach (var vav in variantAttributeValues)
        {
            var variant = Variants.FirstOrDefault(v => v.Id == vav.Key)
                ?? throw new ArgumentException($"Variant with ID {vav.Key} does not exist");

            variant.Attribtues[attribute.Id] = vav.Value;
        }
        return new ProductAttributeAdded(Id, attribute.Id, attribute.Name, displayOrder, groupVariants, variantAttributeValues);
    }

    public ProductAttributeRemoved RemoveAttribute(Guid attributeId)
    {
        var attribute = Attributes.FirstOrDefault(a => a.AttributeId == attributeId)
            ?? throw new ArgumentException($"Attribute with ID {attributeId} does not exist");

        Attributes.Remove(attribute);
        return new ProductAttributeRemoved(Id, attributeId);
    }

    public ProductVariantAdded AddVariant(Variant variant)
    {
        if (Variants.Any(v => v.Name == variant.Name))
            throw new ArgumentException("Duplicate variant name");

        Variants.Add(variant);
        return new ProductVariantAdded(Id, variant);
    }

    public ProductVariantNameEdited EditVariantName(Guid variantId, string name)
    {
        var variant = Variants.FirstOrDefault(v => v.Id == variantId)
            ?? throw new ArgumentException($"variant with ID {variantId} does not exist");

        variant.Name = name;
        return new ProductVariantNameEdited(Id, variantId, name);
    }

    public ProductVariantSkuEdited EditVariantSku(Guid variantId, string sku)
    {
        var variant = Variants.FirstOrDefault(v => v.Id == variantId)
            ?? throw new ArgumentException($"variant with ID {variantId} does not exist");

        variant.Sku = sku;
        return new ProductVariantSkuEdited(Id, variantId, sku);
    }

    public ProductVariantCostChanged ChangeVariantCost(Guid variantId, decimal cost)
    {
        var variant = Variants.FirstOrDefault(v => v.Id == variantId)
            ?? throw new ArgumentException($"variant with ID {variantId} does not exist");

        variant.Cost = cost;
        return new ProductVariantCostChanged(Id, variantId, cost);
    }

    public ProductVariantPriceChanged ChangeVariantPrice(Guid variantId, decimal price)
    {
        var variant = Variants.FirstOrDefault(v => v.Id == variantId)
            ?? throw new ArgumentException($"variant with ID {variantId} does not exist");

        variant.Price = price;
        return new ProductVariantPriceChanged(Id, variantId, price);
    }

    public ProductVariantDimensionsChanged ChangeVariantDimension(Guid variantId, ProductDimensions dimensions)
    {
        var variant = Variants.FirstOrDefault(v => v.Id == variantId)
            ?? throw new ArgumentException($"variant with ID {variantId} does not exist");

        variant.Dimensions = dimensions;
        return new ProductVariantDimensionsChanged(Id, variantId, dimensions);
    }

    public ProductVariantAttributeValueChanged ChangeVariantAttributeValue(Guid variantId, Guid attributeId, AttributeValue value)
    {
        var variant = Variants.FirstOrDefault(v => v.Id == variantId)
            ?? throw new ArgumentException($"variant with ID {variantId} does not exist");

        if (!Attributes.Any(a => a.AttributeId == attributeId))
            throw new ArgumentException($"attribute with ID {attributeId} does not exist");

        variant.Attribtues[attributeId] = value;

        return new ProductVariantAttributeValueChanged(Id, variantId, attributeId, value);
    }

    public ProductVariantRemoved RemoveVariant(Guid variantId)
    {
        var variant = Variants.FirstOrDefault(v => v.Id == variantId)
            ?? throw new ArgumentException($"variant with ID {variantId} does not exist");

        Variants.Remove(variant);

        return new ProductVariantRemoved(Id, variantId);
    }

    public ProductCategoryChanged ChangeCategory(Category category)
    {
        CategoryId = category.Id;
        return new ProductCategoryChanged(Id, category.Id, category.Name);
    }

    public ProductDimensionsChanged ChangeDimensions(ProductDimensions dimensions)
    {
        Dimensions = dimensions;
        return new ProductDimensionsChanged(Id, dimensions);
    }
}
public class Variant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal? Cost { get; set; } // override cost
    public decimal? Price { get; set; } // override price
    public ProductDimensions? Dimensions { get; set; } // override dimesions
    public string MainImage { get; set; } = string.Empty;
    public List<string> Images { get; set; } = [];
    public Dictionary<Guid, AttributeValue> Attribtues { get; set; } = [];
}

public class ProductAttribute
{
    public Guid AttributeId { get; set; }
    public int DisplayOrder { get; set; }
    public bool GroupVariants { get; set; }
}

public class ProductDimensions
{
    public ProductDimensions(double width, double height, double depth, double weight)
    {
        if (width <= 0 || height <= 0 || depth <= 0 || weight <= 0)
            throw new ArgumentException("Invalid dimensions arguments");

        Width = width;
        Height = height;
        Depth = depth;
        Weight = weight;
    }

    public double Width { get; set; } = 1.0; // cm
    public double Height { get; set; } = 1.0; // cm
    public double Depth { get; set; } = 1.0; // cm
    public double Weight { get; set; } = 1.0; // kg
}
