using Contracts.Protos.OrderReservation;
using Grpc.Core;

namespace Inventory.API.GrpcServices.Callers;

public interface IGetOrderReservationItemsCaller
{
    Task<GetOrderReservationItemsResponse> GetOrderReservationItemsAsync(Guid orderId, CancellationToken ct = default);
}

public class GetOrderReservationItemsCaller(OrderReservationItemsGrpc.OrderReservationItemsGrpcClient client) : IGetOrderReservationItemsCaller
{
    public async Task<GetOrderReservationItemsResponse> GetOrderReservationItemsAsync(Guid orderId, CancellationToken ct = default)
    {
		try
		{
            GetOrderReservationItemsRequest request = new GetOrderReservationItemsRequest
            {
                OrderId = orderId.ToString()
            };

            return await client.GetItemsAsync(request, cancellationToken: ct);
        }
        catch (RpcException ex)
        {
            throw new Exception($"Order gRPC failed with status {ex.StatusCode}: {ex.Status.Detail}");
        }
    }
}
