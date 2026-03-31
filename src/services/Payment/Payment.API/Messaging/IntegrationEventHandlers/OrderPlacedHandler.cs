using Order.IntegrationEvents;

namespace Payment.Messaging;

public static class OrderPlacedHandler
{
    public static Task Handle(OrderPlaced e)
    {
        // No action needed for OrderCreated in Payment service
        return Task.CompletedTask;
    }
}
