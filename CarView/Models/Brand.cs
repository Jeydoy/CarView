using System.ComponentModel.DataAnnotations;

namespace CarView.Models
{
    public class Brand
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Введіть назву марки")]
        [MaxLength(50)]
        public string Name { get; set; }
        [MaxLength(50)]
        public string Country { get; set; } 
        public string? LogoUrl { get; set; }
        public string Founders { get; set; }
        public int YearFounded { get; set; }
        public int? AnnualSales { get; set; }
        public string PopularModels { get; set; }
        public string Description { get; set; } 
        public List<Car> Cars { get; set; } = new List<Car>();
    }
}
