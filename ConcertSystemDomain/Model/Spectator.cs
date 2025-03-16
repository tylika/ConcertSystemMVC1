using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ConcertSystemDomain.Model;

public partial class Spectator : Entity
{
    [Required(ErrorMessage = "Поле ПІБ є обов'язковим")]
    [Display(Name = "ПІБ")]
    public string FullName { get; set; } = null!;

    [Display(Name = "Телефон")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Поле Електронна пошта є обов'язковим")]
    [EmailAddress(ErrorMessage = "Невірний формат електронної пошти")]
    [Display(Name = "Електронна пошта")]
    public string Email { get; set; } = null!;

    public virtual ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
}