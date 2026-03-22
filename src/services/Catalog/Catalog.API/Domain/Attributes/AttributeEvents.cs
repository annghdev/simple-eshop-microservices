namespace Catalog.API;

public record AttributeCreated(Attribute Attribute);
public record AttributeNameEdited(Guid AttributeId, string Name);
public record AttributeDeleted(Guid AttributeId);
public record AttributeValueAdded(Guid AttributeId, AttributeValue Value);
public record AttributeValueRemoved(Guid AttributeId, string ValueText);
