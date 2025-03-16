using System;
using System.Collections.Generic;

namespace ConcertSystemDomain.Model;

public partial class Ticket : Entity
{
    public int ConcertId { get; set; }

    public string? Row { get; set; }

    public int? SeatNumber { get; set; }

    public decimal BasePrice { get; set; }

    public string Status { get; set; } = null!;

    public virtual Concert Concert { get; set; } = null!;

    public virtual ICollection<PurchaseItem> PurchaseItems { get; set; } = new List<PurchaseItem>();
}
