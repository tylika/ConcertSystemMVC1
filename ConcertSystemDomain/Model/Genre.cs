using System;
using System.Collections.Generic;

namespace ConcertSystemDomain.Model;

public partial class Genre : Entity
{
    public string Name { get; set; } = null!;

    public virtual ICollection<Concert> Concerts { get; set; } = new List<Concert>();
}
