namespace LinkKeeper.Core;

public enum LinkStatus { Active, Expired, Archived }
public sealed record LinkItem(
    string Id, string OwnerId, string OriginalUrl, string NormalizedUrl,
    string Category, string Description, string Source, DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt, DateTimeOffset? ExpiresAt, LinkStatus Status,
    DateTimeOffset? ArchivedAt);
