using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Yaam.Domain.Services;

namespace Yaam.Infrastructure.Auth;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid UserId
    {
        get
        {
            var sub = httpContextAccessor.HttpContext?.User?.FindFirstValue("sub")
                ?? throw new InvalidOperationException("User is not authenticated.");
            return Guid.Parse(sub);
        }
    }
}
