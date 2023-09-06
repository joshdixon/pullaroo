using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Pullaroo.Contracts.StronglyTypedIds;

public static class StronglyTypedIdHelper
{
    private static readonly ConcurrentDictionary<Type, Func<string, StronglyTypedId>> _stronglyTypedIdFactories = new();

    public static Func<string, StronglyTypedId> GetFactory(Type stronglyTypedIdType)
    {
        return _stronglyTypedIdFactories.GetOrAdd(stronglyTypedIdType, CreateFactory);
    }

    private static Func<string, StronglyTypedId> CreateFactory(Type stronglyTypedIdType)
    {
        var ctor = stronglyTypedIdType.GetConstructor(new[] { typeof(string) });
        if (ctor is null)
            throw new ArgumentException($"Type '{stronglyTypedIdType}' doesn't have a constructor with a single string parameter", nameof(stronglyTypedIdType));

        var param = Expression.Parameter(typeof(string), "value");
        var body = Expression.New(ctor, param);
        var lambda = Expression.Lambda<Func<string, StronglyTypedId>>(body, param);
        return lambda.Compile();
    }

    public static bool IsStronglyTypedId(Type type)
    {
        return type.IsSubclassOf(typeof(StronglyTypedId));
    }
}
