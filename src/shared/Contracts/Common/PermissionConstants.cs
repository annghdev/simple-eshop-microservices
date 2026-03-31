namespace Contracts.Common;

public static class PermissionConstants
{
    public static class Catalog
    {
        public const string Read = "catalog.read";
        public const string Write = "catalog.write";
        public const string Publish = "catalog.publish";
    }

    public static class Inventory
    {
        public const string Read = "inventory.read";
        public const string Adjust = "inventory.adjust";
        public const string Transfer = "inventory.transfer";
        public const string Receive = "inventory.receive";
    }

    public static class Order
    {
        public const string ReadAll = "order.read.all";
        public const string ReadOwn = "order.read.own";
        public const string Create = "order.create";
        public const string CancelOwn = "order.cancel.own";
        public const string Confirm = "order.confirm";
    }

    public static class Payment
    {
        public const string Read = "payment.read";
        public const string Process = "payment.process";
        public const string Refund = "payment.refund";
    }

    public static class Shipping
    {
        public const string Read = "shipping.read";
        public const string UpdateStatus = "shipping.update_status";
    }

    public static readonly string[] All =
    [
        Catalog.Read,
        Catalog.Write,
        Catalog.Publish,
        Inventory.Read,
        Inventory.Adjust,
        Inventory.Transfer,
        Inventory.Receive,
        Order.ReadAll,
        Order.ReadOwn,
        Order.Create,
        Order.CancelOwn,
        Order.Confirm,
        Payment.Read,
        Payment.Process,
        Payment.Refund,
        Shipping.Read,
        Shipping.UpdateStatus
    ];
}
