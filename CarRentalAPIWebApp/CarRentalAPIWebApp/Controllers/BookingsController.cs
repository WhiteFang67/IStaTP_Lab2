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
        private readonly ILogger<BookingsController> _logger;

        // Константи для статусів
        private const int BOOKING_STATUS_ACTIVE = 1;
        // private const int BOOKING_STATUS_COMPLETED = 2; // Закоментовано, якщо не використовується прямо в логіці нижче
        // private const int BOOKING_STATUS_CANCELLED = 3; // Закоментовано, якщо не використовується прямо в логіці нижче

        private const int CAR_STATUS_AVAILABLE = 1;
        private const int CAR_STATUS_RENTED = 2;
        private const int CAR_STATUS_UNDER_REPAIR = 3;


        public BookingsController(CarRentalAPIContext context, ILogger<BookingsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // DTO для бронювань
        public class BookingDto
        {
            public int Id { get; set; }
            public string UserName { get; set; }
            public System.DateTime StartDate { get; set; }
            public System.DateTime EndDate { get; set; }
            public int StatusId { get; set; }
            public int CarId { get; set; }
        }

        // GET: api/Bookings
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

        // GET: api/Bookings/5
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

        // POST: api/Bookings
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
                return BadRequest($"Автомобіль '{car.Brand} {car.Model}' недоступний для активного бронювання ( поточний статус ID: {car.StatusId}, потрібен ID: {CAR_STATUS_AVAILABLE}).");
            }

            if (bookingDto.StartDate >= bookingDto.EndDate)
            {
                return BadRequest("Дата початку повинна бути раніше дати закінчення.");
            }

            if (!await _context.BookingStatusTypes.AnyAsync(s => s.Id == bookingDto.StatusId))
            {
                return BadRequest("Некоректний статус бронювання.");
            }

            // Перевірка на перетин активних бронювань
            if (bookingDto.StatusId == BOOKING_STATUS_ACTIVE)
            {
                bool isOverlapping = await _context.Bookings
                    .AnyAsync(b => b.CarId == bookingDto.CarId &&
                                   b.StatusId == BOOKING_STATUS_ACTIVE && // Перевіряємо лише активні бронювання
                                   b.StartDate < bookingDto.EndDate &&
                                   b.EndDate > bookingDto.StartDate);
                if (isOverlapping)
                {
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
            _logger.LogInformation("New Booking entity created and added to context. Its StatusId is {BookingStatusId}", booking.StatusId);

            bool carStatusShouldChange = booking.StatusId == BOOKING_STATUS_ACTIVE && car.StatusId == CAR_STATUS_AVAILABLE;
            _logger.LogInformation("Checking condition to change car status: booking.StatusId ({BookingStatus}) == BOOKING_STATUS_ACTIVE ({ConstBookingActive}) && car.StatusId ({CarStatus}) == CAR_STATUS_AVAILABLE ({ConstCarAvailable}) -> Result: {ShouldChange}",
                booking.StatusId, BOOKING_STATUS_ACTIVE, car.StatusId, CAR_STATUS_AVAILABLE, carStatusShouldChange);

            if (carStatusShouldChange)
            {
                _logger.LogInformation("Condition MET. Changing car status from {OldCarStatus} to {NewCarStatus} (RENTED).", car.StatusId, CAR_STATUS_RENTED);
                car.StatusId = CAR_STATUS_RENTED;
                _context.Entry(car).State = EntityState.Modified;
            }
            else
            {
                _logger.LogInformation("Condition NOT MET. Car status will not be changed by this operation directly.");
            }

            _logger.LogInformation("ChangeTracker before SaveChanges: {ChangeTrackerDebugView}", _context.ChangeTracker.DebugView.LongView);

            try
            {
                int changes = await _context.SaveChangesAsync();
                _logger.LogInformation("SaveChangesAsync successful. Number of state entries written to the database: {ChangesCount}", changes);
                if (carStatusShouldChange)
                {
                    var carAfterSave = await _context.Cars.AsNoTracking().FirstOrDefaultAsync(c => c.Id == car.Id); // Перевіряємо з БД
                    _logger.LogInformation("Car status for CarId {CarId} after SaveChanges (re-fetched AsNoTracking): {CarStatusAfterSave}", car.Id, carAfterSave?.StatusId);
                }
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DbUpdateException during SaveChanges. InnerException: {InnerException}", ex.InnerException?.Message);
                return StatusCode(500, "Помилка збереження даних: " + ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "General Exception during SaveChanges or processing.");
                return StatusCode(500, "Загальна помилка сервера: " + ex.Message);
            }

            bookingDto.Id = booking.Id; // Повертаємо ID створеного бронювання
            _logger.LogInformation("----- PostBooking END - Success -----");
            return CreatedAtAction(nameof(GetBooking), new { id = booking.Id }, bookingDto);
        }

        // PUT: api/Bookings/5
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

            int originalCarId = bookingToUpdate.CarId;
            // int originalBookingStatusId = bookingToUpdate.StatusId; // Зберігаємо для можливої більш складної логіки

            var newSelectedCar = await _context.Cars.FindAsync(bookingDto.CarId);
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

            // Перевірка доступності автомобіля, якщо бронювання активне і автомобіль змінюється
            if (bookingDto.StatusId == BOOKING_STATUS_ACTIVE && bookingDto.CarId != originalCarId && newSelectedCar.StatusId != CAR_STATUS_AVAILABLE)
            {
                return BadRequest("Обраний новий автомобіль недоступний для активного бронювання (має бути 'Доступне').");
            }

            // Перевірка на перетин активних бронювань, якщо поточне бронювання активне
            if (bookingDto.StatusId == BOOKING_STATUS_ACTIVE)
            {
                var overlappingBookings = await _context.Bookings
                    .Where(b => b.CarId == bookingDto.CarId &&
                                b.Id != bookingDto.Id && // Виключаємо поточне бронювання
                                b.StatusId == BOOKING_STATUS_ACTIVE) // Перевіряємо лише інші активні бронювання
                    .Where(b => b.StartDate < bookingDto.EndDate && b.EndDate > bookingDto.StartDate)
                    .AnyAsync();
                if (overlappingBookings)
                {
                    return BadRequest("Автомобіль уже активно заброньовано на ці дати іншим бронюванням.");
                }
            }

            // Оновлюємо дані бронювання
            bookingToUpdate.UserName = bookingDto.UserName;
            bookingToUpdate.StartDate = bookingDto.StartDate;
            bookingToUpdate.EndDate = bookingDto.EndDate;
            bookingToUpdate.StatusId = bookingDto.StatusId;
            bookingToUpdate.CarId = bookingDto.CarId; // CarId міг змінитися

            _context.Entry(bookingToUpdate).State = EntityState.Modified;

            // Оновлення статусів автомобілів
            bool carEffectivelyChanged = originalCarId != bookingToUpdate.CarId;

            if (carEffectivelyChanged)
            {
                // Оновлюємо статус старого автомобіля
                await UpdateCarStatusInternal(originalCarId);
            }

            // Оновлюємо статус нового/поточного автомобіля (завжди, оскільки статус бронювання міг змінитися)
            await UpdateCarStatusInternal(bookingToUpdate.CarId);

            await _context.SaveChangesAsync();
            return Ok(bookingDto); // Або NoContent() для PUT
        }

        // DELETE: api/Bookings/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBooking(int id)
        {
            var bookingToDelete = await _context.Bookings.FindAsync(id);
            if (bookingToDelete == null)
            {
                return NotFound(new { message = "Бронювання не знайдено." });
            }

            int carIdOfDeletedBooking = bookingToDelete.CarId;

            _context.Bookings.Remove(bookingToDelete);

            await UpdateCarStatusInternal(carIdOfDeletedBooking);

            await _context.SaveChangesAsync();
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
    }
}