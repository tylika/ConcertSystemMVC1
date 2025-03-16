using System;
using System.Collections.Generic;

namespace ConcertSystemDomain.Model;

public partial class PurchaseItem : Entity
{
    public int PurchaseId { get; set; }

    public int TicketId { get; set; }

    public int Quantity { get; set; }

    public decimal Price { get; set; }

    public virtual Purchase Purchase { get; set; } = null!;

    public virtual Ticket Ticket { get; set; } = null!;
}
