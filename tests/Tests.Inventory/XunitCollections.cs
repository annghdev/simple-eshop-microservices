using Tests.Common;

namespace Tests.Inventory;

[CollectionDefinition("integration-containers", DisableParallelization = true)]
public sealed class IntegrationContainerCollection : ICollectionFixture<CompositeIntegrationFixture>
{
}

