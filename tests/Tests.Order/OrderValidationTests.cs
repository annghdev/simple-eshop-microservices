using FluentAssertions;
using Order.Features.Orders;
using Tests.Common;

namespace Tests.Order;

public class OrderValidationTests
{
    private readonly CreateOrderValidator _validator = new();

    [Fact]
    [Trait("Category", TestCategories.Functional)]
    public void CreateOrderValidator_ShouldFail_WhenItemsIsEmpty()
    {
        var command = new CreateOrderCommand
        {
            CustomerName = "User",
            Address = "Address",
            PhoneNumber = "0900000000",
            CustomerId = Guid.CreateVersion7(),
            Items = []
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Items");
    }

    [Fact]
    [Trait("Category", TestCategories.Functional)]
    public void CreateOrderValidator_ShouldFail_WhenCustomerIdAndGuestIdAreBothProvided()
    {
        var command = new CreateOrderCommand
        {
            CustomerName = "User",
            Address = "Address",
            PhoneNumber = "0900000000",
            CustomerId = Guid.CreateVersion7(),
            GuestId = Guid.CreateVersion7(),
            Items =
            [
                new OrderItemDto(Guid.CreateVersion7(), null, 1)
            ]
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.ErrorMessage.Contains("Exactly one of CustomerId or GuestId", StringComparison.Ordinal));
    }
}

