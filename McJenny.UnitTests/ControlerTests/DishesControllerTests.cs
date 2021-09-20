using EntityFrameworkCore3Mock;
using FluentAssertions;
using McJenny.WebAPI.Controllers;
using McJenny.WebAPI.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace McJenny.UnitTests.ControlerTests
{
    [TestClass]
    public class DishesControllerTests
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
            var expected = await _context.Dishes.Select(d => new { ID = d.DishId, d.Name }).ToArrayAsync();

            // Act
            var result = (await _controller.GetDishes()).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetDish_HappyFlow_ShouldReturnDish()
        {
            // Arrange
            var dishId = await _context.Dishes.Select(d => d.DishId).FirstOrDefaultAsync();
            var dbDish = await _context.Dishes.FindAsync(dishId);
            var supCatIds = _context.DishRequirements
                .Where(r => r.DishId == dbDish.DishId)
                .Select(r => r.SupplyCategoryId)
                .ToArray();

            var supplyCategories = _context.SupplyCategories
                .Where(c => supCatIds.Contains(c.SupplyCategoryId))
                .Select(c => new { ID = c.SupplyCategoryId, c.Name })
                .ToArray();

            var expected = (object)new { dish = dbDish.Name, supplyCategories };

            // Act
            var result = (object)(await _controller.GetDish(dishId)).Value;

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
            var dishId = await _context.Dishes
                .Select(d => d.DishId)
                .FirstOrDefaultAsync();
            var expected = (object)await _context.Dishes.FindAsync(dishId);

            // Act
            var result = (object)(await _controller.GetDishBasic(dishId)).Value;

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

            var expected = (object)await _context.SupplyCategories
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

            var expected = (object)_context.MenuItems
                .Where(i => i.DishId == dishId)
                .Select(i => i.MenuId)
                .Distinct().ToArray();

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

            while (true)
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
            var result = (object)((await _controller.GetDishesPaged(pgInd, pgSz)).Value);
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
            var pgSz = dishCount + 1;
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

        [TestMethod]
        public async Task CreateDish_HappyFlow_ShouldCreateAndReturnDish()
        {
            // Arrange
            var dish = new Dish { Name = "Pretzel" };

            // Act
            await _controller.CreateDish(dish);
            var result = (await _controller.GetDishBasic(dish.DishId)).Value as Dish;

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
            var dish = new Dish { Name = name };

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
            var dish = new Dish { Name = exName };
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
            var dish = new Dish { Name = "Pretzel", DishRequirements = null };

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
                Name = "Pretzel",
                MenuItems = new MenuItem[] { new MenuItem() }
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
                DishRequirements = new DishRequirement[] { new DishRequirement() }
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
            var result = (await _controller.GetDishBasic(oldDish.DishId)).Value as Dish;

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

            var dish = new Dish { Name = dupeSource };

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
                MenuItems = new MenuItem[] { new MenuItem() }
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
                DishRequirements = new DishRequirement[] { new DishRequirement() }
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

        private static void Setup()
        {
            var dishes = FakeRepo.Dishes;
            var dishRequirements = FakeRepo.DishRequirements;
            var supplyCategories = FakeRepo.SupplyCategories;
            var locations = FakeRepo.Locations;
            var items = FakeRepo.MenuItems;

            //DbContextOptions<FoodChainsDbContext>
            var DummyOptions = new DbContextOptionsBuilder<FoodChainsDbContext>().Options;

            var dbContextMock = new DbContextMock<FoodChainsDbContext>(DummyOptions);
            var dishesDbSetMock = dbContextMock.CreateDbSetMock(x => x.Dishes, dishes);
            var categoriesDbSetMock = dbContextMock.CreateDbSetMock(x => x.SupplyCategories, supplyCategories);
            var requirementsDbSetMock = dbContextMock.CreateDbSetMock(x => x.DishRequirements, dishRequirements);
            var locationsDbSetMock = dbContextMock.CreateDbSetMock(x => x.Locations, locations);
            var itemsDbSetMock = dbContextMock.CreateDbSetMock(x => x.MenuItems, items);
            dbContextMock.Setup(m => m.MenuItems).Returns(itemsDbSetMock.Object);
            dbContextMock.Setup(m => m.Locations).Returns(locationsDbSetMock.Object);
            dbContextMock.Setup(m => m.Dishes).Returns(dishesDbSetMock.Object);
            dbContextMock.Setup(m => m.SupplyCategories).Returns(categoriesDbSetMock.Object);
            dbContextMock.Setup(m => m.DishRequirements).Returns(requirementsDbSetMock.Object);
            _context = dbContextMock.Object;
            _controller = new DishesController(_context, _logger);
        }
    }
}