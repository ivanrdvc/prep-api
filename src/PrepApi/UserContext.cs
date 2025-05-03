using System.Security.Claims;

namespace PrepApi;

public class UserContext
{
    public bool IsAuthenticated { get; }
    public string? UserId { get; }

    public UserContext(IHttpContextAccessor httpContextAccessor)
    {
        ClaimsPrincipal? user = httpContextAccessor.HttpContext?.User;
        IsAuthenticated = user?.Identity?.IsAuthenticated == true;
        UserId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}