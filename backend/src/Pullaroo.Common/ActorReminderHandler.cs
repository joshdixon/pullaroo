using System;
using System.Threading.Tasks;

using MassTransit;

using Microsoft.Extensions.DependencyInjection;

using Orleans;

namespace Pullaroo.Common;

public abstract class ActorReminderHandler<TGrain, TReminder> : IConsumer<TReminder>
    where TGrain : ActorGrain, IActorGrain
    where TReminder : class
{
    public async Task Consume(ConsumeContext<TReminder> context)
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
        string actorKey = context.GetHeader("ActorKey") ?? throw new InvalidOperationException("ActorKey header is missing");
        Type actorInterface = typeof(TGrain).GetActorInterfaceType();
        var grain = clusterClient.GetGrain(actorInterface, actorKey) as IActorGrain;
        await grain.Execute<TGrain, TReminder>(GetType());
    }

    public abstract Task Handle(TGrain actor);
}
