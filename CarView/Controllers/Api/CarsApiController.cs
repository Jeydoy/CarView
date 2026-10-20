using CarView.Data;
using CarView.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CarView.Data; 
using CarView.Services;
using Microsoft.Extensions.Caching.Memory;  

namespace CarView.Controllers.Api
{
    // Маршрут буде виглядати як: https://localhost:44342/api/carsapi
    [Route("api/[controller]")]
    [ApiController]
    public class CarsApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly LuceneSearchService _luceneService;
        private readonly IMemoryCache _cache;
        private const string CacheKey = "CarsCache"; 

        public CarsApiController(AppDbContext context, LuceneSearchService luceneService, IMemoryCache cache)
        {
            _context = context;
            _luceneService = luceneService;
            _cache = cache;
        }

        // 1. GET: api/carsapi
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Car>>> GetCars()
        {
            if (_cache.TryGetValue(CacheKey, out List<Car>? cachedCars))
            {
                Response.Headers.Append("X-Cache", "HIT");
                return Ok(cachedCars);
            }

            // Якщо в кеші немає — беремо з SQL Server
            var cars = await _context.Cars
                .Include(c => c.Brand)
                .Include(c => c.MediaFiles)
                .ToListAsync();

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5)) 
                .SetSlidingExpiration(TimeSpan.FromMinutes(2));  

            // Записуємо в пам'ять
            _cache.Set(CacheKey, cars, cacheEntryOptions);

            Response.Headers.Append("X-Cache", "MISS");
            return Ok(cars);
        }

        // 2. GET: api/carsapi/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Car>> GetCar(int id)
        {
            string individualCacheKey = $"Car_{id}_Cache_Key";

            if (_cache.TryGetValue(individualCacheKey, out Car? cachedCar))
            {
                Response.Headers.Append("X-Cache", "HIT");
                return Ok(cachedCar);
            }

            var car = await _context.Cars
                .Include(c => c.Brand)
                .Include(c => c.MediaFiles)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (car == null)
            {
                return NotFound();
            }

            _cache.Set(individualCacheKey, car, TimeSpan.FromMinutes(5));
            Response.Headers.Append("X-Cache", "MISS");
            return Ok(car);
        }

        // 3. POST: api/carsapi
        [HttpPost]
        public async Task<ActionResult<Car>> PostCar(Car car)
        {
            _context.Cars.Add(car);
            await _context.SaveChangesAsync();
            _cache.Remove(CacheKey);

            return CreatedAtAction(nameof(GetCar), new { id = car.Id }, car);
        }

        // 4. PUT: api/carsapi/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCar(int id, Car car)
        {
            if (id != car.Id)
            {
                return BadRequest();
            }

            _context.Entry(car).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();

                _cache.Remove(CacheKey);
                _cache.Remove($"Car_{id}_Cache_Key");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CarExists(id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        // 5. DELETE: api/carsapi/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCar(int id)
        {
            var car = await _context.Cars.FindAsync(id);
            if (car == null)
            {
                return NotFound();
            }

            _context.Cars.Remove(car);
            await _context.SaveChangesAsync();

            // Очищаємо кеш
            _cache.Remove(CacheKey);
            _cache.Remove($"Car_{id}_Cache_Key");

            return NoContent();
        }


        [HttpGet("paged")]
        public async Task<IActionResult> GetCarsPaged(int skip = 0, int limit = 10)
        {
            
            var totalRecords = await _context.Cars.CountAsync();

            //(Skip та Limit)
            var cars = await _context.Cars
                .Include(c => c.Brand) 
                .OrderBy(c => c.Id)    
                .Skip(skip)
                .Take(limit)
                .Select(c => new
                {
                    id = c.Id,
                    modelName = c.ModelName,
                    brandName = c.Brand != null ? c.Brand.Name : "",
                    price = c.Price
                })
                .ToListAsync();

            string nextLink = null;
            if (skip + limit < totalRecords)
            {
                nextLink = $"/api/carsapi/paged?skip={skip + limit}&limit={limit}";
            }

            return Ok(new
            {
                skip = skip,
                limit = limit,
                total = totalRecords,
                nextLink = nextLink,
                data = cars
            });
        }

        private bool CarExists(int id)
        {
            return _context.Cars.Any(e => e.Id == id);
        }

   
        // POST: api/carsapi/rebuild-index
        [HttpPost("rebuild-index")]
        public async Task<IActionResult> RebuildIndex()
        {
            var cars = await _context.Cars.Include(c => c.Brand).ToListAsync();
            _luceneService.BuildIndex(cars);
            return Ok(new { message = "Пошуковий індекс Lucene успішно створено!" });
        }

        // GET: api/carsapi/search?q=BMW
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Car>>> SearchCars([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest("Пошуковий запит не може бути порожнім.");

            var foundCarIds = _luceneService.Search(q);

            if (!foundCarIds.Any())
                return Ok(new List<Car>());


            var cars = await _context.Cars
                .Include(c => c.Brand)
                .Where(c => foundCarIds.Contains(c.Id))
                .ToListAsync();

            return Ok(cars);
        }
    }
}