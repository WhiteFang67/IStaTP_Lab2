using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace CarRentalAPIWebApp.Models
{
    public class Booking
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Автомобіль є обов'язковим.")]
        public int CarId { get; set; }

        [Required(ErrorMessage = "Ім'я користувача є обов'язковим.")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Дата початку є обов'язковою.")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Дата закінчення є обов'язковою.")]
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "Статус є обов'язковим.")]
        public int StatusId { get; set; }

        [ValidateNever]
        public Car Car { get; set; }

        [ValidateNever]
        public BookingStatusType Status { get; set; }
    }
}