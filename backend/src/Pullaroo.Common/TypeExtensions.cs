namespace Pullaroo.Common;

internal static class TypeExtensions
{
    public static Type GetActorInterfaceType(this Type grainType)
    {
        return grainType.GetInterfaces().Single(i => i.GetInterfaces().Contains(typeof(IActorGrain)));
    }
}
