using MediatR;
using Polly;

namespace Common.Infrastructure.Mediator;

public class RetryBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
     where TRequest : IRequest<TResponse>
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var policy = Policy
            .Handle<Exception>()
            .RetryAsync(3);

        return policy.ExecuteAsync(() => next());
    }
}
