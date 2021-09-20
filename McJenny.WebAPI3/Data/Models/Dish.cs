using System;
using System.Collections.Generic;

namespace McJenny.WebAPI.Data.Models
{
    public partial class Dish
    {
        public Dish()
        {
            DishRequirements = new HashSet<DishRequirement>();
            MenuItems = new HashSet<MenuItem>();
        }
        public int DishId { get; set; }
        public string Name { get; set; }

        public virtual ICollection<DishRequirement> DishRequirements { get; set; }
        public virtual ICollection<MenuItem> MenuItems { get; set; }
    }
}
