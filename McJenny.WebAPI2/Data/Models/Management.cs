using System;
using System.Collections.Generic;

namespace McJenny.WebAPI.Data.Models
{
    public partial class Management
    {
        public int LocationId { get; set; }
        public int ManagerId { get; set; }
        public int ManagementId { get; set; }

        public virtual Location Location { get; set; }
        public virtual Employee Manager { get; set; }
    }
}
