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

            if (Booking.CarId != 0)
            {
                var carExists = await _context.Cars.AnyAsync(c => c.Id == Booking.CarId);
                if (!carExists)
                {
                    Debug.WriteLine($"Invalid CarId: {Booking.CarId} - car does not exist in the database.");
                    TempData["WarningMessage"] = $"Автомобіль (ID: {Booking.CarId}), що був раніше пов'язаний з цим бронюванням, більше не існує в системі. Будь ласка, оберіть інший автомобіль.";
                }
            }

            var availableCars = await _context.Cars
                .Where(c => c.StatusId == CAR_STATUS_AVAILABLE)
                .Select(c => new
                {
                    c.Id,
                    DisplayName = $"{c.Brand} {c.Model}"
                })
                .ToListAsync();

            if (!availableCars.Any())
            {
                Debug.WriteLine($"No cars with status 'Available' (StatusId = {CAR_STATUS_AVAILABLE}) found.");
                string noCarsMessage = "Увага: немає доступних автомобілів для вибору.";
                if (Booking.CarId != 0 && !availableCars.Any(c => c.Id == Booking.CarId))
                {
                    var currentCar = await _context.Cars.FindAsync(Booking.CarId);
                    if (currentCar != null)
                        noCarsMessage = $"Поточний автомобіль '{currentCar.Brand} {currentCar.Model}' більше не доступний. Доступних автомобілів для вибору немає.";
                    else
                        noCarsMessage = $"Попередньо обраний автомобіль не знайдено. Доступних автомобілів для вибору також немає.";
                }
                TempData["InfoMessage"] = noCarsMessage;
                Cars = new SelectList(Enumerable.Empty<SelectListItem>(), "Id", "DisplayName");
            }
            else
            {
                Cars = new SelectList(availableCars, "Id", "DisplayName", Booking.CarId);
            }

            var bookingStatuses = await _context.BookingStatusTypes.ToListAsync();
            if (!bookingStatuses.Any())
            {
                Debug.WriteLine("No booking statuses found in database.");
                TempData["ErrorMessage"] = (TempData["ErrorMessage"]?.ToString() ?? "") + " Не вдалося завантажити список статусів бронювання.";
                Statuses = new SelectList(Enumerable.Empty<SelectListItem>(), "Id", "DisplayName");
            }
            else
            {
                if (!bookingStatuses.Any(s => s.Id == Booking.StatusId))
                {
                    Debug.WriteLine($"Invalid Booking.StatusId: {Booking.StatusId} not found in BookingStatusTypes.");
                    TempData["WarningMessage"] = (TempData["WarningMessage"]?.ToString() ?? "") + " Попередній статус бронювання не знайдено. Будь ласка, оберіть статус.";
                }
                Statuses = new SelectList(bookingStatuses, "Id", "DisplayName", Booking.StatusId);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Debug.WriteLine($"OnPostAsync called with Booking.Id: {Booking?.Id}");

            if (!ModelState.IsValid)
            {
                Debug.WriteLine("ModelState is invalid.");

                var availableCarsForPost = await _context.Cars
                    .Where(c => c.StatusId == CAR_STATUS_AVAILABLE)
                    .Select(c => new
                    {
                        c.Id,
                        DisplayName = $"{c.Brand} {c.Model}"
                    })
                    .ToListAsync();
                Cars = new SelectList(availableCarsForPost, "Id", "DisplayName", Booking?.CarId);

                var bookingStatusesForPost = await _context.BookingStatusTypes.ToListAsync();
                Statuses = new SelectList(bookingStatusesForPost, "Id", "DisplayName", Booking?.StatusId);

                return Page();
            }

            var carToBook = await _context.Cars.FindAsync(Booking.CarId);
            if (carToBook == null)
            {
                TempData["ErrorMessage"] = "Обраний автомобіль не знайдено в базі даних.";
                await RepopulateListsForErrorAsync();
                return Page();
            }

            const int BOOKING_STATUS_ACTIVE = 1;
            if (Booking.StatusId == BOOKING_STATUS_ACTIVE && carToBook.StatusId != CAR_STATUS_AVAILABLE)
            {
                TempData["ErrorMessage"] = $"Автомобіль '{carToBook.Brand} {carToBook.Model}' більше не доступний для активного бронювання. Його поточний статус ID: {carToBook.StatusId}.";
                await RepopulateListsForErrorAsync();
                return Page();
            }

            if (!await _context.BookingStatusTypes.AnyAsync(s => s.Id == Booking.StatusId))
            {
                TempData["ErrorMessage"] = "Обраний статус бронювання недійсний.";
                await RepopulateListsForErrorAsync();
                return Page();
            }

            var bookingToUpdate = await _context.Bookings.FindAsync(Booking.Id);
            if (bookingToUpdate == null)
            {
                TempData["ErrorMessage"] = "Бронювання, яке ви намагаєтесь оновити, не знайдено.";
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
            catch (DbUpdateConcurrencyException ex)
            {
                Debug.WriteLine($"Concurrency error updating booking: {ex.Message}");
                TempData["ErrorMessage"] = "Помилка конкурентного доступу при оновленні бронювання. Можливо, дані були змінені іншим користувачем. Спробуйте ще раз.";
                var entry = ex.Entries.Single();
                var databaseValues = await entry.GetDatabaseValuesAsync();
                if (databaseValues == null)
                {
                    TempData["ErrorMessage"] = "Бронювання було видалено іншим користувачем.";
                }
                await RepopulateListsForErrorAsync(Booking.CarId, Booking.StatusId);
                return Page();
            }
            catch (DbUpdateException ex)
            {
                Debug.WriteLine($"Error updating booking: {ex.InnerException?.Message ?? ex.Message}");
                TempData["ErrorMessage"] = "Помилка при збереженні змін: " + (ex.InnerException?.Message ?? ex.Message);
                await RepopulateListsForErrorAsync(Booking.CarId, Booking.StatusId);
                return Page();
            }
        }

        private async Task RepopulateListsForErrorAsync(int? selectedCarId = null, int? selectedStatusId = null)
        {
            var availableCarsForError = await _context.Cars
                .Where(c => c.StatusId == CAR_STATUS_AVAILABLE)
                .Select(c => new
                {
                    c.Id,
                    DisplayName = $"{c.Brand} {c.Model}"
                })
                .ToListAsync();
            Cars = new SelectList(availableCarsForError, "Id", "DisplayName", selectedCarId ?? Booking?.CarId);

            var bookingStatusesForError = await _context.BookingStatusTypes.ToListAsync();
            Statuses = new SelectList(bookingStatusesForError, "Id", "DisplayName", selectedStatusId ?? Booking?.StatusId);
        }
    }
}
