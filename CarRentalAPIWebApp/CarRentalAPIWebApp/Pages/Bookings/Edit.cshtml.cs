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
        private const int CAR_STATUS_RENTED = 2;
        private const int BOOKING_STATUS_COMPLETED = 2;
        private const int BOOKING_STATUS_CANCELLED = 3;

        public EditModel(CarRentalAPIContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Booking Booking { get; set; }

        public SelectList Cars { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Booking = await _context.Bookings.FirstOrDefaultAsync(m => m.Id == id);

            if (Booking == null)
            {
                TempData["ErrorMessage"] = "Бронювання не знайдено.";
                return RedirectToPage("./Index");
            }

            // Завантажуємо тільки поточний автомобіль
            var currentCar = await _context.Cars
                .Where(c => c.Id == Booking.CarId)
                .Select(c => new
                {
                    c.Id,
                    DisplayName = $"{c.Brand} {c.Model}"
                })
                .FirstOrDefaultAsync();

            if (currentCar == null)
            {
                TempData["ErrorMessage"] = $"Автомобіль (ID: {Booking.CarId}) більше не існує.";
                return RedirectToPage("./Index");
            }

            Cars = new SelectList(new List<object> { currentCar }, "Id", "DisplayName", Booking.CarId);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await RepopulateListsForErrorAsync();
                return Page();
            }

            // Отримуємо оригінальне бронювання з бази
            var originalBooking = await _context.Bookings.AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == Booking.Id);

            if (originalBooking == null)
            {
                TempData["ErrorMessage"] = "Бронювання не знайдено.";
                return RedirectToPage("./Index");
            }

            // Встановлюємо оригінальний CarId, ігноруючи зміни з форми
            Booking.CarId = originalBooking.CarId;

            // Дозволяємо тільки статуси "Завершене" (2) або "Скасоване" (3)
            if (Booking.StatusId != BOOKING_STATUS_COMPLETED && Booking.StatusId != BOOKING_STATUS_CANCELLED)
            {
                ModelState.AddModelError("Booking.StatusId", "Можна встановити тільки статус 'Завершене' або 'Скасоване'");
                await RepopulateListsForErrorAsync();
                return Page();
            }

            // Валідація дат
            if (Booking.StartDate >= Booking.EndDate)
            {
                ModelState.AddModelError("Booking.StartDate", "Дата початку повинна бути раніше дати закінчення.");
                await RepopulateListsForErrorAsync();
                return Page();
            }

            var bookingToUpdate = await _context.Bookings.FindAsync(Booking.Id);
            if (bookingToUpdate == null)
            {
                TempData["ErrorMessage"] = "Бронювання не знайдено.";
                return RedirectToPage("./Index");
            }

            // Оновлюємо всі поля крім CarId
            bookingToUpdate.UserName = Booking.UserName;
            bookingToUpdate.StartDate = Booking.StartDate;
            bookingToUpdate.EndDate = Booking.EndDate;
            bookingToUpdate.StatusId = Booking.StatusId;

            // Якщо статус змінився на "Завершене" або "Скасоване", перевіряємо чи потрібно змінити статус авто
            if (Booking.StatusId == BOOKING_STATUS_COMPLETED || Booking.StatusId == BOOKING_STATUS_CANCELLED)
            {
                var car = await _context.Cars.FindAsync(Booking.CarId);
                if (car != null && car.StatusId == CAR_STATUS_RENTED)
                {
                    // Перевіряємо чи є інші активні бронювання для цього авто
                    var hasOtherActiveBookings = await _context.Bookings
                        .AnyAsync(b => b.CarId == Booking.CarId &&
                                     b.Id != Booking.Id &&
                                     b.StatusId != BOOKING_STATUS_COMPLETED &&
                                     b.StatusId != BOOKING_STATUS_CANCELLED);

                    if (!hasOtherActiveBookings)
                    {
                        car.StatusId = CAR_STATUS_AVAILABLE;
                        _context.Entry(car).State = EntityState.Modified;
                    }
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Бронювання успішно оновлено!";
                return RedirectToPage("/Bookings/Index");
            }
            catch (DbUpdateException ex)
            {
                TempData["ErrorMessage"] = "Помилка при збереженні змін: " + (ex.InnerException?.Message ?? ex.Message);
                await RepopulateListsForErrorAsync();
                return Page();
            }
        }

        private async Task RepopulateListsForErrorAsync()
        {
            // Завантажуємо тільки поточний автомобіль
            var currentCar = await _context.Cars
                .Where(c => c.Id == Booking.CarId)
                .Select(c => new
                {
                    Id = c.Id,
                    DisplayName = $"{c.Brand} {c.Model}"
                })
                .FirstOrDefaultAsync();

            Cars = new SelectList(currentCar != null ? new List<object> { currentCar } : Enumerable.Empty<object>(),
                                "Id", "DisplayName", Booking?.CarId);
        }
    }
}