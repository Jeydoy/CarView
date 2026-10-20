namespace CarView.Models
{
    public class Tenant
    {
        public int Id { get; set; }
        public string Name { get; set; } 
    }

    public interface ITenantEntity
    {
        int? TenantId { get; set; }
    }
}
