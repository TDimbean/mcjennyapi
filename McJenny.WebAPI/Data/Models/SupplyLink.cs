using System;
using System.Collections.Generic;

namespace McJenny.WebAPI.Data.Models
{
    public partial class SupplyLink
    {
        public int SupplyLinkId { get; set; }
        public int SupplierId { get; set; }
        public int LocationId { get; set; }
        public int SupplyCategoryId { get; set; }

        public virtual Location Location { get; set; }
        public virtual Supplier Supplier { get; set; }
        public virtual SupplyCategory SupplyCategory { get; set; }
    }
}
