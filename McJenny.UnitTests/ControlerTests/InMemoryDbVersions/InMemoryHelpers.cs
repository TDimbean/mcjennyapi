using McJenny.WebAPI.Controllers;
using McJenny.WebAPI.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace McJenny.UnitTests.ControlerTests.InMemoryDbVersions
{
    public static class InMemoryHelpers
    {
        public static FoodChainsDbContext GetContext()
        {
            var options = new DbContextOptionsBuilder<FoodChainsDbContext>()
            .UseInMemoryDatabase(databaseName: "FakeFoodsDatabase")
            .Options;

            var context = new FoodChainsDbContext(options);

            var dishes = new List<Dish>
            {
                new Dish { DishId = 1, Name = "Apple Pie" },
                new Dish { DishId = 2, Name = "Meat Pie" },
                new Dish { DishId = 3, Name = "Cheesy Danish"}
            };

            var dishRequirements = new List<DishRequirement>
            {
                //Apple Pie
                new DishRequirement { DishRequirementId = 1, DishId = 1, SupplyCategoryId = 2 },
                new DishRequirement { DishRequirementId = 2, DishId = 1, SupplyCategoryId = 4 },
                //Meat Pie
                new DishRequirement { DishRequirementId = 3, DishId = 2, SupplyCategoryId = 3 },
                new DishRequirement { DishRequirementId = 4, DishId = 2, SupplyCategoryId = 4 },
                //Cheesy Danish
                new DishRequirement { DishRequirementId = 5, DishId = 3, SupplyCategoryId = 5 },
                new DishRequirement { DishRequirementId = 6, DishId = 3, SupplyCategoryId = 4 },
            };
            var employees = new List<Employee>
            {
                new Employee
                {
                    EmployeeId = 1,
                    FirstName = "Will",
                    LastName = "Smith",
                    LocationId = 1,
                    PositionId = 1,
                    StartedOn = new DateTime(2018, 4, 4),
                    WeeklyHours = 38
                },
                new Employee
                {
                    EmployeeId = 2,
                    FirstName = "Giusepe",
                    LastName = "Giorgio",
                    LocationId = 2,
                    PositionId = 1,
                    StartedOn = new DateTime(2019, 3, 3),
                    WeeklyHours = 34
                },
                new Employee
                {
                    EmployeeId = 3,
                    FirstName = "Kwent",
                    LastName = "Dellaross",
                    LocationId = 2,
                    PositionId = 2,
                    StartedOn = new DateTime(2019, 6, 3),
                    WeeklyHours = 30
                }
            };
            var locations = new List<Location>
            {
                new Location{ LocationId = 1 , AbreviatedCountry="FR", AbreviatedState="97", City="Bordeaux", State="Aquitaine",
                    OpenSince=new DateTime(2016, 8, 26), MenuId=1, ScheduleId=1, Street="12 Beauclaire Walk", Country="France"},
                new Location{ LocationId = 2 , AbreviatedCountry="FR", AbreviatedState="B9", City="Chambéry", State="Rhône-Alpes",
                    OpenSince=new DateTime(2015, 5, 15), MenuId=2, ScheduleId=2, Street="9 Debussy Road", Country="France"},
                new Location{ LocationId = 3 , AbreviatedCountry="FR", AbreviatedState="A1", City="Nevers", State="Bourgogne",
                    OpenSince=new DateTime(2018, 4, 20), MenuId=3, ScheduleId=2, Street="10 Blanc Avenue", Country="France"},
                new Location{ LocationId = 4 , AbreviatedCountry="IT", AbreviatedState="LZ", City="Roma", State="Lazio",
                    OpenSince=new DateTime(2017, 3 , 20), MenuId=1, ScheduleId=1, Street="32 Apple Boulevard", Country="Italy"},
            };
            var managements = new List<Management>
            {
                new Management {ManagementId=1, ManagerId=1, LocationId=1},
                new Management {ManagementId=2, ManagerId=2, LocationId=2}
            };
            var menus = new List<Menu>
            {
                new Menu{MenuId=1}, new Menu{MenuId=2}, new Menu{MenuId=3},
            };
            var menuItems = new List<MenuItem>
            {
                new MenuItem{MenuItemId=1, MenuId=1, DishId=1/*Apple Pie*/},
                new MenuItem{MenuItemId=2, MenuId=2, DishId=2/*Meat Pie*/},
                new MenuItem{MenuItemId=3, MenuId=3, DishId=3/*Cheesy Danish*/}
            };
            var positions = new List<Position>
            {
            new Position{PositionId=1, Title="Manager", Wage=  24.00m},
            new Position{PositionId=2, Title="Intern", Wage=  12.45m},
            new Position{PositionId=3, Title="Casheer", Wage=  14.35m},
            new Position{PositionId=4, Title="Cook", Wage=  16.50m },
            new Position{PositionId=5, Title="Custodian", Wage=   10.00m },
            new Position{PositionId=6, Title="Waiter", Wage=  17.80m },
            };
            var schedules = new List<Schedule> 
            { 
                new Schedule{ScheduleId=1, TimeTable="24/7"},
                new Schedule{ScheduleId=2, TimeTable="Weekdays: 8:00-22:00; Saturays: 10~23:00; Closed on Sundays"}
            };
            var suppliers = new List<Supplier> 
            { 
                new Supplier { SupplierId=1, AbreviatedCountry="FR", AbreviatedState = "97", City="Bordeaux", Name="Les Vegetales",
                Country = "France", State="Aquitaine"},
                new Supplier { SupplierId=2, AbreviatedCountry="FR", AbreviatedState = "B3", City="Balma", Name="A la Moutton",
                Country = "France", State="Midi-Pyrénées"},
                new Supplier { SupplierId=3, AbreviatedCountry="FR", AbreviatedState = "A8", City="Gentilly", Name="Patiserie Hills",
                Country = "France", State="Île-de-France"},
                new Supplier { SupplierId=4, AbreviatedCountry="FR", AbreviatedState = "A1", City="Nevers", Name="Lait du Bourgogne",
                Country = "France", State="Bourgogne"}
            };
            var supplierStocks = new List<SupplierStock> 
            { 
                new SupplierStock { SupplierStockId=1, SupplierId=1, SupplyCategoryId=1},
                new SupplierStock { SupplierStockId=2, SupplierId=1, SupplyCategoryId=2},
                new SupplierStock { SupplierStockId=3, SupplierId=2, SupplyCategoryId=3},
                new SupplierStock { SupplierStockId=4, SupplierId=3, SupplyCategoryId=4},
                new SupplierStock { SupplierStockId=5, SupplierId=4, SupplyCategoryId=5},
            };
            var supplyLinks = new List<SupplyLink> 
            { 
                new SupplyLink{ SupplyLinkId = 1, LocationId = 1, SupplierId=1, SupplyCategoryId=2},
                new SupplyLink{ SupplyLinkId = 2, LocationId = 1, SupplierId=3, SupplyCategoryId=4},
                new SupplyLink{ SupplyLinkId = 3, LocationId = 2, SupplierId=2, SupplyCategoryId=3},
                new SupplyLink{ SupplyLinkId = 4, LocationId = 2, SupplierId=3, SupplyCategoryId=4},
                new SupplyLink{ SupplyLinkId = 5, LocationId = 3, SupplierId=4, SupplyCategoryId=5},
                new SupplyLink{ SupplyLinkId = 6, LocationId = 3, SupplierId=3, SupplyCategoryId=4},
                new SupplyLink{ SupplyLinkId = 7, LocationId = 4, SupplierId=1, SupplyCategoryId=2},
                new SupplyLink{ SupplyLinkId = 8, LocationId = 4, SupplierId=3, SupplyCategoryId=4},
            };
            var supplyCategories = new List<SupplyCategory>
            {
                new SupplyCategory { SupplyCategoryId = 1, Name = "Vegetables" },
                new SupplyCategory { SupplyCategoryId = 2, Name = "Fruit" },
                new SupplyCategory { SupplyCategoryId = 3, Name = "Meat" },
                new SupplyCategory { SupplyCategoryId = 4, Name = "Baked Goods" },
                new SupplyCategory { SupplyCategoryId = 5, Name = "Dairy" },
                new SupplyCategory { SupplyCategoryId = 6, Name = "Beverages" }
            };

            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            context.Dishes.AddRange(dishes);
            context.DishRequirements.AddRange(dishRequirements);
            context.Employees.AddRange(employees);
            context.Locations.AddRange(locations);
            context.Managements.AddRange(managements);
            context.Menus.AddRange(menus);
            context.MenuItems.AddRange(menuItems);
            context.Positions.AddRange(positions);
            context.Schedules.AddRange(schedules);
            context.Suppliers.AddRange(suppliers);
            context.SupplierStocks.AddRange(supplierStocks);
            context.SupplyLinks.AddRange(supplyLinks);
            context.SupplyCategories.AddRange(supplyCategories);
            context.SaveChanges();

            return context;
        }
    }
}
