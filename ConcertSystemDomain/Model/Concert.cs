using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ConcertSystemDomain.Model
{
    public partial class Concert : Entity
    {
        [Required(ErrorMessage = "Поле Артист є обов'язковим")]
        public int ArtistId { get; set; }

        [Required(ErrorMessage = "Поле Дата концерту є обов'язковим")]
        [CustomDateValidation(ErrorMessage = "Концерт має бути запланований щонайменше за місяць від поточної дати")]
        public DateTime ConcertDate { get; set; }

        [Required(ErrorMessage = "Поле Місто проведення є обов'язковим")]
        [Display(Name = "Місто проведення")]
        public string Location { get; set; } = null!;

        [Required(ErrorMessage = "Поле Загальна кількість квитків є обов'язковим")]
        [Range(1, int.MaxValue, ErrorMessage = "Загальна кількість квитків має бути більше 0")]
        public int TotalTickets { get; set; }

        [Required(ErrorMessage = "Поле Доступні квитки є обов'язковим")]
        [CustomTicketsValidation(ErrorMessage = "Доступних квитків не може бути більше, ніж загальна кількість квитків")]
        public int AvailableTickets { get; set; }

        public virtual Artist Artist { get; set; } = null!;
        public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
        public virtual ICollection<Genre> Genres { get; set; } = new List<Genre>();
    }

    // Кастомна валідація для дати концерту
    public class CustomDateValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is DateTime concertDate)
            {
                // Поточна дата
                DateTime currentDate = DateTime.Now;
                // Додаємо місяць до поточної дати
                DateTime minAllowedDate = currentDate.AddMonths(1);

                if (concertDate < minAllowedDate)
                {
                    return new ValidationResult(ErrorMessage);
                }
            }
            return ValidationResult.Success;
        }
    }

    // Кастомна валідація для кількості квитків
    public class CustomTicketsValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var concert = (Concert)validationContext.ObjectInstance;
            int availableTickets = (int)value;
            int totalTickets = concert.TotalTickets;

            if (availableTickets > totalTickets)
            {
                return new ValidationResult(ErrorMessage);
            }

            if (availableTickets < 0)
            {
                return new ValidationResult("Доступних квитків не може бути менше 0");
            }

            return ValidationResult.Success;
        }
    }
}