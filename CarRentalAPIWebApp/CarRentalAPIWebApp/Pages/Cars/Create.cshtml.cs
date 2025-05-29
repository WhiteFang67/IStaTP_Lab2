using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CarRentalAPIWebApp.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CarRentalAPIWebApp.Pages.Cars
{
    public class CreateModel : PageModel
    {
        private readonly ILogger<CreateModel> _logger;
        private readonly CarRentalAPIContext _context;
        private const int CAR_STATUS_RENTED = 2; // ID ������� "����������"

        public CreateModel(ILogger<CreateModel> logger, CarRentalAPIContext context)
        {
            _logger = logger;
            _context = context;
        }

        [BindProperty]
        public Car Car { get; set; }

        public List<CarStatusType> AvailableStatuses { get; set; }

        public async Task OnGetAsync()
        {
            _logger.LogInformation("³���������� ������� ��������� ���������.");
            // Գ������� �������, ���������� "����������"
            AvailableStatuses = await _context.CarStatusTypes
                .Where(s => s.Id != CAR_STATUS_RENTED)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("������� �������� ��� �������� ���������.");
                AvailableStatuses = await _context.CarStatusTypes
                    .Where(s => s.Id != CAR_STATUS_RENTED)
                    .ToListAsync();
                return Page();
            }

            _logger.LogInformation("��������� ������ ���������: {@Car}", Car);
            _context.Cars.Add(Car);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "��������� ������ ������!";
            return RedirectToPage("/Cars/Index");
        }
    }
}