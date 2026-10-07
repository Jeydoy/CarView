
using CarView.Data;
using CarView.Models;
using CarView.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using System;
using System.IO;

public class CarsController : Controller
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public CarsController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
    {
        _context = context;
        _webHostEnvironment = webHostEnvironment;
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
    // GET: Cars/Favorites
    public async Task<IActionResult> Favorites()
    {
        // Читаємо збережені ID автомобілів з Cookies
        string likedCookie = Request.Cookies["LikedCars"] ?? "";
        var likedIds = likedCookie.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();

        // Шукаємо в базі тільки ті авто, які є в списку лайканих
        var favoriteCars = await _context.Cars
            .Include(c => c.Brand)
            .Include(c => c.MediaFiles)
            .Where(c => likedIds.Contains(c.Id))
            .ToListAsync();

        // Передаємо список ID, щоб сердечка були червоними і на цій сторінці
        ViewBag.LikedCars = likedIds.Select(id => id.ToString()).ToList();

        return View(favoriteCars);
    }

    // POST: Cars/ToggleLike
    // Цей метод викликається, коли натискають на сердечко
    [HttpPost]
    public async Task<IActionResult> ToggleLike(int id, string returnUrl)
    {
        var car = await _context.Cars.FindAsync(id);
        if (car == null) return NotFound();

        string likedCookie = Request.Cookies["LikedCars"] ?? "";
        var likedIds = likedCookie.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

        if (likedIds.Contains(id.ToString()))
        {
            // Якщо вже лайкнуто - забираємо лайк
            likedIds.Remove(id.ToString());
            if (car.LikesCount > 0) car.LikesCount--;
        }
        else
        {
            // Якщо ще не лайкнуто - додаємо
            likedIds.Add(id.ToString());
            car.LikesCount++;
        }

        _context.Update(car);
        await _context.SaveChangesAsync();

        // Зберігаємо кукі на 1 рік
        CookieOptions options = new CookieOptions { Expires = DateTimeOffset.Now.AddMonths(1) };
        Response.Cookies.Append("LikedCars", string.Join(",", likedIds), options);

        // Повертаємо користувача туди, де він натиснув кнопку (зберігаючи всі фільтри!)
        return Redirect(returnUrl ?? "/Cars/Index");
    }

    // GET: CARS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var car = await _context.Cars
            .Include(c => c.Brand)        // Обов'язково підтягуємо Марку
            .Include(c => c.MediaFiles)   // Обов'язково підтягуємо Галерею
            .FirstOrDefaultAsync(m => m.Id == id);

        if (car == null)
        {
            return NotFound();
        }

        // Збільшуємо лічильник переглядів (наш додатковий функціонал)
        car.ViewsCount++;
        _context.Update(car);
        await _context.SaveChangesAsync();

        return View(car);
    }

    // GET: CARS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: CARS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CarCreateViewModel viewModel)
    {
       
        ModelState.Remove("Car.Brand");
        ModelState.Remove("Car.MediaFiles");

        if (ModelState.IsValid)
        {
         
            _context.Add(viewModel.Car);
            await _context.SaveChangesAsync();

            
            if (viewModel.UploadedFile != null)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                Directory.CreateDirectory(uploadsFolder); 

            
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + viewModel.UploadedFile.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await viewModel.UploadedFile.CopyToAsync(fileStream);
                }

              
                var carMedia = new CarMedia
                {
                    CarId = viewModel.Car.Id,
                    FileUrl = "/images/" + uniqueFileName,
                    MediaType = MediaType.Photo,
                    IsMain = true
                };

                _context.CarMedias.Add(carMedia);
                await _context.SaveChangesAsync();
            }
            if (viewModel.AdditionalFiles != null && viewModel.AdditionalFiles.Count > 0)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");

                foreach (var file in viewModel.AdditionalFiles)
                {
                    if (file.Length > 0)
                    {
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }

                        var extraMedia = new CarMedia
                        {
                            CarId = viewModel.Car.Id,
                            FileUrl = "/images/" + uniqueFileName,
                            MediaType = MediaType.Photo,
                            IsMain = false // Вказуємо, що це звичайне фото для галереї
                        };

                        _context.CarMedias.Add(extraMedia);
                    }
                }
                // Зберігаємо всі додаткові фото в базу за один раз
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));

            return RedirectToAction(nameof(Index));
        }

        ViewData["BrandId"] = new SelectList(_context.Brands, "Id", "Name", viewModel.Car.BrandId);
        return View(viewModel);
    }

    // GET: CARS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        // Замість FindAsync використовуємо FirstOrDefaultAsync з Include
        var car = await _context.Cars
            .Include(c => c.MediaFiles) // Підтягуємо фотографії!
            .FirstOrDefaultAsync(m => m.Id == id);

        if (car == null)
        {
            return NotFound();
        }

        ViewData["BrandId"] = new SelectList(_context.Brands, "Id", "Name", car.BrandId);
        return View(car);
    }

    // POST: CARS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    //[HttpPost]
    //[ValidateAntiForgeryToken]
    //public async Task<IActionResult> Edit(int? id, [Bind("Id,ModelName,Year,Price,BodyType,Transmission,FuelType,Description,ViewsCount,LikesCount,BrandId,EngineCapacity,HorsePower,Drivetrain")] Car car)
    //{
    //    if (id != car.Id)
    //    {
    //        return NotFound();
    //    }
    //    ModelState.Remove("Brand");
    //    ModelState.Remove("MediaFiles");

    //    if (ModelState.IsValid)
    //    {
    //        try
    //        {
    //            _context.Update(car);
    //            await _context.SaveChangesAsync();
    //        }
    //        catch (DbUpdateConcurrencyException)
    //        {
    //            if (!CarExists(car.Id))
    //            {
    //                return NotFound();
    //            }
    //            else
    //            {
    //                throw;
    //            }
    //        }
    //        return RedirectToAction(nameof(Index));
    //    }
    //    ViewData["BrandId"] = new SelectList(_context.Brands, "Id", "Name", car.BrandId);
    //    return View(car);

    //}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,ModelName,Year,Price,BodyType,Transmission,FuelType,Description,ViewsCount,LikesCount,BrandId,EngineCapacity,HorsePower,Drivetrain")] Car car, IFormFile? UploadedFile, List<IFormFile>? AdditionalFiles)
    {
        if (id != car.Id)
        {
            return NotFound();
        }

        // Примусово ігноруємо навігаційні властивості, щоб валідація пройшла успішно
        ModelState.Remove("Brand");
        ModelState.Remove("MediaFiles");

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(car);

                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");

                // 1. Якщо завантажили нове ГОЛОВНЕ фото
                if (UploadedFile != null && UploadedFile.Length > 0)
                {
                    // Знаходимо старе головне фото і робимо його звичайним (або можна видалити)
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

                // 2. Якщо завантажили ДОДАТКОВІ фотографії
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

        // Якщо щось пішло не так (наприклад, не заповнено якесь текстове поле), повертаємо форму
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
            // 1. БЕЗПЕЧНЕ видалення фізичного файлу
            try
            {
                if (!string.IsNullOrEmpty(media.FileUrl))
                {
                    // Працюємо з файлом, тільки якщо це нормальний відносний шлях
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
                // Якщо шлях кривий або сталася помилка доступу — просто ігноруємо, 
                // щоб програма не "вилітала"
            }

            // 2. ГАРАНТОВАНЕ видалення запису з бази даних
            _context.CarMedias.Remove(media);
            await _context.SaveChangesAsync();
        }

        // Повертаємося на сторінку редагування
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
}
