using System;
using System.Collections.Generic;

namespace McJenny.WebAPI.Data.Models
{
    public partial class Supplier
    {
        public Supplier()
        {
            SupplierStocks = new HashSet<SupplierStock>();
            SupplyLinks = new HashSet<SupplyLink>();
        }

        public int SupplierId { get; set; }
        public string Name { get; set; }
        public string AbreviatedCountry { get; set; }
        public string AbreviatedState { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string State { get; set; }

        public virtual ICollection<SupplierStock> SupplierStocks { get; set; }
        public virtual ICollection<SupplyLink> SupplyLinks { get; set; }
    }
}
