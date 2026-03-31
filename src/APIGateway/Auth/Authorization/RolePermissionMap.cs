using Contracts.Common;

namespace APIGateway.Auth;

public static class RolePermissionMap
{
    public static readonly IReadOnlyDictionary<string, string[]> Map = new Dictionary<string, string[]>
    {
        [AuthRoles.Customer] =
        [
            PermissionConstants.Order.ReadOwn,
            PermissionConstants.Order.Create,
            PermissionConstants.Order.CancelOwn
        ],
        [AuthRoles.WarehouseManager] =
        [
            PermissionConstants.Inventory.Read,
            PermissionConstants.Inventory.Adjust,
            PermissionConstants.Inventory.Transfer,
            PermissionConstants.Inventory.Receive
        ],
        [AuthRoles.OrderManager] =
        [
            PermissionConstants.Order.ReadAll,
            PermissionConstants.Order.Confirm
        ],
        [AuthRoles.PaymentManager] =
        [
            PermissionConstants.Payment.Read,
            PermissionConstants.Payment.Process,
            PermissionConstants.Payment.Refund
        ],
        [AuthRoles.ShippingManager] =
        [
            PermissionConstants.Shipping.Read,
            PermissionConstants.Shipping.UpdateStatus
        ],
        [AuthRoles.Admin] = PermissionConstants.All
    };
}
