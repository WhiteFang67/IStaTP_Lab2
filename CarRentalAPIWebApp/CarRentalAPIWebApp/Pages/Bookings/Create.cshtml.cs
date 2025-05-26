using CarRentalAPIWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic; // Додано для List<T>
using System.Linq; // Додано для .Where()
using System.Threading.Tasks; // Додано для Task
using static CarRentalAPIWebApp.Controllers.BookingsController; // BookingDto тут не використовується напряму, якщо він визначений у контролері, то це залежність. Якщо він локальний, то все гаразд. Припускаємо, що BookingDto визначено коректно.

namespace CarRentalAPIWebApp.Pages.Bookings
{
    public class CreateModel : PageModel
    {
        private readonly CarRentalAPIContext _context;

        public CreateModel(CarRentalAPIContext context)
        {
            _context = context;
        }

        [BindProperty]
        public BookingDto Booking { get; set; } // Припускаємо, що BookingDto визначено і містить потрібні поля

        public List<Car> Cars { get; set; }
        public List<BookingStatusType> BookingStatuses { get; set; }

        public async Task OnGetAsync()
        {
            // Фільтруємо автомобілі за StatusId = 1 (Доступне)
            Cars = await _context.Cars.Where(c => c.StatusId == 1).ToListAsync();
            BookingStatuses = await _context.BookingStatusTypes.ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // Якщо модель не валідна, перезавантажуємо список доступних автомобілів
                Cars = await _context.Cars.Where(c => c.StatusId == 1).ToListAsync();
                BookingStatuses = await _context.BookingStatusTypes.ToListAsync();
                return Page();
            }

            var booking = new Booking
            {
                CarId = Booking.CarId,
                UserName = Booking.UserName,
                StartDate = Booking.StartDate,
                EndDate = Booking.EndDate,
                StatusId = Booking.StatusId
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Bookings/Index");
        }
    }

}