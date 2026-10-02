using System.Collections.Generic;
using CarView.Models;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace CarView.ViewModels
{
    public class HomeViewModel
    {
        public Car HeroCar { get; set; }
        public List<Car> SimpleCars { get; set; }
    }
}
