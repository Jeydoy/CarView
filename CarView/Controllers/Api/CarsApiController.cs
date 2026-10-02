using CarView.Data;
using CarView.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CarView.Data; 

namespace CarView.Controllers.Api
{
    // Маршрут буде виглядати як: https://localhost:44342/api/carsapi
    [Route("api/[controller]")]
    [ApiController]
    public class CarsApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CarsApiController(AppDbContext context)
        {
            _context = context;
        }

        // 1. GET: api/carsapi
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Car>>> GetCars()
        {
            return await _context.Cars.ToListAsync(); // Статус 200 OK (повертається автоматично)
        }

        // 2. GET: api/carsapi/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Car>> GetCar(int id)
        {
            var car = await _context.Cars.FindAsync(id);

            if (car == null)
            {
                return NotFound(); 
            }

            return car;
        }

        // 3. POST: api/carsapi
        [HttpPost]
        public async Task<ActionResult<Car>> PostCar(Car car)
        {
            _context.Cars.Add(car);
            await _context.SaveChangesAsync();

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
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CarExists(id))
                {
                    return NotFound(); 
                }
                else
                {
                    throw; 
                }
            }

            return NoContent(); // Статус 204 No Content (успішно оновлено, повертати тіло не потрібно)
        }

        // 5. DELETE: api/carsapi/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCar(int id)
        {
            var car = await _context.Cars.FindAsync(id);
            if (car == null)
            {
                return NotFound(); // Статус 404 Not Found
            }

            _context.Cars.Remove(car);
            await _context.SaveChangesAsync();

            return NoContent(); // Статус 204 No Content
        }

        private bool CarExists(int id)
        {
            return _context.Cars.Any(e => e.Id == id);
        }
    }
}