using Common.Infrastructure.Email;

namespace Common.Testing.Email;

public record SentMail(string To, string Subject, string Body);

/// <summary>Keeps the mails in memory, so that a test can read what a service sent.</summary>
public class InMemoryEmailSender : IEmailSender
{
    private readonly List<SentMail> mails = [];
    private readonly List<(int Count, TaskCompletionSource Signal)> waiters = [];

    public IReadOnlyList<SentMail> Mails
    {
        get
        {
            lock (mails)
            {
                return [.. mails];
            }
        }
    }

    /// <summary>
    /// Waits until the sender holds at least <paramref name="count"/> mails, and returns them. Use it when the mail comes
    /// from another thread, such as a consumer of the bus, a moment after the reply. It throws a
    /// <see cref="TimeoutException"/> if the mails do not come in time.
    /// </summary>
    public async Task<IReadOnlyList<SentMail>> WaitForMails(int count, TimeSpan? timeout = null)
    {
        Task wait;
        lock (mails)
        {
            if (mails.Count >= count)
            {
                return [.. mails];
            }

            var signal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            waiters.Add((count, signal));
            wait = signal.Task;
        }

        await wait.WaitAsync(timeout ?? TimeSpan.FromSeconds(10));

        return Mails;
    }

    public void Clear()
    {
        lock (mails)
        {
            mails.Clear();
        }
    }

    public Task Send(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        lock (mails)
        {
            mails.Add(new SentMail(to, subject, body));
            foreach (var waiter in waiters.Where(waiter => waiter.Count <= mails.Count).ToList())
            {
                waiter.Signal.SetResult();
                waiters.Remove(waiter);
            }
        }

        return Task.CompletedTask;
    }
}
