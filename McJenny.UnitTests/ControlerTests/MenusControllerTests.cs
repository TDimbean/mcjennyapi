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
    public class MenusControllerTests
    {
        private static MenusController _controller;
        private static FoodChainsDbContext _context;
        private static ILogger<MenusController> _logger;

        [ClassInitialize]
        public static void Init(TestContext testContext)
        {
            _logger = (new Mock<ILogger<MenusController>>()).Object;
            Setup();
        }

        [TestInitialize]
        public void InitTest() => Setup();

        #region Gets

        [TestMethod]
        [Ignore]
        public async Task GetMenus_HappyFlow_ShouldReturnAllMenus()
        {
            // Arrange
            var menuIds = await _context.Menus.Select(m => m.MenuId).ToArrayAsync();

            var dbLocations = await _context.Locations.Select(l =>
                new
                {
                    l.MenuId,
                    Line = string.Format("({0}) {1}, {2}{3}, {4}",
                                    l.LocationId, l.AbreviatedCountry,
                                    l.AbreviatedState == "N/A" ? string.Empty : l.AbreviatedState + ", ",
                                    l.City, l.Street)
                }).ToListAsync();

            var menuItems = await _context.MenuItems.Select(i => new
            {
                i.DishId,
                i.MenuId
            }).ToArrayAsync();
            var dishes = await _context.Dishes.Select(d => new { d.DishId, d.Name }).ToArrayAsync();

            var menus = new dynamic[menuIds.Length];

            for (int i = 0; i < menuIds.Length; i++)
            {
                var menuId = menuIds[i];

                var dishIds = menuItems.Where(i => i.MenuId == menuId).Select(i => i.DishId).ToArray();
                var menuDishes = dishes.Where(d => dishIds.Contains(d.DishId)).Select(d => d.Name).ToArray();

                var locations = dbLocations.Where(l => l.MenuId == menuId).Select(l => l.Line).ToArray();

                menus[i] = new { locations, dishes = menuDishes };
            }
            var expected = (object)menus;

            //// Act
            //var result = (object)(await _controller.GetMenus()).Value;

            //// Assert
            //result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        [Ignore]
        public async Task GetMenu_HappyFlow_ShouldReturnMenu()
        {
            // Arrange
            var menu = await GetFirstMenu();

            var dbLocations = await _context.Locations.Where(l => l.MenuId == menu.MenuId).ToArrayAsync();
            var dishIds = await _context.MenuItems.Where(i => i.MenuId == menu.MenuId).Select(i => i.DishId).ToArrayAsync();
            var dbDishes = await _context.Dishes.Where(d => dishIds.Contains(d.DishId)).ToArrayAsync();

            var locations = dbLocations.Select(l => new {
                l.LocationId,
                l.AbreviatedCountry,
                l.AbreviatedState,
                l.City,
                l.Street
            });

            var dishes = dbDishes.Select(d => new
            {
                d.DishId,
                d.Name
            });

            var expected = (object)new { locations, dishes };

            //// Act
            //var result = (object)(await _controller.GetMenu(menu.MenuId)).Value;

            //// Assert
            //result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        [Ignore]
        public async Task GetMenu_InexistentId_ShouldReturnNotFound()
        {
            //// Act
            //var result = (await _controller.GetMenu(GenBadMenuId)).Result;

            //// Assert
            //result.Should().BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetMenuBasic_HappyFlow_ShouldReturnMenu()
        {
            // Arrange
            var expected = await GetFirstMenu();

            // Act
            var result = (await _controller.GetMenuBasic(GetFirstMenuId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetMenuBasic_InexistentId_ShouldReturnNotFound()
        {
            // Act
            var result = (await _controller.GetMenuBasic(GenBadMenuId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetRequirements_HappyFlow_ShouldReturnMenuRequirements()
        {
            // Arrange
            var menu = await GetFirstMenu();

            var dishIds = await _context.MenuItems
                .Where(i => i.MenuId == menu.MenuId)
                .Select(i => i.DishId)
                .ToArrayAsync();

            var reqIds = await _context.DishRequirements.Where(r => dishIds.Contains(r.DishId))
                .Select(r => r.SupplyCategoryId)
                .Distinct()
                .ToArrayAsync();

            var query = await _context.SupplyCategories.Where(c => reqIds.Contains(c.SupplyCategoryId))
                .Select(c => new
                {
                    c.SupplyCategoryId,
                    c.Name
                }).ToArrayAsync();

            var expected = (object)query;

            // Act
            var result = (object)(await _controller.GetRequirements(menu.MenuId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetRequirements_InexistentId_ShouldReturnNotFound()
        {
            // Act
            var result = (await _controller.GetRequirements(GenBadMenuId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetLocations_HappyFlow_ShouldReturnLocationsServingMenu()
        {
            // Arrange
            var menuId = GetFirstMenuId;

            var expected = (object)await _context.Locations.Where(l => l.MenuId == menuId).Select(l => new
            {
                l.LocationId,
                l.AbreviatedCountry,
                l.AbreviatedState,
                l.City,
                l.Street
            }).ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetLocations(menuId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetLocations_InexistentId_ShouldReturnNotFound()
        {
            // Act
            var result = (await _controller.GetLocations(GenBadMenuId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetDishes_HappyFlow_ShouldReturnDishesOnMenu()
        {
            // Arrange
            var menuId = GetFirstMenuId;

            var dishIds = await _context.MenuItems.Where(i => i.MenuId == menuId).Select(i => i.DishId).ToArrayAsync();
            var expected = (object)(await _context.Dishes
                .Where(d => dishIds.Contains(d.DishId))
                .ToArrayAsync())
                .Select(d => d.Name)
                .ToArray();

            // Act
            var result = (object)(await _controller.GetDishes(menuId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetDishes_InexistentId_ShouldReturnNtFound()
        {
            // Act
            var result = (await _controller.GetDishes(GenBadMenuId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        #endregion

        #region Creates

        [TestMethod]
        public async Task CreateMenu_HappyFlow_ShouldCreateEmptyMenu()
        {
            // Arrange
            var menu = new Menu();

            // Act
            await _controller.CreateMenu(menu);
            var result = (object)(await _controller.GetMenuBasic(menu.MenuId)).Value;

            var expected = (object)menu;

            // Assert
            result.Should().BeEquivalentTo(menu);
        }

        [TestMethod]
        public async Task CreateMenu_TriesToSetId_ShouldReturnBadRequest()
        {
            // Arrange
            var menu = new Menu { MenuId = await _context.Menus.Select(m => m.MenuId).LastOrDefaultAsync() + 1 };

            // Act
            var result = (await _controller.CreateMenu(menu)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateMenu_MenuItemsNull_ShouldReturnBadRequest()
        {
            // Arrange
            var menu = new Menu { MenuItems = null };

            // Act
            var result = (await _controller.CreateMenu(menu)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateMenu_LocationsNull_ShouldReturnBadRequest()
        {
            // Arrange
            var menu = new Menu { Locations = null };

            // Act
            var result = (await _controller.CreateMenu(menu)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateMenu_MenuItemsNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var menu = new Menu { MenuItems = new MenuItem[] { new MenuItem() } };

            // Act
            var result = (await _controller.CreateMenu(menu)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateMenu_LocationsNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var menu = new Menu { Locations = new Location[] { new Location() } };

            // Act
            var result = (await _controller.CreateMenu(menu)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        #endregion

        #region Deletes

        [TestMethod]
        public async Task DeleteMenu_InexistenId_ShouldReturnNotFound()
        {
            // Arrange
            var badId = GenBadMenuId;

            // Act
            var result = (await _controller.DeleteMenu(badId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        //The following 2 tests are ignored because checking is nearly impossible,
        // without rewriting the controller

        [Ignore]
        [TestMethod]
        public async Task DeleteMenu_LastWithNoRel_ShouldDelete()
        {
            // Arrange
            var initCount = await _context.Menus.CountAsync();
            var menu = new Menu();
            await _controller.CreateMenu(menu);

            // Act
            await _controller.DeleteMenu(menu.MenuId);

            var menus = new List<Menu>();
            for (int i = 0; i < initCount + 1; i++)
            {
                var newMen = (await _controller.GetMenuBasic(i)).Value;
                if (newMen != null) menus.Add(newMen);
            }
            var newCount = menus.Count;

            // Assert
            newCount.Should().Be(initCount);
            menus.Any(m => m.MenuId == menu.MenuId).Should().BeFalse();
        }

        [Ignore]
        [TestMethod]
        public async Task DeleteMenu_NotLastNoItems_ShouldSwapDelete()
        {
            // Arrange
            var initCount = await _context.Menus.CountAsync();
            var menu1 = new Menu { MenuId = initCount + 1 };
            var menu2 = new Menu { MenuId = initCount + 2 };
            var dish = new Dish();
            await _context.Dishes.AddAsync(dish);
            await _context.Menus.AddRangeAsync(new Menu[] { menu1, menu2 });
            await _context.SaveChangesAsync();

            var initMenuItemCount = await _context.MenuItems.CountAsync();

            var item = new MenuItem { MenuId = menu2.MenuId, DishId = dish.DishId };
            await _context.MenuItems.AddAsync(item);
            await _context.SaveChangesAsync();

            // Act
            await _controller.DeleteMenu(menu1.MenuId);
            var fetchItem = await _context.MenuItems.FindAsync(initMenuItemCount + 1);

            // Assert
            fetchItem.MenuId.Should().Be(menu1.MenuId);
        }

        [TestMethod]
        public async Task DeleteMenu_HasItems_ShouldReturnBadRequest()
        {
            // Arrange
            var menu = new Menu();
            await _context.Menus.AddAsync(menu);
            await _context.SaveChangesAsync();

            var item = new MenuItem { MenuId = menu.MenuId, DishId = 1 };
            await _context.MenuItems.AddAsync(item);
            await _context.SaveChangesAsync();

            // Act
            var result = (await _controller.DeleteMenu(menu.MenuId)).Result;

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        #endregion

        [TestMethod]
        public void MenuExitst_ItDoes_ShouldReturnTrue()
        {
            //Arrange
            var methodInfo = typeof(MenusController).GetMethod("MenuExists", BindingFlags.NonPublic | BindingFlags.Instance);
            var parameters = new object[] { GetFirstMenuId };

            // Act
            var result = ((methodInfo.Invoke(_controller, parameters)) as Task<bool>).Result;

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public void MenuExitst_ItDoesNot_ShouldReturnFalse()
        {
            //Arrange
            var methodInfo = typeof(MenusController).GetMethod("MenuExists", BindingFlags.NonPublic | BindingFlags.Instance);
            var parameters = new object[] { GenBadMenuId };

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
            _controller = new MenusController(_context, _logger);
        }

        /// Helpers
        int GetFirstMenuId => _context.Menus.Select(m => m.MenuId).OrderBy(id => id).FirstOrDefault();
        int GenBadMenuId => _context.Menus.Select(m => m.MenuId).ToArray().OrderBy(id => id).LastOrDefault() + 1;
        async Task<Menu> GetFirstMenu() => await _context.Menus.FirstOrDefaultAsync();

        private static void Setup()
        {
            var menus = FakeRepo.Menus;
            var dishes = FakeRepo.Dishes;
            var requirements = FakeRepo.DishRequirements;
            var categories = FakeRepo.SupplyCategories;
            var locations = FakeRepo.Locations;
            var items = FakeRepo.MenuItems;

            var DummyOptions = new DbContextOptionsBuilder<FoodChainsDbContext>().Options;

            var dbContextMock = new DbContextMock<FoodChainsDbContext>(DummyOptions);
            var menusDbSetMock = dbContextMock.CreateDbSetMock(x => x.Menus, menus);
            var dishesDbSetMock = dbContextMock.CreateDbSetMock(x => x.Dishes, dishes);
            var requirementsDbSetMock = dbContextMock.CreateDbSetMock(x => x.DishRequirements, requirements);
            var categoriesDbSetMock = dbContextMock.CreateDbSetMock(x => x.SupplyCategories, categories);
            var locationsDbSetMock = dbContextMock.CreateDbSetMock(x => x.Locations, locations);
            var itemsDbSetMock = dbContextMock.CreateDbSetMock(x => x.MenuItems, items);
            dbContextMock.Setup(m => m.Menus).Returns(menusDbSetMock.Object);
            dbContextMock.Setup(m => m.Dishes).Returns(dishesDbSetMock.Object);
            dbContextMock.Setup(m => m.DishRequirements).Returns(requirementsDbSetMock.Object);
            dbContextMock.Setup(m => m.SupplyCategories).Returns(categoriesDbSetMock.Object);
            dbContextMock.Setup(m => m.Locations).Returns(locationsDbSetMock.Object);
            dbContextMock.Setup(m => m.MenuItems).Returns(itemsDbSetMock.Object);
            _context = dbContextMock.Object;
            _controller = new MenusController(_context, _logger);
        }
    }
}
