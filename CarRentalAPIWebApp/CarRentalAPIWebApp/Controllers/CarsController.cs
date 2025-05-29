using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private const int CAR_STATUS_AVAILABLE = 1;
        private const int CAR_STATUS_RENTED = 2;
        private const int CAR_STATUS_UNDER_REPAIR = 3;
        private const int BOOKING_STATUS_ACTIVE = 1;
        private const int BOOKING_STATUS_PLANNED = 4;

        public CarsController(CarRentalAPIContext context)
        {
            _context = context;
        }

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

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CarDto>>> GetCars()
        {
            return await _context.Cars
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
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CarDto>> GetCar(int id)
        {
            var car = await _context.Cars
                .Include(c => c.Status)
                .FirstOrDefaultAsync(c => c.Id == id);

            return car == null ? NotFound() : (ActionResult<CarDto>)new CarDto
            {
                Id = car.Id,
                Brand = car.Brand,
                Model = car.Model,
                Year = car.Year,
                StatusId = car.StatusId,
                PricePerDay = car.PricePerDay,
                Status = car.Status
            };
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutCar(int id, Car car)
        {
            if (id != car.Id)
                return BadRequest("Car ID mismatch.");

            var existingCar = await _context.Cars
                .Include(c => c.Bookings)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (existingCar == null)
                return NotFound();

            if (existingCar.Bookings.Any(b =>
                b.StatusId == BOOKING_STATUS_ACTIVE ||
                b.StatusId == BOOKING_STATUS_PLANNED))
                return BadRequest("Cannot edit car with active or planned bookings.");

            if (car.StatusId == CAR_STATUS_RENTED)
                return BadRequest("Cannot manually set status to 'Rented'");

            existingCar.Brand = car.Brand;
            existingCar.Model = car.Model;
            existingCar.Year = car.Year;
            existingCar.PricePerDay = car.PricePerDay;

            if (car.StatusId == CAR_STATUS_AVAILABLE || car.StatusId == CAR_STATUS_UNDER_REPAIR)
                existingCar.StatusId = car.StatusId;

            try
            {
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await CarExists(id))
                    return NotFound();
                throw;
            }
        }

        [HttpPost]
        public async Task<ActionResult<Car>> PostCar(Car car)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (car.StatusId == CAR_STATUS_RENTED)
                return BadRequest("Cannot set car status to 'Rented' when creating");

            if (!await _context.CarStatusTypes.AnyAsync(s => s.Id == car.StatusId))
                return BadRequest("Invalid StatusId.");

            _context.Cars.Add(car);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCar), new { id = car.Id }, car);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCar(int id)
        {
            var car = await _context.Cars
                .Include(c => c.Bookings)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (car == null)
                return NotFound();

            if (car.Bookings.Any())
                return BadRequest("Cannot delete car with existing bookings");

            _context.Cars.Remove(car);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Car deleted successfully" });
        }

        private async Task<bool> CarExists(int id) =>
            await _context.Cars.AnyAsync(e => e.Id == id);
    }
}