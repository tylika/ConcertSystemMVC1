using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ConcertSystemDomain.Model;

public partial class Concert : Entity
{
    [Required]
    public int ArtistId { get; set; }

    [Required]
    public DateTime ConcertDate { get; set; }

    [Required]
    [StringLength(100)]
    public string Location { get; set; } = null!;

    [Range(1, int.MaxValue)]
    public int TotalTickets { get; set; }

    [Range(0, int.MaxValue)]
    public int AvailableTickets { get; set; }

    public virtual Artist Artist { get; set; } = null!;

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    public virtual ICollection<Genre> Genres { get; set; } = new List<Genre>();
}
