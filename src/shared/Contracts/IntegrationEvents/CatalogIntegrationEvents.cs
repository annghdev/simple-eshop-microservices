using Kernel.Interfaces;

namespace Catalog.IntegrationEvents;

public interface ICatalogIntegrationEvent : IIntegrationEvent;

public record ProductPublished(
    Guid ProductId,
    string Name,
    string MainImage,
    List<VariantInfo> Variants) : ICatalogIntegrationEvent; // ==> Init Inventory Items
public record VariantInfo(Guid VariantId, string Name, string MainImage) : ICatalogIntegrationEvent;

public record ProductDeactivated(Guid ProductId) : ICatalogIntegrationEvent; // ==> Lock Inventory Items
public record ProductReactivated(Guid ProductId) : ICatalogIntegrationEvent; // ==> Unlock Inventory Items
public record ProductVariantDeactivated(Guid ProductId, Guid VariantId) : ICatalogIntegrationEvent; // ==> Lock Inventory Items
public record ProductVariantReactivated(Guid ProductId, Guid VariantId) : ICatalogIntegrationEvent; // ==> Unlock Inventory Items