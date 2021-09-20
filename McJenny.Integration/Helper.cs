using McJenny.WebAPI.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McJenny.Integration
{
    public static class HelperFuck
    {
        public static bool MessedUpDB(FoodChainsDbContext context)
        {
            if (context.Positions.Count() > 6) return true;
            if (context.Employees.Count() > 5000) return true;
            if (context.Locations.Count() > 500) return true;
            if (context.Managements.Count() > 500) return true;
            if (context.SupplyLinks.Count() > 6390) return true;
            if (context.Suppliers.Count() > 234) return true;
            if (context.MenuItems.Count() > 621) return true;
            if (context.Dishes.Count() > 62) return true;
            if (context.Menus.Count() > 15) return true;
            return false;
        }
    }
}
