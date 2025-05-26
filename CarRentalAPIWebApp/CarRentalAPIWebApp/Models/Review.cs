using System.ComponentModel.DataAnnotations;

namespace CarRentalAPIWebApp.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ім'я користувача є обов'язковим.")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Коментар є обов'язковим.")]
        [StringLength(500, ErrorMessage = "Коментар не може перевищувати 500 символів.")]
        public string Comment { get; set; }

        [Required(ErrorMessage = "Дата є обов'язковою.")]
        public DateTime Date { get; set; }
    }
}