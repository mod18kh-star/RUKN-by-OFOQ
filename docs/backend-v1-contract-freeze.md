# RUKN Backend V1 Contract Freeze

Status: **FROZEN after final verification**.

This document defines the V1 backend boundary. After the freeze, backend changes are limited to defect, security, migration, or compatibility fixes unless a new product phase explicitly reopens scope.

## Security invariants

- Tenant Back Office authorization continues to require both `amr=pwd` and `amr=mfa` plus live user, tenant, and membership checks.
- Google sign-in never impersonates password authentication. Google-backed sessions emit `amr=google`; MFA adds `amr=mfa` but does not add `amr=pwd`.
- Google sign-in requires a valid Google OIDC ID token, configured audience, verified email, and an exact nonce match.
- Google sign-in links only to an existing local account. It does not create a passwordless merchant account that could bypass the Back Office assurance contract.
- Interactive access tokens carry a session id and live session/account state is checked on authenticated requests.
- Refresh-token rotation, MFA challenge consumption, trusted-device expiry/revocation, and privileged authorization rules remain enforced.

## Frozen V1 areas

Identity and sessions; MFA and recovery codes; email verification; Google sign-in/linking; trusted devices; tenant membership and Back Office authorization; platform administration; merchant verification; tenant store profile/readiness; catalog/categories/products/variants/content/recommendations; carts/checkout/orders/payments; shipping/fulfillment; coupons; returns; CMS/navigation; reviews/trust; notifications/email outbox; merchant operations dashboard and abandoned carts.

## Change policy

Any post-freeze API contract change must be deliberate and reviewed for backward compatibility. Database model changes require an explicit migration. Security-sensitive changes must preserve tenant isolation and authentication-assurance invariants.
