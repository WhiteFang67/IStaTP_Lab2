using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CarRentalAPIWebApp.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;

namespace CarRentalAPIWebApp.Pages.Bookings
{
    public class EditModel : PageModel
    {
        private readonly CarRentalAPIContext _context;
        private const int CAR_STATUS_AVAILABLE = 1;
        private const int BOOKING_STATUS_ACTIVE = 1;

        public EditModel(CarRentalAPIContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Booking Booking { get; set; }

        public SelectList Cars { get; set; }
        public SelectList Statuses { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Debug.WriteLine($"OnGetAsync called with id: {id}");

            Booking = await _context.Bookings.FirstOrDefaultAsync(m => m.Id == id);

            if (Booking == null)
            {
                Debug.WriteLine($"Booking with id {id} not found.");
                TempData["ErrorMessage"] = "Бронювання не знайдено.";
                return RedirectToPage("./Index");
            }

            Debug.WriteLine($"Booking found: Id={Booking.Id}, CarId={Booking.CarId}, StatusId={Booking.StatusId}");

            if (Booking.CarId != 0)
            {
                var carExists = await _context.Cars.AnyAsync(c => c.Id == Booking.CarId);
                if (!carExists)
                {
                    Debug.WriteLine($"Invalid CarId: {Booking.CarId} - car does not exist.");
                    TempData["WarningMessage"] = $"Автомобіль (ID: {Booking.CarId}) більше не існує. Оберіть інший.";
                    Booking.CarId = 0;
                }
            }

            var availableCars = await _context.Cars
                .Where(c => c.StatusId == CAR_STATUS_AVAILABLE || c.Id == Booking.CarId)
                .Select(c => new
                {
                    c.Id,
                    DisplayName = $"{c.Brand} {c.Model}"
                })
                .ToListAsync();

            if (!availableCars.Any())
            {
                Debug.WriteLine($"No available cars found.");
                TempData["ErrorMessage"] = "Немає доступних автомобілів.";
                Cars = new SelectList(Enumerable.Empty<object>(), "Id", "DisplayName");
            }
            else
            {
                Cars = new SelectList(availableCars, "Id", "DisplayName", Booking.CarId);
            }

            var bookingStatuses = await _context.BookingStatusTypes.ToListAsync();
            if (!bookingStatuses.Any())
            {
                Debug.WriteLine("No booking statuses found.");
                TempData["ErrorMessage"] = "Не вдалося завантажити статуси.";
                Statuses = new SelectList(Enumerable.Empty<object>(), "Id", "DisplayName");
            }
            else
            {
                if (!bookingStatuses.Any(s => s.Id == Booking.StatusId))
                {
                    Debug.WriteLine($"Invalid StatusId: {Booking.StatusId}.");
                    TempData["WarningMessage"] = (TempData["WarningMessage"]?.ToString() ?? "") + " Статус не знайдено. Оберіть статус.";
                    Booking.StatusId = 0;
                }
                Statuses = new SelectList(bookingStatuses, "Id", "DisplayName", Booking.StatusId);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Debug.WriteLine($"OnPostAsync called with Booking.Id: {Booking?.Id}");

            // Explicitly validate StartDate < EndDate
            if (Booking.StartDate >= Booking.EndDate)
            {
                ModelState.AddModelError("Booking.StartDate", "Дата початку повинна бути раніше дати закінчення.");
            }

            if (!ModelState.IsValid)
            {
                Debug.WriteLine("ModelState is invalid.");
                await RepopulateListsForErrorAsync();
                return Page();
            }

            var carToBook = await _context.Cars.FindAsync(Booking.CarId);
            if (carToBook == null)
            {
                TempData["ErrorMessage"] = "Автомобіль не знайдено.";
                await RepopulateListsForErrorAsync();
                return Page();
            }

            if (Booking.StatusId == BOOKING_STATUS_ACTIVE && carToBook.StatusId != CAR_STATUS_AVAILABLE)
            {
                TempData["ErrorMessage"] = "Автомобіль недоступний для активного бронювання.";
                await RepopulateListsForErrorAsync();
                return Page();
            }

            if (Booking.StatusId == BOOKING_STATUS_ACTIVE)
            {
                var overlappingBookings = await _context.Bookings
                    .AnyAsync(m => m.CarId == Booking.CarId && m.Id != Booking.Id &&
                                   m.StatusId == BOOKING_STATUS_ACTIVE &&
                                   m.StartDate < Booking.EndDate && m.EndDate > Booking.StartDate);
                if (overlappingBookings)
                {
                    ModelState.AddModelError("Booking.EndDate", "Автомобіль уже заброньовано на ці дати.");
                    await RepopulateListsForErrorAsync();
                    return Page();
                }
            }

            var bookingToUpdate = await _context.Bookings.FindAsync(Booking.Id);
            if (bookingToUpdate == null)
            {
                TempData["ErrorMessage"] = "Бронювання не знайдено.";
                return RedirectToPage("./Index");
            }

            bookingToUpdate.CarId = Booking.CarId;
            bookingToUpdate.UserName = Booking.UserName;
            bookingToUpdate.StartDate = Booking.StartDate;
            bookingToUpdate.EndDate = Booking.EndDate;
            bookingToUpdate.StatusId = Booking.StatusId;

            try
            {
                await _context.SaveChangesAsync();
                Debug.WriteLine($"Booking with id {Booking.Id} updated successfully.");
                TempData["SuccessMessage"] = "Бронювання успішно оновлено!";
                return RedirectToPage("/Bookings/Index");
            }
            catch (DbUpdateException ex)
            {
                Debug.WriteLine($"Error updating booking: {ex.InnerException?.Message ?? ex.Message}");
                TempData["ErrorMessage"] = "Помилка при збереженні змін: " + (ex.InnerException?.Message ?? ex.Message);
                await RepopulateListsForErrorAsync();
                return Page();
            }
        }

        private async Task RepopulateListsForErrorAsync()
        {
            var availableCars = await _context.Cars
                .Where(c => c.StatusId == CAR_STATUS_AVAILABLE || c.Id == (Booking != null ? Booking.CarId : 0))
                .Select(c => new
                {
                    Id = c.Id,
                    DisplayName = $"{c.Brand} {c.Model}"
                })
                .ToListAsync();
            Cars = new SelectList(availableCars, "Id", "DisplayName", Booking != null ? Booking.CarId : null);

            var statuses = await _context.BookingStatusTypes.ToListAsync();
            Statuses = new SelectList(statuses, "Id", "DisplayName", Booking != null ? Booking.StatusId : null);
        }
    }
}