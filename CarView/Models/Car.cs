using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace CarView.Models
{
    public class Car
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Введіть назву моделі")]
        [MaxLength (100)]
        public string ModelName { get; set; }
       
        [Range(1900, 2050)]
        public int Year { get; set; }
        [Range(0,1000000000)]
        public int Price { get; set; }  
        public BodyType BodyType { get; set; }
        public Transmission Transmission { get; set; }
       
        [Display(Name = "Потужність (к.с.)")]
        [Range(1, 2000)]
        public int HorsePower { get; set; }

        [Display(Name = "Привід")]
        [MaxLength(50)]
        public string Drivetrain { get; set; } 

        [Display(Name = "Двигун")]
        [MaxLength(100)]
        public string EngineCapacity { get; set; }
        public FuelType FuelType { get; set; }
        public string Description { get; set; }
        public int ViewsCount { get; set; } = 0;
        public int LikesCount { get; set; } = 0;
        public int BrandId { get; set; }
        public Brand Brand { get; set; }    

        public List<CarMedia> MediaFiles { get; set; } = new List<CarMedia>();

        public double? Latitude { get; set; } //широта
        public double? Longitude { get; set; } //довгота

       public string? EngineSoundUrl { get; set; } 


    }
}
