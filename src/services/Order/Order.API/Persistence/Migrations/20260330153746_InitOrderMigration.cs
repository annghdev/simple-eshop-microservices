using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Order.API.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitOrderMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", maxLength: 50, nullable: true),
                    GuestId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerName = table.Column<string>(type: "text", nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    Address = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Promotions = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    CouponCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    CouponName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    OriginalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    CouponDiscount = table.Column<decimal>(type: "numeric", nullable: false),
                    CustomerNote = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    ShopNote = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    SystemNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ConfirmBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Paid = table.Column<bool>(type: "boolean", nullable: false),
                    PaymentMethod = table.Column<int>(type: "integer", nullable: false),
                    ReservationStatus = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastUpdated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ItemName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalQuantity = table.Column<int>(type: "integer", nullable: false),
                    PromotionDiscount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Actor = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StatusBefore = table.Column<int>(type: "integer", nullable: false),
                    StatusAfter = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderLogs_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderLogs_Orders_OrderId1",
                        column: x => x.OrderId1,
                        principalTable: "Orders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FreeItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ItemName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FreeItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FreeItems_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemReservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemReservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemReservations_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FreeItems_OrderItemId",
                table: "FreeItems",
                column: "OrderItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemReservations_OrderItemId",
                table: "ItemReservations",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                table: "OrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderLogs_OrderId",
                table: "OrderLogs",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderLogs_OrderId1",
                table: "OrderLogs",
                column: "OrderId1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FreeItems");

            migrationBuilder.DropTable(
                name: "ItemReservations");

            migrationBuilder.DropTable(
                name: "OrderLogs");

            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "Orders");
        }
    }
}
