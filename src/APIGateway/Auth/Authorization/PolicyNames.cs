using Contracts.Common;

namespace APIGateway.Auth;

public static class PolicyNames
{
    public const string CanReadCatalog = "CanReadCatalog";

    public const string CanCreateOrder = "CanCreateOrder";
    public const string CanReadOwnOrder = "CanReadOwnOrder";
    public const string CanReadAllOrders = "CanReadAllOrders";
    public const string CanCancelOwnOrder = "CanCancelOwnOrder";
    public const string CanConfirmOrder = "CanConfirmOrder";

    public const string CanReadInventory = "CanReadInventory";
    public const string CanAdjustInventory = "CanAdjustInventory";
    public const string CanTransferInventory = "CanTransferInventory";
    public const string CanReceiveInventory = "CanReceiveInventory";

    public const string IsOrderOwner = "IsOrderOwner";
    public const string CanModifyOwnedWarehouse = "CanModifyOwnedWarehouse";

    public static readonly IReadOnlyDictionary<string, string> PermissionToPolicy = new Dictionary<string, string>
    {
        [PermissionConstants.Catalog.Read] = CanReadCatalog,
        [PermissionConstants.Order.Create] = CanCreateOrder,
        [PermissionConstants.Order.ReadOwn] = CanReadOwnOrder,
        [PermissionConstants.Order.ReadAll] = CanReadAllOrders,
        [PermissionConstants.Order.CancelOwn] = CanCancelOwnOrder,
        [PermissionConstants.Order.Confirm] = CanConfirmOrder,
        [PermissionConstants.Inventory.Read] = CanReadInventory,
        [PermissionConstants.Inventory.Adjust] = CanAdjustInventory,
        [PermissionConstants.Inventory.Transfer] = CanTransferInventory,
        [PermissionConstants.Inventory.Receive] = CanReceiveInventory
    };

    public static string GetPolicyByPermission(string permission)
        => PermissionToPolicy.TryGetValue(permission, out var policyName)
            ? policyName
            : $"Can{permission.Replace(".", string.Empty, StringComparison.OrdinalIgnoreCase)}";
}
