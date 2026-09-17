using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Domain.Commerce.Configuration;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Platform;
using OFOQ.Market.Domain.Tenancy;
using OFOQ.Market.Infrastructure.Persistence;

namespace OFOQ.Market.Api.Endpoints.Tenancy;

public static class MerchantPlatformRequestEndpoints
{
    public static IEndpointRouteBuilder MapMerchantPlatformRequestEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group =
            endpoints
                .MapGroup("/api/tenants/{tenantId:guid}/requests")
                .WithTags("Merchant Platform Requests")
                .RequireAuthorization();

        group.MapGet("/mine", GetMineAsync);
        group.MapPost("/registration", SubmitRegistrationAsync);
        group.MapPost("/plan-change", SubmitPlanChangeAsync);
        group.MapPost("/store-identity", SubmitStoreIdentityChangeAsync);
        group.MapPost("/owner-email", SubmitOwnerEmailChangeAsync);

        return endpoints;
    }

    private static async Task<IResult> GetMineAsync(
        Guid tenantId,
        HttpContext httpContext,
        MarketDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var context =
            await GetOwnerContextAsync(
                tenantId,
                httpContext,
                dbContext,
                cancellationToken);

        if (context.Error is not null)
        {
            return context.Error;
        }

        var requests =
            await dbContext
                .Set<PlatformRequest>()
                .AsNoTracking()
                .Where(item => item.TenantId == context.Tenant!.Id)
                .OrderByDescending(item => item.RequestedAtUtc)
                .Take(50)
                .Select(
                    item =>
                        new MerchantRequestResponse(
                            item.Id.Value,
                            item.Type.ToString(),
                            item.Status.ToString(),
                            item.Summary,
                            item.PayloadJson,
                            item.RequestedAtUtc,
                            item.ReviewedAtUtc,
                            item.ReviewReason))
                .ToArrayAsync(cancellationToken);

        return Results.Ok(requests);
    }

    private static async Task<IResult> SubmitRegistrationAsync(
        Guid tenantId,
        SubmitRegistrationRequest request,
        HttpContext httpContext,
        MarketDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var context =
            await GetOwnerContextAsync(
                tenantId,
                httpContext,
                dbContext,
                cancellationToken);

        if (context.Error is not null)
        {
            return context.Error;
        }

        var tenant = context.Tenant!;

        if (tenant.Status != TenantStatus.Draft)
        {
            return Results.Conflict(
                new
                {
                    code = "store_registration_not_draft",
                    message = "Only Draft stores can submit a registration request."
                });
        }

        if (!TryNormalizePlan(
                request.PlanCode,
                out var plan,
                out var planError))
        {
            return planError!;
        }

        if (!TryParseBillingCycle(
                request.BillingCycle,
                out var billingCycle))
        {
            return ValidationError(
                "billing_cycle_invalid",
                "Billing cycle must be Monthly or Annual.");
        }

        var payload =
            JsonSerializer.Serialize(
                new RegistrationPayload(
                    plan.Code,
                    billingCycle.ToString()));

        return await CreateOrReviseAsync(
            tenant.Id,
            context.ActorUserId!.Value,
            PlatformRequestType.StoreRegistration,
            $"طلب اعتماد المتجر على باقة {plan.Name}",
            payload,
            allowPendingIdempotency: true,
            dbContext,
            cancellationToken);
    }

    private static async Task<IResult> SubmitPlanChangeAsync(
        Guid tenantId,
        SubmitPlanChangeRequest request,
        HttpContext httpContext,
        MarketDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var context =
            await GetOwnerContextAsync(
                tenantId,
                httpContext,
                dbContext,
                cancellationToken);

        if (context.Error is not null)
        {
            return context.Error;
        }

        if (!TryNormalizePlan(
                request.PlanCode,
                out var plan,
                out var planError))
        {
            return planError!;
        }

        if (!TryParseBillingCycle(
                request.BillingCycle,
                out var billingCycle))
        {
            return ValidationError(
                "billing_cycle_invalid",
                "Billing cycle must be Monthly or Annual.");
        }

        if (!TryNormalizeMerchantReason(
                request.Reason,
                out var reason,
                out var reasonError))
        {
            return reasonError!;
        }

        var payload =
            JsonSerializer.Serialize(
                new PlanChangePayload(
                    plan.Code,
                    billingCycle.ToString(),
                    reason));

        return await CreateOrReviseAsync(
            context.Tenant!.Id,
            context.ActorUserId!.Value,
            PlatformRequestType.PlanChange,
            $"طلب تغيير الباقة إلى {plan.Name}",
            payload,
            allowPendingIdempotency: false,
            dbContext,
            cancellationToken);
    }

    private static async Task<IResult> SubmitStoreIdentityChangeAsync(
        Guid tenantId,
        SubmitStoreIdentityChangeRequest request,
        HttpContext httpContext,
        MarketDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var context =
            await GetOwnerContextAsync(
                tenantId,
                httpContext,
                dbContext,
                cancellationToken);

        if (context.Error is not null)
        {
            return context.Error;
        }

        var name = request.Name?.Trim();
        var slug = request.Slug?.Trim();
        var verticalCode = request.VerticalCode?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(name) &&
            string.IsNullOrWhiteSpace(slug) &&
            string.IsNullOrWhiteSpace(verticalCode))
        {
            return ValidationError(
                "store_change_empty",
                "At least one store field must be changed.");
        }

        if (!string.IsNullOrWhiteSpace(name) &&
            name.Length is < 2 or > 160)
        {
            return ValidationError(
                "store_name_invalid",
                "Store name must be between 2 and 160 characters.");
        }

        if (!string.IsNullOrWhiteSpace(slug))
        {
            try
            {
                slug = TenantSlug.Create(slug).Value;
            }
            catch (ArgumentException exception)
            {
                return ValidationError(
                    "store_slug_invalid",
                    exception.Message);
            }
        }

        if (!string.IsNullOrWhiteSpace(verticalCode) &&
            !TryGetVerticalByCode(verticalCode, out _))
        {
            return ValidationError(
                "commerce_vertical_invalid",
                "A supported commerce vertical is required.");
        }

        if (!TryNormalizeMerchantReason(
                request.Reason,
                out var reason,
                out var reasonError))
        {
            return reasonError!;
        }

        var payload =
            JsonSerializer.Serialize(
                new StoreIdentityChangePayload(
                    name,
                    slug,
                    verticalCode,
                    reason));

        return await CreateOrReviseAsync(
            context.Tenant!.Id,
            context.ActorUserId!.Value,
            PlatformRequestType.StoreIdentityChange,
            "طلب تغيير بيانات المتجر",
            payload,
            allowPendingIdempotency: false,
            dbContext,
            cancellationToken);
    }

    private static async Task<IResult> SubmitOwnerEmailChangeAsync(
        Guid tenantId,
        SubmitOwnerEmailChangeRequest request,
        HttpContext httpContext,
        MarketDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var context =
            await GetOwnerContextAsync(
                tenantId,
                httpContext,
                dbContext,
                cancellationToken);

        if (context.Error is not null)
        {
            return context.Error;
        }

        string email;

        try
        {
            email = EmailAddress.Create(request.Email).Value;
        }
        catch (ArgumentException exception)
        {
            return ValidationError(
                "owner_email_invalid",
                exception.Message);
        }

        if (!TryNormalizeMerchantReason(
                request.Reason,
                out var reason,
                out var reasonError))
        {
            return reasonError!;
        }

        var payload =
            JsonSerializer.Serialize(
                new OwnerEmailChangePayload(
                    email,
                    reason));

        return await CreateOrReviseAsync(
            context.Tenant!.Id,
            context.ActorUserId!.Value,
            PlatformRequestType.OwnerEmailChange,
            $"طلب تغيير بريد المالك إلى {email}",
            payload,
            allowPendingIdempotency: false,
            dbContext,
            cancellationToken);
    }

    private static async Task<IResult> CreateOrReviseAsync(
        TenantId tenantId,
        UserId actorUserId,
        PlatformRequestType type,
        string summary,
        string payloadJson,
        bool allowPendingIdempotency,
        MarketDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var existing =
            await dbContext
                .Set<PlatformRequest>()
                .Where(
                    item =>
                        item.TenantId == tenantId &&
                        item.Type == type &&
                        (item.Status == PlatformRequestStatus.Pending ||
                         item.Status == PlatformRequestStatus.MoreInfoRequested))
                .OrderByDescending(item => item.RequestedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;

        if (existing is not null)
        {
            if (existing.Status == PlatformRequestStatus.Pending)
            {
                if (allowPendingIdempotency &&
                    existing.PayloadJson == payloadJson)
                {
                    return Results.Ok(Map(existing));
                }

                return Results.Conflict(
                    new
                    {
                        code = "platform_request_already_pending",
                        message = "A request of this type is already waiting for review."
                    });
            }

            existing.Revise(
                summary,
                payloadJson,
                now);

            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Ok(Map(existing));
        }

        var platformRequest =
            PlatformRequest.Create(
                tenantId,
                actorUserId,
                type,
                summary,
                payloadJson,
                now);

        dbContext.Add(platformRequest);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/api/tenants/{tenantId.Value}/requests/{platformRequest.Id.Value}",
            Map(platformRequest));
    }

    private static MerchantRequestResponse Map(
        PlatformRequest item)
    {
        return new MerchantRequestResponse(
            item.Id.Value,
            item.Type.ToString(),
            item.Status.ToString(),
            item.Summary,
            item.PayloadJson,
            item.RequestedAtUtc,
            item.ReviewedAtUtc,
            item.ReviewReason);
    }

    private static async Task<OwnerContext> GetOwnerContextAsync(
        Guid tenantId,
        HttpContext httpContext,
        MarketDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var actor = GetActorUserId(httpContext);

        if (!actor.HasValue)
        {
            return new OwnerContext(null, null, Results.Unauthorized());
        }

        if (tenantId == Guid.Empty)
        {
            return new OwnerContext(
                null,
                null,
                ValidationError(
                    "tenant_id_invalid",
                    "Tenant ID must be a valid non-empty GUID."));
        }

        var tenantKey = TenantId.From(tenantId);

        var tenant =
            await dbContext
                .Set<Tenant>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.Id == tenantKey && !item.IsDeleted,
                    cancellationToken);

        if (tenant is null)
        {
            return new OwnerContext(
                null,
                null,
                Results.NotFound(
                    new
                    {
                        code = "tenant_not_found",
                        message = "Tenant was not found."
                    }));
        }

        var membership =
            await dbContext
                .Set<TenantMembership>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item =>
                        item.TenantId == tenantKey &&
                        item.UserId == actor.Value &&
                        !item.IsDeleted,
                    cancellationToken);

        if (membership is null || membership.Role != TenantRole.Owner)
        {
            return new OwnerContext(null, null, Results.Forbid());
        }

        return new OwnerContext(actor, tenant, null);
    }

    private static UserId? GetActorUserId(HttpContext httpContext)
    {
        var subject =
            httpContext.User
                .FindFirst(JwtRegisteredClaimNames.Sub)?
                .Value;

        return Guid.TryParse(subject, out var userGuid) &&
               userGuid != Guid.Empty
            ? UserId.From(userGuid)
            : null;
    }

    private static bool TryNormalizePlan(
        string? value,
        out PlatformPlanDefinition plan,
        out IResult? error)
    {
        error = null;

        if (PlatformPlanCatalog.TryGet(value, out var foundPlan))
        {
            plan = foundPlan;
            return true;
        }

        plan = null!;

        error = ValidationError(
            "plan_code_invalid",
            "A supported plan code is required.");

        return false;
    }

    private static bool TryParseBillingCycle(
        string? value,
        out StoreBillingCycle billingCycle)
    {
        return Enum.TryParse(
                   value?.Trim(),
                   ignoreCase: true,
                   out billingCycle) &&
               Enum.IsDefined(billingCycle);
    }

    private static bool TryGetVerticalByCode(
        string code,
        out CommerceVerticalDefinition definition)
    {
        definition =
            CommerceVerticalCatalog.All
                .FirstOrDefault(
                    item =>
                        string.Equals(
                            item.Code,
                            code,
                            StringComparison.OrdinalIgnoreCase))!;

        return definition is not null;
    }

    private static bool TryNormalizeMerchantReason(
        string? value,
        out string normalized,
        out IResult? error)
    {
        normalized = value?.Trim() ?? string.Empty;
        error = null;

        if (normalized.Length is < 3 or > 500)
        {
            error = ValidationError(
                "request_reason_invalid",
                "Request reason must be between 3 and 500 characters.");

            return false;
        }

        return true;
    }

    private static IResult ValidationError(
        string code,
        string message)
    {
        return Results.BadRequest(new { code, message });
    }

    private sealed record OwnerContext(
        UserId? ActorUserId,
        Tenant? Tenant,
        IResult? Error);

    private sealed record SubmitRegistrationRequest(
        string PlanCode,
        string BillingCycle);

    private sealed record SubmitPlanChangeRequest(
        string PlanCode,
        string BillingCycle,
        string? Reason);

    private sealed record SubmitStoreIdentityChangeRequest(
        string? Name,
        string? Slug,
        string? VerticalCode,
        string? Reason);

    private sealed record SubmitOwnerEmailChangeRequest(
        string Email,
        string? Reason);

    private sealed record RegistrationPayload(
        string PlanCode,
        string BillingCycle);

    private sealed record PlanChangePayload(
        string PlanCode,
        string BillingCycle,
        string Reason);

    private sealed record StoreIdentityChangePayload(
        string? Name,
        string? Slug,
        string? VerticalCode,
        string Reason);

    private sealed record OwnerEmailChangePayload(
        string Email,
        string Reason);

    private sealed record MerchantRequestResponse(
        Guid RequestId,
        string Type,
        string Status,
        string Summary,
        string PayloadJson,
        DateTimeOffset RequestedAtUtc,
        DateTimeOffset? ReviewedAtUtc,
        string? ReviewReason);
}
