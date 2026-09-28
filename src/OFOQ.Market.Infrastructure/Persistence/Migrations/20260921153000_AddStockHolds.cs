using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OFOQ.Market.Infrastructure.Persistence;

namespace OFOQ.Market.Infrastructure.Persistence.Migrations;

// The reservation ledger is intentionally managed with SQL, not as a mapped EF entity.
// Never back-fill historical pending orders; they already deducted stock at checkout.
[DbContext(typeof(MarketDbContext))]
[Migration("20260921153000_AddStockHolds")]
public sealed class AddStockHolds : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE commerce_stock_hold_orders (
                tenant_id uuid NOT NULL,
                order_id uuid NOT NULL,
                created_at_utc timestamptz NOT NULL,
                CONSTRAINT pk_commerce_stock_hold_orders PRIMARY KEY (tenant_id, order_id),
                CONSTRAINT fk_stock_hold_orders_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id),
                CONSTRAINT fk_stock_hold_orders_order FOREIGN KEY (tenant_id, order_id)
                    REFERENCES commerce_orders(tenant_id, id) ON DELETE RESTRICT
            );
            CREATE TABLE commerce_stock_holds (
                id uuid PRIMARY KEY,
                tenant_id uuid NOT NULL,
                variant_id uuid NOT NULL,
                cart_id uuid NULL,
                order_id uuid NULL,
                quantity integer NOT NULL CHECK (quantity > 0),
                state varchar(12) NOT NULL CHECK (state IN ('Cart','Order','Review','Captured','Released')),
                expires_at_utc timestamptz NULL,
                created_at_utc timestamptz NOT NULL,
                updated_at_utc timestamptz NOT NULL,
                CONSTRAINT ck_stock_hold_owner CHECK (
                    (state = 'Cart' AND cart_id IS NOT NULL AND order_id IS NULL AND expires_at_utc IS NOT NULL) OR
                    (state IN ('Order','Review') AND cart_id IS NULL AND order_id IS NOT NULL) OR
                    (state IN ('Captured','Released') AND cart_id IS NULL)
                ),
                CONSTRAINT fk_stock_hold_variant FOREIGN KEY (variant_id)
                    REFERENCES catalog_product_variants(id) ON DELETE RESTRICT,
                CONSTRAINT fk_stock_hold_tenant FOREIGN KEY (tenant_id)
                    REFERENCES tenants(id) ON DELETE RESTRICT
            );
            CREATE UNIQUE INDEX ux_stock_hold_cart_variant
                ON commerce_stock_holds(tenant_id, cart_id, variant_id) WHERE cart_id IS NOT NULL;
            CREATE UNIQUE INDEX ux_stock_hold_order_variant
                ON commerce_stock_holds(tenant_id, order_id, variant_id) WHERE order_id IS NOT NULL;
            CREATE INDEX ix_stock_hold_availability
                ON commerce_stock_holds(tenant_id, variant_id, state, expires_at_utc);
            CREATE INDEX ix_stock_hold_order
                ON commerce_stock_holds(tenant_id, order_id);
        """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Deliberately disallow silent loss of reservations during rollback.
        migrationBuilder.Sql("""
            DO $$ BEGIN
              IF EXISTS (SELECT 1 FROM commerce_stock_holds WHERE state IN ('Cart','Order','Review')) THEN
                RAISE EXCEPTION 'Active stock holds exist: explicit reconciliation is required before rollback';
              END IF;
            END $$;
            DROP TABLE commerce_stock_holds;
            DROP TABLE commerce_stock_hold_orders;
        """);
    }
}
