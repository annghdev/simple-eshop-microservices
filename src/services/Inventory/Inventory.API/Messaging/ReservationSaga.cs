using Inventory.Domain;
using Inventory.Features;
using Inventory.Features.Commands;
using Inventory.IntegrationEvents;
using Order.IntegrationEvents;
using Serilog.Core;
using Wolverine;
using Wolverine.Persistence.Sagas;

namespace Inventory.Messaging;

public class ReservationSaga : Saga
{
    // OrderId
    public Guid Id { get; set; }
    public int TotalItems { get; set; }
    public HashSet<Guid> ReservedItems { get; set; } = [];
    public Dictionary<Guid, int> Quantities { get; set; } = [];

    public int Handled { get; set; }

    public static (ReservationSaga, object[]) Start([SagaIdentityFrom("OrderId")] OrderPlaced e, ILogger<ReservationSaga> logger)
    {
        logger.LogInformation("Got a new Order with ID {Id}", e.OrderId);
        var saga = new ReservationSaga
        {
            Id = e.OrderId,
        };

        var reserveCmds = new List<ReserveStockCommand>();

        foreach (var item in e.Items)
        {
            reserveCmds.Add(new ReserveStockCommand(item.InventoryItemId, item.Quantity, e.OrderId));
        }
        return (saga, [.. reserveCmds]);
    }

    public object[] Handle([SagaIdentityFrom("OrderId")] StockReserved e, ILogger<ReservationSaga> logger)
    {
        logger.LogDebug("Order with ID {Id} reserved for Item {ItemId}", e.OrderId, e.Id);

        if (ReservedItems.Contains(e.Id)) return [];

        ReservedItems.Add(e.Id);

        Handled++;

        if (ReservedItems.Count == TotalItems)
        {
            MarkCompleted();
            logger.LogInformation("Order with ID {Id} reservation succeeded", e.OrderId);
        }

        else if (Handled == TotalItems)
            return Rollback(logger);

        return [];
    }

    public object[] Handle([SagaIdentityFrom("OrderId")] ReservationFailed e, ILogger<ReservationSaga> logger)
    {
        logger.LogDebug("Order with ID {Id} reserve fail for Item {ItemId}", e.OrderId, e.Id);

        Handled++;

        if (Handled < TotalItems)
            return [];

        else
            return Rollback(logger);

    }

    private object[] Rollback(ILogger<ReservationSaga> logger)
    {
        var releaseCmds = ReservedItems.Select(id => new ReleaseStockCommand(id, Quantities[id], Id));
        var failedEvent = new InventoryReservationFailed(Id);
        MarkCompleted();
        logger.LogWarning("Order with ID {Id} reservation failed", Id);
        return [.. releaseCmds, failedEvent];
    }
}