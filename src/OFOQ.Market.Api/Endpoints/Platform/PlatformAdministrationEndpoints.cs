using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Api.Security.Authorization;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Configuration;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Platform;
using OFOQ.Market.Domain.Tenancy;
using OFOQ.Market.Infrastructure.Persistence;

namespace OFOQ.Market.Api.Endpoints.Platform;

public static class PlatformAdministrationEndpoints
{
    public static IEndpointRouteBuilder MapPlatformAdministrationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapPost(
                "/api/platform/bootstrap/first-admin",
                BootstrapFirstAdministratorAsync)
            .WithTags("Platform Administration")
            .RequireAuthorization();

        var group =
            endpoints
                .MapGroup("/api/platform")
                .WithTags("Platform Administration")
                .RequireAuthorization(
                    PlatformAdministrationAuthorization.PolicyName);

        group.MapGet("/me", GetMeAsync);

        group.MapGet(
            "/administrators",
            GetAdministratorsAsync);

        group.MapPost(
            "/administrators",
            AddAdministratorAsync);

        group.MapGet("/stores", GetStoresAsync);
        group.MapGet("/stores/{tenantId:guid}", GetStoreAsync);

        group.MapPost(
            "/stores/{tenantId:guid}/suspend",
            SuspendStoreAsync);

        group.MapPost(
            "/stores/{tenantId:guid}/activate",
            ActivateStoreAsync);

        group.MapPut(
            "/stores/{tenantId:guid}/identity",
            UpdateStoreIdentityAsync);

        group.MapPut(
            "/stores/{tenantId:guid}/primary-vertical",
            ChangePrimaryVerticalAsync);

        group.MapPut(
            "/stores/{tenantId:guid}/capabilities/{capability}",
            SetCapabilityOverrideAsync);

        group.MapPut(
            "/stores/{tenantId:guid}/owner-email",
            SetOwnerEmailAsync);

        group.MapPut(
            "/stores/{tenantId:guid}/subscription",
            SetStoreSubscriptionAsync);

        group.MapGet(
            "/requests",
            GetRequestsAsync);

        group.MapGet(
            "/requests/stats",
            GetRequestStatsAsync);

        group.MapGet(
            "/requests/{requestId:guid}",
            GetRequestAsync);

        group.MapPost(
            "/requests/{requestId:guid}/approve",
            ApproveRequestAsync);

        group.MapPost(
            "/requests/{requestId:guid}/reject",
            RejectRequestAsync);

        group.MapPost(
            "/requests/{requestId:guid}/request-info",
            RequestMoreInfoAsync);

