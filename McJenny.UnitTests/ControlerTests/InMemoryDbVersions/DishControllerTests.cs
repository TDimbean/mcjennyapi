using FluentAssertions;
using McJenny.WebAPI.Controllers;
using McJenny.WebAPI.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
//using Nito.AsyncEx.UnitTests;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.Logging;
using Moq;

namespace McJenny.UnitTests.ControlerTests.InMemoryDbVersions
{
    [TestClass]
    public class DishControllerTests
    {
        private static DishesController _controller;
        private static FoodChainsDbContext _context;
        private static ILogger<DishesController> _logger;
        
        [ClassInitialize]
        public static void Init(TestContext testContext)
        {
            var loggerMock = new Mock<ILogger<DishesController>>();
            _logger = loggerMock.Object;
            Setup();
        }

        [TestInitialize]
        public void InitTest() => Setup();

        #region Gets

        #region Basic

        [TestMethod]
        public async Task GetDishes_HappyFlow_ShouldReturnAllDishes()
        {
            // Arrange
            var expected = (object)await _context.Dishes.Select(d => new { ID = d.DishId, d.Name }).ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetDishes()).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetDish_HappyFlow_ShouldReturnDish()
        {
            // Arrange
            var dbDish = _context.Dishes.FirstOrDefault();
            var supCatIds = _context.DishRequirements
                .Where(r => r.DishId == dbDish.DishId)
                .Select(r => r.SupplyCategoryId)
                .ToArray();

            var supplyCategories = _context.SupplyCategories
                .Where(c => supCatIds.Contains(c.SupplyCategoryId))
                .Select(c => new { ID = c.SupplyCategoryId, c.Name })
                .ToArray();

            var expected = (object)new { dish=dbDish.Name , supplyCategories };

            // Act
            var result = (object)(await _controller.GetDish(dbDish.DishId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetDish_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var badId = GenBadDishId;

            // Act
            var result = (await _controller.GetDish(badId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetDishBasic_HappyFlow_ShouldReturnDish()
        {
            // Arrange
            var expected = await _context.Dishes.FirstOrDefaultAsync();

            // Act
            var result = (await _controller.GetDishBasic(GetFirstDishId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetDishBasic_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var badId = GenBadDishId;

            // Act
            var result = (await _controller.GetDishBasic(badId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetLocations_HappyFlow_ShoudReturnLocationsServingDish()
        {
            // Arrange
            var dishId = GetFirstDishId;
            
            var menuIds = _context.MenuItems.Where(i => i.DishId == dishId)
                .Select(i => i.MenuId)
                .Distinct().ToArray();

            var expected = (object)_context.Locations.Where(l => menuIds.Contains(l.MenuId))
                .ToArray()
                .Select(l => string.Format("({0}) {1}, {2}{3} {4}",
                    l.LocationId,
                    l.AbreviatedCountry,
                    l.AbreviatedState == "N/A" ? string.Empty : l.AbreviatedState + ", ",
                    l.City, l.Street)
                );

            // Act
            var result = (object)(await _controller.GetLocations(dishId)).Value;

            // Assert
            (result).Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetLocations_InexistentId_ShoudReturnNotFound()
        {
            //Arrange
            var badId = GenBadDishId;

            // Act
            var resut = (await _controller.GetLocations(badId)).Result;

            // Assert
            resut.Should().BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetRequirements_HappyFlow_ShoudReturnRequirements()
        {
            // Arrange
            var dishId = GetFirstDishId;

            var catIds = await _context.DishRequirements
                .Where(r => r.DishId == dishId)
                .Select(r => r.SupplyCategoryId)
                .ToArrayAsync();

            var expected = await _context.SupplyCategories
                .Where(c => catIds.Contains(c.SupplyCategoryId))
                .Select(c => string.Format("({0}) {1}", c.SupplyCategoryId, c.Name))
                .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetRequirements(dishId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetRequirements_InexistentId_ShoudReturnNotFound()
        {
            //Arrange
            var badId = _context.Dishes.Select(d => d.DishId).ToArray().OrderBy(id => id).LastOrDefault() + 1;

            // Act
            var resut = (await _controller.GetRequirements(badId)).Result;

            // Assert
            resut.Should().BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetMenus_HappyFlowShould_ReturnMenusContainingDish()
        {
            // Arrange
            var dishId = GetFirstDishId;

            var expected = (object)_context.MenuItems.Where(i => i.DishId == dishId).Select(i => i.MenuId).Distinct().ToArray();

            // Act
            var result = (object)(await _controller.GetMenus(dishId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }
        
        [TestMethod]
        public async Task GetMenus_InexistentId_ShoudReturnNotFound()
        {
            //Arrange
            var badId = GenBadDishId;

            // Act
            var resut = (await _controller.GetMenus(badId)).Result;

            // Assert
            resut.Should().BeOfType<NotFoundResult>();
        }

        #endregion

        #region Advanced

        [TestMethod]
        public async Task GetDishesFiltered_ByName_ShouldReturnFiltered()
        {
            // Arrange
            var filter = await _context.Dishes.Select(d => d.Name).FirstOrDefaultAsync();

            var expected = (object)await _context.Dishes
                 .Select(d => new { d.DishId, d.Name })
                 .Where(d => d.Name.ToUpper()
                     .Contains(filter.ToUpper()))
                 .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetDishesFiltered(filter)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetDishesPaged_HappyFlow_ShouldReturnPaged()
        {
            // Arrange
            var pgSz = 2;
            var pgInd = 1;

            var expected = (object)await _context.Dishes
                 .Select(d => new { d.DishId, d.Name })
                 .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetDishesPaged(pgInd, pgSz)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetDishesPaged_EndPageIncomplete_ShouldReturnPaged()
        {
            // Arrange
            var dishCount = await _context.Dishes.CountAsync();
            var pgSz = 1;

            while(true)
            {
                if (dishCount % pgSz == 0) pgSz++;
                else break;
            }

            var pgInd = dishCount / (pgSz - 1);

            var expected = (object)await _context.Dishes
                 .Select(d => new { d.DishId, d.Name })
                 .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArrayAsync();

            // Act
            var call = (await _controller.GetDishesPaged(pgInd, pgSz)).Value;
            var result = (object)call;
            var resCount = (result as IEnumerable<dynamic>).Count();

            // Assert
            result.Should().BeEquivalentTo(expected);
            resCount.Should().BeLessThan(pgSz);
        }

        [TestMethod]
        public async Task GetDishesPaged_SizeOver_ShouldReturnPaged()
        {
            // Arrange
            var dishCount = await _context.Dishes.CountAsync();
            var pgSz = dishCount+1;
            var pgInd = 1;

            var expected = (object)await _context.Dishes
                 .Select(d => new { d.DishId, d.Name })
                 .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetDishesPaged(pgInd, pgSz)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetDishesPaged_IndexOver_ShouldReturnPaged()
        {
            // Arrange
            var dishCount = await _context.Dishes.CountAsync();
            var pgInd = dishCount + 1;
            var pgSz = 1;

            var expected = (object)await _context.Dishes
                 .Select(d => new { d.DishId, d.Name })
                 .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetDishesPaged(pgInd, pgSz)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetDishesSorted_ByNameAsc_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "name";

            var expected = (object)await _context.Dishes
                            .Select(d => new { d.DishId, d.Name })
                            .OrderBy(d => d.Name)
                            .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetDishesSorted(sortBy, false)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetDishesSorted_Desc_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "name";

            var expected = (object)await _context.Dishes
                            .Select(d => new { d.DishId, d.Name })
                            .OrderByDescending(d => d.Name)
                            .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetDishesSorted(sortBy, true)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetDishesSorted_GarbledText_ShouldDefaultoIdSort()
        {
            // Arrange
            var sortBy = "asd";

            var expected = (object)await _context.Dishes
                            .Select(d => new { d.DishId, d.Name })
                            .OrderBy(d => d.DishId)
                            .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetDishesSorted(sortBy, false)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        #endregion

        #endregion

        #region Creates

        #region Deleted

        //[TestMethod]
        //public async Task CreateDish_RequirementsWithBadCategoryIds_ShouldReturnBadRequest()
        //{
        //    // Arrange
        //    var inexistentSupplyCategoryId = (await _context.SupplyCategories
        //        .Select(c => c.SupplyCategoryId)
        //        .LastOrDefaultAsync()) + 1;
        //    var intendedRequirements = new DishRequirement[]
        //        { new DishRequirement { SupplyCategoryId = inexistentSupplyCategoryId } };

        //    var dish = new Dish { Name = "Pretzel", DishRequirements = intendedRequirements };

        //    // Act
        //    var result = (await _controller.CreateDish(dish)).Result;

        //    // Assert
        //    result.Should().BeOfType<BadRequestResult>();
        //}

        //[TestMethod]
        //public async Task CreateDish_ItemsWithBadMenuIds_ShouldReturnBadRequest()
        //{
        //    // Arrange
        //    var inexistentMenuId = (await _context.Menus
        //        .Select(m=>m.MenuId)
        //        .LastOrDefaultAsync()) + 1;
        //    var intendedItems = new MenuItem[]
        //        { new MenuItem { MenuId = inexistentMenuId } };

        //    var dish = new Dish { Name = "Pretzel", MenuItems=intendedItems };

        //    // Act
        //    var result = (await _controller.CreateDish(dish)).Result;

        //    // Assert
        //    result.Should().BeOfType<BadRequestResult>();
        //}

        //[TestMethod]
        //public async Task CreateDish_WithDupeRequirements_ShouldCreateAndReturnDish()
        //{
        //    // Arrange
        //    var intendedRequirements = new DishRequirement[] 
        //    { 
        //        new DishRequirement { SupplyCategoryId = 4 },
        //        new DishRequirement { SupplyCategoryId = 4 },
        //    };

        //    //Requirements are automatically submitted to the db
        //    var dish = new Dish { Name = "Pretzel", DishRequirements = intendedRequirements };

        //    // Act
        //    var result = (await _controller.CreateDish(dish)).Result;

        //    // Assert
        //    result.Should().BeOfType<BadRequestResult>();
        //}

        //[TestMethod]
        //public async Task CreateDish_WithDupeItems_ShouldCreateAndReturnDish()
        //{
        //    // Arrange
        //    var intendedMenuItems = new MenuItem[]
        //    {
        //        new MenuItem { MenuId = 1 },
        //        new MenuItem { MenuId = 1 }
        //    };

        //    //MenuItems are automatically submitted to the db
        //    var dish = new Dish { Name = "Pretzel", MenuItems = intendedMenuItems };

        //    // Act
        //    var result = (await _controller.CreateDish(dish)).Result;

        //    // Assert
        //    result.Should().BeOfType<BadRequestResult>();
        //}

        //[TestMethod]
        //public async Task CreateDish_ItemHasDish_ShouldCreateAndReturnDish()
        //{
        //    // Arrange
        //    var intendedMenuItems = new MenuItem[]
        //    {
        //        new MenuItem { MenuId = 1, Dish = new Dish()  }
        //    };

        //    //MenuItems are automatically submitted to the db
        //    var dish = new Dish { Name = "Pretzel", MenuItems = intendedMenuItems };

        //    // Act
        //    var result = (await _controller.CreateDish(dish)).Result;

        //    // Assert
        //    result.Should().BeOfType<BadRequestResult>();
        //}

        //[TestMethod]
        //public async Task CreateDish_ItemHasMenu_ShouldCreateAndReturnDish()
        //{
        //    // Arrange
        //    var intendedMenuItems = new MenuItem[]
        //    {
        //        new MenuItem { MenuId = 1, Menu = new Menu()  }
        //    };

        //    //MenuItems are automatically submitted to the db
        //    var dish = new Dish { Name = "Pretzel", MenuItems = intendedMenuItems };

        //    // Act
        //    var result = (await _controller.CreateDish(dish)).Result;

        //    // Assert
        //    result.Should().BeOfType<BadRequestResult>();
        //}

        //[TestMethod]
        //public async Task CreateDish_RequirementHasDish_ShouldCreateAndReturnDish()
        //{
        //    // Arrange
        //    var intendedDishRequirements = new DishRequirement[]
        //    {
        //        new DishRequirement { Dish=new Dish()  }
        //    };

        //    //MenuItems are automatically submitted to the db
        //    var dish = new Dish { Name = "Pretzel", DishRequirements = intendedDishRequirements };

        //    // Act
        //    var result = (await _controller.CreateDish(dish)).Result;

        //    // Assert
        //    result.Should().BeOfType<BadRequestResult>();
        //}

        //[TestMethod]
        //public async Task CreateDish_RequirementHasCategory_ShouldCreateAndReturnDish()
        //{
        //    // Arrange
        //    var intendedDishRequirements = new DishRequirement[]
        //    {
        //        new DishRequirement { SupplyCategory=new SupplyCategory()  }
        //    };

        //    //MenuItems are automatically submitted to the db
        //    var dish = new Dish { Name = "Pretzel", DishRequirements = intendedDishRequirements };

        //    // Act
        //    var result = (await _controller.CreateDish(dish)).Result;

        //    // Assert
        //    result.Should().BeOfType<BadRequestResult>();
        //}

        //[TestMethod]
        //public async Task CreateDish_WithRequirementsNoItem_ShouldCreateAndReturnDish()
        //{
        //    //// Arrange
        //    //Make intended Requirements and store index of last req BEFORE adding dish
        //    var intendedRequirements = new DishRequirement[] { new DishRequirement { SupplyCategoryId = 4 } };
        //    var lastReqId = (await _context.DishRequirements.LastOrDefaultAsync()).DishRequirementId;

        //    //Requirements are automatically submitted to the db
        //    var dish = new Dish { Name = "Pretzel" , DishRequirements = intendedRequirements};

        //    //// Act
        //    await _controller.CreateDish(dish);
        //    var result = (await _controller.GetDishBasic(dish.DishId)).Value;

        //    //Reconstruct what the Requirements Without their naviagtional props look like
        //    var expectedRequirements = IntendedToExpectedDishRequirementArray(intendedRequirements, lastReqId, dish.DishId);

        //    //Get actual db Requirements
        //    var reqResult = await GetSimpleDishRequirementArray(lastReqId);

        //    //// Assert
        //    result.Should().BeEquivalentTo(dish);
        //    reqResult.Should().BeEquivalentTo(expectedRequirements);  
        //}

        //[TestMethod]
        //public async Task CreateDish_WithItemsNoRequirement_ShouldCreateAndReturnDish()
        //{
        //    //// Arrange
        //    //Make intended MenuItems and store index of last req BEFORE adding dish
        //    var intendedMenuItems = new MenuItem[] 
        //    { 
        //        new MenuItem { MenuId = 1 }, 
        //        new MenuItem { MenuId = 2 } 
        //    };
        //    var lastItemId = (await _context.MenuItems.LastOrDefaultAsync()).MenuItemId;

        //    //MenuItems are automatically submitted to the db
        //    var dish = new Dish { Name = "Pretzel", MenuItems = intendedMenuItems };

        //    //// Act
        //    await _controller.CreateDish(dish);
        //    var result = (await _controller.GetDishBasic(dish.DishId)).Value;

        //    //Reconstruct what the MenuItems Without their naviagtional props look like
        //    var expectedMenuItems = IntendedToExpectedMenuItemArray(intendedMenuItems, lastItemId, dish.DishId);

        //    //Get actual db MenuItems
        //    var itmResult = await GetSimpleMenuItemArray(lastItemId);

        //    //// Assert
        //    result.Should().BeEquivalentTo(dish);
        //    itmResult.Should().BeEquivalentTo(expectedMenuItems);
        //}

        //[TestMethod]
        //public async Task CreateDish_WithRequirementsAndItems_ShouldCreateAndReturnDish()
        //{
        //    //// Arrange
        //    var intendedRequirements = new DishRequirement[] { new DishRequirement { SupplyCategoryId = 4 } };
        //    var intendedMenuItems = new MenuItem[]
        //    {
        //        new MenuItem { MenuId = 1 },
        //        new MenuItem { MenuId = 2 }
        //    };
        //    var lastReqId = (await _context.DishRequirements.LastOrDefaultAsync()).DishRequirementId;
        //    var lastItemId = (await _context.MenuItems.LastOrDefaultAsync()).MenuItemId;


        //    //Requirements are automatically submitted to the db
        //    var dish = new Dish 
        //    { 
        //        Name = "Pretzel", 
        //        DishRequirements = intendedRequirements,
        //        MenuItems = intendedMenuItems
        //    };

        //    //// Act
        //    await _controller.CreateDish(dish);
        //    var result = (await _controller.GetDishBasic(dish.DishId)).Value;

        //    //Reconstruct
        //    var expectedMenuItems = IntendedToExpectedMenuItemArray(intendedMenuItems, lastItemId, dish.DishId);
        //    var expectedRequirements = IntendedToExpectedDishRequirementArray(intendedRequirements, lastReqId, dish.DishId);

        //    //Get actual
        //    var itmResult = await GetSimpleMenuItemArray(lastItemId);
        //    var reqResult = await GetSimpleDishRequirementArray(lastReqId);

        //    //// Assert
        //    result.Should().BeEquivalentTo(dish);
        //    reqResult.Should().BeEquivalentTo(expectedRequirements);
        //    itmResult.Should().BeEquivalentTo(expectedMenuItems);
        //}

        #endregion

        [TestMethod]
        public async Task CreateDish_HappyFlow_ShouldCreateAndReturnDish()
        {
            // Arrange
            var dish = new Dish { Name = "Pretzel" };

            // Act
            await _controller.CreateDish(dish);
            var result = (await _controller.GetDishBasic(dish.DishId)).Value;
            
            // Assert
            result.Should().BeEquivalentTo(dish);
        }

        [TestMethod]
        public async Task CreateDish_TriesToSetId_ShouldReturnBadRequest()
        {
            // Arrange
            var dish = new Dish { DishId = 7, Name = "Pretzel" };

            // Act
            var result = (await _controller.CreateDish(dish)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [DataTestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow(null)]
        [DataRow("012345678911234567892123456789312345678941234567890")]
        public async Task CreateDish_BadNames_ShouldReturnBadRequest(string name)
        {
            // Arrange
            var dish = new Dish { Name=name};

            // Act
            var result = (await _controller.CreateDish(dish)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateDish_DuplicateName_ShouldReturnBadRequest()
        {
            // Arrange
            var exName = await _context.Dishes.Select(d => d.Name).FirstOrDefaultAsync();
            var dish = new Dish {Name = exName };
            var lwrDish = new Dish { Name = exName.ToLower() };
            var uprDish = new Dish { Name = exName.ToUpper() };

            // Act
            var result = (await _controller.CreateDish(dish)).Result;
            var lwrResult = (await _controller.CreateDish(lwrDish)).Result;
            var uprResult = (await _controller.CreateDish(uprDish)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            lwrResult.Should().BeOfType<BadRequestResult>();
            uprResult.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateDish_MenuItemsNull_ShouldReturnBadRequest()
        {
            // Arrange
            var dish = new Dish { Name = "Pretzel", MenuItems = null };

            // Act
            var result = (await _controller.CreateDish(dish)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateDish_DishRequirementsNull_ShouldReturnBadRequest()
        {
            // Arrange
            var dish = new Dish { Name = "Pretzel", DishRequirements=null };

            // Act
            var result = (await _controller.CreateDish(dish)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateDish_MenuItemsNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var dish = new Dish 
            { 
                Name = "Pretzel" ,
                MenuItems = new MenuItem[] {new MenuItem() } 
            };

            // Act
            var result = (await _controller.CreateDish(dish)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateDish_DishRequirementsNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var dish = new Dish 
            { 
                Name = "Pretzel", 
                DishRequirements=new DishRequirement[] { new DishRequirement() }
            };

            // Act
            var result = (await _controller.CreateDish(dish)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        #endregion

        #region Updates

        [TestMethod]
        public async Task UpdateDish_HappyFlow_ShouldUpdateAndReturnDish()
        {
            // Arrange
            var oldDish = await _context.Dishes.FindAsync(GetFirstDishId);
            var oldDishCopy = new Dish
            {
                DishId = oldDish.DishId,
                DishRequirements = oldDish.DishRequirements,
                MenuItems = oldDish.MenuItems,
                Name = oldDish.Name
            };

            var dish = new Dish { Name = "Cherry Pie" };

            // Act
            await _controller.UpdateDish(oldDish.DishId, dish);
            var result = (await _controller.GetDishBasic(oldDish.DishId)).Value;

            dish.DishRequirements = oldDishCopy.DishRequirements;
            dish.MenuItems = oldDishCopy.MenuItems;
            dish.DishId = oldDishCopy.DishId;

            // Assert
            result.Should().BeEquivalentTo(dish);
            result.Should().NotBeEquivalentTo(oldDishCopy);
        }

        [TestMethod]
        public async Task UpdateDish_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var oldDish = await _context.Dishes.FindAsync(GetFirstDishId);
            var oldDishCopy = new Dish
            {
                DishId = oldDish.DishId,
                DishRequirements = oldDish.DishRequirements,
                MenuItems = oldDish.MenuItems,
                Name = oldDish.Name
            };

            var dish = new Dish { Name = "Cherry Pie" };

            // Act
            var result = await _controller.UpdateDish(GenBadDishId, dish);
            var updDish = (await _controller.GetDishBasic(oldDish.DishId)).Value;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
            updDish.Should().BeEquivalentTo(oldDishCopy);
        }

        [TestMethod]
        public async Task UpdateDish_TriesToSetId_ShouldReturnBadRequest()
        {
            // Arrange
            var oldDish = await _context.Dishes.FindAsync(GetFirstDishId);
            var oldDishCopy = new Dish
            {
                DishId = oldDish.DishId,
                DishRequirements = oldDish.DishRequirements,
                MenuItems = oldDish.MenuItems,
                Name = oldDish.Name
            };

            var dish = new Dish 
            { 
                Name = "Cherry Pie", 
                DishId = GenBadDishId
            };

            // Act
            var result = await _controller.UpdateDish(oldDish.DishId, dish);
            var updatedDish = await _context.Dishes.FindAsync(GetFirstDishId);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedDish.Should().BeEquivalentTo(oldDishCopy);
        }

        [DataTestMethod]
        [DataRow(" ")]
        [DataRow("012345678911234567892123456789312345678941234567890")]
        public async Task UpdateDish_BadNames_ShouldReturnBadRequest(string name)
        {
            // Arrange
            var oldDish = await _context.Dishes.FindAsync(GetFirstDishId);
            var oldDishCopy = new Dish
            {
                DishId = oldDish.DishId,
                DishRequirements = oldDish.DishRequirements,
                MenuItems = oldDish.MenuItems,
                Name = oldDish.Name
            };

            var dish = new Dish
            {
                Name = name
            };

            // Act
            var result = await _controller.UpdateDish(oldDish.DishId, dish);
            var updatedDish = await _context.Dishes.FindAsync(GetFirstDishId);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedDish.Should().BeEquivalentTo(oldDishCopy);
        }

        [TestMethod]
        public async Task UpdateDish_DuplicateName_ShouldReturnBadRequest()
        {
            // Arrange
            var oldDish = await _context.Dishes.FindAsync(GetFirstDishId);
            var oldDishCopy = new Dish
            {
                DishId = oldDish.DishId,
                DishRequirements = oldDish.DishRequirements,
                MenuItems = oldDish.MenuItems,
                Name = oldDish.Name
            };
            var dupeSource = await _context.Dishes
                .Select(d => d.Name).Skip(1)
                .FirstOrDefaultAsync();

            var dish = new Dish{ Name = dupeSource };

            // Act
            var result = await _controller.UpdateDish(oldDish.DishId, dish);
            var updatedDish = await _context.Dishes.FindAsync(GetFirstDishId);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedDish.Should().BeEquivalentTo(oldDishCopy);
        }

        [TestMethod]
        public async Task UpdateDish_MenuItemsNull_ShouldReturnBadRequest()
        {
            // Arrange
            var oldDish = await _context.Dishes.FindAsync(GetFirstDishId);
            var oldDishCopy = new Dish
            {
                DishId = oldDish.DishId,
                DishRequirements = oldDish.DishRequirements,
                MenuItems = oldDish.MenuItems,
                Name = oldDish.Name
            };

            var dish = new Dish
            {
                Name = "Cherry Pie",
                MenuItems = null
            };

            // Act
            var result = await _controller.UpdateDish(oldDish.DishId, dish);
            var updatedDish = await _context.Dishes.FindAsync(GetFirstDishId);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedDish.Should().BeEquivalentTo(oldDishCopy);
        }

        [TestMethod]
        public async Task UpdateDish_DishRequirementsNull_ShouldReturnBadRequest()
        {
            // Arrange
            var oldDish = await _context.Dishes.FindAsync(GetFirstDishId);
            var oldDishCopy = new Dish
            {
                DishId = oldDish.DishId,
                DishRequirements = oldDish.DishRequirements,
                MenuItems = oldDish.MenuItems,
                Name = oldDish.Name
            };

            var dish = new Dish
            {
                Name = "Cherry Pie",
                DishRequirements = null
            };

            // Act
            var result = await _controller.UpdateDish(oldDish.DishId, dish);
            var updatedDish = await _context.Dishes.FindAsync(GetFirstDishId);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedDish.Should().BeEquivalentTo(oldDishCopy);
        }

        [TestMethod]
        public async Task UpdateDish_MenuItemsNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var oldDish = await _context.Dishes.FindAsync(GetFirstDishId);
            var oldDishCopy = new Dish
            {
                DishId = oldDish.DishId,
                DishRequirements = oldDish.DishRequirements,
                MenuItems = oldDish.MenuItems,
                Name = oldDish.Name
            };

            var dish = new Dish
            {
                Name = "Cherry Pie",
                MenuItems = new MenuItem[] {new MenuItem()}
            };

            // Act
            var result = await _controller.UpdateDish(oldDish.DishId, dish);
            var updatedDish = await _context.Dishes.FindAsync(GetFirstDishId);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedDish.Should().BeEquivalentTo(oldDishCopy);
        }

        [TestMethod]
        public async Task UpdateDish_DishRequirementsNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var oldDish = await _context.Dishes.FindAsync(GetFirstDishId);
            var oldDishCopy = new Dish
            {
                DishId = oldDish.DishId,
                DishRequirements = oldDish.DishRequirements,
                MenuItems = oldDish.MenuItems,
                Name = oldDish.Name
            };

            var dish = new Dish
            {
                Name = "Cherry Pie",
                DishRequirements = new DishRequirement[] {new DishRequirement()}
            };

            // Act
            var result = await _controller.UpdateDish(oldDish.DishId, dish);
            var updatedDish = await _context.Dishes.FindAsync(GetFirstDishId);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedDish.Should().BeEquivalentTo(oldDishCopy);
        }

        #endregion

        [TestMethod]
        public void DishExists_ItDoes_ShoudReturnTrue()
        {
            //Arrange
            var methodInfo = typeof(DishesController).GetMethod("DishExists", BindingFlags.NonPublic | BindingFlags.Instance);
            var parameters = new object[] { GetFirstDishId };

            // Act
            var result = ((methodInfo.Invoke(_controller, parameters)) as Task<bool>).Result;

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public void DishExists_ItDoesNot_ShoudReturnFalse()
        {
            //Arrange
            var methodInfo = typeof(DishesController).GetMethod("DishExists", BindingFlags.NonPublic | BindingFlags.Instance);
            var parameters = new object[] { GenBadDishId };

            // Act
            var result = ((methodInfo.Invoke(_controller, parameters)) as Task<bool>).Result;

            // Assert
            result.Should().BeFalse();
        }

        [ClassCleanup]
        public static void CleanUp()
        {
            _controller = null;
            _context.Dispose();
        }

        [TestCleanup]
        public void TestClear()
        {
            _context.Dispose();
            _controller = new DishesController(_context, _logger);
        }

        /// Helpers

        int GetFirstDishId => _context.Dishes.Select(d => d.DishId).OrderBy(id => id).FirstOrDefault();
        int GenBadDishId => _context.Dishes.Select(d => d.DishId).ToArray().OrderBy(id => id).LastOrDefault() + 1;

        DishRequirement[] IntendedToExpectedDishRequirementArray
            (DishRequirement[] intendedRequirements, int lastReqId, int dishId)
        {
            var expectedRequirements = new DishRequirement[intendedRequirements.Length];
            var dbReqIndex = 0;
            foreach (var req in intendedRequirements)
            {
                dbReqIndex++;
                expectedRequirements[dbReqIndex - 1] = new DishRequirement
                {
                    DishRequirementId = lastReqId + dbReqIndex,
                    DishId = dishId,
                    SupplyCategoryId = req.SupplyCategoryId
                };
            }
            return expectedRequirements;
        }

        MenuItem[] IntendedToExpectedMenuItemArray
            (MenuItem[] intendedItems, int lastItmId, int dishId)
        {
            var expectedItems = new MenuItem[intendedItems.Length];
            var dbItmIndex = 0;
            foreach (var itm in intendedItems)
            {
                dbItmIndex++;
                expectedItems[dbItmIndex - 1] = new MenuItem
                {
                    MenuItemId = lastItmId + dbItmIndex,
                    DishId = dishId,
                    MenuId = itm.MenuId
                };
            }
            return expectedItems;
        }
   
        async Task<DishRequirement[]> GetSimpleDishRequirementArray(int lastReqId)
        {
            var reqQuery = await _context.DishRequirements.Where(r => r.DishRequirementId > lastReqId).ToArrayAsync();
            var reqResult = new DishRequirement[reqQuery.Length];
            var dbReqIndex = 0;
            foreach (var req in reqQuery)
            {
                reqResult[dbReqIndex] = new DishRequirement
                {
                    DishRequirementId = reqQuery[dbReqIndex].DishRequirementId,
                    DishId = reqQuery[dbReqIndex].DishId,
                    SupplyCategoryId = reqQuery[dbReqIndex].SupplyCategoryId
                };
                dbReqIndex++;
            };
            return reqResult;
        }

        async Task<MenuItem[]> GetSimpleMenuItemArray(int lastItmId)
        {
            var itmQuery = await _context.MenuItems.Where(r => r.MenuItemId > lastItmId).ToArrayAsync();
            var itmResult = new MenuItem[itmQuery.Length];
            var dbItmIndex = 0;
            foreach (var itm in itmQuery)
            {
                itmResult[dbItmIndex] = new MenuItem
                {
                    MenuItemId = itmQuery[dbItmIndex].MenuItemId,
                    DishId = itmQuery[dbItmIndex].DishId,
                    MenuId = itmQuery[dbItmIndex].MenuId
                };
                dbItmIndex++;
            };
            return itmResult;
        }

        private static void Setup()
        {
            _context = InMemoryHelpers.GetContext();
            _controller = new DishesController(_context, _logger);
        }
    }
}
