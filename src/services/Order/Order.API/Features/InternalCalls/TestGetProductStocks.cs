using Contracts.Protos.InventoryStocks;
using Grpc.Core;
using Order.GrpcServices;
using Wolverine.Http;

namespace Order.Features.InternalCalls;

public record TestGetProductStocksRequest(List<TestProductVariantItem> Items);
public record TestProductVariantItem(Guid ProductId, Guid? VariantId);

public static class TestGetProductStocksEndpoint
{
    [WolverinePost("/order/internal-test/product-stocks")]
    public static async Task<GetProductStocksResponse> Test(
        TestGetProductStocksRequest request,
        IGetProductStocksCaller caller,
        CancellationToken ct)
    {
        var grpcRequest = new GetProductStocksRequest();
        grpcRequest.Items.AddRange(request.Items.Select(item => new ProductVariantQuery
        {
            ProductId = item.ProductId.ToString(),
            VariantId = item.VariantId?.ToString() ?? string.Empty
        }));

        return await caller.Get(grpcRequest, ct);
    }
}
