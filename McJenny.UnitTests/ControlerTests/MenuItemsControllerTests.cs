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
    public class MenuItemsControllerTests
    {
        private static MenuItemsController _controller;
        private static FoodChainsDbContext _context;
        private static ILogger<MenuItemsController> _logger;

        [ClassInitialize]
        public static void Init(TestContext testContext)
        {
            _logger = (new Mock<ILogger<MenuItemsController>>()).Object;
            Setup();
        }

        [TestInitialize]
        public void InitTest() => Setup();

        #region Gets

        [TestMethod]
        public async Task GetMenuItems_HappyFlow_ShouldReturnAllMenuItems()
        {
            // Arrange
            var items = await _context.MenuItems.ToArrayAsync();

            var dishIds = items.Select(i => i.DishId).Distinct().ToArray();

            var dishes = await _context.Dishes
                .Where(d => dishIds.Contains(d.DishId))
                .Select(d => new { d.DishId, d.Name })
                .ToArrayAsync();

            var strings = new string[items.Length];
            for (int i = 0; i < items.Length; i++)
                strings[i] = string.Format("Item [{0}]: Dish({1}) \"{2}\" available on Menu: {3}",
                    items[i],
                    items[i].DishId,
                    dishes.SingleOrDefault(d => d.DishId == items[i].DishId).Name,
                    items[i].MenuId);

            var expected = (object)strings;

            // Act
            var result = (object)(await _controller.GetMenuItems()).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetMenuItem_HappyFlow_ShouldReturnMenuItem()
        {
            // Arrange
            var menuItem = await _context.MenuItems.FindAsync(GetFirstMenuItemId);

            var dish = await _context.Dishes
                .Select(d => new { d.DishId, d.Name })
                .FirstAsync(d => d.DishId == menuItem.DishId);

            var expected = (object)string.Format("(MenuItem {0}): Dish {1} \"{2}\" available on Menu {3}",
                    menuItem.MenuItemId,
                    menuItem.DishId,
                    dish.Name,
                    menuItem.MenuId);

            // Act
            var result = (object)(await _controller
                .GetMenuItem(GetFirstMenuItemId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetMenuItem_InexistentId_ShouldReturnNotFound()
        {
            // Act
            var result = (object)(await _controller
                .GetMenuItem(GenBadMenuItemId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetMenuItemBasic_HappyFlow_ShouldReturnRequirement()
        {
            // Arrange
            var expected = (object)(await _context.MenuItems.FirstOrDefaultAsync());

            // Act
            var result = (object)(await _controller
                .GetMenuItemBasic(GetFirstMenuItemId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetMenuItemBasic_InexistentId_ShouldReturnNotFound()
        {
            // Act
            var result = (object)(await _controller
                .GetMenuItemBasic(GenBadMenuItemId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        #endregion

        #region Creates

        [TestMethod]
        public async Task CreateMenuItem_HappyFlow_ShouldCreateAndReturnMenuItem()
        {
            // Arrange
            var dish = new Dish { Name = "Crudités" };
            await _context.Dishes.AddAsync(dish);
            await _context.SaveChangesAsync();

            var item = new MenuItem
            {
                DishId = dish.DishId,
                MenuId = await _context.Menus.Select(m => m.MenuId).FirstOrDefaultAsync()
            };

            // Act
            await _controller.CreateMenuItem(item);
            var result = (object)(await _controller.GetMenuItemBasic(item.MenuItemId)).Value;

            var expected = (object)item;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task CreateMenuItem_TriesToSetId_ShouldReturnBadRequest()
        {
            // Arrange
            var dish = new Dish { Name = "Crudités" };
            await _context.Dishes.AddAsync(dish);

            var item = new MenuItem
            {
                MenuItemId = await _context.MenuItems.Select(i => i.MenuItemId).LastOrDefaultAsync() + 1,
                DishId = dish.DishId,
                MenuId = await _context.Menus.Select(m => m.MenuId).FirstOrDefaultAsync()
            };

            // Act
            var result = (object)(await _controller.CreateMenuItem(item)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateMenuItem_HasMenu_ShouldReturnBadRequest()
        {
            // Arrange
            var dish = new Dish { Name = "Crudités" };
            await _context.Dishes.AddAsync(dish);

            var item = new MenuItem
            {
                DishId = dish.DishId,
                Menu = new Menu(),
                MenuId = await _context.Menus.Select(m => m.MenuId).FirstOrDefaultAsync()
            };

            // Act
            var result = (object)(await _controller.CreateMenuItem(item)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateMenuItem_HasDish_ShouldReturnBadRequest()
        {
            // Arrange
            var dish = new Dish { Name = "Crudités" };
            await _context.Dishes.AddAsync(dish);

            var item = new MenuItem
            {
                Dish = new Dish(),
                DishId = dish.DishId,
                MenuId = await _context.Menus.Select(m => m.MenuId).FirstOrDefaultAsync()
            };

            // Act
            var result = (object)(await _controller.CreateMenuItem(item)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateMenuItem_InexistentMenuId_ShouldReturnBadRequest()
        {
            // Arrange
            var dish = new Dish { Name = "Crudités" };
            await _context.Dishes.AddAsync(dish);

            var item = new MenuItem
            {
                DishId = dish.DishId,
                MenuId = await _context.Menus.Select(m => m.MenuId).LastOrDefaultAsync() + 1
            };

            // Act
            var result = (object)(await _controller.CreateMenuItem(item)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateMenuItem_InexistentDishId_ShouldReturnBadRequest()
        {
            // Arrange
            var item = new MenuItem
            {
                DishId = await _context.Dishes.Select(d => d.DishId).LastOrDefaultAsync() + 1,
                MenuId = await _context.Menus.Select(m => m.MenuId).FirstOrDefaultAsync()
            };

            // Act
            var result = (object)(await _controller.CreateMenuItem(item)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateMenuItem_Duplicate_ShouldReturnBadRequest()
        {
            // Arrange
            var source = await _context.MenuItems
                .Select(i => new { i.DishId, i.MenuId })
                .FirstOrDefaultAsync();

            var item = new MenuItem
            {
                DishId = source.DishId,
                MenuId = source.MenuId
            };

            // Act
            var result = (object)(await _controller.CreateMenuItem(item)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        #endregion

        [TestMethod]
        public void MenuItemExists_ItDoes_ShoudReturnTrue()
        {
            //Arrange
            var methodInfo = typeof(MenuItemsController).GetMethod("MenuItemExists",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var parameters = new object[] { GetFirstMenuItemId };

            // Act
            var result = ((methodInfo.Invoke(_controller, parameters)) as Task<bool>).Result;

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public void MenuItemExists_ItDoesNot_ShoudReturnFalse()
        {
            //Arrange
            var methodInfo = typeof(MenuItemsController).GetMethod("MenuItemExists",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var parameters = new object[] { GenBadMenuItemId };

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
            _controller = new MenuItemsController(_context, _logger);
        }

        /// Helpers

        int GetFirstMenuItemId => _context.MenuItems
            .Select(r => r.MenuItemId)
            .OrderBy(id => id)
            .FirstOrDefault();
        int GenBadMenuItemId => _context.MenuItems
            .Select(r => r.MenuItemId)
            .ToArray()
            .OrderBy(id => id)
            .LastOrDefault() + 1;

        private static void Setup()
        {
            var items = FakeRepo.MenuItems;
            var dishes = FakeRepo.Dishes;
            var menus = FakeRepo.Menus;

            var DummyOptions = new DbContextOptionsBuilder<FoodChainsDbContext>().Options;

            var dbContextMock = new DbContextMock<FoodChainsDbContext>(DummyOptions);
            var itemsDbSetMock = dbContextMock.CreateDbSetMock(x => x.MenuItems, items);
            var dishesDbSetMock = dbContextMock.CreateDbSetMock(x => x.Dishes, dishes);
            var menusDbSetMock = dbContextMock.CreateDbSetMock(x => x.Menus, menus);
            dbContextMock.Setup(m => m.MenuItems).Returns(itemsDbSetMock.Object);
            dbContextMock.Setup(m => m.Dishes).Returns(dishesDbSetMock.Object);
            dbContextMock.Setup(m => m.Menus).Returns(menusDbSetMock.Object);
            _context = dbContextMock.Object;
            _controller = new MenuItemsController(_context, _logger);
        }
    }
}
