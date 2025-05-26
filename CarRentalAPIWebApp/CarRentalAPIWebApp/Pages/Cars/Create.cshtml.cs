using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CarRentalAPIWebApp.Models;
using System.Threading.Tasks;

namespace CarRentalAPIWebApp.Pages.Cars
{
    public class CreateModel : PageModel
    {
        private readonly ILogger<CreateModel> _logger;
        private readonly CarRentalAPIContext _context;

        public CreateModel(ILogger<CreateModel> logger, CarRentalAPIContext context)
        {
            _logger = logger;
            _context = context;
        }

        [BindProperty]
        public Car Car { get; set; }

        public SelectList Statuses { get; set; }

        public async Task OnGetAsync()
        {
            _logger.LogInformation("Відображення сторінки створення автомобіля.");
            Statuses = new SelectList(await _context.CarStatusTypes.ToListAsync(), "Id", "DisplayName");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Помилка валідації при створенні автомобіля.");
                Statuses = new SelectList(await _context.CarStatusTypes.ToListAsync(), "Id", "DisplayName");
                return Page();
            }

            _logger.LogInformation("Створення нового автомобіля: {@Car}", Car);
            _context.Cars.Add(Car);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Автомобіль успішно додано!";
            return RedirectToPage("/Cars/Index");
        }
    }
}