using System.ComponentModel.DataAnnotations;

namespace CarRentalAPIWebApp.Models
{
    public class CarStatusType
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [Required]
        [StringLength(100)]
        public string DisplayName { get; set; }
    }
}