using CarView.Data; 
using CarView.ViewModels;
using CarView.Data;
using CarView.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class HomeController : Controller
{
    private readonly AppDbContext _context;

  
    public HomeController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
       
        var heroCar = await _context.Cars
            .Include(c => c.Brand)
            .Include(c => c.MediaFiles)
            .OrderByDescending(c => c.ViewsCount)
            .FirstOrDefaultAsync();

       
        var simpleCars = await _context.Cars
            .Include(c => c.Brand)
            .Include(c => c.MediaFiles)
            .Where(c => heroCar == null || c.Id != heroCar.Id)
            .OrderByDescending(c => c.Id)
            .Take(6)
            .ToListAsync();

        var model = new HomeViewModel
        {
            HeroCar = heroCar,
            SimpleCars = simpleCars
        };

        return View(model);
    }
}