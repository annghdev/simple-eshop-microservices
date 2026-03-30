using Contracts.Protos.OrderReservation;
using Grpc.Core;
using Order.Features.Orders;
using Wolverine;

namespace Order.API.GrpcServices.Handlers;

public class GetOrderReservationItemsGrpcHandler(IMessageBus bus) : OrderReservationItemsGrpc.OrderReservationItemsGrpcBase
{
    public override async Task<GetOrderReservationItemsResponse> GetItems(GetOrderReservationItemsRequest request, ServerCallContext context)
    {
        var orderId = Guid.Parse(request.OrderId);

        var query = new GetOrderReservationDetailsQuery(orderId);

        var result = await bus.InvokeAsync<OrderReservationDetailsResponse>(query, context.CancellationToken);

        var response = new GetOrderReservationItemsResponse
        {
            OrderId = result.OrderId.ToString(),
            ReservationStatus = (int)result.ReservationStatus,
            Items =
            {
                result.Items.Select(i => new ReservationItems
                {
                    InventoryItemId = i.InventoryItemId.ToString(),
                    Quantity = i.Quantity,
                })
            }
        };

        return response;
    }
}
