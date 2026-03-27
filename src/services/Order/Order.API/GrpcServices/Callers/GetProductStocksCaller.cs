using Contracts.Protos.InventoryStocks;
using Grpc.Core;

namespace Order.GrpcServices;

public interface IGetProductStocksCaller
{
    Task<GetProductStocksResponse> Get(GetProductStocksRequest request, CancellationToken ct = default);
}

public class GetProductStocksCaller(InventoryStockGrpc.InventoryStockGrpcClient inventoryStockClient) : IGetProductStocksCaller
{
    public async Task<GetProductStocksResponse> Get(GetProductStocksRequest request, CancellationToken ct = default)
    {
        try
        {
            return await inventoryStockClient.GetProductStocksAsync(request, cancellationToken: ct);
        }
        catch (RpcException ex)
        {
            throw new Exception($"Inventory gRPC failed with status {ex.StatusCode}: {ex.Status.Detail}");
        }
    }
}
