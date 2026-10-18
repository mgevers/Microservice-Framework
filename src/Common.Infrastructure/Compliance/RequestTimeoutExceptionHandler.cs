using MassTransit;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Common.Infrastructure.Compliance;

/// <summary>Returns 504 when the message processor does not answer a command in time, for example when it is down.</summary>
public class RequestTimeoutExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not RequestTimeoutException)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status504GatewayTimeout;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status504GatewayTimeout,
                Title = "The message processor did not respond in time.",
            }
        });
    }
}
