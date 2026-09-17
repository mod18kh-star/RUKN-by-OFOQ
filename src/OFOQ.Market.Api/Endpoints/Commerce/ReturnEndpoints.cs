using System.IdentityModel.Tokens.Jwt;
using OFOQ.Market.Api.Security.Authorization;
using OFOQ.Market.Application.Commerce.Returns;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Contracts.Commerce.Returns;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Returns;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Api.Endpoints.Commerce;

public static class ReturnEndpoints
{
    public static IEndpointRouteBuilder MapReturnEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var account=endpoints.MapGroup("/api/tenants/{tenantId:guid}/account/returns").WithTags("Customer Returns").RequireAuthorization();
        account.MapGet("",GetMineAsync); account.MapPost("",CreateAsync); account.MapPost("/{id:guid}/cancel",CancelMineAsync);

        var backoffice=endpoints.MapGroup("/api/tenants/{tenantId:guid}/backoffice/returns").WithTags("Back Office Returns").RequireAuthorization(AuthorizationPolicies.TenantBackOffice);
        backoffice.MapGet("",GetAllAsync); backoffice.MapGet("/{id:guid}",GetAsync); backoffice.MapPost("/{id:guid}/approve",ApproveAsync); backoffice.MapPost("/{id:guid}/reject",RejectAsync); backoffice.MapPost("/{id:guid}/receive",ReceiveAsync); backoffice.MapPost("/{id:guid}/complete",CompleteAsync);
        return endpoints;
    }

    private static async Task<IResult> CreateAsync(CreateReturnRequest request,ReturnManagementService service,HttpContext http,CancellationToken ct)
    {
        var user=User(http); if(!user.HasValue)return Results.Unauthorized();
        try{return Results.Ok(Map(await service.CreateAsync(UserId.From(user.Value),OrderId.From(request.OrderId),request.Reason,request.Items.Select(x=>new CreateReturnItemInput(x.OrderItemId,x.Quantity)).ToArray(),ct)));}
        catch(KeyNotFoundException){return Results.NotFound();} catch(UnauthorizedAccessException){return Results.Forbid();} catch(TenantScopeViolationException){return Results.Forbid();} catch(Exception e) when(e is ArgumentException or InvalidOperationException){return Validation(e.Message);}
    }

    private static async Task<IResult> GetMineAsync(ReturnManagementService service,HttpContext http,CancellationToken ct)
    { var user=User(http); if(!user.HasValue)return Results.Unauthorized(); try{return Results.Ok((await service.GetMineAsync(UserId.From(user.Value),ct)).Select(Map).ToArray());}catch(TenantScopeViolationException){return Results.Forbid();} }

    private static async Task<IResult> CancelMineAsync(Guid id,ReturnManagementService service,HttpContext http,CancellationToken ct)
    { var user=User(http); if(!user.HasValue)return Results.Unauthorized(); try{var x=await service.CancelMineAsync(UserId.From(user.Value),ReturnRequestId.From(id),ct);return x is null?Results.NotFound():Results.Ok(Map(x));}catch(UnauthorizedAccessException){return Results.Forbid();}catch(Exception e) when(e is ArgumentException or InvalidOperationException){return Validation(e.Message);} }

    private static async Task<IResult> GetAllAsync(int? take,ReturnManagementService service,CancellationToken ct)
    { try{return Results.Ok((await service.GetAllAsync(take??100,ct)).Select(Map).ToArray());}catch(ArgumentException e){return Validation(e.Message);} }
    private static async Task<IResult> GetAsync(Guid id,ReturnManagementService service,CancellationToken ct){var x=await service.GetAsync(ReturnRequestId.From(id),ct);return x is null?Results.NotFound():Results.Ok(Map(x));}
    private static Task<IResult> ApproveAsync(Guid id,ReturnDecisionRequest r,ReturnManagementService s,HttpContext h,CancellationToken ct)=>Action(async a=>await s.ApproveAsync(ReturnRequestId.From(id),r.Note,a,ct),h);
    private static Task<IResult> RejectAsync(Guid id,ReturnDecisionRequest r,ReturnManagementService s,HttpContext h,CancellationToken ct)=>Action(async a=>await s.RejectAsync(ReturnRequestId.From(id),r.Note,a,ct),h);
    private static Task<IResult> ReceiveAsync(Guid id,ReturnManagementService s,HttpContext h,CancellationToken ct)=>Action(async a=>await s.ReceiveAndRestockAsync(ReturnRequestId.From(id),a,ct),h);
    private static Task<IResult> CompleteAsync(Guid id,ReturnDecisionRequest r,ReturnManagementService s,HttpContext h,CancellationToken ct)=>Action(async a=>await s.CompleteAsync(ReturnRequestId.From(id),r.Note,a,ct),h);

    private static async Task<IResult> Action(Func<Guid,Task<ReturnRequestResult?>> action,HttpContext h)
    { var actor=User(h); if(!actor.HasValue)return Results.Unauthorized(); try{var x=await action(actor.Value);return x is null?Results.NotFound():Results.Ok(Map(x));}catch(TenantScopeViolationException){return Results.Forbid();}catch(Exception e) when(e is ArgumentException or InvalidOperationException){return Validation(e.Message);} }

    private static Guid? User(HttpContext h)=>Guid.TryParse(h.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value,out var id)&&id!=Guid.Empty?id:null;
    private static ReturnRequestResponse Map(ReturnRequestResult x)=>new(x.Id,x.OrderId,x.CustomerUserId,x.Reason,x.Status,x.MerchantNote,x.CreatedAtUtc,x.ApprovedAtUtc,x.RejectedAtUtc,x.ReceivedAtUtc,x.CompletedAtUtc,x.CancelledAtUtc,x.Items.Select(i=>new ReturnItemResponse(i.Id,i.OrderItemId,i.ProductId,i.ProductVariantId,i.Quantity,i.RestockedQuantity,i.RestockedAtUtc)).ToArray());
    private static IResult Validation(string message)=>Results.BadRequest(new{code="return_request_invalid",message});
}
