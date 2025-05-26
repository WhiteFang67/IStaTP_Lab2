using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CarRentalAPIWebApp.Models
{
    public class Car
    {
        public Car()
        {
            Bookings = new List<Booking>();
        }

        public int Id { get; set; }

        [Required(ErrorMessage = "Поле Марка є обов'язковим")]
        [Display(Name = "Марка")]
        public string Brand { get; set; }

        [Required(ErrorMessage = "Поле Модель є обов'язковим")]
        [Display(Name = "Модель")]
        public string Model { get; set; }

        [Required(ErrorMessage = "Поле Рік є обов'язковим")]
        [Range(1950, 2025, ErrorMessage = "Рік має бути між 1950 і 2025")]
        [Display(Name = "Рік")]
        public int Year { get; set; }

        [Required(ErrorMessage = "Поле Статус є обов'язковим")]
        [Display(Name = "Статус")]
        public int StatusId { get; set; } // Зовнішній ключ до CarStatusType

        [Required(ErrorMessage = "Поле Ціна за день є обов'язковим")]
        [Range(0.01, 10000, ErrorMessage = "Ціна за день має бути між 0.01 і 10000")]
        [Display(Name = "Ціна за день")]
        public decimal PricePerDay { get; set; }

        [ValidateNever]
        public CarStatusType Status { get; set; } // Навігаційна властивість

        [ValidateNever]
        public ICollection<Booking> Bookings { get; set; }
    }
}