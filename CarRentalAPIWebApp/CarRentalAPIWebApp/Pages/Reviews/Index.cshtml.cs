using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CarRentalAPIWebApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CarRentalAPIWebApp.Pages.Reviews
{
    public class IndexModel : PageModel
    {
        private readonly CarRentalAPIContext _context;

        public IndexModel(CarRentalAPIContext context)
        {
            _context = context;
        }

        public List<Review> Reviews { get; set; }

        [BindProperty]
        public Review Review { get; set; }

        [BindProperty]
        public Review EditReview { get; set; }

        public async Task OnGetAsync()
        {
            Reviews = await _context.Reviews.ToListAsync();
        }
    }
}