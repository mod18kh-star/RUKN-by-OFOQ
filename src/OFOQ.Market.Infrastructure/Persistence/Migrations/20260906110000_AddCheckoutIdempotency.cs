using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OFOQ.Market.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckoutIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "checkout_idempotency_key",
                table: "commerce_orders",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_commerce_orders_checkout_idempotency",
                table: "commerce_orders",
                columns: new[]
                {
                    "tenant_id",
                    "customer_user_id",
                    "checkout_idempotency_key"
                },
                unique: true,
                filter: "checkout_idempotency_key IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_commerce_orders_checkout_idempotency",
                table: "commerce_orders");

            migrationBuilder.DropColumn(
                name: "checkout_idempotency_key",
                table: "commerce_orders");
        }
    }
}
