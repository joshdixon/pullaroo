using System.Security.Claims;

using MassTransit;

using Orleans.Runtime;

using Pullaroo.Contracts.StronglyTypedIds;

using RequestContext = Pullaroo.Common.RequestContext;

namespace Pullaroo.Server;

internal class AuthorisationPublishMiddleware<T> : IFilter<PublishContext<T>> where T : class
{
    private readonly ILogger<T> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthorisationPublishMiddleware(ILogger<T> logger, IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public void Probe(ProbeContext context)
    {
    }

    public Task Send(PublishContext<T> context, IPipe<PublishContext<T>> next)
    {
        string? userId = _httpContextAccessor.HttpContext?.User?.FindFirst(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        string? userDisplayName = _httpContextAccessor.HttpContext?.User?.FindFirst(c => c.Type == ClaimTypes.Name)?.Value;
        string? timezoneOffsetMinutes = _httpContextAccessor.HttpContext?.Request.Headers["X-Timezone-Offset"];    
        
        context.Headers.Set(nameof(userId), userId);
        context.Headers.Set(nameof(userDisplayName), userDisplayName);
        context.Headers.Set(nameof(timezoneOffsetMinutes), timezoneOffsetMinutes);

        return next.Send(context);
    }
}

internal class AuthorisationConsumeMiddleware<T> : IFilter<ConsumeContext<T>> where T : class
{
    private readonly ILogger<T> _logger;

    public AuthorisationConsumeMiddleware(ILogger<T> logger)
    {
        _logger = logger;
    }

    public void Probe(ProbeContext context)
    {
    }

    public Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        string? userId = context.GetHeader(nameof(userId));
        string? userDisplayName = context.GetHeader(nameof(userDisplayName));
        string? timezoneOffsetMinutes = context.GetHeader(nameof(timezoneOffsetMinutes));
        
        var timezone = timezoneOffsetMinutes != null ? TimeSpan.FromMinutes(-int.Parse(timezoneOffsetMinutes)) : (TimeSpan?)null;
        DateTimeOffset localTime = timezone.HasValue ? DateTimeOffset.UtcNow.ToOffset(timezone.Value) : DateTimeOffset.UtcNow;
        
        Orleans.Runtime.RequestContext.Set(nameof(RequestContext), new RequestContext()
        {
            ActualUserId = userId is not null ? new UserId(userId) : null,
            ActualUserDisplayName = userDisplayName,
            RequestedAt = localTime
        });

        return next.Send(context);
    }
}
