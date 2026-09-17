using OFOQ.Market.Domain.Notifications;

namespace OFOQ.Market.Domain.Tests.Notifications;

public sealed class EmailOutboxMessageTests
{
    [Fact]
    public void Create_StartsPendingAndDueImmediately()
    {
        var now =
            DateTimeOffset.UtcNow;

        var message =
            EmailOutboxMessage.Create(
                null,
                "merchant@example.com",
                "Subject",
                "Body",
                null,
                "test",
                $"test:{Guid.NewGuid():N}",
                now);

        Assert.Equal(
            EmailOutboxState.Pending,
            message.State);

        Assert.Equal(
            now,
            message.NextAttemptAtUtc);

        Assert.Equal(
            0,
            message.Attempts);
    }

    [Fact]
    public void MarkSent_EndsRetryLifecycle()
    {
        var now =
            DateTimeOffset.UtcNow;

        var message =
            EmailOutboxMessage.Create(
                null,
                "merchant@example.com",
                "Subject",
                "Body",
                null,
                "test",
                $"test:{Guid.NewGuid():N}",
                now);

        message.MarkSent(
            now.AddMinutes(1));

        Assert.Equal(
            EmailOutboxState.Sent,
            message.State);

        Assert.NotNull(
            message.SentAtUtc);
    }
}
