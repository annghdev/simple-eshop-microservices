namespace Catalog.API;

public record ProducPublished(Product Product);


// Status events
public record ProductDeactivated(Guid ProductId);
public record ProductReactivated(Guid ProductId);


// Text information events
public record ProductNameEdited(Guid ProductId, string Name);
public record ProductShortDescriptionEdited(Guid ProductId, string ShortDescription);
public record ProductDescriptionEdited(Guid ProductId, string Description);


// Pricing events
public record ProductCostChanged(Guid ProductId, decimal Cost);
public record ProductPriceChanged(Guid ProductId, decimal Price);


// Image Events
public record ProductMainImageChanged(Guid ProductId, string MainImage);
public record ProductSecondaryImageChanged(Guid ProductId, string SecondaryImage);
public record ProductImageAdded(Guid ProductId, string Image);
public record ProductImageRemoved(Guid ProductId, string Image);


// Attribute events
public record ProductAttributeAdded(
    Guid ProductId,
    Guid AttributeId,
    string AttributeName,
    int DisplayOrder,
    bool GroupVariants,
    Dictionary<Guid, AttributeValue> VariantAttributeValues // VariantId - new AttributeValue
    );
public record ProductAttributeRemoved(Guid ProductId, Guid AttributeId);


// Variant events
public record ProductVariantAdded(Guid ProductId, Variant Variant);
public record ProductVariantNameEdited(Guid ProductId, Guid VariantId, string Name);
public record ProductVariantSkuEdited(Guid ProductId, Guid VariantId, string Sku);
public record ProductVariantCostChanged(Guid ProductId, Guid VariantId, decimal Cost);
public record ProductVariantPriceChanged(Guid ProductId, Guid VariantId, decimal Price);
public record ProductVariantDimensionsChanged(Guid ProductId, Guid VariantId, ProductDimensions Dimensions);
public record ProductVariantAttributeValueChanged(Guid ProductId, Guid VariantId, Guid AttributeId, AttributeValue Value);
public record ProductVariantRemoved(Guid ProductId, Guid VariantId);


// Other events
public record ProductCategoryChanged(Guid ProductId, Guid CategoryId, string CategoryName);
public record ProductDimensionsChanged(Guid ProductId, ProductDimensions Dimensions);