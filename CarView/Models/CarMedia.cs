using System.ComponentModel.DataAnnotations;

namespace CarView.Models
{
    public class CarMedia
    {
        public int Id { get; set; }
        [Required(ErrorMessage ="Введіть URL файлу")]
        public string FileUrl { get; set; }
        public MediaType MediaType { get; set; }
        public bool IsMain { get; set; } 
        public int CarId { get; set; }  
        public Car Car { get; set; }
    }
}
