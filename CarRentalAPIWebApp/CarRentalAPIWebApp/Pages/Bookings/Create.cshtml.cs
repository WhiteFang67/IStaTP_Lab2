using CarRentalAPIWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic; // ������ ��� List<T>
using System.Linq; // ������ ��� .Where()
using System.Threading.Tasks; // ������ ��� Task
using static CarRentalAPIWebApp.Controllers.BookingsController; // BookingDto ��� �� ��������������� �������, ���� �� ���������� � ���������, �� �� ���������. ���� �� ���������, �� ��� ������. ����������, �� BookingDto ��������� ��������.

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
        public BookingDto Booking { get; set; } // ����������, �� BookingDto ��������� � ������ ������ ����

        public List<Car> Cars { get; set; }
        public List<BookingStatusType> BookingStatuses { get; set; }

        public async Task OnGetAsync()
        {
            // Գ������� �������� �� StatusId = 1 (��������)
            Cars = await _context.Cars.Where(c => c.StatusId == 1).ToListAsync();
            BookingStatuses = await _context.BookingStatusTypes.ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // ���� ������ �� ������, ��������������� ������ ��������� ���������
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