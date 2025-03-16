using System;
using System.Collections.Generic;

namespace ConcertSystemDomain.Model;

public partial class Spectator : Entity
{
    public string FullName { get; set; } = null!;

    public string? Phone { get; set; }

    public string Email { get; set; } = null!;

    public virtual ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
}
