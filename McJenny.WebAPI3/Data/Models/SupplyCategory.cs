using System;
using System.Collections.Generic;

namespace McJenny.WebAPI.Data.Models
{
    public partial class SupplyCategory
    {
        public SupplyCategory()
        {
            DishRequirements = new HashSet<DishRequirement>();
            SupplierStocks = new HashSet<SupplierStock>();
            SupplyLinks = new HashSet<SupplyLink>();
        }

        public int SupplyCategoryId { get; set; }
        public string Name { get; set; }

        public virtual ICollection<DishRequirement> DishRequirements { get; set; }
        public virtual ICollection<SupplierStock> SupplierStocks { get; set; }
        public virtual ICollection<SupplyLink> SupplyLinks { get; set; }
    }
}
