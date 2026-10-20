using CarView.Data; 
using CarView.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Security.Cryptography;
using System.Text.Json;

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

    public IActionResult About()
    {
        return View();
    }

    public IActionResult Donation()
    {
        string publicKey = "sandbox_i4964407222";
        string privateKey = "sandbox_lRTmcrLPucr443X4rXXJ6snFHSihPknGVDyTIial";

      
        var paymentData = new Dictionary<string, object>
    {
        { "public_key", publicKey },
        { "version", 3 }, 
        { "action", "pay" },
        { "amount", 100 }, 
        { "currency", "UAH" },
        { "description", "Донат на розвиток проєкту CarView" },
        { "order_id", "donate_" + Guid.NewGuid().ToString() },
        { "sandbox", 1 } 
    };

        var options = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        string jsonString = JsonSerializer.Serialize(paymentData, options);
        string dataBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonString));

        string signString = privateKey + dataBase64 + privateKey;
        byte[] signBytes = Encoding.UTF8.GetBytes(signString);
        byte[] hashBytes = SHA1.HashData(signBytes);
        string signature = Convert.ToBase64String(hashBytes);

        ViewBag.LiqPayData = dataBase64;
        ViewBag.LiqPaySignature = signature;

        return View();
    }
}