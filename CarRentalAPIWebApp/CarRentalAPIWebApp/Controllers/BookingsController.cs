using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CarRentalAPIWebApp.Models;

namespace CarRentalAPIWebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingsController : ControllerBase
    {
        private readonly CarRentalAPIContext _context;

        private const int BOOKING_STATUS_ACTIVE = 1;
        private const int CAR_STATUS_AVAILABLE = 1;
        private const int CAR_STATUS_RENTED = 2;
        private const int CAR_STATUS_UNDER_REPAIR = 3;
        private const int BOOKING_STATUS_PLANNED = 4;

        public BookingsController(CarRentalAPIContext context, ILogger<BookingsController> logger)
        {
            _context = context;
        }

        public class BookingDto
        {
            public int Id { get; set; }
            public string UserName { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public int StatusId { get; set; }
            public int CarId { get; set; }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BookingDto>>> GetBookings()
        {
            var bookings = await _context.Bookings
                .Select(b => new BookingDto
                {
                    Id = b.Id,
                    UserName = b.UserName,
                    StartDate = b.StartDate,
                    EndDate = b.EndDate,
                    StatusId = b.StatusId,
                    CarId = b.CarId
                })
                .ToListAsync();
            return Ok(bookings);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BookingDto>> GetBooking(int id)
        {
            var booking = await _context.Bookings
                .Where(b => b.Id == id)
                .Select(b => new BookingDto
                {
                    Id = b.Id,
                    UserName = b.UserName,
                    StartDate = b.StartDate,
                    EndDate = b.EndDate,
                    StatusId = b.StatusId,
                    CarId = b.CarId
                })
                .FirstOrDefaultAsync();

            if (booking == null)
            {
                return NotFound();
            }
            return Ok(booking);
        }

        [HttpPost]
        public async Task<ActionResult<BookingDto>> PostBooking([FromBody] BookingDto bookingDto)
        {
            if (bookingDto == null)
            {
                return BadRequest("Дані бронювання не надано.");
            }

            if (bookingDto.StartDate >= bookingDto.EndDate)
            {
                return BadRequest("Дата початку повинна бути раніше дати закінчення.");
            }

            var car = await _context.Cars.FindAsync(bookingDto.CarId);

            if (car == null)
            {
                return BadRequest("Автомобіль не знайдено.");
            }

            if (bookingDto.StatusId == BOOKING_STATUS_ACTIVE && car.StatusId != CAR_STATUS_AVAILABLE)
            {
                return BadRequest($"Автомобіль '{car.Brand} {car.Model}' недоступний для активного бронювання (поточний статус ID: {car.StatusId}).");
            }

            if (!await _context.BookingStatusTypes.AnyAsync(s => s.Id == bookingDto.StatusId))
            {
                return BadRequest("Некоректний статус бронювання.");
            }

            if (bookingDto.StatusId == BOOKING_STATUS_ACTIVE)
            {
                bool isOverlapping = await _context.Bookings
                    .AnyAsync(b => b.CarId == bookingDto.CarId &&
                                   b.StatusId == BOOKING_STATUS_ACTIVE &&
                                   b.StartDate < bookingDto.EndDate &&
                                   b.EndDate > bookingDto.StartDate);
                if (isOverlapping)
                {
                    return BadRequest("Автомобіль уже активно заброньовано на ці дати.");
                }
            }

            if (bookingDto.StatusId == 0) // Якщо статус не вказаний
            {
                bookingDto.StatusId = bookingDto.StartDate.Date <= DateTime.Today
                    ? BOOKING_STATUS_ACTIVE
                    : BOOKING_STATUS_PLANNED;
            }

            // Перевірка доступності авто
            if (bookingDto.StatusId == BOOKING_STATUS_ACTIVE && car.StatusId != CAR_STATUS_AVAILABLE)
                return BadRequest("Автомобіль недоступний для активного бронювання");

            var booking = new Booking
            {
                UserName = bookingDto.UserName,
                StartDate = bookingDto.StartDate,
                EndDate = bookingDto.EndDate,
                StatusId = bookingDto.StatusId,
                CarId = bookingDto.CarId
            };

            _context.Bookings.Add(booking);

            // Оновлення статусу авто
            if (booking.StatusId == BOOKING_STATUS_ACTIVE)
            {
                car.StatusId = CAR_STATUS_RENTED;
                _context.Entry(car).State = EntityState.Modified;
            }

            bool carStatusShouldChange = booking.StatusId == BOOKING_STATUS_ACTIVE && car.StatusId == CAR_STATUS_AVAILABLE;
            if (carStatusShouldChange)
            {
                car.StatusId = CAR_STATUS_RENTED;
                _context.Entry(car).State = EntityState.Modified;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, "Помилка збереження даних: " + ex.Message);
            }


            await _context.SaveChangesAsync();
            bookingDto.Id = booking.Id;
            return CreatedAtAction(nameof(GetBooking), new { id = booking.Id }, bookingDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutBooking(int id, [FromBody] BookingDto bookingDto)
        {
            if (id != bookingDto.Id)
            {
                return BadRequest("ID бронювання не співпадає.");
            }

            var bookingToUpdate = await _context.Bookings.FindAsync(id);
            if (bookingToUpdate == null)
            {
                return NotFound();
            }

            var newSelectedCar = await _context.Cars.FindAsync(bookingToUpdate.CarId);
            if (newSelectedCar == null)
            {
                return BadRequest("Новий обраний автомобіль не знайдено.");
            }

            if (bookingDto.StartDate >= bookingDto.EndDate)
            {
                return BadRequest("Дата початку повинна бути раніше дати закінчення.");
            }

            if (!await _context.BookingStatusTypes.AnyAsync(s => s.Id == bookingDto.StatusId))
            {
                return BadRequest("Некоректний статус бронювання.");
            }

            if (bookingDto.StatusId == BOOKING_STATUS_ACTIVE)
            {
                var overlappingBookings = await _context.Bookings
                    .Where(b => b.CarId == bookingToUpdate.CarId && b.Id != bookingDto.Id && b.StatusId == BOOKING_STATUS_ACTIVE)
                    .Where(b => b.StartDate < bookingDto.EndDate && b.EndDate > bookingDto.StartDate)
                    .AnyAsync();
                if (overlappingBookings)
                {
                    return BadRequest("Автомобіль уже активно заброньовано на ці дати.");
                }
            }

            var car = await _context.Cars.FindAsync(bookingToUpdate.CarId);
            if (car == null)
            {
                return BadRequest("Автомобіль не знайдено.");
            }

            if (bookingDto.StatusId == BOOKING_STATUS_ACTIVE && car.StatusId != CAR_STATUS_AVAILABLE)
            {
                return BadRequest("Автомобіль недоступний для активного бронювання.");
            }

            bookingDto.CarId = bookingToUpdate.CarId;
            bookingToUpdate.UserName = bookingDto.UserName;
            bookingToUpdate.StartDate = bookingDto.StartDate;
            bookingToUpdate.EndDate = bookingDto.EndDate;
            bookingToUpdate.StatusId = bookingDto.StatusId;

            _context.Entry(bookingToUpdate).State = EntityState.Modified;

            await UpdateCarStatusInternal(bookingToUpdate.CarId);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, "Помилка збереження даних: " + ex.Message);
            }

            return Ok(bookingDto);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBooking(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
                return NotFound();

            var carId = booking.CarId;
            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();

            // Оновлюємо статус авто після видалення бронювання
            await UpdateCarStatus(carId);
            return Ok();
        }

        private async Task UpdateCarStatusInternal(int carId)
        {
            var car = await _context.Cars.FindAsync(carId);
            if (car == null || car.StatusId == CAR_STATUS_UNDER_REPAIR)
            {
                return;
            }

            var hasActiveBookings = await _context.Bookings
                .AnyAsync(b => b.CarId == carId && b.StatusId == BOOKING_STATUS_ACTIVE);

            if (hasActiveBookings)
            {
                if (car.StatusId != CAR_STATUS_RENTED)
                {
                    car.StatusId = CAR_STATUS_RENTED;
                    _context.Entry(car).State = EntityState.Modified;
                }
            }
            else
            {
                if (car.StatusId == CAR_STATUS_RENTED)
                {
                    car.StatusId = CAR_STATUS_AVAILABLE;
                    _context.Entry(car).State = EntityState.Modified;
                }
            }
        }

        private async Task UpdateCarStatus(int carId)
        {
            var car = await _context.Cars.FindAsync(carId);
            if (car == null || car.StatusId == CAR_STATUS_UNDER_REPAIR)
                return;

            var hasActiveBookings = await _context.Bookings
                .AnyAsync(b => b.CarId == carId &&
                    (b.StatusId == BOOKING_STATUS_ACTIVE || b.StatusId == BOOKING_STATUS_PLANNED));

            car.StatusId = hasActiveBookings ? CAR_STATUS_RENTED : CAR_STATUS_AVAILABLE;
            await _context.SaveChangesAsync();
        }
    }
}