using Contracts.Enums;
using FluentAssertions;
using Payment.Domain;
using Tests.Common;

namespace Tests.Payment;

public class PaymentDomainTests
{
    [Fact]
    [Trait("Category", TestCategories.Unit)]
    public void PaymentTransaction_ShouldInitializeTimestampToCurrentUtc()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var tx = new PaymentTransaction
        {
            Id = Guid.CreateVersion7(),
            OrderId = Guid.CreateVersion7(),
            Method = PaymentMethod.Online
        };
        var after = DateTimeOffset.UtcNow.AddSeconds(1);

        tx.Timestamp.Should().BeOnOrAfter(before);
        tx.Timestamp.Should().BeOnOrBefore(after);
    }

    [Fact]
    [Trait("Category", TestCategories.Functional)]
    public void PaymentTransaction_ShouldKeepAssignedStatus()
    {
        var tx = new PaymentTransaction
        {
            Id = Guid.CreateVersion7(),
            OrderId = Guid.CreateVersion7(),
            Status = PaymentStatus.Paid
        };

        tx.Status.Should().Be(PaymentStatus.Paid);
    }
}

