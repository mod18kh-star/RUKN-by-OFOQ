using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OFOQ.Market.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSmartContentPages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "hero_image_url",
                table: "content_pages",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "page_kind",
                table: "content_pages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "show_average_rating",
                table: "content_pages",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "show_completed_order_count",
                table: "content_pages",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "show_country_count",
                table: "content_pages",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "show_customer_count",
                table: "content_pages",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "show_review_count",
                table: "content_pages",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "show_units_sold",
                table: "content_pages",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "hero_image_url",
                table: "content_pages");

            migrationBuilder.DropColumn(
                name: "page_kind",
                table: "content_pages");

            migrationBuilder.DropColumn(
                name: "show_average_rating",
                table: "content_pages");

            migrationBuilder.DropColumn(
                name: "show_completed_order_count",
                table: "content_pages");

            migrationBuilder.DropColumn(
                name: "show_country_count",
                table: "content_pages");

            migrationBuilder.DropColumn(
                name: "show_customer_count",
                table: "content_pages");

            migrationBuilder.DropColumn(
                name: "show_review_count",
                table: "content_pages");

            migrationBuilder.DropColumn(
                name: "show_units_sold",
                table: "content_pages");
        }
    }
}
