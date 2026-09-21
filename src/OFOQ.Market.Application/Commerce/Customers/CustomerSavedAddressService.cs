using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Commerce.Customers;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Commerce.Customers;

public sealed class CustomerSavedAddressService
{
    private readonly ICustomerSavedAddressRepository _addresses;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public CustomerSavedAddressService(
        ICustomerSavedAddressRepository addresses,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _addresses = addresses;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public Task<IReadOnlyList<CustomerSavedAddress>>
        GetAddressesAsync(
            UserId userId,
            CancellationToken cancellationToken = default)
    {
        return _addresses.GetActiveForUserAsync(
            userId,
            cancellationToken);
    }

    public async Task<CustomerSavedAddress> AddAddressAsync(
        UserId userId,
        SavedAddressDetails details,
        bool makeDefault,
        CancellationToken cancellationToken = default)
    {
        var existing = await _addresses.GetActiveForUserAsync(
            userId,
            cancellationToken);

        var now = _timeProvider.GetUtcNow();

        var address = CustomerSavedAddress.Create(
            userId,
            details,
            now);

        if (makeDefault || existing.Count == 0)
        {
            foreach (var item in existing.Where(
                x => x.IsDefault))
            {
                item.ClearDefault(now);
            }

            address.MakeDefault(now);
        }

        await _addresses.AddAsync(
            address,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return address;
    }

    public async Task<CustomerSavedAddress?>
        UpdateAddressAsync(
            UserId userId,
            Guid addressId,
            SavedAddressDetails details,
            bool makeDefault,
            CancellationToken cancellationToken = default)
    {
        var address = await _addresses.GetByIdForUserAsync(
            addressId,
            userId,
            cancellationToken);

        if (address is null)
        {
            return null;
        }

        var now = _timeProvider.GetUtcNow();

        address.Update(details, now);

        if (makeDefault)
        {
            var existing = await _addresses.GetActiveForUserAsync(
                userId,
                cancellationToken);

            foreach (var item in existing.Where(
                x => x.IsDefault && x.Id != address.Id))
            {
                item.ClearDefault(now);
            }

            address.MakeDefault(now);
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return address;
    }

    public async Task<bool> DeleteAddressAsync(
        UserId userId,
        Guid addressId,
        CancellationToken cancellationToken = default)
    {
        var address = await _addresses.GetByIdForUserAsync(
            addressId,
            userId,
            cancellationToken);

        if (address is null)
        {
            return false;
        }

        var now = _timeProvider.GetUtcNow();
        var wasDefault = address.IsDefault;

        address.Deactivate(now);

        if (wasDefault)
        {
            var existing = await _addresses.GetActiveForUserAsync(
                userId,
                cancellationToken);

            var replacement = existing.FirstOrDefault(
                x => x.Id != address.Id);

            replacement?.MakeDefault(now);
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return true;
    }

    public async Task<CustomerSavedAddress?>
        SetDefaultAsync(
            UserId userId,
            Guid addressId,
            CancellationToken cancellationToken = default)
    {
        var address = await _addresses.GetByIdForUserAsync(
            addressId,
            userId,
            cancellationToken);

        if (address is null)
        {
            return null;
        }

        var now = _timeProvider.GetUtcNow();

        var existing = await _addresses.GetActiveForUserAsync(
            userId,
            cancellationToken);

        foreach (var item in existing.Where(
            x => x.IsDefault && x.Id != address.Id))
        {
            item.ClearDefault(now);
        }

        address.MakeDefault(now);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return address;
    }
}