        return endpoints;
    }

    private static async Task<IResult> BootstrapFirstAdministratorAsync(
        HttpContext httpContext,
        MarketDbContext dbContext,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return Results.NotFound();
        }

        var actorUserId = GetActorUserId(httpContext);

        if (!actorUserId.HasValue)
        {
            return Results.Unauthorized();
        }

        var user =
            await dbContext
                .Set<User>()
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(
                    candidate =>
                        candidate.Id == actorUserId.Value,
                    cancellationToken);

        if (user is null ||
            user.IsDeleted ||
            user.Status != UserStatus.Active)
        {
            return Results.Unauthorized();
        }

        var currentUserAlreadyAdministrator =
            await dbContext
                .Set<PlatformUserRoleAssignment>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .AnyAsync(
                    assignment =>
                        assignment.UserId == actorUserId.Value &&
                        assignment.Role == PlatformRole.PlatformAdministrator &&
                        !assignment.IsDeleted,
                    cancellationToken);

        if (currentUserAlreadyAdministrator)
        {
            return Results.Ok(
                new
                {
                    role = PlatformRole.PlatformAdministrator.ToString(),
                    alreadyConfigured = true
                });
        }

        var anotherAdministratorExists =
            await dbContext
                .Set<PlatformUserRoleAssignment>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .AnyAsync(
                    assignment =>
                        assignment.Role == PlatformRole.PlatformAdministrator &&
                        !assignment.IsDeleted,
                    cancellationToken);

        if (anotherAdministratorExists)
        {
            return Results.Conflict(
                new
                {
                    code = "platform_administrator_already_exists",
                    message = "The first platform administrator has already been configured."
                });
        }

        var assignmentId = Guid.NewGuid();
        var actorGuid = actorUserId.Value.Value;
        var now = DateTimeOffset.UtcNow;
        var roleName = PlatformRole.PlatformAdministrator.ToString();

        await dbContext
            .Database
            .ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO platform_user_role_assignments
                (
                    id,
                    user_id,
                    role,
                    created_at_utc,
                    created_by_user_id,
                    is_deleted
                )
                VALUES
                (
                    {assignmentId},
                    {actorGuid},
                    {roleName},
                    {now},
                    {actorGuid},
                    false
                )
                """,
                cancellationToken);

        return Results.Ok(
            new
            {
                role = roleName,
                alreadyConfigured = false
            });
    }

    private static Task<IResult> GetMeAsync(
        HttpContext httpContext,
        IHostEnvironment environment)
    {
        var actorUserId = GetActorUserId(httpContext);

        if (!actorUserId.HasValue)
        {
            return Task.FromResult(Results.Unauthorized());
        }

        IResult result =
            Results.Ok(
                new
                {
                    userId = actorUserId.Value.Value,
                    role = "SuperAdmin",
                    backendRole = PlatformRole.PlatformAdministrator.ToString(),
                    environment = environment.EnvironmentName,
                    mfaRequired = !environment.IsDevelopment()
                });

        return Task.FromResult(result);
    }

    private static async Task<IResult> GetAdministratorsAsync(
        MarketDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var assignments =
            await dbContext
                .Set<PlatformUserRoleAssignment>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(
                    assignment =>
                        assignment.Role == PlatformRole.PlatformAdministrator &&
                        !assignment.IsDeleted)
                .OrderBy(assignment => assignment.CreatedAtUtc)
                .ToArrayAsync(cancellationToken);

        var administrators =
            new List<PlatformAdministratorResponse>(
                assignments.Length);

        foreach (var assignment in assignments)
        {
            var user =
                await dbContext
                    .Set<User>()
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        candidate =>
                            candidate.Id == assignment.UserId &&
                            !candidate.IsDeleted,
                        cancellationToken);

            if (user is null)
            {
                continue;
            }

            administrators.Add(
                new PlatformAdministratorResponse(
                    user.Id.Value,
                    user.Email.Value,
                    user.Status.ToString(),
                    user.EmailVerifiedAtUtc.HasValue,
                    assignment.CreatedAtUtc));
        }

        return Results.Ok(administrators);
    }

    private static async Task<IResult> AddAdministratorAsync(
        AddPlatformAdministratorRequest request,
        HttpContext httpContext,
        MarketDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var actor =
            GetActorUserId(
                httpContext);

        if (!actor.HasValue)
        {
            return Results.Unauthorized();
        }

        EmailAddress email;

        try
        {
            email =
                EmailAddress.Create(
                    request.Email);
        }
        catch (ArgumentException)
        {
            return InvalidRequest(
                "platform_administrator_email_invalid",
                "The administrator email address is invalid.");
        }

        var user =
            await dbContext
                .Set<User>()
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(
                    candidate =>
                        candidate.Email == email,
                    cancellationToken);

        if (user is null ||
            user.IsDeleted)
        {
            return Results.NotFound(
                new
                {
                    code = "platform_user_not_found",
                    message = "No active RUKN account exists for this email."
                });
        }

        if (user.Status != UserStatus.Active)
        {
            return Results.Conflict(
                new
                {
                    code = "platform_user_not_active",
                    message = "The selected user account must be active before platform access can be granted."
                });
        }

        var existing =
            await dbContext
                .Set<PlatformUserRoleAssignment>()
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(
                    assignment =>
                        assignment.UserId == user.Id &&
                        assignment.Role == PlatformRole.PlatformAdministrator &&
                        !assignment.IsDeleted,
                    cancellationToken);

        if (existing is not null)
        {
            return Results.Ok(
                new PlatformAdministratorMutationResponse(
                    user.Id.Value,
                    user.Email.Value,
                    true));
        }

        var now =
            DateTimeOffset.UtcNow;

        var assignment =
            PlatformUserRoleAssignment.Create(
                user.Id,
                PlatformRole.PlatformAdministrator,
                now,
                actor.Value.Value);

        dbContext.Add(
            assignment);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return Results.Ok(
            new PlatformAdministratorMutationResponse(
                user.Id.Value,
                user.Email.Value,
                false));
    }

    private static async Task<IResult> GetStoresAsync(
        MarketDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var tenants =
            await dbContext
                .Set<Tenant>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(tenant => !tenant.IsDeleted)
                .OrderByDescending(tenant => tenant.CreatedAtUtc)
                .ToArrayAsync(cancellationToken);

        var results =
            new List<PlatformStoreSummaryResponse>(tenants.Length);

        foreach (var tenant in tenants)
        {
            results.Add(
                await BuildStoreSummaryAsync(
                    dbContext,
                    tenant,
                    cancellationToken));
        }

        return Results.Ok(results);
    }

    private static async Task<IResult> GetStoreAsync(
        Guid tenantId,
        MarketDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var tenant =
            await FindTenantAsync(
                tenantId,
                dbContext,
                tracking: false,
                cancellationToken);

        if (tenant is null)
        {
            return StoreNotFound();
        }

        return Results.Ok(
            await BuildStoreDetailAsync(
                dbContext,
                tenant,
                cancellationToken));
    }

    private static async Task<IResult> SuspendStoreAsync(
        Guid tenantId,
        PlatformActionRequest request,
        HttpContext httpContext,
        MarketDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var actor = GetActorUserId(httpContext);

        if (!actor.HasValue)
        {
            return Results.Unauthorized();
        }

        if (!TryNormalizeReason(
                request.Reason,
                out var reason,
                out var reasonError))
        {
            return reasonError!;
        }

        var tenant =
            await FindTenantAsync(
                tenantId,
                dbContext,
                tracking: true,
                cancellationToken);

        if (tenant is null)
        {
            return StoreNotFound();
        }

        if (tenant.Status == TenantStatus.Suspended)
        {
            return Results.Ok(
                new StoreMutationResponse(
                    tenant.Id.Value,
                    tenant.Status.ToString()));
        }

        var oldStatus = tenant.Status.ToString();
        var now = DateTimeOffset.UtcNow;

        tenant.Suspend(now, actor.Value.Value);

        AddAudit(
            dbContext,
            httpContext,
            actor.Value,
            tenant.Id,
            "store.status.suspended",
            reason,
            new { status = oldStatus },
            new { status = tenant.Status.ToString() },
            now);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(
            new StoreMutationResponse(
                tenant.Id.Value,
                tenant.Status.ToString()));
    }

    private static async Task<IResult> ActivateStoreAsync(
        Guid tenantId,
        PlatformActionRequest request,
        HttpContext httpContext,
        MarketDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var actor = GetActorUserId(httpContext);

        if (!actor.HasValue)
        {
            return Results.Unauthorized();
        }

        if (!TryNormalizeReason(
                request.Reason,
                out var reason,
                out var reasonError))
        {
            return reasonError!;
        }

        var tenant =
            await FindTenantAsync(
                tenantId,
                dbContext,
                tracking: true,
                cancellationToken);

        if (tenant is null)
        {
            return StoreNotFound();
        }

        if (tenant.Status == TenantStatus.Active)
        {
            return Results.Ok(
                new StoreMutationResponse(
                    tenant.Id.Value,
                    tenant.Status.ToString()));
        }

        var oldStatus = tenant.Status.ToString();
        var now = DateTimeOffset.UtcNow;

        tenant.Activate(now, actor.Value.Value);

        AddAudit(
            dbContext,
            httpContext,
            actor.Value,
            tenant.Id,
            "store.status.activated",
            reason,
            new { status = oldStatus },
            new { status = tenant.Status.ToString() },
            now);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(
            new StoreMutationResponse(
                tenant.Id.Value,
                tenant.Status.ToString()));
    }

    private static async Task<IResult> UpdateStoreIdentityAsync(
        Guid tenantId,
        UpdateStoreIdentityRequest request,
        HttpContext httpContext,
        MarketDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var actor = GetActorUserId(httpContext);

        if (!actor.HasValue)
        {
            return Results.Unauthorized();
        }

        if (!TryNormalizeReason(
                request.Reason,
                out var reason,
                out var reasonError))
        {
            return reasonError!;
        }

        var tenant =
            await FindTenantAsync(
                tenantId,
                dbContext,
                tracking: true,
                cancellationToken);

        if (tenant is null)
        {
            return StoreNotFound();
        }

        TenantSlug newSlug;

        try
        {
            newSlug = TenantSlug.Create(request.Slug);
        }
        catch (ArgumentException exception)
        {
            return InvalidRequest(
                "store_slug_invalid",
                exception.Message);
        }

        var normalizedName = request.Name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedName) ||
            normalizedName.Length > 200)
        {
            return InvalidRequest(
                "store_name_invalid",
                "Store name is required and cannot exceed 200 characters.");
        }

        var slugTaken =
            await dbContext
                .Set<Tenant>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .AnyAsync(
                    candidate =>
                        candidate.Id != tenant.Id &&
                        !candidate.IsDeleted &&
                        candidate.Slug == newSlug,
                    cancellationToken);

        if (slugTaken)
        {
            return Results.Conflict(
                new
                {
                    code = "store_slug_already_exists",
                    message = "This store link is already in use."
                });
        }

        var oldValue =
            new
            {
                name = tenant.Name,
                slug = tenant.Slug.Value
            };

        if (tenant.Name == normalizedName &&
            tenant.Slug.Equals(newSlug))
        {
            return Results.Ok(
                new StoreIdentityResponse(
                    tenant.Id.Value,
                    tenant.Name,
                    tenant.Slug.Value));
        }

        var now = DateTimeOffset.UtcNow;

        tenant.Rename(
            normalizedName,
            now,
            actor.Value.Value);

        tenant.ChangeSlug(
            newSlug.Value,
            now,
            actor.Value.Value);

        AddAudit(
            dbContext,
            httpContext,
            actor.Value,
            tenant.Id,
            "store.identity.updated",
            reason,
            oldValue,
            new
            {
                name = tenant.Name,
                slug = tenant.Slug.Value
            },
            now);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(
                new
                {
                    code = "store_identity_conflict",
                    message = "The requested store identity conflicts with another store."
                });
        }

        return Results.Ok(
            new StoreIdentityResponse(
                tenant.Id.Value,
                tenant.Name,
                tenant.Slug.Value));
    }

    private static async Task<IResult> ChangePrimaryVerticalAsync(
        Guid tenantId,
        ChangePrimaryVerticalRequest request,
        HttpContext httpContext,
        MarketDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var actor = GetActorUserId(httpContext);

        if (!actor.HasValue)
        {
            return Results.Unauthorized();
        }

        if (!TryNormalizeReason(
                request.Reason,
                out var reason,
                out var reasonError))
        {
            return reasonError!;
        }

        var tenant =
            await FindTenantAsync(
                tenantId,
                dbContext,
                tracking: true,
                cancellationToken);

        if (tenant is null)
        {
            return StoreNotFound();
        }

        var requestedCode = request.VerticalCode?.Trim() ?? string.Empty;

        var definition =
            CommerceVerticalCatalog.All
                .SingleOrDefault(
                    item =>
                        string.Equals(
                            item.Code,
                            requestedCode,
                            StringComparison.OrdinalIgnoreCase));

        if (definition is null)
        {
            return InvalidRequest(
                "commerce_vertical_invalid",
                "A supported commerce vertical is required.");
        }

        var verticals =
            await dbContext
                .Set<TenantCommerceVertical>()
                .IgnoreQueryFilters()
                .Where(item => item.TenantId == tenant.Id)
                .ToListAsync(cancellationToken);

        var currentPrimary =
            verticals.SingleOrDefault(item => item.IsPrimary);

        if (currentPrimary?.VerticalType == definition.VerticalType)
        {
            return Results.Ok(
                new StoreVerticalResponse(
                    tenant.Id.Value,
                    definition.VerticalType.ToString(),
                    definition.Code));
        }

        var target =
            verticals.SingleOrDefault(
                item => item.VerticalType == definition.VerticalType);

        var oldVertical =
            currentPrimary is null
                ? null
                : CommerceVerticalCatalog.Get(
                    currentPrimary.VerticalType);

        var now = DateTimeOffset.UtcNow;

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        if (currentPrimary is not null)
        {
            currentPrimary.RemovePrimary(
                now,
                actor.Value.Value);

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (target is null)
        {
            target =
                TenantCommerceVertical.Create(
                    tenant.Id,
                    definition.VerticalType,
                    isPrimary: false,
                    now,
                    actor.Value.Value);

            dbContext.Set<TenantCommerceVertical>().Add(target);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        target.MakePrimary(
            now,
            actor.Value.Value);

        AddAudit(
            dbContext,
            httpContext,
            actor.Value,
            tenant.Id,
            "store.vertical.changed",
            reason,
            oldVertical is null
                ? null
                : new
                {
                    vertical = oldVertical.VerticalType.ToString(),
                    code = oldVertical.Code
                },
            new
            {
                vertical = definition.VerticalType.ToString(),
                code = definition.Code
            },
            now);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Results.Ok(
            new StoreVerticalResponse(
                tenant.Id.Value,
                definition.VerticalType.ToString(),
                definition.Code));
    }

    private static async Task<IResult> SetCapabilityOverrideAsync(
        Guid tenantId,
        string capability,
        SetCapabilityOverrideRequest request,
        HttpContext httpContext,
        MarketDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var actor = GetActorUserId(httpContext);

        if (!actor.HasValue)
        {
            return Results.Unauthorized();
        }

        if (!TryNormalizeReason(
                request.Reason,
                out var reason,
                out var reasonError))
        {
            return reasonError!;
        }

        if (!Enum.TryParse<CommerceCapabilityType>(
                capability,
                ignoreCase: true,
                out var capabilityType) ||
            capabilityType == CommerceCapabilityType.Unknown ||
            !Enum.IsDefined(capabilityType))
        {
            return InvalidRequest(
                "commerce_capability_invalid",
                "A supported commerce capability is required.");
        }

        var tenant =
            await FindTenantAsync(
                tenantId,
                dbContext,
                tracking: false,
                cancellationToken);

        if (tenant is null)
        {
            return StoreNotFound();
        }

        var verticals =
            await dbContext
                .Set<TenantCommerceVertical>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(
                    item =>
                        item.TenantId == tenant.Id &&
                        item.IsEnabled)
                .ToArrayAsync(cancellationToken);

        if (verticals.Length == 0)
        {
            return Results.Conflict(
                new
                {
                    code = "commerce_vertical_required",
                    message = "Configure a store activity before changing capabilities."
                });
        }

        var defaultEnabled =
            verticals.Any(
                vertical =>
                    CommerceVerticalCatalog
                        .Get(vertical.VerticalType)
                        .DefaultCapabilities
                        .Contains(capabilityType));

        var existing =
            await dbContext
                .Set<TenantCommerceCapabilityOverride>()
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(
                    item =>
                        item.TenantId == tenant.Id &&
                        item.CapabilityType == capabilityType,
                    cancellationToken);

        var oldOverride = existing?.IsEnabled;
        var oldEffective = oldOverride ?? defaultEnabled;
        var now = DateTimeOffset.UtcNow;

        if (!request.IsEnabled.HasValue)
        {
            if (existing is not null)
            {
                dbContext.Remove(existing);
            }
        }
        else if (existing is null)
        {
            var created =
                TenantCommerceCapabilityOverride.Create(
                    tenant.Id,
                    capabilityType,
                    request.IsEnabled.Value,
                    now,
                    actor.Value.Value);

            dbContext.Add(created);
        }
        else
        {
            existing.SetEnabled(
                request.IsEnabled.Value,
                now,
                actor.Value.Value);
        }

        var newEffective = request.IsEnabled ?? defaultEnabled;

        if (oldOverride != request.IsEnabled)
        {
            AddAudit(
                dbContext,
                httpContext,
                actor.Value,
                tenant.Id,
                request.IsEnabled.HasValue
                    ? "store.capability.override_set"
                    : "store.capability.override_reset",
                reason,
                new
                {
                    capability = capabilityType.ToString(),
                    overrideValue = oldOverride,
                    effectiveEnabled = oldEffective
                },
                new
                {
                    capability = capabilityType.ToString(),
                    overrideValue = request.IsEnabled,
                    effectiveEnabled = newEffective
                },
                now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(
            new StoreCapabilityResponse(
                capabilityType.ToString(),
                defaultEnabled,
                newEffective,
                request.IsEnabled));
    }

    private static async Task<IResult> SetOwnerEmailAsync(
        Guid tenantId,
        SetOwnerEmailRequest request,
        HttpContext httpContext,
        MarketDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var actor = GetActorUserId(httpContext);

        if (!actor.HasValue)
        {
            return Results.Unauthorized();
        }

        if (!TryNormalizeReason(
                request.Reason,
                out var reason,
                out var reasonError))
        {
            return reasonError!;
        }

        var tenant =
            await FindTenantAsync(
                tenantId,
                dbContext,
                tracking: false,
                cancellationToken);

        if (tenant is null)
        {
            return StoreNotFound();
        }

        var result =
            await ApplyOwnerEmailAsync(
                tenant,
                request.Email,
                actor.Value,
                DateTimeOffset.UtcNow,
                dbContext,
                cancellationToken);

        if (result.Error is not null)
        {
            return result.Error;
        }

        AddAudit(
            dbContext,
            httpContext,
            actor.Value,
            tenant.Id,
            "store.owner.email_changed",
            reason,
            result.OldEmail is null
                ? null
                : new { email = result.OldEmail },
            new
            {
                email = result.NewEmail,
                ownerUserId = result.OwnerUserId
            },
            DateTimeOffset.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(
            new OwnerEmailResponse(
                tenant.Id.Value,
                result.OwnerUserId!.Value,
                result.NewEmail!));
    }

    private static async Task<IResult> SetStoreSubscriptionAsync(
        Guid tenantId,
        SetStoreSubscriptionRequest request,
        HttpContext httpContext,
        MarketDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var actor = GetActorUserId(httpContext);

        if (!actor.HasValue)
        {
            return Results.Unauthorized();
        }

        if (!TryNormalizeReason(
                request.Reason,
                out var reason,
                out var reasonError))
        {
            return reasonError!;
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
            return InvalidRequest(
                "billing_cycle_invalid",
                "Billing cycle must be Monthly or Annual.");
        }

        if (!TryParseSubscriptionStatus(
                request.Status,
                out var status))
        {
            return InvalidRequest(
                "subscription_status_invalid",
                "Subscription status must be Active or Suspended.");
        }

        var tenant =
            await FindTenantAsync(
                tenantId,
                dbContext,
                tracking: false,
                cancellationToken);

        if (tenant is null)
        {
            return StoreNotFound();
        }

        var subscription =
            await dbContext
                .Set<StoreSubscription>()
                .SingleOrDefaultAsync(
                    item => item.TenantId == tenant.Id,
                    cancellationToken);

        var oldValue = subscription is null
            ? null
            : new
            {
                planCode = subscription.PlanCode,
                billingCycle = subscription.BillingCycle.ToString(),
                status = subscription.Status.ToString()
            };

        var now = DateTimeOffset.UtcNow;

        if (subscription is null)
        {
            subscription =
                StoreSubscription.Create(
                    tenant.Id,
                    plan.Code,
                    billingCycle,
                    now,
                    actor.Value.Value);

            dbContext.Add(subscription);
        }
        else
        {
            subscription.ChangePlan(
                plan.Code,
                billingCycle,
                now,
                actor.Value.Value);
        }

        if (status == StoreSubscriptionStatus.Suspended)
        {
            subscription.Suspend(now, actor.Value.Value);
        }
        else
        {
            subscription.Activate(now, actor.Value.Value);
        }

        AddAudit(
            dbContext,
            httpContext,
            actor.Value,
            tenant.Id,
            "store.subscription.updated",
            reason,
            oldValue,
            new
            {
                planCode = subscription.PlanCode,
                billingCycle = subscription.BillingCycle.ToString(),
                status = subscription.Status.ToString()
            },
            now);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(MapSubscription(subscription));
    }

    private static async Task<IResult> GetRequestStatsAsync(
        MarketDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var pending =
            await dbContext
                .Set<PlatformRequest>()
                .AsNoTracking()
                .CountAsync(
                    item => item.Status == PlatformRequestStatus.Pending,
                    cancellationToken);

        var moreInfo =
            await dbContext
                .Set<PlatformRequest>()
                .AsNoTracking()
                .CountAsync(
                    item => item.Status == PlatformRequestStatus.MoreInfoRequested,
                    cancellationToken);

        return Results.Ok(
            new RequestStatsResponse(
                pending,
                moreInfo));
    }

    private static async Task<IResult> GetRequestsAsync(
        string? status,
        string? type,
        string? search,
        MarketDbContext dbContext,
        CancellationToken cancellationToken)
    {
        IQueryable<PlatformRequest> query =
            dbContext
                .Set<PlatformRequest>()
                .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<PlatformRequestStatus>(
                    status.Trim(),
                    true,
                    out var parsedStatus) ||
                !Enum.IsDefined(parsedStatus))
            {
                return InvalidRequest(
                    "platform_request_status_invalid",
                    "Unsupported platform request status.");
            }

            query = query.Where(item => item.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            if (!Enum.TryParse<PlatformRequestType>(
                    type.Trim(),
                    true,
                    out var parsedType) ||
                !Enum.IsDefined(parsedType))
            {
                return InvalidRequest(
                    "platform_request_type_invalid",
                    "Unsupported platform request type.");
            }

            query = query.Where(item => item.Type == parsedType);
        }

        var requestItems =
            await query
                .OrderByDescending(item => item.RequestedAtUtc)
                .Take(250)
                .ToArrayAsync(cancellationToken);

        var responses =
            new List<PlatformRequestSummaryResponse>(requestItems.Length);

        var normalizedSearch = search?.Trim().ToLowerInvariant();

        foreach (var item in requestItems)
        {
            var tenant =
                await dbContext
                    .Set<Tenant>()
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        candidate => candidate.Id == item.TenantId,
                        cancellationToken);

            var requester =
                await dbContext
                    .Set<User>()
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        candidate => candidate.Id == item.RequestedByUserId,
                        cancellationToken);

            if (tenant is null || tenant.IsDeleted)
            {
                continue;
            }

            var response =
                new PlatformRequestSummaryResponse(
                    item.Id.Value,
                    tenant.Id.Value,
                    tenant.Name,
                    item.Type.ToString(),
                    item.Status.ToString(),
                    item.Summary,
                    requester?.Email.ToString(),
                    item.RequestedAtUtc,
                    item.ReviewedAtUtc,
                    item.ReviewReason);

            if (!string.IsNullOrWhiteSpace(normalizedSearch) &&
                !new[]
                {
                    response.TenantName,
                    response.RequestedByEmail ?? string.Empty,
                    response.Summary,
                    response.Type
                }.Any(
                    value =>
                        value.ToLowerInvariant().Contains(normalizedSearch)))
            {
                continue;
            }

            responses.Add(response);
        }

        return Results.Ok(responses);
    }

    private static async Task<IResult> GetRequestAsync(
        Guid requestId,
        MarketDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var item =
            await FindPlatformRequestAsync(
                requestId,
                dbContext,
                tracking: false,
                cancellationToken);

        if (item is null)
        {
            return RequestNotFound();
        }

        var tenant =
            await FindTenantAsync(
                item.TenantId.Value,
                dbContext,
                tracking: false,
                cancellationToken);

        if (tenant is null)
        {
            return StoreNotFound();
        }

        var requester =
            await dbContext
                .Set<User>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    candidate => candidate.Id == item.RequestedByUserId,
                    cancellationToken);

        string? reviewerEmail = null;

        if (item.ReviewedByUserId.HasValue)
        {
            var reviewer =
                await dbContext
                    .Set<User>()
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        candidate => candidate.Id == item.ReviewedByUserId.Value,
                        cancellationToken);

            reviewerEmail = reviewer?.Email.ToString();
        }

        return Results.Ok(
            new PlatformRequestDetailResponse(
                item.Id.Value,
                tenant.Id.Value,
                tenant.Name,
                tenant.Slug.ToString(),
                item.Type.ToString(),
                item.Status.ToString(),
                item.Summary,
                item.PayloadJson,
                item.RequestedByUserId.Value,
                requester?.Email.ToString(),
                item.RequestedAtUtc,
                item.ReviewedAtUtc,
                item.ReviewedByUserId?.Value,
                reviewerEmail,
                item.ReviewReason));
    }

    private static async Task<IResult> RequestMoreInfoAsync(
        Guid requestId,
        PlatformActionRequest request,
        HttpContext httpContext,
        MarketDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var actor = GetActorUserId(httpContext);

        if (!actor.HasValue)
        {
            return Results.Unauthorized();
        }

        if (!TryNormalizeReason(
                request.Reason,
                out var reason,
                out var reasonError))
        {
            return reasonError!;
        }

        var item =
            await FindPlatformRequestAsync(
                requestId,
                dbContext,
                tracking: true,
                cancellationToken);

        if (item is null)
        {
            return RequestNotFound();
        }

        if (item.Status is PlatformRequestStatus.Approved or PlatformRequestStatus.Rejected)
        {
            return Results.Conflict(
                new
                {
                    code = "platform_request_completed",
                    message = "A completed request cannot be changed."
                });
        }

        var previousStatus = item.Status.ToString();
        var now = DateTimeOffset.UtcNow;

        item.RequestMoreInfo(actor.Value, reason, now);

        AddAudit(
            dbContext,
            httpContext,
            actor.Value,
            item.TenantId,
            "platform.request.more_info",
            reason,
            new
            {
                requestId = item.Id.Value,
                status = previousStatus
            },
            new
            {
                requestId = item.Id.Value,
                status = item.Status.ToString()
            },
            now);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(
            new RequestMutationResponse(
                item.Id.Value,
                item.Status.ToString()));
    }

    private static async Task<IResult> RejectRequestAsync(
        Guid requestId,
        PlatformActionRequest request,
        HttpContext httpContext,
        MarketDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var actor = GetActorUserId(httpContext);

        if (!actor.HasValue)
        {
            return Results.Unauthorized();
        }

        if (!TryNormalizeReason(
                request.Reason,
                out var reason,
                out var reasonError))
        {
            return reasonError!;
        }

        var item =
            await FindPlatformRequestAsync(
                requestId,
                dbContext,
                tracking: true,
                cancellationToken);

        if (item is null)
        {
            return RequestNotFound();
        }

        if (item.Status == PlatformRequestStatus.Rejected)
        {
            return Results.Ok(
                new RequestMutationResponse(
                    item.Id.Value,
                    item.Status.ToString()));
        }

        if (item.Status == PlatformRequestStatus.Approved)
        {
            return Results.Conflict(
                new
                {
                    code = "platform_request_completed",
                    message = "An approved request cannot be rejected."
                });
        }

        var previousStatus = item.Status.ToString();
        var now = DateTimeOffset.UtcNow;

        item.Reject(actor.Value, reason, now);

        AddAudit(
            dbContext,
            httpContext,
            actor.Value,
            item.TenantId,
            "platform.request.rejected",
            reason,
            new
            {
                requestId = item.Id.Value,
                status = previousStatus,
                type = item.Type.ToString()
            },
            new
            {
                requestId = item.Id.Value,
                status = item.Status.ToString(),
                type = item.Type.ToString()
            },
            now);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(
            new RequestMutationResponse(
                item.Id.Value,
                item.Status.ToString()));
    }

    private static async Task<IResult> ApproveRequestAsync(
        Guid requestId,
        PlatformActionRequest request,
        HttpContext httpContext,
        MarketDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var actor = GetActorUserId(httpContext);

        if (!actor.HasValue)
        {
            return Results.Unauthorized();
        }

        if (!TryNormalizeReason(
                request.Reason,
                out var reason,
                out var reasonError))
        {
            return reasonError!;
        }

        var item =
            await FindPlatformRequestAsync(
                requestId,
                dbContext,
                tracking: true,
                cancellationToken);

        if (item is null)
        {
            return RequestNotFound();
        }

        if (item.Status == PlatformRequestStatus.Approved)
        {
            return Results.Ok(
                new RequestMutationResponse(
                    item.Id.Value,
                    item.Status.ToString()));
        }

        if (item.Status == PlatformRequestStatus.Rejected)
        {
            return Results.Conflict(
                new
                {
                    code = "platform_request_completed",
                    message = "A rejected request cannot be approved."
                });
        }

        var tenant =
            await FindTenantAsync(
                item.TenantId.Value,
                dbContext,
                tracking: true,
                cancellationToken);

        if (tenant is null)
        {
            return StoreNotFound();
        }

        var now = DateTimeOffset.UtcNow;
        IResult? applyError = null;

        switch (item.Type)
        {
            case PlatformRequestType.StoreRegistration:
            {
                var payload =
                    DeserializePayload<RegistrationPayload>(item.PayloadJson);

                if (payload is null ||
                    !TryNormalizePlan(
                        payload.PlanCode,
                        out var plan,
                        out applyError) ||
                    !TryParseBillingCycle(
                        payload.BillingCycle,
                        out var billingCycle))
                {
                    applyError ??=
                        InvalidRequest(
                            "platform_request_payload_invalid",
                            "Registration request payload is invalid.");

                    break;
                }

                if (tenant.Status == TenantStatus.Suspended)
                {
                    applyError =
                        Results.Conflict(
                            new
                            {
                                code = "store_registration_suspended",
                                message = "A suspended store cannot be approved through registration."
                            });
                    break;
                }

                if (tenant.Status == TenantStatus.Draft)
                {
                    tenant.Activate(now, actor.Value.Value);
                }

                await UpsertSubscriptionAsync(
                    tenant.Id,
                    plan.Code,
                    billingCycle,
                    StoreSubscriptionStatus.Active,
                    actor.Value,
                    now,
                    dbContext,
                    cancellationToken);

                break;
            }

            case PlatformRequestType.PlanChange:
            {
                var payload =
                    DeserializePayload<PlanChangePayload>(item.PayloadJson);

                if (payload is null ||
                    !TryNormalizePlan(
                        payload.PlanCode,
                        out var plan,
                        out applyError) ||
                    !TryParseBillingCycle(
                        payload.BillingCycle,
                        out var billingCycle))
                {
                    applyError ??=
                        InvalidRequest(
                            "platform_request_payload_invalid",
                            "Plan request payload is invalid.");
                    break;
                }

                await UpsertSubscriptionAsync(
                    tenant.Id,
                    plan.Code,
                    billingCycle,
                    StoreSubscriptionStatus.Active,
                    actor.Value,
                    now,
                    dbContext,
                    cancellationToken);

                break;
            }

            case PlatformRequestType.StoreIdentityChange:
            {
                var payload =
                    DeserializePayload<StoreIdentityChangePayload>(item.PayloadJson);

                if (payload is null)
                {
                    applyError =
                        InvalidRequest(
                            "platform_request_payload_invalid",
                            "Store identity request payload is invalid.");
                    break;
                }

                applyError =
                    await ApplyStoreIdentityChangeAsync(
                        tenant,
                        payload,
                        actor.Value,
                        now,
                        dbContext,
                        cancellationToken);

                break;
            }

            case PlatformRequestType.OwnerEmailChange:
            {
                var payload =
                    DeserializePayload<OwnerEmailChangePayload>(item.PayloadJson);

                if (payload is null)
                {
                    applyError =
                        InvalidRequest(
                            "platform_request_payload_invalid",
                            "Owner email request payload is invalid.");
                    break;
                }

                var ownerResult =
                    await ApplyOwnerEmailAsync(
                        tenant,
                        payload.Email,
                        actor.Value,
                        now,
                        dbContext,
                        cancellationToken);

                applyError = ownerResult.Error;
                break;
            }

            default:
                applyError =
                    InvalidRequest(
                        "platform_request_type_invalid",
                        "Unsupported platform request type.");
                break;
        }

        if (applyError is not null)
        {
            return applyError;
        }

        var previousStatus = item.Status.ToString();
        item.Approve(actor.Value, reason, now);

        AddAudit(
            dbContext,
            httpContext,
            actor.Value,
            tenant.Id,
            "platform.request.approved",
            reason,
            new
            {
                requestId = item.Id.Value,
                status = previousStatus,
                type = item.Type.ToString()
            },
            new
            {
                requestId = item.Id.Value,
                status = item.Status.ToString(),
                type = item.Type.ToString()
            },
            now);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(
            new RequestMutationResponse(
                item.Id.Value,
                item.Status.ToString()));
    }

    private static async Task<IResult?> ApplyStoreIdentityChangeAsync(
        Tenant tenant,
        StoreIdentityChangePayload payload,
        UserId actor,
        DateTimeOffset now,
        MarketDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(payload.Name))
        {
            try
            {
                tenant.Rename(
                    payload.Name,
                    now,
                    actor.Value);
            }
            catch (ArgumentException exception)
            {
                return InvalidRequest(
                    "store_name_invalid",
                    exception.Message);
            }
        }

        if (!string.IsNullOrWhiteSpace(payload.Slug))
        {
            TenantSlug normalizedSlug;

            try
            {
                normalizedSlug = TenantSlug.Create(payload.Slug);
            }
            catch (ArgumentException exception)
            {
                return InvalidRequest(
                    "store_slug_invalid",
                    exception.Message);
            }

            var slugExists =
                await dbContext
                    .Set<Tenant>()
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .AnyAsync(
                        candidate =>
                            candidate.Id != tenant.Id &&
                            candidate.Slug == normalizedSlug &&
                            !candidate.IsDeleted,
                        cancellationToken);

            if (slugExists)
            {
                return Results.Conflict(
                    new
                    {
                        code = "store_slug_already_exists",
                        message = "Store slug is already used by another store."
                    });
            }

            tenant.ChangeSlug(
                normalizedSlug.Value,
                now,
                actor.Value);
        }

        if (!string.IsNullOrWhiteSpace(payload.VerticalCode))
        {
            if (!TryGetVerticalByCode(
                    payload.VerticalCode,
                    out var definition))
            {
                return InvalidRequest(
                    "commerce_vertical_invalid",
                    "A supported commerce vertical is required.");
            }

            var verticals =
                await dbContext
                    .Set<TenantCommerceVertical>()
                    .IgnoreQueryFilters()
                    .Where(item => item.TenantId == tenant.Id)
                    .ToArrayAsync(cancellationToken);

            var target =
                verticals.FirstOrDefault(
                    item => item.VerticalType == definition.VerticalType);

            foreach (var vertical in verticals.Where(item => item.IsPrimary))
            {
                if (vertical.VerticalType != definition.VerticalType)
                {
                    vertical.RemovePrimary(now, actor.Value);
                }
            }

            if (target is null)
            {
                target =
                    TenantCommerceVertical.Create(
                        tenant.Id,
                        definition.VerticalType,
                        isPrimary: false,
                        now,
                        actor.Value);

                dbContext.Add(target);
            }
            else if (!target.IsEnabled)
            {
                target.Enable(now, actor.Value);
            }

            target.MakePrimary(now, actor.Value);
        }

        return null;
    }

    private static async Task<OwnerEmailApplyResult> ApplyOwnerEmailAsync(
        Tenant tenant,
        string? email,
        UserId actor,
        DateTimeOffset now,
        MarketDbContext dbContext,
        CancellationToken cancellationToken)
    {
        EmailAddress normalizedEmail;

        try
        {
            normalizedEmail = EmailAddress.Create(email ?? string.Empty);
        }
        catch (ArgumentException exception)
        {
            return new OwnerEmailApplyResult(
                null,
                null,
                null,
                InvalidRequest(
                    "owner_email_invalid",
                    exception.Message));
        }

        var ownerMembership =
            await dbContext
                .Set<TenantMembership>()
                .IgnoreQueryFilters()
                .Where(
                    membership =>
                        membership.TenantId == tenant.Id &&
                        membership.Role == TenantRole.Owner &&
                        !membership.IsDeleted)
                .OrderBy(membership => membership.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

        if (ownerMembership is not null)
        {
            var owner =
                await dbContext
                    .Set<User>()
                    .IgnoreQueryFilters()
                    .SingleOrDefaultAsync(
                        user =>
                            user.Id == ownerMembership.UserId &&
                            !user.IsDeleted,
                        cancellationToken);

            if (owner is null)
            {
                return new OwnerEmailApplyResult(
                    null,
                    null,
                    null,
                    Results.Conflict(
                        new
                        {
                            code = "store_owner_user_missing",
                            message = "The store owner account could not be found."
                        }));
            }

            var duplicate =
                await dbContext
                    .Set<User>()
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .AnyAsync(
                        user =>
                            user.Id != owner.Id &&
                            user.Email == normalizedEmail &&
                            !user.IsDeleted,
                        cancellationToken);

            if (duplicate)
            {
                return new OwnerEmailApplyResult(
                    null,
                    null,
                    null,
                    Results.Conflict(
                        new
                        {
                            code = "owner_email_already_exists",
                            message = "This email belongs to another account."
                        }));
            }

            var oldEmail = owner.Email.Value;
            owner.ChangeEmail(
                normalizedEmail.Value,
                now,
                actor.Value);

            return new OwnerEmailApplyResult(
                owner.Id.Value,
                oldEmail,
                owner.Email.Value,
                null);
        }

        var existingUser =
            await dbContext
                .Set<User>()
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(
                    user =>
                        user.Email == normalizedEmail &&
                        !user.IsDeleted &&
                        user.Status == UserStatus.Active,
                    cancellationToken);

        if (existingUser is null)
        {
            return new OwnerEmailApplyResult(
                null,
                null,
                null,
                Results.Conflict(
                    new
                    {
                        code = "owner_account_not_found",
                        message = "No active user account exists with this email. Create the account first, then link it as the store owner."
                    }));
        }

        var membership =
            await dbContext
                .Set<TenantMembership>()
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(
                    item =>
                        item.TenantId == tenant.Id &&
                        item.UserId == existingUser.Id,
                    cancellationToken);

        if (membership is null)
        {
            dbContext.Add(
                TenantMembership.Create(
                    tenant.Id,
                    existingUser.Id,
                    TenantRole.Owner,
                    now,
                    actor.Value));
        }
        else if (membership.IsDeleted)
        {
            return new OwnerEmailApplyResult(
                null,
                null,
                null,
                Results.Conflict(
                    new
                    {
                        code = "owner_membership_deleted",
                        message = "The matching user has a deleted store membership and cannot be linked automatically."
                    }));
        }
        else
        {
            membership.ChangeRole(
                TenantRole.Owner,
                now,
                actor.Value);
        }

        return new OwnerEmailApplyResult(
            existingUser.Id.Value,
            null,
            existingUser.Email.Value,
            null);
    }

    private static async Task<StoreSubscription> UpsertSubscriptionAsync(
        TenantId tenantId,
        string planCode,
        StoreBillingCycle billingCycle,
        StoreSubscriptionStatus status,
        UserId actor,
        DateTimeOffset now,
        MarketDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var subscription =
            await dbContext
                .Set<StoreSubscription>()
                .SingleOrDefaultAsync(
                    item => item.TenantId == tenantId,
                    cancellationToken);

        if (subscription is null)
        {
            subscription =
                StoreSubscription.Create(
                    tenantId,
                    planCode,
                    billingCycle,
                    now,
                    actor.Value);

            dbContext.Add(subscription);
        }
        else
        {
            subscription.ChangePlan(
                planCode,
                billingCycle,
                now,
                actor.Value);
        }

        if (status == StoreSubscriptionStatus.Suspended)
        {
            subscription.Suspend(now, actor.Value);
        }
        else
        {
            subscription.Activate(now, actor.Value);
        }

        return subscription;
    }

    private static async Task<PlatformRequest?> FindPlatformRequestAsync(
        Guid requestId,
        MarketDbContext dbContext,
        bool tracking,
        CancellationToken cancellationToken)
    {
        if (requestId == Guid.Empty)
        {
            return null;
        }

        var id = PlatformRequestId.From(requestId);

        IQueryable<PlatformRequest> query =
            dbContext.Set<PlatformRequest>();

        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return await query.SingleOrDefaultAsync(
            item => item.Id == id,
            cancellationToken);
    }

    private static T? DeserializePayload<T>(string payloadJson)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(payloadJson);
        }
        catch (JsonException)
        {
            return default;
        }
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

        error = InvalidRequest(
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

    private static bool TryParseSubscriptionStatus(
        string? value,
        out StoreSubscriptionStatus status)
    {
        return Enum.TryParse(
                   value?.Trim(),
                   ignoreCase: true,
                   out status) &&
               Enum.IsDefined(status);
    }

    private static bool TryGetVerticalByCode(
        string? code,
        out CommerceVerticalDefinition definition)
    {
        definition =
            CommerceVerticalCatalog.All
                .FirstOrDefault(
                    item =>
                        string.Equals(
                            item.Code,
                            code?.Trim(),
                            StringComparison.OrdinalIgnoreCase))!;

        return definition is not null;
    }

    private static StoreSubscriptionResponse MapSubscription(
        StoreSubscription subscription)
    {
        var plan =
            PlatformPlanCatalog.TryGet(subscription.PlanCode, out var definition)
                ? definition
                : new PlatformPlanDefinition(
                    subscription.PlanCode,
                    subscription.PlanCode,
                    0);

        return new StoreSubscriptionResponse(
            plan.Code,
            plan.Name,
            subscription.BillingCycle.ToString(),
            subscription.Status.ToString(),
            subscription.StartedAtUtc,
            subscription.EndsAtUtc);
    }

    private static IResult RequestNotFound()
    {
        return Results.NotFound(
            new
            {
                code = "platform_request_not_found",
                message = "The requested platform request was not found."
            });
    }

    private static async Task<PlatformStoreDetailResponse> BuildStoreDetailAsync(
        MarketDbContext dbContext,
        Tenant tenant,
        CancellationToken cancellationToken)
    {
        var summary =
            await BuildStoreSummaryAsync(
                dbContext,
                tenant,
                cancellationToken);

        var teamSize =
            await dbContext
                .Set<TenantMembership>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .CountAsync(
                    membership =>
                        membership.TenantId == tenant.Id &&
                        !membership.IsDeleted,
                    cancellationToken);

        var productsCount =
            await dbContext
                .Set<Product>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .CountAsync(
                    product => product.TenantId == tenant.Id,
                    cancellationToken);

        var ordersCount =
            await dbContext
                .Set<Order>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .CountAsync(
                    order => order.TenantId == tenant.Id,
                    cancellationToken);

        var verticals =
            await dbContext
                .Set<TenantCommerceVertical>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(item => item.TenantId == tenant.Id)
                .OrderByDescending(item => item.IsPrimary)
                .ThenBy(item => item.VerticalType)
                .ToArrayAsync(cancellationToken);

        var overrides =
            await dbContext
                .Set<TenantCommerceCapabilityOverride>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(item => item.TenantId == tenant.Id)
                .ToArrayAsync(cancellationToken);

        var defaultCapabilities =
            verticals
                .Where(item => item.IsEnabled)
                .SelectMany(
                    item =>
                        CommerceVerticalCatalog
                            .Get(item.VerticalType)
                            .DefaultCapabilities)
                .ToHashSet();

        var effectiveCapabilities =
            CommerceCapabilityResolver
                .Resolve(verticals, overrides)
                .ToHashSet();

        var overrideMap =
            overrides.ToDictionary(
                item => item.CapabilityType,
                item => (bool?)item.IsEnabled);

        var capabilities =
            Enum.GetValues<CommerceCapabilityType>()
                .Where(item => item != CommerceCapabilityType.Unknown)
                .OrderBy(item => item)
                .Select(
                    item =>
                        new StoreCapabilityResponse(
                            item.ToString(),
                            defaultCapabilities.Contains(item),
                            effectiveCapabilities.Contains(item),
                            overrideMap.GetValueOrDefault(item)))
                .ToArray();

        var availableVerticals =
            CommerceVerticalCatalog.All
                .Select(
                    definition =>
                        new PlatformVerticalOptionResponse(
                            definition.VerticalType.ToString(),
                            definition.Code))
                .ToArray();

        var recentAudit =
            await dbContext
                .Set<PlatformAuditEntry>()
                .AsNoTracking()
                .Where(entry => entry.TenantId == tenant.Id)
                .OrderByDescending(entry => entry.OccurredAtUtc)
                .Take(20)
                .Select(
                    entry =>
                        new PlatformAuditEntryResponse(
                            entry.Id.Value,
                            entry.ActorUserId.Value,
                            entry.Action,
                            entry.Reason,
                            entry.OldValueJson,
                            entry.NewValueJson,
                            entry.OccurredAtUtc))
                .ToArrayAsync(cancellationToken);

        var availablePlans =
            PlatformPlanCatalog.All
                .OrderBy(item => item.Rank)
                .Select(
                    item =>
                        new PlatformPlanOptionResponse(
                            item.Code,
                            item.Name,
                            item.Rank))
                .ToArray();

        return new PlatformStoreDetailResponse(
            summary.TenantId,
            summary.Name,
            summary.Slug,
            summary.Status,
            summary.OwnerUserId,
            summary.OwnerEmail,
            summary.PrimaryVertical,
            summary.PrimaryVerticalCode,
            summary.PlanCode,
            summary.PlanName,
            summary.SubscriptionStatus,
            summary.BillingCycle,
            summary.PendingRequests,
            summary.CreatedAtUtc,
            summary.UpdatedAtUtc,
            teamSize,
            productsCount,
            ordersCount,
            availablePlans,
            availableVerticals,
            capabilities,
            recentAudit);
    }

    private static async Task<Tenant?> FindTenantAsync(
        Guid tenantId,
        MarketDbContext dbContext,
        bool tracking,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty)
        {
            return null;
        }

        var id = TenantId.From(tenantId);

        IQueryable<Tenant> query =
            dbContext
                .Set<Tenant>()
                .IgnoreQueryFilters();

        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return await query.SingleOrDefaultAsync(
            tenant =>
                tenant.Id == id &&
                !tenant.IsDeleted,
            cancellationToken);
    }

    private static async Task<PlatformStoreSummaryResponse> BuildStoreSummaryAsync(
        MarketDbContext dbContext,
        Tenant tenant,
        CancellationToken cancellationToken)
    {
        var ownerMembership =
            await dbContext
                .Set<TenantMembership>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(
                    membership =>
                        membership.TenantId == tenant.Id &&
                        membership.Role == TenantRole.Owner &&
                        !membership.IsDeleted)
                .OrderBy(membership => membership.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

        User? owner = null;

        if (ownerMembership is not null)
        {
            owner =
                await dbContext
                    .Set<User>()
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        user =>
                            user.Id == ownerMembership.UserId &&
                            !user.IsDeleted,
                        cancellationToken);
        }

        var primaryVertical =
            await dbContext
                .Set<TenantCommerceVertical>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(
                    vertical =>
                        vertical.TenantId == tenant.Id &&
                        vertical.IsPrimary &&
                        vertical.IsEnabled)
                .OrderBy(vertical => vertical.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

        string? verticalCode = null;

        if (primaryVertical is not null)
        {
            verticalCode =
                CommerceVerticalCatalog
                    .Get(primaryVertical.VerticalType)
                    .Code;
        }

        var subscription =
            await dbContext
                .Set<StoreSubscription>()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.TenantId == tenant.Id,
                    cancellationToken);

        string? planName = null;

        if (subscription is not null &&
            PlatformPlanCatalog.TryGet(subscription.PlanCode, out var plan))
        {
            planName = plan.Name;
        }

        var pendingRequests =
            await dbContext
                .Set<PlatformRequest>()
                .AsNoTracking()
                .CountAsync(
                    item =>
                        item.TenantId == tenant.Id &&
                        item.Status == PlatformRequestStatus.Pending,
                    cancellationToken);

        return new PlatformStoreSummaryResponse(
            tenant.Id.Value,
            tenant.Name,
            tenant.Slug.ToString(),
            tenant.Status.ToString(),
            ownerMembership?.UserId.Value,
            owner?.Email.ToString(),
            primaryVertical?.VerticalType.ToString(),
            verticalCode,
            subscription?.PlanCode,
            planName,
            subscription?.Status.ToString(),
            subscription?.BillingCycle.ToString(),
            pendingRequests,
            tenant.CreatedAtUtc,
            tenant.UpdatedAtUtc);
    }

    private static void AddAudit(
        MarketDbContext dbContext,
        HttpContext httpContext,
        UserId actorUserId,
        TenantId tenantId,
        string action,
        string reason,
        object? oldValue,
        object? newValue,
        DateTimeOffset occurredAtUtc)
    {
        var entry =
            PlatformAuditEntry.Create(
                actorUserId,
                tenantId,
                action,
                reason,
                oldValue is null
                    ? null
                    : JsonSerializer.Serialize(oldValue),
                newValue is null
                    ? null
                    : JsonSerializer.Serialize(newValue),
                httpContext.Connection.RemoteIpAddress?.ToString(),
                httpContext.Request.Headers.UserAgent.ToString(),
                occurredAtUtc);

        dbContext.Add(entry);
    }

    private static bool TryNormalizeReason(
        string? value,
        out string normalized,
        out IResult? error)
    {
        normalized = value?.Trim() ?? string.Empty;
        error = null;

        if (string.IsNullOrWhiteSpace(normalized))
        {
            error = InvalidRequest(
                "platform_action_reason_required",
                "A reason is required for this platform action.");

            return false;
        }

        if (normalized.Length > 500)
        {
            error = InvalidRequest(
                "platform_action_reason_too_long",
                "The platform action reason cannot exceed 500 characters.");

            return false;
        }

        return true;
    }

    private static UserId? GetActorUserId(HttpContext httpContext)
    {
        var subject =
            httpContext.User
                .FindFirst(JwtRegisteredClaimNames.Sub)?
                .Value;

        if (!Guid.TryParse(subject, out var userGuid) ||
            userGuid == Guid.Empty)
        {
            return null;
        }

        return UserId.From(userGuid);
    }

    private static IResult StoreNotFound()
    {
        return Results.NotFound(
            new
            {
                code = "platform_store_not_found",
                message = "The requested store was not found."
            });
    }

    private static IResult InvalidRequest(
        string code,
        string message)
    {
        return Results.BadRequest(
            new
            {
                code,
                message
            });
    }

    private sealed record AddPlatformAdministratorRequest(
        string Email);

    private sealed record PlatformAdministratorResponse(
        Guid UserId,
        string Email,
        string Status,
        bool EmailVerified,
        DateTimeOffset AddedAtUtc);

    private sealed record PlatformAdministratorMutationResponse(
        Guid UserId,
        string Email,
        bool AlreadyAdministrator);

    private sealed record PlatformActionRequest(string? Reason);

    private sealed record UpdateStoreIdentityRequest(
        string Name,
        string Slug,
        string? Reason);

    private sealed record ChangePrimaryVerticalRequest(
        string VerticalCode,
        string? Reason);

    private sealed record SetCapabilityOverrideRequest(
        bool? IsEnabled,
        string? Reason);

    private sealed record SetOwnerEmailRequest(
        string Email,
        string? Reason);

    private sealed record SetStoreSubscriptionRequest(
        string PlanCode,
        string BillingCycle,
        string Status,
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

    private sealed record OwnerEmailApplyResult(
        Guid? OwnerUserId,
        string? OldEmail,
        string? NewEmail,
        IResult? Error);

    private sealed record OwnerEmailResponse(
        Guid TenantId,
        Guid OwnerUserId,
        string OwnerEmail);

    private sealed record RequestStatsResponse(
        int Pending,
        int MoreInfoRequested);

    private sealed record RequestMutationResponse(
        Guid RequestId,
        string Status);

    private sealed record PlatformRequestSummaryResponse(
        Guid RequestId,
        Guid TenantId,
        string TenantName,
        string Type,
        string Status,
        string Summary,
        string? RequestedByEmail,
        DateTimeOffset RequestedAtUtc,
        DateTimeOffset? ReviewedAtUtc,
        string? ReviewReason);

    private sealed record PlatformRequestDetailResponse(
        Guid RequestId,
        Guid TenantId,
        string TenantName,
        string TenantSlug,
        string Type,
        string Status,
        string Summary,
        string PayloadJson,
        Guid RequestedByUserId,
        string? RequestedByEmail,
        DateTimeOffset RequestedAtUtc,
        DateTimeOffset? ReviewedAtUtc,
        Guid? ReviewedByUserId,
        string? ReviewedByEmail,
        string? ReviewReason);

    private sealed record PlatformPlanOptionResponse(
        string Code,
        string Name,
        int Rank);

    private sealed record StoreSubscriptionResponse(
        string PlanCode,
        string PlanName,
        string BillingCycle,
        string Status,
        DateTimeOffset StartedAtUtc,
        DateTimeOffset? EndsAtUtc);

    private sealed record StoreMutationResponse(
        Guid TenantId,
        string Status);

    private sealed record StoreIdentityResponse(
        Guid TenantId,
        string Name,
        string Slug);

    private sealed record StoreVerticalResponse(
        Guid TenantId,
        string Vertical,
        string VerticalCode);

    private sealed record PlatformStoreSummaryResponse(
        Guid TenantId,
        string Name,
        string Slug,
        string Status,
        Guid? OwnerUserId,
        string? OwnerEmail,
        string? PrimaryVertical,
        string? PrimaryVerticalCode,
        string? PlanCode,
        string? PlanName,
        string? SubscriptionStatus,
        string? BillingCycle,
        int PendingRequests,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? UpdatedAtUtc);

    private sealed record PlatformVerticalOptionResponse(
        string Vertical,
        string Code);

    private sealed record StoreCapabilityResponse(
        string Capability,
        bool DefaultEnabled,
        bool EffectiveEnabled,
        bool? OverrideValue);

    private sealed record PlatformAuditEntryResponse(
        Guid AuditEntryId,
        Guid ActorUserId,
        string Action,
        string Reason,
        string? OldValueJson,
        string? NewValueJson,
        DateTimeOffset OccurredAtUtc);

    private sealed record PlatformStoreDetailResponse(
        Guid TenantId,
        string Name,
        string Slug,
        string Status,
        Guid? OwnerUserId,
        string? OwnerEmail,
        string? PrimaryVertical,
        string? PrimaryVerticalCode,
        string? PlanCode,
        string? PlanName,
        string? SubscriptionStatus,
        string? BillingCycle,
        int PendingRequests,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? UpdatedAtUtc,
        int TeamSize,
        int ProductsCount,
        int OrdersCount,
        IReadOnlyCollection<PlatformPlanOptionResponse> AvailablePlans,
        IReadOnlyCollection<PlatformVerticalOptionResponse> AvailableVerticals,
        IReadOnlyCollection<StoreCapabilityResponse> Capabilities,
        IReadOnlyCollection<PlatformAuditEntryResponse> RecentAudit);
}
