using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CarRentalAPIWebApp.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CarRentalAPIWebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CarsController : ControllerBase
    {
        private readonly CarRentalAPIContext _context;
        private readonly ILogger<CarsController> _logger;

        public CarsController(CarRentalAPIContext context, ILogger<CarsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // DTO для уникнення циклічної серіалізації
        public class CarDto
        {
            public int Id { get; set; }
            public string Brand { get; set; }
            public string Model { get; set; }
            public int Year { get; set; }
            public int StatusId { get; set; }
            public decimal PricePerDay { get; set; }
            public CarStatusType Status { get; set; }
        }

        // GET: api/Cars
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CarDto>>> GetCars()
        {
            _logger.LogInformation("Fetching all cars.");
            var cars = await _context.Cars
                .Include(c => c.Status)
                .Select(c => new CarDto
                {
                    Id = c.Id,
                    Brand = c.Brand,
                    Model = c.Model,
                    Year = c.Year,
                    StatusId = c.StatusId,
                    PricePerDay = c.PricePerDay,
                    Status = c.Status
                })
                .ToListAsync();

            foreach (var car in cars)
            {
                _logger.LogInformation("Car ID {Id}, Status: {Status}", car.Id, car.Status?.DisplayName ?? "null");
            }

            return cars;
        }

        // GET: api/Cars/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CarDto>> GetCar(int id)
        {
            var car = await _context.Cars
                .Include(c => c.Status)
                .Where(c => c.Id == id)
                .Select(c => new CarDto
                {
                    Id = c.Id,
                    Brand = c.Brand,
                    Model = c.Model,
                    Year = c.Year,
                    StatusId = c.StatusId,
                    PricePerDay = c.PricePerDay,
                    Status = c.Status
                })
                .FirstOrDefaultAsync();

            return car == null ? NotFound() : car;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutCar(int id, Car car)
        {
            if (id != car.Id)
            {
                _logger.LogWarning("Car ID mismatch: URL ID {UrlId}, Body ID {BodyId}", id, car.Id);
                return BadRequest("Car ID mismatch.");
            }

            if (car == null || !ModelState.IsValid)
            {
                _logger.LogWarning("Car data is null or validation failed.");
                return BadRequest(ModelState);
            }

            var existingCar = await _context.Cars.FindAsync(id);
            if (existingCar == null)
            {
                _logger.LogWarning("Car with ID {Id} not found.", id);
                return NotFound();
            }

            if (!await _context.CarStatusTypes.AnyAsync(s => s.Id == car.StatusId))
            {
                _logger.LogWarning("Invalid StatusId: {StatusId}", car.StatusId);
                return BadRequest("Invalid StatusId.");
            }

            existingCar.Brand = car.Brand;
            existingCar.Model = car.Model;
            existingCar.Year = car.Year;
            existingCar.StatusId = car.StatusId;
            existingCar.PricePerDay = car.PricePerDay;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Car with ID {Id} successfully updated.", id);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await CarExists(id))
                {
                    _logger.LogWarning("Concurrency conflict: Car with ID {Id} not found.", id);
                    return NotFound();
                }
                _logger.LogError("Concurrency conflict when updating car with ID {Id}.", id);
                throw;
            }

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<Car>> PostCar(Car car)
        {
            if (car == null || !ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!await _context.CarStatusTypes.AnyAsync(s => s.Id == car.StatusId))
            {
                return BadRequest("Invalid StatusId.");
            }

            _context.Cars.Add(car);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCar), new { id = car.Id }, car);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCar(int id)
        {
            if (id <= 0)
            {
                return BadRequest("Invalid car ID.");
            }

            var car = await _context.Cars
                .Include(c => c.Bookings)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (car == null)
            {
                return NotFound();
            }

            if (car.Bookings.Any())
            {
                return BadRequest("Cannot delete car with existing bookings.");
            }

            _context.Cars.Remove(car);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Автомобіль успішно видалено." });
        }

        private async Task<bool> CarExists(int id)
        {
            return await _context.Cars.AnyAsync(e => e.Id == id);
        }
    }
}