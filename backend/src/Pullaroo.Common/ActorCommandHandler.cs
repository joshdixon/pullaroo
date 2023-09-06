using System;
using System.Threading.Tasks;

using MassTransit;

using Microsoft.Extensions.DependencyInjection;

using Orleans;
using Orleans.Runtime;

using Pullaroo.Contracts;

namespace Pullaroo.Common;

public abstract class ActorCommandHandler<TGrain, TCommand, TResult> : IConsumer<TCommand>
    where TGrain : ActorGrain, IActorGrain
    where TCommand : class, IRequest<TResult>
    where TResult : class
{
    public async Task Consume(ConsumeContext<TCommand> context)
    {
        IServiceProvider serviceProvider;

        if (context.TryGetPayload(out IServiceScope serviceScope))
        {
            serviceProvider = serviceScope.ServiceProvider;
        }
        else if (context.TryGetPayload(out serviceProvider))
        {
            serviceProvider = serviceProvider;
        }

        var clusterClient = serviceProvider.GetRequiredService<IClusterClient>();


        Type actorInterface = typeof(TGrain).GetActorInterfaceType();
        IActorGrain grain = clusterClient.GetGrain(actorInterface, GetActorKey(context.Message)) as IActorGrain;
        
        context.TryGetHeader("SourceActorId", out string? sourceActorId);
        string actorId = $"{typeof(TGrain).Name}-{GetActorKey(context.Message)}";
        Orleans.Runtime.RequestContext.Set(nameof(actorId), actorId); 
        
        TResult result = await grain.Execute<TGrain, TCommand, TResult>(context.Message, GetType(), actorId == sourceActorId);
        await context.RespondAsync(result);
    }

    public abstract string GetActorKey(TCommand command);

    public abstract Task<TResult> Handle(TGrain actor, TCommand command);
}
