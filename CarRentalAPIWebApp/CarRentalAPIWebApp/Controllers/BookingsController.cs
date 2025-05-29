using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using CarRentalAPIWebApp.Models;

namespace CarRentalAPIWebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingsController : ControllerBase
    {
        private readonly CarRentalAPIContext _context;
        private readonly ILogger<BookingsController> _logger;

        private const int BOOKING_STATUS_ACTIVE = 5;
        private const int CAR_STATUS_AVAILABLE = 1;
        private const int CAR_STATUS_RENTED = 5;
        private const int CAR_STATUS_UNDER_REPAIR = 3;

        public BookingsController(CarRentalAPIContext context, ILogger<BookingsController> logger)
        {
            _context = context;
            _logger = logger;
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
            _logger.LogInformation("----- PostBooking START -----");
            _logger.LogInformation("Received BookingDto: CarId={CarId}, UserName={UserName}, StartDate={StartDate}, EndDate={EndDate}, StatusId={StatusId}",
                bookingDto.CarId, bookingDto.UserName, bookingDto.StartDate, bookingDto.EndDate, bookingDto.StatusId);

            if (bookingDto == null)
            {
                _logger.LogWarning("BookingDto is null.");
                return BadRequest("Дані бронювання не надано.");
            }

            // Валідація дат перенесена з CompareDatesAttribute
            if (bookingDto.StartDate >= bookingDto.EndDate)
            {
                _logger.LogWarning("Validation failed: StartDate ({StartDate}) is not before EndDate ({EndDate}).", bookingDto.StartDate, bookingDto.EndDate);
                return BadRequest("Дата початку повинна бути раніше дати закінчення.");
            }

            var car = await _context.Cars.FindAsync(bookingDto.CarId);
            if (car == null)
            {
                _logger.LogWarning("Car with Id {CarId} not found.", bookingDto.CarId);
                return BadRequest("Автомобіль не знайдено.");
            }
            _logger.LogInformation("Fetched Car: Id={CarActualId}, Brand={CarBrand}, Model={CarModel}, CurrentStatusId={CarStatusId}",
                car.Id, car.Brand, car.Model, car.StatusId);

            if (bookingDto.StatusId == BOOKING_STATUS_ACTIVE && car.StatusId != CAR_STATUS_AVAILABLE)
            {
                _logger.LogWarning("Validation failed: Attempt to create active booking for unavailable car. CarId={CarId}, CarStatusId={CarStatusId}. Expected car status: {ExpectedCarStatus}",
                    car.Id, car.StatusId, CAR_STATUS_AVAILABLE);
                return BadRequest($"Автомобіль '{car.Brand} {car.Model}' недоступний для активного бронювання (поточний статус ID: {car.StatusId}).");
            }

            if (!await _context.BookingStatusTypes.AnyAsync(s => s.Id == bookingDto.StatusId))
            {
                _logger.LogWarning("Invalid booking status: StatusId {StatusId} does not exist.", bookingDto.StatusId);
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
                    _logger.LogWarning("Validation failed: Overlapping active booking for CarId {CarId}.", bookingDto.CarId);
                    return BadRequest("Автомобіль уже активно заброньовано на ці дати.");
                }
            }

            var booking = new Booking
            {
                UserName = bookingDto.UserName,
                StartDate = bookingDto.StartDate,
                EndDate = bookingDto.EndDate,
                StatusId = bookingDto.StatusId,
                CarId = bookingDto.CarId
            };

            _context.Bookings.Add(booking);
            _logger.LogInformation("New Booking entity created: Id={BookingId}, StatusId={BookingStatusId}", booking.Id, booking.StatusId);

            bool carStatusShouldChange = booking.StatusId == BOOKING_STATUS_ACTIVE && car.StatusId == CAR_STATUS_AVAILABLE;
            if (carStatusShouldChange)
            {
                _logger.LogInformation("Changing car status from {OldCarStatus} to {NewCarStatus} (RENTED).", car.StatusId, CAR_STATUS_RENTED);
                car.StatusId = CAR_STATUS_RENTED;
                _context.Entry(car).State = EntityState.Modified;
            }

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("SaveChangesAsync successful for Booking Id {BookingId}.", booking.Id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DbUpdateException during SaveChanges: {ErrorMessage}", ex.InnerException?.Message);
                return StatusCode(500, "Помилка збереження даних: " + ex.Message);
            }

            bookingDto.Id = booking.Id;
            _logger.LogInformation("----- PostBooking END - Success -----");
            return CreatedAtAction(nameof(GetBooking), new { id = booking.Id }, bookingDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutBooking(int id, [FromBody] BookingDto bookingDto)
        {
            _logger.LogInformation("----- PutBooking START for Id {Id} -----", id);
            if (id != bookingDto.Id)
            {
                _logger.LogWarning("ID mismatch: Route Id {RouteId} does not match BookingDto.Id {DtoId}.", id, bookingDto.Id);
                return BadRequest("ID бронювання не співпадає.");
            }

            var bookingToUpdate = await _context.Bookings.FindAsync(id);
            if (bookingToUpdate == null)
            {
                _logger.LogWarning("Booking with Id {Id} not found.", id);
                return NotFound();
            }

            int originalCarId = bookingToUpdate.CarId;
            var newSelectedCar = await _context.Cars.FindAsync(bookingDto.CarId);
            if (newSelectedCar == null)
            {
                _logger.LogWarning("Car with Id {CarId} not found.", bookingDto.CarId);
                return BadRequest("Новий обраний автомобіль не знайдено.");
            }

            // Валідація дат перенесена з CompareDatesAttribute
            if (bookingDto.StartDate >= bookingDto.EndDate)
            {
                _logger.LogWarning("Validation failed: StartDate ({StartDate}) is not before EndDate ({EndDate}).", bookingDto.StartDate, bookingDto.EndDate);
                return BadRequest("Дата початку повинна бути раніше дати закінчення.");
            }

            if (!await _context.BookingStatusTypes.AnyAsync(s => s.Id == bookingDto.StatusId))
            {
                _logger.LogWarning("Invalid booking status: StatusId {StatusId} does not exist.", bookingDto.StatusId);
                return BadRequest("Некоректний статус бронювання.");
            }

            if (bookingDto.StatusId == BOOKING_STATUS_ACTIVE && bookingDto.CarId != originalCarId && newSelectedCar.StatusId != CAR_STATUS_AVAILABLE)
            {
                _logger.LogWarning("Validation failed: New car (Id {CarId}) is not available for active booking. CarStatusId={CarStatusId}.", bookingDto.CarId, newSelectedCar.StatusId);
                return BadRequest("Обраний новий автомобіль недоступний для активного бронювання.");
            }

            if (bookingDto.StatusId == BOOKING_STATUS_ACTIVE)
            {
                var overlappingBookings = await _context.Bookings
                    .Where(b => b.CarId == bookingDto.CarId && b.Id != bookingDto.Id && b.StatusId == BOOKING_STATUS_ACTIVE)
                    .Where(b => b.StartDate < bookingDto.EndDate && b.EndDate > bookingDto.StartDate)
                    .AnyAsync();
                if (overlappingBookings)
                {
                    _logger.LogWarning("Validation failed: Overlapping active booking for CarId {CarId}.", bookingDto.CarId);
                    return BadRequest("Автомобіль уже активно заброньовано на ці дати.");
                }
            }

            bookingToUpdate.UserName = bookingDto.UserName;
            bookingToUpdate.StartDate = bookingDto.StartDate;
            bookingToUpdate.EndDate = bookingDto.EndDate;
            bookingToUpdate.StatusId = bookingDto.StatusId;
            bookingToUpdate.CarId = bookingDto.CarId;

            _context.Entry(bookingToUpdate).State = EntityState.Modified;

            bool carEffectivelyChanged = originalCarId != bookingToUpdate.CarId;
            if (carEffectivelyChanged)
            {
                await UpdateCarStatusInternal(originalCarId);
            }
            await UpdateCarStatusInternal(bookingToUpdate.CarId);

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("----- PutBooking END - Success for Id {Id} -----", id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DbUpdateException during SaveChanges: {ErrorMessage}", ex.InnerException?.Message);
                return StatusCode(500, "Помилка збереження даних: " + ex.Message);
            }

            return Ok(bookingDto);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBooking(int id)
        {
            _logger.LogInformation("----- DeleteBooking START for Id {Id} -----", id);
            var bookingToDelete = await _context.Bookings.FindAsync(id);
            if (bookingToDelete == null)
            {
                _logger.LogWarning("Booking with Id {Id} not found.", id);
                return NotFound(new { message = "Бронювання не знайдено." });
            }

            int carIdOfDeletedBooking = bookingToDelete.CarId;
            _context.Bookings.Remove(bookingToDelete);
            await UpdateCarStatusInternal(carIdOfDeletedBooking);

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("----- DeleteBooking END - Success for Id {Id} -----", id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DbUpdateException during SaveChanges: {ErrorMessage}", ex.InnerException?.Message);
                return StatusCode(500, "Помилка видалення даних: " + ex.Message);
            }

            return Ok(new { message = "Бронювання успішно видалено." });
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

            if (hasActiveBookings && car.StatusId != CAR_STATUS_RENTED)
            {
                car.StatusId = CAR_STATUS_RENTED;
                _context.Entry(car).State = EntityState.Modified;
            }
            else if (!hasActiveBookings && car.StatusId == CAR_STATUS_RENTED)
            {
                car.StatusId = CAR_STATUS_AVAILABLE;
                _context.Entry(car).State = EntityState.Modified;
            }
        }
    }
}