using System.Threading.Tasks;

using MassTransit;

using Pullaroo.Contracts;

namespace Pullaroo.Common;

public abstract class CommandHandler<TCommand, TResult> : IConsumer<TCommand>
    where TCommand : class, IRequest<TResult>
    where TResult : class
{
    public async Task Consume(ConsumeContext<TCommand> context)
    {
        TResult result = await Handle(context.Message);
        await context.RespondAsync(result);
    }

    public abstract Task<TResult> Handle(TCommand command);
}
