namespace Pullaroo.Server;

public interface IDomainEvent<TState>
{
    public TState Fold(TState state);
}
