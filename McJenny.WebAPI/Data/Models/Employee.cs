using System;
using System.Collections.Generic;

namespace McJenny.WebAPI.Data.Models
{
    public partial class Employee
    {
        public Employee()
        {
            Managements = new HashSet<Management>();
        }

        public int EmployeeId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int LocationId { get; set; }
        public int PositionId { get; set; }
        public int WeeklyHours { get; set; }
        public DateTime StartedOn { get; set; }

        public virtual Location Location { get; set; }
        public virtual Position Position { get; set; }
        public virtual ICollection<Management> Managements { get; set; }
    }
}
