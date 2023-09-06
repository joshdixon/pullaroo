using System;
using System.Threading;
using System.Threading.Tasks;

using MassTransit;

using Orleans.FluentResults;

using Pullaroo.Contracts;

namespace Pullaroo.Common;

public interface IMediator
{
    public Task Publish(object message, Func<PublishContext, Task>? configure = null, CancellationToken cancelToken = default);
    
    public Task<Result> Send(IRequest<Result> request, CancellationToken cancelToken = default);

    public Task<Result<TResult>> Send<TResult>(IRequest<Result<TResult>> request,
        CancellationToken cancelToken = default);
}
