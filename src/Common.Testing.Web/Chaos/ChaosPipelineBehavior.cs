using MediatR;

namespace Common.Testing.Integration.Chaos;

public class ChaosPipelineBehavior<TRequest, TResponse>(ChaosRequestManager chaosManager) : IPipelineBehavior<TRequest, TResponse>
     where TRequest : IRequest<TResponse>
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!chaosManager.ChaosMap.ContainsKey(request))
        {
            chaosManager.ChaosMap.Add(request, false);
        }

        var hasCausedChaos = chaosManager.ChaosMap[request];

        if (hasCausedChaos)
        {
            return next(cancellationToken);
        }

        chaosManager.ChaosMap[request] = true;
        throw new Exception("Chaos caused an exception!");
    }
}
