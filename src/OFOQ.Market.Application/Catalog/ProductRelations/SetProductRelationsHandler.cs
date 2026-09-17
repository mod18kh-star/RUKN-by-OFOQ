using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Catalog.ProductRelations;

public sealed class SetProductRelationsHandler
{
    private const int MaximumRelationsPerType = 24;

    private readonly IProductRepository _products;
    private readonly IProductRelationRepository _relations;
    private readonly ICurrentTenant _currentTenant;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public SetProductRelationsHandler(
        IProductRepository products,
        IProductRelationRepository relations,
        ICurrentTenant currentTenant,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _products = products;
        _relations = relations;
        _currentTenant = currentTenant;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<bool> HandleAsync(
        SetProductRelationsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Relations);

        EnsureTenant();

        if (!Enum.IsDefined(command.Type))
            throw new ArgumentOutOfRangeException(nameof(command.Type));

        if (command.Relations.Count > MaximumRelationsPerType)
        {
            throw new ArgumentException(
                $"A product cannot have more than {MaximumRelationsPerType} relations of one type.");
        }

        if (await _products.GetByIdAsync(command.ProductId, cancellationToken) is null)
            return false;

        var duplicate =
            command.Relations
                .GroupBy(item => item.TargetProductId)
                .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
            throw new ArgumentException("A target product can appear only once per relation type.");

        foreach (var input in command.Relations)
        {
            if (input.TargetProductId == command.ProductId)
                throw new ArgumentException("A product cannot be related to itself.");

            if (await _products.GetByIdAsync(input.TargetProductId, cancellationToken) is null)
            {
                throw new ArgumentException(
                    $"Related product '{input.TargetProductId.Value}' was not found.");
            }
        }

        var existing =
            await _relations.GetBySourceProductIdAndTypeAsync(
                command.ProductId,
                command.Type,
                cancellationToken);

        if (existing.Count > 0)
            _relations.RemoveRange(existing);

        var now = _timeProvider.GetUtcNow();

        var replacements =
            command.Relations
                .Select((input, index) =>
                    ProductRelation.Create(
                        _currentTenant.TenantId!.Value,
                        command.ProductId,
                        input.TargetProductId,
                        command.Type,
                        index,
                        input.IsVisible,
                        now,
                        command.ActorUserId.Value))
                .ToArray();

        if (replacements.Length > 0)
            await _relations.AddRangeAsync(replacements, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    private void EnsureTenant()
    {
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue ||
            _currentTenant.TenantId.Value.IsEmpty)
        {
            throw new TenantScopeViolationException(
                "A tenant context is required to manage product relations.");
        }
    }
}
