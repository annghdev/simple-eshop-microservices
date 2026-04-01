using Catalog.Domain;
using FluentAssertions;
using Tests.Common;

namespace Tests.Catalog;

public class CatalogDomainTests
{
    [Fact]
    [Trait("Category", TestCategories.Unit)]
    public void ProductDraft_Publish_ShouldThrow_WhenDraftNotConfirmed()
    {
        var draft = new ProductDraft
        {
            Id = Guid.CreateVersion7(),
            Name = "Draft"
        };

        var act = () => draft.Publish();

        act.Should().Throw<InvalidOperationException>().WithMessage("*before confirm*");
    }

    [Fact]
    [Trait("Category", TestCategories.Functional)]
    public void Product_AddVariant_ShouldRejectDuplicateVariantName()
    {
        var draft = new ProductDraft
        {
            Id = Guid.CreateVersion7(),
            Name = "Phone",
            CurrentStep = ProductDraftStep.Step6_Confirm
        };
        var product = draft.Publish();
        product.AddVariant(new Variant { Id = Guid.CreateVersion7(), Name = "Black" });

        var act = () => product.AddVariant(new Variant { Id = Guid.CreateVersion7(), Name = "Black" });

        act.Should().Throw<ArgumentException>().WithMessage("*Duplicate variant name*");
    }

    [Fact]
    [Trait("Category", TestCategories.Unit)]
    public void ProductDimensions_ShouldThrow_WhenAnyDimensionIsNotPositive()
    {
        var act = () => new ProductDimensions(0, 10, 10, 1);

        act.Should().Throw<ArgumentException>().WithMessage("*Invalid dimensions*");
    }
}

