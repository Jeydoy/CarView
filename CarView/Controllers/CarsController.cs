
using CarView.Data;
using CarView.Models;
using CarView.Services;
using CarView.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using System;
using System.IO;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json; 

public class CarsController : Controller
{
    private readonly ICrmService _crmService; 
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IBlobStorageService _blobStorageService;

    public CarsController(AppDbContext context, IWebHostEnvironment webHostEnvironment, ICrmService crmService, IBlobStorageService blobStorageService)
    {
        _context = context;
        _webHostEnvironment = webHostEnvironment;
        _crmService = crmService;
        _blobStorageService = blobStorageService;
    }


    [HttpGet]
    public async Task<IActionResult> SearchBrands(string term)
    {
       
        if (string.IsNullOrEmpty(term) || term.Length < 3)
        {
            return Json(new List<object>()); 
        }

        var brands = await _context.Brands
            .Where(b => b.Name.Contains(term))
            .Select(b => new
            {
                id = b.Id,
                text = b.Name
            })
            .Take(50) 
            .ToListAsync();

        return Json(brands);
    }

    // GET: Cars
    public async Task<IActionResult> Index(
        string searchString, int? brandId,
        int? minPrice, int? maxPrice,
        int? minYear, int? maxYear,
        BodyType? bodyType, Transmission? transmission,
        FuelType? fuelType, string? drivetrain) 
    {
        var carsQuery = _context.Cars
            .Include(c => c.Brand)
            .Include(c => c.MediaFiles)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchString))
        {
            carsQuery = carsQuery.Where(c => c.ModelName.Contains(searchString));
        }

        if (brandId.HasValue)
        {
            carsQuery = carsQuery.Where(c => c.BrandId == brandId);
        }

        if (minPrice.HasValue) carsQuery = carsQuery.Where(c => c.Price >= minPrice);
        if (maxPrice.HasValue) carsQuery = carsQuery.Where(c => c.Price <= maxPrice);
        if (minYear.HasValue) carsQuery = carsQuery.Where(c => c.Year >= minYear);
        if (maxYear.HasValue) carsQuery = carsQuery.Where(c => c.Year <= maxYear);

        if (bodyType.HasValue) carsQuery = carsQuery.Where(c => c.BodyType == bodyType);
        if (transmission.HasValue) carsQuery = carsQuery.Where(c => c.Transmission == transmission);
        if (fuelType.HasValue) carsQuery = carsQuery.Where(c => c.FuelType == fuelType);
        if (!string.IsNullOrEmpty(drivetrain)) carsQuery = carsQuery.Where(c => c.Drivetrain.Contains(drivetrain));

      
        ViewBag.CurrentSearch = searchString;
        ViewBag.MinPrice = minPrice;
        ViewBag.MaxPrice = maxPrice;
        ViewBag.MinYear = minYear;
        ViewBag.MaxYear = maxYear;

        ViewBag.BodyType = bodyType;
        ViewBag.Transmission = transmission;
        ViewBag.FuelType = fuelType;
        ViewBag.Drivetrain = drivetrain;

        ViewBag.BrandList = new SelectList(await _context.Brands.ToListAsync(), "Id", "Name", brandId);

        ViewBag.LikedCars = Request.Cookies["LikedCars"]?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>();
        var finalResult = await carsQuery.OrderByDescending(c => c.Id).ToListAsync();
        return View(finalResult);
    }

    // GET: Cars/Favorites
    public async Task<IActionResult> Favorites()
    {
 
        string likedCookie = Request.Cookies["LikedCars"] ?? "";
        var likedIds = likedCookie.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();

        var favoriteCars = await _context.Cars
            .Include(c => c.Brand)
            .Include(c => c.MediaFiles)
            .Where(c => likedIds.Contains(c.Id))
            .ToListAsync();

        ViewBag.LikedCars = likedIds.Select(id => id.ToString()).ToList();

        return View(favoriteCars);
    }

    // POST: Cars/ToggleLike
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> ToggleLike(int id, string returnUrl)
    {
        var car = await _context.Cars.FindAsync(id);
        if (car == null) return NotFound();

        string likedCookie = Request.Cookies["LikedCars"] ?? "";
        var likedIds = likedCookie.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

        if (likedIds.Contains(id.ToString()))
        {
            likedIds.Remove(id.ToString());
            if (car.LikesCount > 0) car.LikesCount--;
        }
        else
        {
            likedIds.Add(id.ToString());
            car.LikesCount++;
        }

        _context.Update(car);
        await _context.SaveChangesAsync();

        CookieOptions options = new CookieOptions { Expires = DateTimeOffset.Now.AddMonths(1) };
        Response.Cookies.Append("LikedCars", string.Join(",", likedIds), options);


        if (string.IsNullOrEmpty(returnUrl) || !Url.IsLocalUrl(returnUrl))
        {
            return RedirectToAction("Index");
        }

        return Redirect(returnUrl);
    }

    [HttpPost]
    public async Task<IActionResult> SendToCrm(string customerName, string customerPhone, string customerEmail, string carName, int carId)
    {
        bool success = await _crmService.CreateLeadAsync(customerEmail, customerName, customerPhone ,carName);

        if (success)
        {
            TempData["SuccessMessage"] = "Вашу заявку успішно відправлено в наш CRM відділ!";
        }
        else
        {
            TempData["ErrorMessage"] = "Помилка зв'язку з CRM системою.";
        }

        return RedirectToAction("Details", new { id = carId });
    }

    // GET: CARS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var car = await _context.Cars
            .Include(c => c.Brand)       
            .Include(c => c.MediaFiles)   
            .FirstOrDefaultAsync(m => m.Id == id);

        if (car == null)
        {
            return NotFound();
        }
        car.ViewsCount++;
        _context.Update(car);
        await _context.SaveChangesAsync();

        return View(car);
    }

    // GET: CARS/Create
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult Create()
    {
        ViewBag.BrandId = new SelectList(_context.Brands, "Id", "Name");
        return View();
    }

    // POST: CARS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CarCreateViewModel viewModel)
    {
        ModelState.Remove("Car.Brand");
        ModelState.Remove("Car.MediaFiles");

        if (ModelState.IsValid)
        {
            _context.Add(viewModel.Car);
            await _context.SaveChangesAsync();


            if (viewModel.UploadedFile != null && viewModel.UploadedFile.Length > 0)
            {
                string? cdnUrl = await _blobStorageService.UploadImageAsync(viewModel.UploadedFile);

                if (!string.IsNullOrEmpty(cdnUrl))
                {
                    var carMedia = new CarMedia
                    {
                        CarId = viewModel.Car.Id,
                        FileUrl = cdnUrl, 
                        MediaType = MediaType.Photo,
                        IsMain = true
                    };

                    _context.CarMedias.Add(carMedia);
                    await _context.SaveChangesAsync();
                }
            }
            if (viewModel.AdditionalFiles != null && viewModel.AdditionalFiles.Count > 0)
            {
                foreach (var file in viewModel.AdditionalFiles)
                {
                    if (file.Length > 0)
                    {
                        string? cdnUrl = await _blobStorageService.UploadImageAsync(file);

                        if (!string.IsNullOrEmpty(cdnUrl))
                        {
                            var extraMedia = new CarMedia
                            {
                                CarId = viewModel.Car.Id,
                                FileUrl = cdnUrl,
                                MediaType = MediaType.Photo,
                                IsMain = false
                            };

                            _context.CarMedias.Add(extraMedia);
                        }
                    }
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        ViewData["BrandId"] = new SelectList(_context.Brands, "Id", "Name", viewModel.Car.BrandId);
        return View(viewModel);
    }

    // GET: CARS/Edit/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var car = await _context.Cars
            .Include(c => c.MediaFiles) 
            .FirstOrDefaultAsync(m => m.Id == id);

        if (car == null)
        {
            return NotFound();
        }

        ViewData["BrandId"] = new SelectList(_context.Brands, "Id", "Name", car.BrandId);
        return View(car);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id, [Bind("Id,ModelName,Year,Price,BodyType,Transmission,FuelType,Description,ViewsCount,LikesCount,BrandId,EngineCapacity,HorsePower,Drivetrain")] Car car, IFormFile? UploadedFile, List<IFormFile>? AdditionalFiles)
    {
        if (id != car.Id)
        {
            return NotFound();
        }

        ModelState.Remove("Brand");
        ModelState.Remove("MediaFiles");

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(car);

                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");

                if (UploadedFile != null && UploadedFile.Length > 0)
                {

                    var oldMain = _context.CarMedias.Where(m => m.CarId == car.Id && m.IsMain).ToList();
                    foreach (var photo in oldMain)
                    {
                        photo.IsMain = false;
                    }

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + UploadedFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await UploadedFile.CopyToAsync(fileStream);
                    }

                    _context.CarMedias.Add(new CarMedia
                    {
                        CarId = car.Id,
                        FileUrl = "/images/" + uniqueFileName,
                        IsMain = true
                    });
                }


                if (AdditionalFiles != null && AdditionalFiles.Count > 0)
                {
                    foreach (var file in AdditionalFiles)
                    {
                        if (file.Length > 0)
                        {
                            string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(fileStream);
                            }

                            _context.CarMedias.Add(new CarMedia
                            {
                                CarId = car.Id,
                                FileUrl = "/images/" + uniqueFileName,
                                IsMain = false
                            });
                        }
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CarExists(car.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }

        ViewData["BrandId"] = new SelectList(_context.Brands, "Id", "Name", car.BrandId);
        return View(car);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePhoto(int mediaId, int carId)
    {
        var media = await _context.CarMedias.FindAsync(mediaId);

        if (media != null)
        {

            try
            {
                if (!string.IsNullOrEmpty(media.FileUrl))
                {

                    if (!media.FileUrl.StartsWith("C:\\") && !media.FileUrl.StartsWith("D:\\"))
                    {
                        string filePath = Path.Combine(_webHostEnvironment.WebRootPath, media.FileUrl.TrimStart('/'));
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                    }
                }
            }
            catch (Exception)
            {

            }

            _context.CarMedias.Remove(media);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Edit), new { id = carId });
    }

    // GET: CARS/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var car = await _context.Cars
            .FirstOrDefaultAsync(m => m.Id == id);
        if (car == null)
        {
            return NotFound();
        }

        return View(car);
    }

    // POST: CARS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var car = await _context.Cars.FindAsync(id);
        if (car != null)
        {
            _context.Cars.Remove(car);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool CarExists(int? id)
    {
        return _context.Cars.Any(e => e.Id == id);
    }

    public async Task<IActionResult> Map()
    {
        var carsWithLocation = await _context.Cars
            .Include(c => c.Brand)
            .Where(c => c.Latitude != null && c.Longitude != null)
            .Select(c => new
            {
                c.Id,
                c.ModelName,
                c.Price,
                BrandName = c.Brand.Name,
                c.Latitude,
                c.Longitude
            })
            .ToListAsync();

        return View(carsWithLocation);
    }




    [HttpPost]
    public IActionResult RequestPdfReport(int id)
    {
        string rabbitHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
        var factory = new ConnectionFactory() { HostName = rabbitHost };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();


        channel.QueueDeclare(queue: "pdf_generation_queue",
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);

        var message = new { CarId = id, RequestTime = DateTime.UtcNow };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        channel.BasicPublish(exchange: "",
                             routingKey: "pdf_generation_queue",
                             basicProperties: null,
                             body: body);

        TempData["Message"] = "Заявку на генерацію PDF прийнято! Файл з'явиться у папці за кілька секунд.";

        return RedirectToAction(nameof(Details), new { id = id });
    }
}
