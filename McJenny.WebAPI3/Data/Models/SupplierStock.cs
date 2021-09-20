using System;
using System.Collections.Generic;

namespace McJenny.WebAPI.Data.Models
{
    public partial class SupplierStock
    {
        public int SupplierId { get; set; }
        public int SupplyCategoryId { get; set; }
        public int SupplierStockId { get; set; }

        public virtual Supplier Supplier { get; set; }
        public virtual SupplyCategory SupplyCategory { get; set; }
    }
}
