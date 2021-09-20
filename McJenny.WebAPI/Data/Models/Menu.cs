using System;
using System.Collections.Generic;

namespace McJenny.WebAPI.Data.Models
{
    public partial class Menu
    {
        public Menu()
        {
            Locations = new HashSet<Location>();
            MenuItems = new HashSet<MenuItem>();
        }

        public int MenuId { get; set; }

        public virtual ICollection<Location> Locations { get; set; }
        public virtual ICollection<MenuItem> MenuItems { get; set; }
    }
}
