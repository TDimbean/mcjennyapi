using System;
using System.Collections.Generic;

namespace McJenny.WebAPI.Data.Models
{
    public partial class Location
    {
        public Location()
        {
            Employees = new HashSet<Employee>();
            Managements = new HashSet<Management>();
            SupplyLinks = new HashSet<SupplyLink>();
        }

        public int LocationId { get; set; }
        public string AbreviatedCountry { get; set; }
        public string AbreviatedState { get; set; }
        public string City { get; set; }
        public string Street { get; set; }
        public string Country { get; set; }
        public string State { get; set; }
        public DateTime OpenSince { get; set; }
        public int MenuId { get; set; }
        public int ScheduleId { get; set; }

        public virtual Menu Menu { get; set; }
        public virtual Schedule Schedule { get; set; }
        public virtual ICollection<Employee> Employees { get; set; }
        public virtual ICollection<Management> Managements { get; set; }
        public virtual ICollection<SupplyLink> SupplyLinks { get; set; }
    }
}
