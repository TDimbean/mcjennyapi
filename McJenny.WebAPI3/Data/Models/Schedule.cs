using System;
using System.Collections.Generic;

namespace McJenny.WebAPI.Data.Models
{
    public partial class Schedule
    {
        public Schedule()
        {
            Locations = new HashSet<Location>();
        }

        public int ScheduleId { get; set; }
        public string TimeTable { get; set; }

        public virtual ICollection<Location> Locations { get; set; }
    }
}
