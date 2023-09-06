using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Eventuous;

using Microsoft.Extensions.DependencyInjection;

using Orleans;

using Pullaroo.Server;

namespace Pullaroo.Common;

public abstract class AggregateGrain<TState> : ActorGrain, IGrain
    where TState : State<TState>, new()
{
    /// <summary>
    /// The collection of previously persisted events
    /// </summary>
    public IDomainEvent<TState>[] Original { get; protected set; } = Array.Empty<IDomainEvent<TState>>();
    
    /// <summary>
    /// Get the list of pending changes (new events) within the scope of the current operation.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent<TState>> Changes => _changes.AsReadOnly();
    
    /// <summary>
    /// A collection with all the aggregate events, previously persisted and new
    /// </summary>
    public IEnumerable<IDomainEvent<TState>> Current => Original.Concat(_changes);
    
    /// <summary>
    /// The original version is the aggregate version we got from the store.
    /// It is used for optimistic concurrency, to check if there were no changes made to the
    /// aggregate state between load and save for the current operation.
    /// </summary>
    public int OriginalVersion => Original.Length - 1;

    /// <summary>
    /// The current version is set to the original version when the aggregate is loaded from the store.
    /// It should increase for each state transition performed within the scope of the current operation.
    /// </summary>
    public int CurrentVersion => OriginalVersion + Changes.Count;
    
    public TState State { get; private set; }

    private readonly IEventStore _store;
    private readonly List<IDomainEvent<TState>> _changes = new();

    public AggregateGrain()
    {
        _store = ServiceProvider.GetRequiredService<IEventStore>();
    }
    private void AddChange<TEvent>(TEvent @event) where TEvent : IDomainEvent<TState> => _changes.Add(@event);

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        FoldedEventStream<TState> loadedState;
        try
        {
            loadedState = await  _store.LoadStateOrNew<TState>(GetStreamName(), cancellationToken);
        }
        catch (Exception e)
        {
            loadedState = new FoldedEventStream<TState>(GetStreamName(), ExpectedStreamVersion.NoStream, Array.Empty<object>());
        }

        State = loadedState.State;
        Original = loadedState.Events.Cast<IDomainEvent<TState>>().ToArray();
    }
    
    private StreamName GetStreamName() => new($"{GetType().Name}-{this.GetPrimaryKeyString()}");
    
    public (TState PreviousState, TState CurrentState) Apply<TEvent>(TEvent @event)
        where TEvent : IDomainEvent<TState>
    {
        AddChange(@event);
        TState previous = State;
        State = @event.Fold(State);
        return (previous, State);
    }

    public override async Task<TResult> Execute<TGrain, TCommand, TResult>(TCommand command, Type handlerType, bool interleave = false)
    {
        TResult result = await base.Execute<TGrain, TCommand, TResult>(command, handlerType, interleave);

        await Save();

        return result;
    }

    public async Task Save()
    {
        await _store.AppendEvents(GetStreamName(),
            new ExpectedStreamVersion(OriginalVersion),
            Changes.Select((o, i) => ToStreamEvent(o, i + OriginalVersion)).ToArray(),
            CancellationToken.None);

        Original = Current.ToArray();
        _changes.Clear();

        StreamEvent ToStreamEvent(object evt, int position)
        {
            var streamEvent = new StreamEvent(Guid.NewGuid(), evt, new Metadata(), "", position);
            return streamEvent;
        }
    }
}
