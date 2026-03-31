using Payment.IntegrationEvents;
using Wolverine;
using Wolverine.Http;

namespace Payment.Features.Payments;

public static class TestPaymentEndpoints
{
    [WolverinePut("payments/test/success/{id}")]
    public static async Task<IResult> Success(Guid id, IMessageBus bus)
    {
        var evt = new PaymentSuceeeded(Guid.NewGuid(), id, 100m);
        await bus.PublishAsync(evt);

        return Results.Ok();
    }

    [WolverinePut("payments/test/fail/{id}")]
    public static async Task<IResult> Fail(Guid id, IMessageBus bus)
    {
        var evt = new PaymentFailed(Guid.NewGuid(), id);
        await bus.PublishAsync(evt);

        return Results.Ok();
    }
}
