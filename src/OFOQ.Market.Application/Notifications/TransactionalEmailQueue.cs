using System.Security.Cryptography;
using System.Text;
using OFOQ.Market.Application.Common.Notifications;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Notifications;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Notifications;

public sealed class TransactionalEmailQueue :
    ITransactionalEmailQueue
{
    private readonly IEmailOutboxRepository _outboxRepository;
    private readonly ITenantNotificationPreferencesRepository _preferencesRepository;
    private readonly ITenantMembershipRepository _membershipRepository;
    private readonly IUserRepository _userRepository;

    public TransactionalEmailQueue(
        IEmailOutboxRepository outboxRepository,
        ITenantNotificationPreferencesRepository preferencesRepository,
        ITenantMembershipRepository membershipRepository,
        IUserRepository userRepository)
    {
        _outboxRepository = outboxRepository;
        _preferencesRepository = preferencesRepository;
        _membershipRepository = membershipRepository;
        _userRepository = userRepository;
    }

    public async Task QueueEmailVerificationAsync(
        UserId userId,
        string email,
        string token,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset createdAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (userId.IsEmpty)
            throw new ArgumentException(
                "User ID cannot be empty.",
                nameof(userId));

        var normalizedEmail =
            EmailAddress.Create(email).Value;

        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException(
                "Verification token is required.",
                nameof(token));

        var tokenDigest =
            Convert.ToHexString(
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(token)));

        var subject =
            "تأكيد بريدك الإلكتروني في ركن";

        var textBody =
            $"رمز التحقق من البريد الإلكتروني:\n{token}\n\nينتهي الرمز في {expiresAtUtc:O}.";

        var htmlBody =
            $"<div dir=\"rtl\"><h2>تأكيد البريد الإلكتروني</h2><p>استخدم الرمز التالي لإكمال التحقق:</p><p style=\"word-break:break-all;font-family:monospace\">{System.Net.WebUtility.HtmlEncode(token)}</p><p>ينتهي في {expiresAtUtc:O}</p></div>";

        await _outboxRepository.AddAsync(
            EmailOutboxMessage.Create(
                tenantId: null,
                normalizedEmail,
                subject,
                textBody,
                htmlBody,
                kind: "email_verification",
                dedupeKey:
                    $"email-verification:{userId.Value:N}:{tokenDigest}",
                createdAtUtc),
            cancellationToken);
    }

    public async Task QueueNewOrderAsync(
        TenantId tenantId,
        OrderId orderId,
        decimal totalAmount,
        string currencyCode,
        DateTimeOffset createdAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (tenantId.IsEmpty)
            throw new ArgumentException(
                "Tenant ID cannot be empty.",
                nameof(tenantId));

        if (orderId.IsEmpty)
            throw new ArgumentException(
                "Order ID cannot be empty.",
                nameof(orderId));

        var preferences =
            await _preferencesRepository.GetAsync(
                cancellationToken);

        if (preferences is not null &&
            !preferences.NewOrderEmailEnabled)
        {
            return;
        }

        var memberships =
            await _membershipRepository.GetByTenantIdAsync(
                tenantId,
                cancellationToken);

        var recipients =
            memberships
                .Where(
                    membership =>
                        membership.Role == TenantRole.Owner ||
                        membership.Role == TenantRole.Admin)
                .OrderBy(
                    membership =>
                        membership.Role == TenantRole.Owner
                            ? 0
                            : 1)
                .ToArray();

        if (recipients.Length == 0)
            return;

        var seenEmails =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        foreach (var membership in recipients)
        {
            var user =
                await _userRepository.GetByIdAsync(
                    membership.UserId,
                    cancellationToken);

            if (user is null ||
                user.IsDeleted ||
                user.Status != UserStatus.Active)
            {
                continue;
            }

            var email = user.Email.Value;

            if (!seenEmails.Add(email))
                continue;

            var subject = "طلب جديد في متجرك";

            var textBody =
                $"تم إنشاء طلب جديد.\nرقم الطلب: {orderId.Value}\nالإجمالي: {totalAmount:0.00} {currencyCode}";

            var htmlBody =
                $"<div dir=\"rtl\"><h2>طلب جديد</h2><p>رقم الطلب: <strong>{orderId.Value}</strong></p><p>الإجمالي: <strong>{totalAmount:0.00} {System.Net.WebUtility.HtmlEncode(currencyCode)}</strong></p></div>";

            await _outboxRepository.AddAsync(
                EmailOutboxMessage.Create(
                    tenantId.Value,
                    email,
                    subject,
                    textBody,
                    htmlBody,
                    kind: "new_order",
                    dedupeKey:
                        $"new-order:{tenantId.Value:N}:{orderId.Value:N}:{email.ToLowerInvariant()}",
                    createdAtUtc),
                cancellationToken);
        }
    }
}
