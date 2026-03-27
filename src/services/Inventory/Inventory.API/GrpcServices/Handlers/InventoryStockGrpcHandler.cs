using Contracts.InternalCalls.Inventory;
using Contracts.Protos.InventoryStocks;
using Grpc.Core;

namespace Inventory.GrpcServices;

public class InventoryStockGrpcHandler(IMessageBus bus) : InventoryStockGrpc.InventoryStockGrpcBase
{
    public override async Task<GetProductStocksResponse> GetProductStocks(GetProductStocksRequest request, ServerCallContext context)
    {
        var queryItems = new List<InventoryItemQuery>();

        foreach (var item in request.Items)
        {
            if (!Guid.TryParse(item.ProductId, out var productId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, $"Invalid product_id: {item.ProductId}"));
            }

            Guid? variantId = null;
            if (!string.IsNullOrWhiteSpace(item.VariantId))
            {
                if (!Guid.TryParse(item.VariantId, out var parsedVariantId))
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, $"Invalid variant_id: {item.VariantId}"));
                }

                variantId = parsedVariantId;
            }

            queryItems.Add(new InventoryItemQuery(productId, variantId));
        }

        var productStocks = await bus.InvokeAsync<List<ProductStockInfo>>(
            new GetProductStockQuery(queryItems),
            context.CancellationToken);

        var response = new GetProductStocksResponse();
        response.Products.AddRange(productStocks.Select(productStock => new ProductStock
        {
            ProductId = productStock.ProductId.ToString(),
            VariantId = productStock.VariantId?.ToString() ?? string.Empty,
            StockInfos =
            {
                productStock.StockInfos.Select(stockInfo => new ItemStock
                {
                    ItemId = stockInfo.ItemId.ToString(),
                    Available = stockInfo.Available,
                    WarehouseId = stockInfo.WarehouseId.ToString(),
                    WarehouseLat = Convert.ToDouble(stockInfo.WarehouseLat),
                    WarehouseLng = Convert.ToDouble(stockInfo.WarehouseLng)
                })
            }
        }));

        return response;
    }
}
