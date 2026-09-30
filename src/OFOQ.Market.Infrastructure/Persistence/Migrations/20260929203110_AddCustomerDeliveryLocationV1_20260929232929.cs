using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OFOQ.Market.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerDeliveryLocationV1_20260929232929 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "accuracy_meters",
                table: "commerce_customer_addresses",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "delivery_notes",
                table: "commerce_customer_addresses",
                type: "character varying(240)",
                maxLength: 240,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "latitude",
                table: "commerce_customer_addresses",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "longitude",
                table: "commerce_customer_addresses",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "map_url",
                table: "commerce_customer_addresses",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "accuracy_meters",
                table: "commerce_customer_addresses");

            migrationBuilder.DropColumn(
                name: "delivery_notes",
                table: "commerce_customer_addresses");

            migrationBuilder.DropColumn(
                name: "latitude",
                table: "commerce_customer_addresses");

            migrationBuilder.DropColumn(
                name: "longitude",
                table: "commerce_customer_addresses");

            migrationBuilder.DropColumn(
                name: "map_url",
                table: "commerce_customer_addresses");
        }
    }
}
