using CarRentalAPIWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CarRentalAPIWebApp.Pages.Bookings
{
    public class CreateModel : PageModel
    {
        private readonly CarRentalAPIContext _context;
        private const int CAR_STATUS_AVAILABLE = 1;
        private const int CAR_STATUS_RENTED = 2;
        private const int BOOKING_STATUS_ACTIVE = 1;
        private const int BOOKING_STATUS_PLANNED = 4;

        public CreateModel(CarRentalAPIContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Booking Booking { get; set; }

        public List<Car> Cars { get; set; }

        public async Task OnGetAsync()
        {
            Cars = await _context.Cars.Where(c => c.StatusId == CAR_STATUS_AVAILABLE).ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                Cars = await _context.Cars.Where(c => c.StatusId == CAR_STATUS_AVAILABLE).ToListAsync();
                return Page();
            }

            // Автоматично визначаємо статус на основі дати
            var today = DateTime.Today;
            if (Booking.StartDate.Date <= today)
            {
                Booking.StatusId = BOOKING_STATUS_ACTIVE;
            }
            else
            {
                Booking.StatusId = BOOKING_STATUS_PLANNED;
            }

            var car = await _context.Cars.FindAsync(Booking.CarId);
            if (car == null)
            {
                ModelState.AddModelError("Booking.CarId", "Автомобіль не знайдено.");
                Cars = await _context.Cars.Where(c => c.StatusId == CAR_STATUS_AVAILABLE).ToListAsync();
                return Page();
            }

            if (Booking.StatusId == BOOKING_STATUS_ACTIVE && car.StatusId != CAR_STATUS_AVAILABLE)
            {
                ModelState.AddModelError("Booking.CarId", "Автомобіль недоступний для активного бронювання.");
                Cars = await _context.Cars.Where(c => c.StatusId == CAR_STATUS_AVAILABLE).ToListAsync();
                return Page();
            }

            if (Booking.StatusId == BOOKING_STATUS_ACTIVE)
            {
                var overlapping = await _context.Bookings
                    .AnyAsync(m => m.CarId == Booking.CarId && m.StatusId == BOOKING_STATUS_ACTIVE &&
                                   m.StartDate < Booking.EndDate && m.EndDate > Booking.StartDate);
                if (overlapping)
                {
                    ModelState.AddModelError("Booking.EndDate", "Автомобіль уже заброньовано на ці дати.");
                    Cars = await _context.Cars.Where(c => c.StatusId == CAR_STATUS_AVAILABLE).ToListAsync();
                    return Page();
                }
            }

            _context.Bookings.Add(Booking);

            // Змінюємо статус авто тільки для активного бронювання
            if (Booking.StatusId == BOOKING_STATUS_ACTIVE)
            {
                car.StatusId = CAR_STATUS_RENTED;
                _context.Entry(car).State = EntityState.Modified;
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Бронювання успішно створено!";
                return RedirectToPage("/Bookings/Index");
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError("", "Помилка збереження: " + (ex.InnerException?.Message ?? ex.Message));
                Cars = await _context.Cars.Where(c => c.StatusId == CAR_STATUS_AVAILABLE).ToListAsync();
                return Page();
            }
        }
    }
}