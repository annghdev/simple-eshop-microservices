namespace Tests.Common;

[CollectionDefinition("integration-containers", DisableParallelization = true)]
public sealed class IntegrationContainerCollection : ICollectionFixture<CompositeIntegrationFixture>
{
}
