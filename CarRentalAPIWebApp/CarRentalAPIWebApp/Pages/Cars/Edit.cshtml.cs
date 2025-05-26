using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CarRentalAPIWebApp.Models;
using System.Threading.Tasks;

namespace CarRentalAPIWebApp.Pages.Cars
{
    public class EditModel : PageModel
    {
        private readonly CarRentalAPIContext _context;
        private readonly ILogger<EditModel> _logger;

        public EditModel(CarRentalAPIContext context, ILogger<EditModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public Car? Car { get; set; }

        public SelectList Statuses { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            _logger.LogInformation("Attempting to load Edit page. OnGetAsync called for Car ID: {CarId}", id);

            if (id == null)
            {
                _logger.LogWarning("Car ID is null in OnGetAsync for Edit page.");
                return NotFound("Car ID is null.");
            }

            Car = await _context.Cars
                .Include(c => c.Status)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Car == null)
            {
                _logger.LogWarning("Car with ID {Id} not found in OnGetAsync for Edit page.", id);
                return NotFound($"Car with ID {id} not found.");
            }

            Statuses = new SelectList(await _context.CarStatusTypes.ToListAsync(), "Id", "DisplayName");
            _logger.LogInformation("Successfully loaded data for Edit page, Car ID {Id}.", id);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Validation failed for Car ID {Id}.", Car?.Id);
                Statuses = new SelectList(await _context.CarStatusTypes.ToListAsync(), "Id", "DisplayName");
                return Page();
            }

            var carToUpdate = await _context.Cars.FindAsync(Car.Id);
            if (carToUpdate == null)
            {
                _logger.LogWarning("Car with ID {Id} not found in OnPostAsync for Edit page.", Car?.Id);
                return NotFound();
            }

            carToUpdate.Brand = Car.Brand;
            carToUpdate.Model = Car.Model;
            carToUpdate.Year = Car.Year;
            carToUpdate.StatusId = Car.StatusId;
            carToUpdate.PricePerDay = Car.PricePerDay;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Car with ID {Id} successfully updated.", Car.Id);

            TempData["SuccessMessage"] = "Автомобіль успішно відредаговано!";
            return RedirectToPage("/Cars/Index");
        }
    }
}