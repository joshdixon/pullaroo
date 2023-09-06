using System.Diagnostics;
using System.Reflection;

namespace Pullaroo.Server;

internal class ServiceActivitySource
{
    private static readonly AssemblyName _assemblyName
        = Assembly.GetEntryAssembly()?.GetName() ?? throw new Exception("Could not get entry assembly name");

    internal static readonly ActivitySource ActivitySource
        = new ActivitySource(_assemblyName.Name.Replace(".", "_"), _assemblyName.Version.ToString());
}

public class ServiceActivity : IDisposable
{
    private IDisposable? _activity;
    private IDisposable _tagScope;

    public ServiceActivity(ILogger logger,
        string className,
        [System.Runtime.CompilerServices.CallerMemberName]
        string memberName = "",
        params (string Key, object? Value)[] customTags)
    {
        _activity = ServiceActivitySource.ActivitySource.StartActivity($"{className}.{memberName}");

        Dictionary<string, object?> tags = new Dictionary<string, object?>()
        {
            { "Class", className },
            { "Method", memberName }
        };

        if (customTags?.Any() == true)
        {
            foreach ((string Key, object? Value) tag in customTags)
            {
                tags.Add(tag.Key, tag.Value);
            }
        }

        _tagScope = logger.BeginScope(tags);
    }

    public ServiceActivity(ILogger logger,
        string className,
        [System.Runtime.CompilerServices.CallerMemberName]
        string memberName = "",
        bool isRoot = false,
        params (string Key, object? Value)[] customTags)
    {
        _activity = ServiceActivitySource.ActivitySource.StartRootActivity($"{className}.{memberName}");

        Dictionary<string, object?> tags = new Dictionary<string, object?>()
        {
            { "Class", className },
            { "Method", memberName }
        };

        if (customTags?.Any() == true)
        {
            foreach ((string Key, object? Value) tag in customTags)
            {
                tags.Add(tag.Key, tag.Value);
            }
        }

        _tagScope = logger.BeginScope(tags);
    }

    public void Dispose()
    {
        _activity?.Dispose();
        _tagScope?.Dispose();
    }
}

public static class ActivitySourceExtensions
{
    public static RootActivity StartRootActivity(this ActivitySource source,
        string name,
        ActivityKind kind = ActivityKind.Internal,
        IEnumerable<KeyValuePair<string, object?>>? tags = null)
    {
        var parent = Activity.Current;
        Activity.Current = null;
        var next = source.StartActivity(name,
            kind,
            parentContext: default,
            tags: tags,
            links: parent is null ? null : new[] { new ActivityLink(parent.Context) });

        return new RootActivity(next, parent);
    }
}

public class RootActivity : IDisposable
{
    public Activity Activity { get; }
    public Activity ParentActivity { get; }

    public RootActivity(Activity activity, Activity parentActivity)
    {
        Activity = activity;
        ParentActivity = parentActivity;
    }

    private bool _disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                Activity?.Dispose();
                Activity.Current = ParentActivity;
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
