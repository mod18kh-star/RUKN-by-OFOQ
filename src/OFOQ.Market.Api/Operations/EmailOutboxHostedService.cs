using OFOQ.Market.Application.Common.Notifications;
using OFOQ.Market.Application.Common.Persistence;

namespace OFOQ.Market.Api.Operations;

public sealed class EmailOutboxHostedService :
    BackgroundService
{
    private static readonly TimeSpan PollDelay =
        TimeSpan.FromSeconds(10);

    private static readonly TimeSpan LeaseDuration =
        TimeSpan.FromMinutes(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<EmailOutboxHostedService> _logger;

    public EmailOutboxHostedService(
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        ILogger<EmailOutboxHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchBatchAsync(
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Email outbox dispatch failed.");
            }

            try
            {
                await Task.Delay(
                    PollDelay,
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task DispatchBatchAsync(
        CancellationToken cancellationToken)
    {
        using var scope =
            _scopeFactory.CreateScope();

        var repository =
            scope.ServiceProvider
                .GetRequiredService<
                    IEmailOutboxRepository>();

        var sender =
            scope.ServiceProvider
                .GetRequiredService<
                    IEmailSender>();

        var unitOfWork =
            scope.ServiceProvider
                .GetRequiredService<
                    IUnitOfWork>();

        var now =
            _timeProvider.GetUtcNow();

        var dueIds =
            await repository.GetDueIdsAsync(
                now,
                take: 20,
                cancellationToken);

        foreach (var id in dueIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var claimed =
                await repository.TryClaimAsync(
                    id,
                    now,
                    now.Add(LeaseDuration),
                    cancellationToken);

            if (!claimed)
                continue;

            var message =
                await repository.GetByIdAsync(
                    id,
                    cancellationToken);

            if (message is null)
                continue;

            try
            {
                await sender.SendAsync(
                    message.ToEmail,
                    message.Subject,
                    message.TextBody,
                    message.HtmlBody,
                    cancellationToken);

                message.MarkSent(
                    _timeProvider.GetUtcNow());
            }
            catch (Exception exception)
                when (exception is not OperationCanceledException)
            {
                var attempt =
                    Math.Max(
                        1,
                        message.Attempts);

                var retryDelay =
                    TimeSpan.FromMinutes(
                        Math.Min(
                            60,
                            Math.Pow(
                                2,
                                Math.Min(
                                    attempt,
                                    6))));

                message.MarkFailed(
                    exception.Message,
                    _timeProvider
                        .GetUtcNow()
                        .Add(retryDelay));

                _logger.LogWarning(
                    exception,
                    "Email outbox message {MessageId} failed.",
                    message.Id.Value);
            }

            await unitOfWork.SaveChangesAsync(
                cancellationToken);
        }
    }
}
