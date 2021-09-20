using System;
using System.Collections.Generic;

namespace McJenny.WebAPI.Data.Models
{
    public partial class MenuItem
    {
        public int MenuId { get; set; }
        public int DishId { get; set; }
        public int MenuItemId { get; set; }

        public virtual Dish Dish { get; set; }
        public virtual Menu Menu { get; set; }
    }
}
