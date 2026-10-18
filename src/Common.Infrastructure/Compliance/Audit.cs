using Common.LanguageExtensions.TestableAlternatives;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Common.Infrastructure.Compliance;

/// <summary>
/// Who did what and when. The subject is what the action touched, such as a tag or an account id.
/// It never holds a value or personal data, so that the trail holds the least possible data.
/// </summary>
public class AuditEntry
{
    public Guid Id { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public required string Actor { get; set; }

    public required string Action { get; set; }

    public string? Subject { get; set; }

    public static AuditEntry For(string actor, string action, string? subject) => new()
    {
        Id = Guid.NewGuid(),
        OccurredAt = CurrentTime.UtcNow,
        Actor = actor,
        Action = action,
        Subject = subject
    };
}

public interface IAuditTrail
{
    Task Record(IReadOnlyCollection<AuditEntry> entries, CancellationToken cancellationToken = default);
}

/// <summary>The caller of the current request. The API reads it from the access token.</summary>
public interface ICurrentActor
{
    string Id { get; }
}

/// <summary>A request that is recorded in the audit trail.</summary>
public interface IAuditedRequest
{
    string Action { get; }

    string? Subject { get; }
}

/// <summary>
/// Records access before the request runs. If the trail cannot be written, the request fails,
/// so that no data is read without a record.
/// </summary>
public class AuditBehavior<TRequest, TResponse>(IAuditTrail audit, ICurrentActor actor) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is IAuditedRequest audited)
        {
            await audit.Record([AuditEntry.For(actor.Id, audited.Action, audited.Subject)], cancellationToken);
        }

        return await next(cancellationToken);
    }
}

/// <summary>Writes the audit entries into the <see cref="DbContext"/> of the service, in the table that <see cref="AuditModelBuilderExtensions.ConfigureAuditEntries"/> maps.</summary>
public class EfAuditTrail(DbContext dbContext) : IAuditTrail
{
    public async Task Record(IReadOnlyCollection<AuditEntry> entries, CancellationToken cancellationToken = default)
    {
        dbContext.Set<AuditEntry>().AddRange(entries);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public static class AuditModelBuilderExtensions
{
    public const int MaxActorLength = 256;
    public const int MaxActionLength = 32;

    /// <param name="maxSubjectLength">The length of the subject column.</param>
    /// <param name="subjectColumn">The column of the subject. GameStats keeps its older name, "Tag".</param>
    public static ModelBuilder ConfigureAuditEntries(this ModelBuilder modelBuilder, int maxSubjectLength, string subjectColumn = "Subject")
    {
        modelBuilder.Entity<AuditEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Actor).HasMaxLength(MaxActorLength);
            entity.Property(e => e.Action).HasMaxLength(MaxActionLength);
            entity.Property(e => e.Subject).HasMaxLength(maxSubjectLength).HasColumnName(subjectColumn);
            entity.HasIndex(e => e.OccurredAt);
        });

        return modelBuilder;
    }
}
