using FluentAssertions;
using Shipping.Domain;
using Tests.Common;

namespace Tests.Shipping;

public class ShippingDomainTests
{
    [Fact]
    [Trait("Category", TestCategories.Unit)]
    public void ShippingLog_ShouldInitializeTimestamp()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var log = new ShippingLog
        {
            Id = Guid.CreateVersion7(),
            ShipmentId = Guid.CreateVersion7(),
            Status = ShippingStatus.Preparing,
            Description = "created"
        };
        var after = DateTimeOffset.UtcNow.AddSeconds(1);

        log.TimeStamp.Should().BeOnOrAfter(before);
        log.TimeStamp.Should().BeOnOrBefore(after);
    }

    [Fact]
    [Trait("Category", TestCategories.Functional)]
    public void Shipment_ShouldStoreDimensionsAndAddress()
    {
        var shipment = new Shipment
        {
            Id = Guid.CreateVersion7(),
            OrderId = Guid.CreateVersion7(),
            Address = "District 1",
            Width = 1.5m,
            Height = 2m,
            Depth = 3m,
            Weight = 0.5m
        };

        shipment.Address.Should().Be("District 1");
        shipment.Weight.Should().Be(0.5m);
        shipment.Width.Should().BePositive();
    }
}

