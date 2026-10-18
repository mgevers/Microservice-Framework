using Common.Infrastructure.Compliance;
using Common.Testing.Persistence;

namespace Common.Testing.Compliance;

/// <summary>
/// An <see cref="IAuditTrail"/> that writes the entries into <see cref="FakeDatabase"/>, so that a fluent test compares
/// them with the database state. Build the expected entry with <see cref="Entry"/>.
/// </summary>
public class FakeAuditTrail : IAuditTrail
{
    public Task Record(IReadOnlyCollection<AuditEntry> entries, CancellationToken cancellationToken = default)
    {
        foreach (var entry in entries)
        {
            FakeDatabase.AddEntity(Entry(entry.Actor, entry.Action, entry.Subject));
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// The entry as the fake stores it. The id and the time of a real entry are new on every call,
    /// so the fake sets them to fixed values, and a test can compare the entry.
    /// </summary>
    public static AuditEntry Entry(string actor, string action, string? subject) => new()
    {
        Id = Guid.Empty,
        OccurredAt = default,
        Actor = actor,
        Action = action,
        Subject = subject,
    };
}
