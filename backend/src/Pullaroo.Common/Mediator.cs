using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

using MassTransit;

using Orleans.FluentResults;
using Orleans.Runtime;

using Pullaroo.Contracts;

namespace Pullaroo.Common;

public class Mediator : IMediator
{
    private static readonly ConcurrentDictionary<Type, RequestHandlerBase> _requestHandlers = new();
    private readonly IBus _masstransitBus;

    private readonly MassTransit.Mediator.IMediator _masstransitMediator;

    public Mediator(MassTransit.Mediator.IMediator masstransitMediator, IBus masstransitBus)
    {
        _masstransitMediator = masstransitMediator;
        _masstransitBus = masstransitBus;
    }

    public async Task Publish(object message, Func<PublishContext, Task>? configure = null, CancellationToken cancelToken = default)
    {
        bool isLocal = MessagingConfiguration.ConsumerMessageTypes.Contains(message.GetType());
        
        IPublishEndpoint publishEndpoint = isLocal ? _masstransitMediator : _masstransitBus;

        await publishEndpoint.Publish(message,
            pipe =>
            {
                pipe.Headers.Set("SourceActorId", (string?)Orleans.Runtime.RequestContext.Get("actorId"));

                return Task.CompletedTask;
            },
            cancelToken);
    }

    public async Task<Result> Send(IRequest<Result> request, CancellationToken cancelToken = default)
    {
        Type requestType = request.GetType();

        var handler = (RequestHandlerWrapper)_requestHandlers.GetOrAdd(requestType,
            static t => (RequestHandlerBase)(Activator.CreateInstance(
                    typeof(RequestHandlerWrapperImpl<>).MakeGenericType(t))
                ?? throw new InvalidOperationException(
                    $"Could not create wrapper type for {t}")));

        return await handler.Send(request, _masstransitMediator, _masstransitBus, cancelToken);
    }

    public async Task<Result<TResult>> Send<TResult>(IRequest<Result<TResult>> request,
        CancellationToken cancelToken = default)
    {
        Type requestType = request.GetType();

        var handler = (RequestHandlerWrapper<TResult>)_requestHandlers.GetOrAdd(requestType,
            static t => (RequestHandlerBase)(Activator.CreateInstance(
                    typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(t,
                        typeof(TResult)))
                ?? throw new InvalidOperationException(
                    $"Could not create wrapper type for {t}")));

        return await handler.Send(request, _masstransitMediator, _masstransitBus, cancelToken);
    }
}

internal abstract class RequestHandlerBase
{
    public abstract Task<object?> Send(object request,
        MassTransit.Mediator.IMediator mediator,
        IBus bus,
        CancellationToken cancelToken);
}

internal abstract class RequestHandlerWrapper<TResult> : RequestHandlerBase
{
    public abstract Task<Result<TResult>> Send(IRequest<Result<TResult>> request,
        MassTransit.Mediator.IMediator mediator,
        IBus bus,
        CancellationToken cancelToken);
}

internal abstract class RequestHandlerWrapper : RequestHandlerBase
{
    public abstract Task<Result> Send(IRequest<Result> request,
        MassTransit.Mediator.IMediator mediator,
        IBus bus,
        CancellationToken cancelToken);
}

internal class RequestHandlerWrapperImpl<TRequest> : RequestHandlerWrapper
    where TRequest : class, IRequest<Result>
{
    public override async Task<object?> Send(object request,
        MassTransit.Mediator.IMediator mediator,
        IBus bus,
        CancellationToken cancelToken)
        => await Send((IRequest<Result>)request, mediator, bus, cancelToken);

    public override async Task<Result> Send(IRequest<Result> request,
        MassTransit.Mediator.IMediator mediator,
        IBus bus,
        CancellationToken cancelToken)
    {
        bool isLocal = MessagingConfiguration.ConsumerMessageTypes.Contains(request.GetType());

        IRequestClient<TRequest>? requestClient = isLocal 
            ? mediator.CreateRequestClient<TRequest>(TimeSpan.FromMinutes(2)) 
            : bus.CreateRequestClient<TRequest>(TimeSpan.FromMinutes(2));

        Response<Result> response = await requestClient.GetResponse<Result>(request, pipe =>
        {
            pipe.UseInlineFilter((context, next) =>
            {
                context.Headers.Set("SourceActorId", (string?)Orleans.Runtime.RequestContext.Get("actorId"));
                return next.Send(context);
            });
        }, cancelToken);

        return response.Message;
    }
}

internal class RequestHandlerWrapperImpl<TRequest, TResult> : RequestHandlerWrapper<TResult>
    where TRequest : class, IRequest<Result<TResult>>
{
    public override async Task<object?> Send(object request,
        MassTransit.Mediator.IMediator mediator,
        IBus bus,
        CancellationToken cancelToken)
        => await Send((IRequest<TResult>)request, mediator, bus, cancelToken);

    public override async Task<Result<TResult>> Send(IRequest<Result<TResult>> request,
        MassTransit.Mediator.IMediator mediator,
        IBus bus,
        CancellationToken cancelToken)
    {
        bool isLocal = MessagingConfiguration.ConsumerMessageTypes.Contains(request.GetType());

        IRequestClient<TRequest>? requestClient = isLocal 
            ? mediator.CreateRequestClient<TRequest>(TimeSpan.FromMinutes(2)) 
            : bus.CreateRequestClient<TRequest>(TimeSpan.FromMinutes(2));

        Response<Result<TResult>> response = await requestClient.GetResponse<Result<TResult>>(request, pipe =>
        {
            pipe.UseInlineFilter((context, next) =>
            {
                context.Headers.Set("SourceActorId", (string?)Orleans.Runtime.RequestContext.Get("actorId"));
                return next.Send(context);
            });
        }, cancelToken);

        return response.Message;
    }
}
