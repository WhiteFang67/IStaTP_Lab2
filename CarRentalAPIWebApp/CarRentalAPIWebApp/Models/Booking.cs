using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.ComponentModel.DataAnnotations;

namespace CarRentalAPIWebApp.Models
{
    public class Booking
    {
        public int Id { get; set; }

        [Required]
        public int CarId { get; set; }

        [Required]
        public string UserName { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public int StatusId { get; set; } // Зовнішній ключ до BookingStatusType

        [ValidateNever]
        public Car Car { get; set; } // Навігаційна властивість, без [Required]

        [ValidateNever]
        public BookingStatusType Status { get; set; } // Навігаційна властивість
    }
}