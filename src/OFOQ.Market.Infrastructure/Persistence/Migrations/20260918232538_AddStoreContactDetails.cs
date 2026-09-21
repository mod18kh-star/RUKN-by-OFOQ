using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OFOQ.Market.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreContactDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "google_maps_url",
                table: "tenant_store_profiles",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "landline_phone",
                table: "tenant_store_profiles",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "physical_address",
                table: "tenant_store_profiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "secondary_phone",
                table: "tenant_store_profiles",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "show_commercial_registration",
                table: "tenant_store_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "show_customer_service_phone",
                table: "tenant_store_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "show_landline_phone",
                table: "tenant_store_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "show_physical_address",
                table: "tenant_store_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "show_secondary_phone",
                table: "tenant_store_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "show_website",
                table: "tenant_store_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "show_whatsapp",
                table: "tenant_store_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "google_maps_url",
                table: "tenant_store_profiles");

            migrationBuilder.DropColumn(
                name: "landline_phone",
                table: "tenant_store_profiles");

            migrationBuilder.DropColumn(
                name: "physical_address",
                table: "tenant_store_profiles");

            migrationBuilder.DropColumn(
                name: "secondary_phone",
                table: "tenant_store_profiles");

            migrationBuilder.DropColumn(
                name: "show_commercial_registration",
                table: "tenant_store_profiles");

            migrationBuilder.DropColumn(
                name: "show_customer_service_phone",
                table: "tenant_store_profiles");

            migrationBuilder.DropColumn(
                name: "show_landline_phone",
                table: "tenant_store_profiles");

            migrationBuilder.DropColumn(
                name: "show_physical_address",
                table: "tenant_store_profiles");

            migrationBuilder.DropColumn(
                name: "show_secondary_phone",
                table: "tenant_store_profiles");

            migrationBuilder.DropColumn(
                name: "show_website",
                table: "tenant_store_profiles");

            migrationBuilder.DropColumn(
                name: "show_whatsapp",
                table: "tenant_store_profiles");
        }
    }
}
