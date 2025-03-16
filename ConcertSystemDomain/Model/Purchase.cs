using System;
using System.Collections.Generic;

namespace ConcertSystemDomain.Model;

public partial class Purchase : Entity
{
    public int SpectatorId { get; set; }

    public DateTime PurchaseDate { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<PurchaseItem> PurchaseItems { get; set; } = new List<PurchaseItem>();

    public virtual Spectator Spectator { get; set; } = null!;
}
