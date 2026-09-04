using System.Text;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Catalog.Products.Options.CreateOption;

public sealed class CreateProductOptionHandler
{
    private readonly IProductRepository
        _productRepository;

    private readonly IProductOptionRepository
        _optionRepository;

    private readonly ICurrentTenant
        _currentTenant;

    private readonly IUnitOfWork
        _unitOfWork;

    private readonly TimeProvider
        _timeProvider;

    public CreateProductOptionHandler(
        IProductRepository productRepository,
        IProductOptionRepository optionRepository,
        ICurrentTenant currentTenant,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _productRepository =
            productRepository;

        _optionRepository =
            optionRepository;

        _currentTenant =
            currentTenant;

        _unitOfWork =
            unitOfWork;

        _timeProvider =
            timeProvider;
    }

    public async Task<ProductOptionResult> HandleAsync(
        CreateProductOptionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        var tenantId =
            GetRequiredTenantId();

        var product =
            await _productRepository
                .GetByIdAsync(
                    command.ProductId,
                    cancellationToken);

        if (product is null)
        {
            throw new ProductOptionProductNotFoundException(
                command.ProductId);
        }

        var normalizedName =
            NormalizeName(
                command.Name);

        if (await _optionRepository
            .NameExistsAsync(
                product.Id,
                normalizedName,
                excludingOptionId: null,
                cancellationToken))
        {
            throw new ProductOptionNameAlreadyExistsException(
                command.Name);
        }

        var option =
            ProductOption.Create(
                tenantId,
                product.Id,
                command.Name,
                command.SortOrder,
                _timeProvider.GetUtcNow(),
                command.ActorUserId.Value);

        await _optionRepository
            .AddAsync(
                option,
                cancellationToken);

        await _unitOfWork
            .SaveChangesAsync(
                cancellationToken);

        return new ProductOptionResult(
            option.Id,
            option.Name,
            option.SortOrder,
            []);
    }

    private OFOQ.Market.Domain.Tenancy.TenantId
        GetRequiredTenantId()
    {
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue)
        {
            throw new TenantScopeViolationException(
                "A tenant context is required to create a product option.");
        }

        return _currentTenant.TenantId.Value;
    }

    private static string NormalizeName(
        string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            name);

        return name
            .Trim()
            .Normalize(
                NormalizationForm.FormKC)
            .ToLowerInvariant();
    }
}