using System.Text;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Catalog.Products.Options.CreateValue;

public sealed class CreateProductOptionValueHandler
{
    private readonly IProductRepository
        _productRepository;

    private readonly IProductOptionRepository
        _optionRepository;

    private readonly IProductOptionValueRepository
        _valueRepository;

    private readonly ICurrentTenant
        _currentTenant;

    private readonly IUnitOfWork
        _unitOfWork;

    private readonly TimeProvider
        _timeProvider;

    public CreateProductOptionValueHandler(
        IProductRepository productRepository,
        IProductOptionRepository optionRepository,
        IProductOptionValueRepository valueRepository,
        ICurrentTenant currentTenant,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _productRepository =
            productRepository;

        _optionRepository =
            optionRepository;

        _valueRepository =
            valueRepository;

        _currentTenant =
            currentTenant;

        _unitOfWork =
            unitOfWork;

        _timeProvider =
            timeProvider;
    }

    public async Task<ProductOptionValueResult> HandleAsync(
        CreateProductOptionValueCommand command,
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

        var option =
            await _optionRepository
                .GetByIdAsync(
                    command.OptionId,
                    cancellationToken);

        if (option is null ||
            option.ProductId != product.Id)
        {
            throw new ProductOptionNotFoundException(
                command.OptionId);
        }

        var normalizedValue =
            NormalizeValue(
                command.Value);

        if (await _valueRepository
            .ValueExistsAsync(
                option.Id,
                normalizedValue,
                excludingValueId: null,
                cancellationToken))
        {
            throw new ProductOptionValueAlreadyExistsException(
                command.Value);
        }

        var value =
            ProductOptionValue.Create(
                tenantId,
                product.Id,
                option.Id,
                command.Value,
                command.SortOrder,
                _timeProvider.GetUtcNow(),
                command.ActorUserId.Value);

        await _valueRepository
            .AddAsync(
                value,
                cancellationToken);

        await _unitOfWork
            .SaveChangesAsync(
                cancellationToken);

        return new ProductOptionValueResult(
            value.Id,
            value.Value,
            value.SortOrder);
    }

    private OFOQ.Market.Domain.Tenancy.TenantId
        GetRequiredTenantId()
    {
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue)
        {
            throw new TenantScopeViolationException(
                "A tenant context is required to create an option value.");
        }

        return _currentTenant.TenantId.Value;
    }

    private static string NormalizeValue(
        string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            value);

        return value
            .Trim()
            .Normalize(
                NormalizationForm.FormKC)
            .ToLowerInvariant();
    }
}