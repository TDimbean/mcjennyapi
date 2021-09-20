using System;
using System.Collections.Generic;

namespace McJenny.WebAPI.Data.Models
{
    public partial class Position
    {
        public Position()
        {
            Employees = new HashSet<Employee>();
        }

        public int PositionId { get; set; }
        public string Title { get; set; }
        public decimal? Wage { get; set; }

        public virtual ICollection<Employee> Employees { get; set; }
    }
}
