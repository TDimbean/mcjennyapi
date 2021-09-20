using System;
using System.Collections.Generic;

namespace McJenny.WebAPI.Data.Models
{
    public partial class DishRequirement
    {
        public int DishId { get; set; }
        public int SupplyCategoryId { get; set; }
        public int DishRequirementId { get; set; }

        public virtual Dish Dish { get; set; }
        public virtual SupplyCategory SupplyCategory { get; set; }
    }
}
