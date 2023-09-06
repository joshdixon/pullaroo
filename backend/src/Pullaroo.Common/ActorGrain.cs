using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;
using Orleans.Serialization.Invocation;

using Pullaroo.Contracts;

namespace Pullaroo.Common;

public interface IActorGrain : IGrainWithStringKey
{
    Task<TResult> Execute<TGrain, TCommand, TResult>(TCommand command, Type handlerType, bool interleave = false)
        where TGrain : ActorGrain, IGrainWithStringKey, IActorGrain
        where TCommand : class, IRequest<TResult>
        where TResult : class;

    Task Execute<TGrain, TReminder>(Type handlerType, bool interleave = false)
        where TGrain : ActorGrain,  IGrainWithStringKey, IActorGrain
        where TReminder : class;
}

[MayInterleave(nameof(MayInterleave))]
public abstract class ActorGrain : Grain, IActorGrain, IRemindable
{
    private readonly Dictionary<string, IDisposable> _timers = new();
    
    public static bool MayInterleave(IInvokable req) => req.GetArgument(req.GetArgumentCount() - 1) is true;

    private string GetActorId() => $"{GetType().Name}-{this.GetPrimaryKeyString()}";
    
    public override Task OnActivateAsync(CancellationToken cancellationToken) => base.OnActivateAsync(cancellationToken);
    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken) => base.OnDeactivateAsync(reason, cancellationToken);

    public virtual async Task<TResult> Execute<TGrain, TCommand, TResult>(TCommand command, Type handlerType, bool interleave = false)
        where TGrain : ActorGrain, IGrainWithStringKey, IActorGrain
        where TCommand : class, IRequest<TResult>
        where TResult : class
    {
        using IServiceScope scope = ServiceProvider.CreateScope();
        object? handler = scope.ServiceProvider.GetService(handlerType);
        
        if (handler is null)
        {
            throw new InvalidOperationException($"Could not create instance of {handlerType}");
        }
        
        if (handler is not ActorCommandHandler<TGrain, TCommand, TResult> typedHandler)
        {
            throw new InvalidOperationException($"Handler {handlerType} is not of type {typeof(ActorCommandHandler<TGrain, TCommand, TResult>)}");
        }

        if (this is TGrain grain)
        {
            return await typedHandler.Handle(grain, command);
        }

        throw new InvalidOperationException($"Grain {GetType()} is not of type {typeof(TGrain)}");
    }
    
    public virtual async Task Execute<TGrain, TReminder>(Type handlerType, bool interleave = false)
        where TGrain : ActorGrain, IGrainWithStringKey, IActorGrain
        where TReminder : class
    {
        object? handler = ServiceProvider.GetService(handlerType);
        
        if (handler is null)
        {
            throw new InvalidOperationException($"Could not create instance of {handlerType}");
        }
        
        if (handler is not ActorReminderHandler<TGrain, TReminder> typedHandler)
        {
            throw new InvalidOperationException($"Handler {handlerType} is not of type {typeof(ActorReminderHandler<TGrain, TReminder>)}");
        }

        if (this is TGrain grain)
        {
            await typedHandler.Handle(grain);
        }

        throw new InvalidOperationException($"Grain {GetType()} is not of type {typeof(TGrain)}");
    }

    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        if (status.Period < TimeSpan.FromMinutes(5))
        {
            TimeSpan period = TimeSpan.Parse(reminderName.Split(" ").Last());
            reminderName = reminderName.Split(" ").First();
            
            if (!_timers.ContainsKey(reminderName))
            {
                IDisposable timer = this.RegisterTimer(
                    _ => ReceiveTimer(reminderName), 
                    null, 
                    period, 
                    period) ?? throw new InvalidOperationException("Could not register timer");
                
                _timers.Add(reminderName, timer);
            }
        }
        else
        {
            await ReceiveTimer(reminderName);
        }
    }

    private async Task ReceiveTimer(string reminderName)
    {
        IMediator mediator = ServiceProvider.GetRequiredService<IMediator>();
        
        object reminder = Activator.CreateInstance(Type.GetType(reminderName) ?? throw new InvalidOperationException($"Could not get type {reminderName}")) ?? throw new InvalidOperationException($"Could not create instance of {reminderName}");
        await mediator.Publish(reminder,
            pipeContext =>
            {
                pipeContext.Headers.Set("ActorKey", this.GetPrimaryKeyString());

                return Task.CompletedTask;
            });
    }

    public async Task RegisterReminder<TReminder>(TimeSpan dueTime, TimeSpan period)
    {
        string typeName = typeof(TReminder).AssemblyQualifiedName ?? throw new InvalidOperationException("Could not get assembly qualified name for type");

        
        if (_timers.TryGetValue(typeName, out IDisposable? timerToRemove))
        {
            timerToRemove.Dispose();
            _timers.Remove(typeName);
        }
        
        if (period < TimeSpan.FromMinutes(5))
        {
            DateTime start = DateTime.UtcNow;
            IDisposable timer = this.RegisterTimer(
                _ => ReceiveTimer(typeName), 
                null, 
                dueTime, 
                period) ?? throw new InvalidOperationException("Could not register timer");
            
            _timers.Add(typeName, timer);
            
            string reminderName = $"{typeName} {period}";
            
            var existingReminders = await this.GetReminders();
            var remindersToRemove = existingReminders
                .Where(r => r.ReminderName.StartsWith($"{typeName} "));
            
            foreach (var reminder in remindersToRemove)
                await this.UnregisterReminder(reminder);
            
            await this.RegisterOrUpdateReminder(reminderName, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }
        else
        {
            await this.RegisterOrUpdateReminder(typeName, dueTime, period);

        }

    }
    
    public async Task UnregisterReminder<TReminder>()
    {
        string typeName = typeof(TReminder).AssemblyQualifiedName ?? throw new InvalidOperationException("Could not get assembly qualified name for type");
        IGrainReminder reminder = await this.GetReminder(typeName);
        await this.UnregisterReminder(reminder);
    }
}
