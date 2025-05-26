using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CarRentalAPIWebApp.Models;

namespace CarRentalAPIWebApp.Pages.Cars
{
    public class IndexModel : PageModel
    {
        private readonly CarRentalAPIContext _context;

        public IndexModel(CarRentalAPIContext context)
        {
            _context = context;
        }

        public List<Car> Cars { get; set; }

        public async Task OnGetAsync()
        {
            Cars = await _context.Cars
                .Include(c => c.Status)
                .ToListAsync();
        }
    }
}