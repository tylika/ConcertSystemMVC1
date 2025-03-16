using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ConcertSystemDomain.Model;

public partial class Artist : Entity
{
    [Required(ErrorMessage = "Поле Назва є обов'язковим")]
    [Display(Name = "Назва")]
    public string FullName { get; set; } = null!;
    [Required(ErrorMessage = "Поле Соціальні мережі є обов'язковим")]
    public string? SocialMedia { get; set; }

    public virtual ICollection<Concert> Concerts { get; set; } = new List<Concert>();
}
