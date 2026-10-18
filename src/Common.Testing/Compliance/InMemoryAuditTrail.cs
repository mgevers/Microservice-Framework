using Common.Infrastructure.Compliance;

namespace Common.Testing.Compliance;

public class InMemoryAuditTrail : IAuditTrail
{
    private readonly List<AuditEntry> entries = [];

    public IReadOnlyList<AuditEntry> Entries
    {
        get
        {
            lock (entries)
            {
                return [.. entries];
            }
        }
    }

    public Task Record(IReadOnlyCollection<AuditEntry> newEntries, CancellationToken cancellationToken = default)
    {
        lock (entries)
        {
            entries.AddRange(newEntries);
        }

        return Task.CompletedTask;
    }

    public void Clear()
    {
        lock (entries)
        {
            entries.Clear();
        }
    }
}
