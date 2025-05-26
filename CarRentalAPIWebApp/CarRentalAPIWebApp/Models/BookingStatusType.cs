using System.ComponentModel.DataAnnotations;

namespace CarRentalAPIWebApp.Models
{
    public class BookingStatusType
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } // Наприклад, "Active", "Completed", "Cancelled"

        [Required]
        [StringLength(100)]
        public string DisplayName { get; set; } // Наприклад, "Активне", "Завершене", "Скасоване"
    }
}