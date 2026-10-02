using CarView.Models;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace CarView.ViewModels
{
    public class CarCreateViewModel
    {
        public Car Car { get; set; }

        [Required(ErrorMessage = "Оберіть головне фото")]
        [Display(Name = "Головне фото (Постер)")]
        public IFormFile UploadedFile { get; set; }

        [Display(Name = "Додаткові фото (Галерея)")]
        public List<IFormFile> AdditionalFiles { get; set; }
    }
}
