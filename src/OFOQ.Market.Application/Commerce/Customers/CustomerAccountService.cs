using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Customers;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Commerce.Customers;

public sealed record CustomerProfileResult(Guid UserId, string? DisplayName, string? Phone, bool IsBlocked, string? MerchantNotes);
public sealed record CustomerAddressResult(Guid AddressId, string Label, string RecipientName, string Phone, string CountryCode, string? Region, string City, string? PostalCode, string Line1, string? Line2, bool IsDefault, bool IsActive);

public sealed class CustomerAccountService
{
    private readonly ICurrentTenant _currentTenant;
    private readonly ICustomerProfileRepository _profiles;
    private readonly ICustomerAddressRepository _addresses;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public CustomerAccountService(ICurrentTenant currentTenant, ICustomerProfileRepository profiles, ICustomerAddressRepository addresses, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _currentTenant = currentTenant;
        _profiles = profiles;
        _addresses = addresses;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<CustomerProfileResult> GetOrCreateProfileAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        EnsureTenant();
        var profile = await _profiles.GetByUserIdAsync(userId, cancellationToken);
        if (profile is null)
        {
            profile = CustomerProfile.Create(_currentTenant.TenantId!.Value, userId, null, null, _timeProvider.GetUtcNow(), userId.Value);
            await _profiles.AddAsync(profile, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        return Map(profile);
    }

    public async Task<CustomerProfileResult> UpdateProfileAsync(UserId userId, string? displayName, string? phone, CancellationToken cancellationToken = default)
    {
        EnsureTenant();
        var profile = await _profiles.GetByUserIdAsync(userId, cancellationToken);
        var now = _timeProvider.GetUtcNow();
        if (profile is null)
        {
            profile = CustomerProfile.Create(_currentTenant.TenantId!.Value, userId, displayName, phone, now, userId.Value);
            await _profiles.AddAsync(profile, cancellationToken);
        }
        else profile.UpdateSelf(displayName, phone, now, userId.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(profile);
    }

    public async Task<IReadOnlyList<CustomerAddressResult>> GetAddressesAsync(UserId userId, CancellationToken cancellationToken = default)
        => (await _addresses.GetForUserAsync(userId, cancellationToken)).Select(Map).ToArray();

    public async Task<CustomerAddressResult> AddAddressAsync(UserId userId, string label, string recipientName, string phone, string countryCode, string? region, string city, string? postalCode, string line1, string? line2, bool isDefault, CancellationToken cancellationToken = default)
    {
        EnsureTenant();
        var existing = await _addresses.GetForUserAsync(userId, cancellationToken);
        var now = _timeProvider.GetUtcNow();
        var makeDefault = isDefault || existing.Count == 0;
        if (makeDefault)
            foreach (var item in existing.Where(x => x.IsDefault)) item.ClearDefault(now, userId.Value);
        var address = CustomerAddress.Create(_currentTenant.TenantId!.Value, userId, label, recipientName, phone, countryCode, region, city, postalCode, line1, line2, makeDefault, now, userId.Value);
        await _addresses.AddAsync(address, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(address);
    }

    public async Task<CustomerAddressResult?> UpdateAddressAsync(UserId userId, CustomerAddressId addressId, string label, string recipientName, string phone, string countryCode, string? region, string city, string? postalCode, string line1, string? line2, bool makeDefault, CancellationToken cancellationToken = default)
    {
        EnsureTenant();
        var address = await _addresses.GetByIdForUserAsync(addressId, userId, cancellationToken);
        if (address is null) return null;
        var now = _timeProvider.GetUtcNow();
        address.Update(label, recipientName, phone, countryCode, region, city, postalCode, line1, line2, now, userId.Value);
        if (makeDefault)
        {
            var all = await _addresses.GetForUserAsync(userId, cancellationToken);
            foreach (var item in all.Where(x => x.Id != address.Id && x.IsDefault)) item.ClearDefault(now, userId.Value);
            address.MakeDefault(now, userId.Value);
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(address);
    }

    public async Task<bool> DeleteAddressAsync(UserId userId, CustomerAddressId addressId, CancellationToken cancellationToken = default)
    {
        EnsureTenant();
        var address = await _addresses.GetByIdForUserAsync(addressId, userId, cancellationToken);
        if (address is null) return false;
        var now = _timeProvider.GetUtcNow();
        var wasDefault = address.IsDefault;
        address.Deactivate(now, userId.Value);
        if (wasDefault)
        {
            var replacement = (await _addresses.GetForUserAsync(userId, cancellationToken)).FirstOrDefault(x => x.Id != address.Id && x.IsActive);
            replacement?.MakeDefault(now, userId.Value);
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private void EnsureTenant()
    {
        if (!_currentTenant.IsAvailable || !_currentTenant.TenantId.HasValue)
            throw new TenantScopeViolationException("Tenant context is required.");
    }

    private static CustomerProfileResult Map(CustomerProfile x) => new(x.UserId.Value, x.DisplayName, x.Phone, x.IsBlocked, x.MerchantNotes);
    private static CustomerAddressResult Map(CustomerAddress x) => new(x.Id.Value, x.Label, x.RecipientName, x.Phone, x.CountryCode, x.Region, x.City, x.PostalCode, x.Line1, x.Line2, x.IsDefault, x.IsActive);
}
