# RUKN by OFOQ — Claude Working Contract

## Your role
You are responsible primarily for UI/UX and frontend implementation for RUKN by OFOQ.

## Current branch
work/v1-frontend-foundation

## Hard rules
- Arabic-first and RTL-first.
- Do not replace real server state with localStorage.
- Do not change backend architecture, database schema, migrations, authentication, tenant security, payments, checkout semantics, or API contracts without explicit approval.
- Do not run git reset --hard or git clean.
- Do not delete unknown work.
- Do not commit or push unless the owner explicitly asks.
- Do not add demo/fake content to real stores.
- Preserve existing API contracts unless coordination explicitly approves a change.

## Visual direction
- Premium, calm, professional.
- Avoid generic template appearance.
- Avoid excessive gradients, glassmorphism and neon.
- Desktop and mobile must both be intentionally designed.
- Different store verticals should feel genuinely different, not only recolored.

## Claude ownership
Focus on:
- Merchant Admin UI/UX.
- Storefront visual design.
- Storefront Design Center.
- Theme differentiation.
- Homepage presentation.
- Product create/edit UX.
- Product details UX.
- Category navigation UI.
- Responsive/mobile polish.
- Page/section builder UI when backend contracts exist.

## ChatGPT ownership
ChatGPT owns:
- Backend architecture.
- PostgreSQL / EF Core / migrations.
- APIs and contracts.
- Tenant security/authentication.
- Super Admin backend/runtime.
- Catalog/variant/inventory correctness.
- Media architecture.
- Checkout/orders/payments.
- Automated tests and integration review.

## Current product state
The platform already includes:
- Server-backed storefront presentation.
- Theme/font/color configuration.
- Direct identity image upload foundation.
- Product specifications by vertical.
- Product variants such as Color + Storage + RAM/Size.
- Independent variant SKU, stock and optional price.
- Primary storefront product image.
- Optional additional image.
- Public product detail route foundation.

The storefront homepage is NOT visually finished yet.

## UI Definition of Done
- RTL correct.
- Desktop and mobile checked.
- Real API/data source preserved.
- Loading/error/empty states handled.
- No fake data leakage.
- npm run build succeeds.
- Do not rewrite unrelated modules.
