using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OFOQ.Market.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStructuredProductOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "ak_catalog_product_variants_tenant_product_id",
                table: "catalog_product_variants",
                columns: new[] { "tenant_id", "product_id", "id" });

            migrationBuilder.CreateTable(
                name: "catalog_product_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_product_options", x => x.id);
                    table.UniqueConstraint("ak_catalog_product_options_tenant_product_id", x => new { x.tenant_id, x.product_id, x.id });
                    table.ForeignKey(
                        name: "fk_catalog_product_options_products",
                        columns: x => new { x.tenant_id, x.product_id },
                        principalTable: "catalog_products",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_catalog_product_options_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "catalog_product_option_values",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_option_id = table.Column<Guid>(type: "uuid", nullable: false),
                    value = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    normalized_value = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_product_option_values", x => x.id);
                    table.UniqueConstraint("ak_catalog_product_option_values_scope_id", x => new { x.tenant_id, x.product_id, x.product_option_id, x.id });
                    table.ForeignKey(
                        name: "fk_catalog_product_option_values_options",
                        columns: x => new { x.tenant_id, x.product_id, x.product_option_id },
                        principalTable: "catalog_product_options",
                        principalColumns: new[] { "tenant_id", "product_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_catalog_product_option_values_products",
                        columns: x => new { x.tenant_id, x.product_id },
                        principalTable: "catalog_products",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_catalog_product_option_values_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "catalog_product_variant_option_values",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_option_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_option_value_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_product_variant_option_values", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_variant_option_values_options",
                        columns: x => new { x.tenant_id, x.product_id, x.product_option_id },
                        principalTable: "catalog_product_options",
                        principalColumns: new[] { "tenant_id", "product_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_catalog_variant_option_values_products",
                        columns: x => new { x.tenant_id, x.product_id },
                        principalTable: "catalog_products",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_catalog_variant_option_values_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_catalog_variant_option_values_values",
                        columns: x => new { x.tenant_id, x.product_id, x.product_option_id, x.product_option_value_id },
                        principalTable: "catalog_product_option_values",
                        principalColumns: new[] { "tenant_id", "product_id", "product_option_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_catalog_variant_option_values_variants",
                        columns: x => new { x.tenant_id, x.product_id, x.product_variant_id },
                        principalTable: "catalog_product_variants",
                        principalColumns: new[] { "tenant_id", "product_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_product_option_values_option_sort",
                table: "catalog_product_option_values",
                columns: new[] { "tenant_id", "product_option_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ux_catalog_product_option_values_option_value_active",
                table: "catalog_product_option_values",
                columns: new[] { "tenant_id", "product_id", "product_option_id", "normalized_value" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_product_options_product_sort",
                table: "catalog_product_options",
                columns: new[] { "tenant_id", "product_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ux_catalog_product_options_product_name_active",
                table: "catalog_product_options",
                columns: new[] { "tenant_id", "product_id", "normalized_name" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_product_variant_option_values_tenant_id_product_id_~",
                table: "catalog_product_variant_option_values",
                columns: new[] { "tenant_id", "product_id", "product_option_id", "product_option_value_id" });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_variant_option_values_variant",
                table: "catalog_product_variant_option_values",
                columns: new[] { "tenant_id", "product_id", "product_variant_id" });

            migrationBuilder.CreateIndex(
                name: "ux_catalog_variant_option_single_value_active",
                table: "catalog_product_variant_option_values",
                columns: new[] { "tenant_id", "product_id", "product_variant_id", "product_option_id" },
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_product_variant_option_values");

            migrationBuilder.DropTable(
                name: "catalog_product_option_values");

            migrationBuilder.DropTable(
                name: "catalog_product_options");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_catalog_product_variants_tenant_product_id",
                table: "catalog_product_variants");
        }
    }
}
