using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OFOQ.Market.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantStorefrontPresentation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tenant_storefront_presentations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    logo_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    cover_image_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    announcement = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    primary_color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    accent_color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    theme_preset_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false, defaultValue: "editorial"),
                    font_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false, defaultValue: "plex"),
                    show_categories_on_home = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    show_products_on_home = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    category_section_title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false, defaultValue: "تصفح الأقسام"),
                    product_section_title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false, defaultValue: "منتجات المتجر"),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_storefront_presentations", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_storefront_presentations_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_tenant_storefront_presentations_tenant",
                table: "tenant_storefront_presentations",
                column: "tenant_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tenant_storefront_presentations");
        }
    }
}
