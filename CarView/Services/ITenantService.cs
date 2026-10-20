using System.Security.Claims;


namespace CarView.Services
{
    // Services/ITenantService.cs
    public interface ITenantService
    {
        int GetTenantId();
    }

// Services/TenantService.cs
public class TenantService : ITenantService
    {
        private readonly IHttpContextAccessor _contextAccessor;

        public TenantService(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public int GetTenantId()
        {
            var user = _contextAccessor.HttpContext?.User;
            if (user == null || !user.Identity.IsAuthenticated)
                return 0;

            var tenantClaim = user.FindFirst("TenantId");
            if (tenantClaim != null && int.TryParse(tenantClaim.Value, out int tenantId))
            {
                return tenantId;
            }

            return 0;
        }
    }
}
