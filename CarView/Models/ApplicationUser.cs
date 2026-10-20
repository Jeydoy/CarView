using Microsoft.AspNetCore.Identity;    

namespace CarView.Models
{
    public class ApplicationUser: IdentityUser
    {
        public int? TenantId { get; set; }
        public Tenant? Tenant { get; set; }
    }
}
