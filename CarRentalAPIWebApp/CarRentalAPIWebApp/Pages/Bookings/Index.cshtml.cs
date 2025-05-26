using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CarRentalAPIWebApp.Models;
using System.Threading.Tasks;

namespace CarRentalAPIWebApp.Pages.Bookings
{
    public class IndexModel : PageModel
    {
        private readonly CarRentalAPIContext _context;

        public IndexModel(CarRentalAPIContext context)
        {
            _context = context;
        }

        public List<Booking> Bookings { get; set; }

        public async Task OnGetAsync()
        {
            Bookings = await _context.Bookings
                .Include(b => b.Car)
                .Include(b => b.Status)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                TempData["ErrorMessage"] = "Бронювання не знайдено.";
                return RedirectToPage();
            }

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Бронювання успішно видалено!";
            return RedirectToPage();
        }
    }
}