using Pullaroo.Contracts.StronglyTypedIds;

namespace Pullaroo.Common;

public class RequestContext : IRequestContext
{
    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.Now;
    public UserId? ActualUserId { get; set; }
    public string? ActualUserDisplayName { get; set; }
    public UserId? ImpersonatedUserId { get; set; }
    public string? ImpersonatedUserDisplayName { get; set; }
    public bool ImpersonatedKeepOwnPermissions { get; set; }

    public string ManagingChannelId { get; set; } = string.Empty;
}

public interface IRequestContext
{
    public DateTimeOffset RequestedAt { get; }
    public UserId ActualUserId { get; }
    public string ActualUserDisplayName { get; }
    public UserId? ImpersonatedUserId { get; }
    public string? ImpersonatedUserDisplayName { get; }
    public bool ImpersonatedKeepOwnPermissions { get; }
    
    public string ManagingChannelId { get; }
    
    public UserId UserId => ImpersonatedUserId ?? ActualUserId;
    public string UserDisplayName => ImpersonatedUserDisplayName ?? ActualUserDisplayName;
}
