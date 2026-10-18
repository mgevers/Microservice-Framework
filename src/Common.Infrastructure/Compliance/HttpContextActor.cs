using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Common.Infrastructure.Compliance;

/// <summary>The caller is the subject of the access token: the user, or the client for a machine-to-machine token.</summary>
public class HttpContextActor(IHttpContextAccessor accessor) : ICurrentActor
{
    public string Id
    {
        get
        {
            var user = accessor.HttpContext?.User;

            // The subject is the NameIdentifier claim when the token handler maps claims, and "sub" when it does not.
            return user?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user?.FindFirstValue("sub")
                ?? throw new InvalidOperationException("the access token has no subject, so the caller cannot be recorded");
        }
    }
}
