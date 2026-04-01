using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Tests.Common;

namespace Tests.E2E.Aspire;

[Collection("aspire-e2e")]
public class OrderLifecycleE2ETests
{
    private readonly AspireE2ECollectionFixture _fixture;

    public OrderLifecycleE2ETests(AspireE2ECollectionFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    [Trait("Category", TestCategories.E2EAspire)]
    public async Task GuestCreateOrderFlow_ShouldCreateOrder_AndAppearInGuestHistory()
    {
        var client = _fixture.CreateClient("apigateway");
        var guestId = Guid.CreateVersion7();
        var orderId = Guid.CreateVersion7();

        var payload = new
        {
            id = orderId,
            customerName = "Guest E2E",
            address = "District 1",
            phoneNumber = "0900000000",
            items = new[]
            {
                new
                {
                    productId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    variantId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    quantity = 1
                }
            }
        };

        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/order/orders")
        {
            Content = JsonContent.Create(payload)
        };
        createRequest.Headers.Add("X-Guest-Id", guestId.ToString());

        using var createResponse = await client.SendAsync(createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var appeared = false;
        for (var i = 0; i < 15 && !appeared; i++)
        {
            using var historyRequest = new HttpRequestMessage(HttpMethod.Get, "/api/order/orders/history");
            historyRequest.Headers.Add("X-Guest-Id", guestId.ToString());
            using var historyResponse = await client.SendAsync(historyRequest);
            historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await historyResponse.Content.ReadAsStringAsync();
            appeared = json.Contains(orderId.ToString(), StringComparison.OrdinalIgnoreCase)
                && json.Contains(guestId.ToString(), StringComparison.OrdinalIgnoreCase);

            if (!appeared)
            {
                await Task.Delay(500);
            }
        }

        appeared.Should().BeTrue("the created order should eventually appear in guest history");
    }

    [Fact]
    [Trait("Category", TestCategories.E2EAspire)]
    public async Task Guest_Order_Reserve_Payment_Confirm_CommitStock_ShouldCompleteBusinessFlow()
    {
        var gatewayClient = _fixture.CreateClient("apigateway");
        var orderClient = _fixture.CreateClient("order");
        var paymentClient = _fixture.CreateClient("payment");
        var guestId = Guid.CreateVersion7();
        var orderId = Guid.CreateVersion7();
        var productId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var variantId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        var stockBefore = await GetTotalAvailableStockAsync(orderClient, productId, variantId);
        stockBefore.Should().BeGreaterThan(0);

        var createPayload = new
        {
            id = orderId,
            customerName = "Guest Lifecycle",
            address = "District 1",
            phoneNumber = "0900000000",
            items = new[]
            {
                new
                {
                    productId,
                    variantId,
                    quantity = 1
                }
            }
        };

        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/order/orders")
        {
            Content = JsonContent.Create(createPayload)
        };
        createRequest.Headers.Add("X-Guest-Id", guestId.ToString());

        using var createResponse = await gatewayClient.SendAsync(createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var placedReached = await WaitForOrderStateInGuestHistoryAsync(
            gatewayClient,
            orderId,
            guestId,
            expectedStatusValue: 1,
            expectedStatusName: "Placed",
            shouldBePaid: false);
        placedReached.Should().BeTrue("order should be placed after reservation is created");

        using var paymentResponse = await paymentClient.PutAsync($"/payments/test/success/{orderId}", content: null);
        paymentResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var confirmedReached = await WaitForOrderStateInGuestHistoryAsync(
            gatewayClient,
            orderId,
            guestId,
            expectedStatusValue: 2,
            expectedStatusName: "Confirmed",
            shouldBePaid: true);
        confirmedReached.Should().BeTrue("payment success should confirm and mark the order as paid");

        var stockAfter = await WaitForCommittedStockAsync(orderClient, productId, variantId, expectedAvailable: stockBefore - 1);
        stockAfter.Should().Be(stockBefore - 1);
    }

    [Fact]
    [Trait("Category", TestCategories.E2EAspire)]
    public async Task ParallelGuestOrders_ShouldExerciseReservationSaga_AndAvoidOversell()
    {
        // Arrange
        var gatewayClient = _fixture.CreateClient("apigateway");
        var orderClient = _fixture.CreateClient("order");
        var guestId = Guid.CreateVersion7();
        var productId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var variantId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        const int quantityPerOrder = 5;


        // Act
        var stockBefore = await GetTotalAvailableStockAsync(orderClient, productId, variantId);
        stockBefore.Should().BeGreaterThan(0);
        var maxReservableOrders = stockBefore / quantityPerOrder;
        var requestCount = maxReservableOrders + 8;

        var orderIds = Enumerable.Range(0, requestCount).Select(_ => Guid.CreateVersion7()).ToArray();
        var createTasks = orderIds.Select(async orderId =>
        {
            var payload = new
            {
                id = orderId,
                customerName = "Parallel Guest",
                address = "District 1",
                phoneNumber = "0900000000",
                items = new[]
                {
                    new
                    {
                        productId,
                        variantId,
                        quantity = quantityPerOrder
                    }
                }
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, "/api/order/orders")
            {
                Content = JsonContent.Create(payload)
            };
            req.Headers.Add("X-Guest-Id", guestId.ToString());
            using var res = await gatewayClient.SendAsync(req);
            return (orderId, res.StatusCode);
        });

        var createResults = await Task.WhenAll(createTasks);
        var acceptedIds = createResults
            .Where(x => x.StatusCode == HttpStatusCode.Accepted)
            .Select(x => x.orderId)
            .ToHashSet();

        // Assert
        acceptedIds.Should().NotBeEmpty();

        var statuses = await WaitForGuestOrdersStatusAsync(gatewayClient, guestId, acceptedIds, expectedCount: acceptedIds.Count);
        statuses.Count.Should().Be(acceptedIds.Count);

        var placedOrConfirmed = statuses.Count(x => x.Value is 1 or 2);

        acceptedIds.Count.Should().BeGreaterThan(maxReservableOrders);
        placedOrConfirmed.Should().BeGreaterThan(0);

        var stockAfter = await GetTotalAvailableStockAsync(orderClient, productId, variantId);
        stockAfter.Should().BeGreaterThanOrEqualTo(0);
    }

    private static async Task<int> GetTotalAvailableStockAsync(HttpClient orderClient, Guid productId, Guid variantId)
    {
        var requestPayload = new
        {
            items = new[]
            {
                new
                {
                    productId,
                    variantId
                }
            }
        };

        using var response = await orderClient.PostAsJsonAsync("/order/internal-test/product-stocks", requestPayload);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        if (!TryGetPropertyIgnoreCase(doc.RootElement, "products", out var products) || products.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Expected products array in stock response.");
        }

        var totalAvailable = 0;
        foreach (var product in products.EnumerateArray())
        {
            if (!TryGetPropertyIgnoreCase(product, "stockInfos", out var stockInfos) || stockInfos.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var item in stockInfos.EnumerateArray())
            {
                if (TryGetPropertyIgnoreCase(item, "available", out var available) && available.TryGetInt32(out var value))
                {
                    totalAvailable += value;
                }
            }
        }

        return totalAvailable;
    }

    private static async Task<bool> WaitForOrderStateInGuestHistoryAsync(
        HttpClient gatewayClient,
        Guid orderId,
        Guid guestId,
        int expectedStatusValue,
        string expectedStatusName,
        bool shouldBePaid)
    {
        for (var i = 0; i < 60; i++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/order/orders/history");
            request.Headers.Add("X-Guest-Id", guestId.ToString());

            using var response = await gatewayClient.SendAsync(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var order in doc.RootElement.EnumerateArray())
                    {
                        if (!TryGetPropertyIgnoreCase(order, "id", out var idElement) ||
                            idElement.ValueKind != JsonValueKind.String ||
                            !Guid.TryParse(idElement.GetString(), out var currentOrderId) ||
                            currentOrderId != orderId)
                        {
                            continue;
                        }

                        if (TryGetPropertyIgnoreCase(order, "status", out var statusElement) &&
                            StatusMatches(statusElement, expectedStatusValue, expectedStatusName) &&
                            TryGetPropertyIgnoreCase(order, "paid", out var paidElement) &&
                            paidElement.ValueKind is JsonValueKind.True or JsonValueKind.False &&
                            paidElement.GetBoolean() == shouldBePaid)
                        {
                            return true;
                        }
                    }
                }
            }

            await Task.Delay(500);
        }

        return false;
    }

    private static bool StatusMatches(JsonElement statusElement, int expectedStatusValue, string expectedStatusName)
    {
        if (statusElement.ValueKind == JsonValueKind.Number && statusElement.TryGetInt32(out var numeric))
        {
            return numeric == expectedStatusValue;
        }

        if (statusElement.ValueKind == JsonValueKind.String)
        {
            var text = statusElement.GetString();
            if (int.TryParse(text, out var parsedNumeric))
            {
                return parsedNumeric == expectedStatusValue;
            }

            return string.Equals(text, expectedStatusName, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static async Task<int> WaitForCommittedStockAsync(
        HttpClient orderClient,
        Guid productId,
        Guid variantId,
        int expectedAvailable)
    {
        var last = -1;
        for (var i = 0; i < 30; i++)
        {
            last = await GetTotalAvailableStockAsync(orderClient, productId, variantId);
            if (last == expectedAvailable)
            {
                return last;
            }

            await Task.Delay(500);
        }

        return last;
    }

    private static async Task<Dictionary<Guid, int>> WaitForGuestOrdersStatusAsync(
        HttpClient gatewayClient,
        Guid guestId,
        HashSet<Guid> targetOrderIds,
        int expectedCount)
    {
        var statuses = new Dictionary<Guid, int>();

        for (var i = 0; i < 120 && statuses.Count < expectedCount; i++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/order/orders/history");
            request.Headers.Add("X-Guest-Id", guestId.ToString());

            using var response = await gatewayClient.SendAsync(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var order in doc.RootElement.EnumerateArray())
                    {
                        if (!TryGetPropertyIgnoreCase(order, "id", out var idElement) ||
                            idElement.ValueKind != JsonValueKind.String ||
                            !Guid.TryParse(idElement.GetString(), out var orderId) ||
                            !targetOrderIds.Contains(orderId))
                        {
                            continue;
                        }

                        if (!TryGetPropertyIgnoreCase(order, "status", out var statusElement))
                        {
                            continue;
                        }

                        if (TryGetStatusAsInt(statusElement, out var status))
                        {
                            statuses[orderId] = status;
                        }
                    }
                }
            }

            if (statuses.Count < expectedCount)
            {
                await Task.Delay(500);
            }
        }

        return statuses;
    }

    private static bool TryGetStatusAsInt(JsonElement statusElement, out int value)
    {
        if (statusElement.ValueKind == JsonValueKind.Number && statusElement.TryGetInt32(out value))
        {
            return true;
        }

        if (statusElement.ValueKind == JsonValueKind.String)
        {
            var text = statusElement.GetString();
            if (int.TryParse(text, out value))
            {
                return true;
            }

            if (string.Equals(text, "Initialized", StringComparison.OrdinalIgnoreCase)) { value = 0; return true; }
            if (string.Equals(text, "Placed", StringComparison.OrdinalIgnoreCase)) { value = 1; return true; }
            if (string.Equals(text, "Confirmed", StringComparison.OrdinalIgnoreCase)) { value = 2; return true; }
            if (string.Equals(text, "Shipped", StringComparison.OrdinalIgnoreCase)) { value = 3; return true; }
            if (string.Equals(text, "Delivered", StringComparison.OrdinalIgnoreCase)) { value = 4; return true; }
            if (string.Equals(text, "Cancelled", StringComparison.OrdinalIgnoreCase)) { value = 5; return true; }
        }

        value = default;
        return false;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
    {
        foreach (var prop in element.EnumerateObject())
        {
            if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}

