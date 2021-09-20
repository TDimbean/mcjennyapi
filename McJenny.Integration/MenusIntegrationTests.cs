using FluentAssertions;
using McJenny.WebAPI;
using McJenny.WebAPI.Data.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace McJenny.Integration
{
    [TestClass]
    public class MenusIntegrationTests
    {
        private static HttpClient _client;
        private static FoodChainsDbContext _context;
        private static WebApplicationFactory<Startup> _appFactory;

        [ClassInitialize]
        public static void InitClass(TestContext testContext)
        {
            _appFactory = new WebApplicationFactory<Startup>();
            _appFactory.ClientOptions.BaseAddress = new Uri("http://localhost/api/menus");
            _context = new FoodChainsDbContext();
            _client = _appFactory.CreateClient();
        }

        [TestCleanup]
        public void TestClean()
        {
            _context.Dispose();
            _context = null;
            _appFactory.Dispose();
            _appFactory = null;
            _client.Dispose();
            _client = null;

            _context = new FoodChainsDbContext();
            _appFactory = new WebApplicationFactory<Startup>();
            _appFactory.ClientOptions.BaseAddress = new Uri("http://localhost/api/menus");
            _client = _appFactory.CreateClient();

            var fked = HelperFuck.MessedUpDB(_context);
            if (fked)
            {
                var fake = false;
            }
        }

        #region Gets

        #region Basic

        [TestMethod]
        [Ignore]
        public async Task GetMenus_HappyFlow_ShouldReturnMenus()
        {
            // Arrange
            var expectedObj = (dynamic)null;
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(string.Empty);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        [Ignore]
        public async Task GetMenu_HappyFlow_ShouldReturnMenu()
        {
            // Arrange
            var menuId = GetFirstMenuId;

            var expectedObj = (dynamic)null;
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + menuId);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        [Ignore]
        public async Task GetMenu_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var menuId = GenBadMenuId;

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + menuId);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task GetMenuBasic_HappyFlow_ShouldReturnMenu()
        {
            // Arrange
            var menuId = GetFirstMenuId;

            var expected = await _context.Menus.FindAsync(menuId);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/" + menuId + "/basic");
            var content = await result.Content.ReadAsStringAsync();
            var retrieved = JsonConvert.DeserializeObject(content, typeof(Menu));

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            retrieved.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetMenuBasic_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var menuId = GenBadMenuId;

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/" + menuId + "/basic");

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task GetRequirements_HappyFlow_ShouldReturnRequirements()
        {
            // Arrange
            var menuId = GetFirstMenuId;

            var dishIds = await _context.MenuItems
                .Where(i => i.MenuId == menuId)
                .Select(i => i.DishId)
                .Distinct()
                .ToArrayAsync();

            var reqIds = await _context
                .DishRequirements
                .Where(r => dishIds.Contains(r.DishId))
                .Select(r => r.SupplyCategoryId)
                .Distinct()
                .ToArrayAsync();

            var expectedObj = await _context.SupplyCategories
                .Where(c => reqIds
                    .Contains(c.SupplyCategoryId))
                .Select(c => new
                {
                    c.SupplyCategoryId,
                    c.Name
                })
                .ToArrayAsync();

            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + menuId + "/requirements");
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetLocations_HappyFlow_ShouldReturnLocations()
        {
            // Arrange
            var menuId = GetFirstMenuId;

            var expectedObj = await _context.Locations
                .Where(l => l.MenuId == menuId)
                .Select(l => new
            {
                l.LocationId,
                l.AbreviatedCountry,
                l.AbreviatedState,
                l.City,
                l.Street
            })
                .ToArrayAsync();

            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + menuId + "/locations");
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetDishes_HappyFlow_ShouldReturnDishes()
        {
            // Arrange
            var menuId = GetFirstMenuId;

            var dishIds = await _context.MenuItems
                .Where(i => i.MenuId == menuId)
                .Select(i => i.DishId)
                .ToArrayAsync();
            var expectedObj = await _context.Dishes
                .Where(d => dishIds
                    .Contains(d.DishId))
                .Select(d => d.Name)
                .ToArrayAsync();

            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + menuId + "/dishes");
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetLocations_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var menuId = GenBadMenuId;

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + menuId + "/locations");
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task GetRequirements_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var menuId = GenBadMenuId;

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + menuId + "/requirements");
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task GetDishes_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var menuId = GenBadMenuId;

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + menuId + "/dishes");
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        #endregion

        #endregion

        #region Deletes

        [TestMethod]
        public async Task DeleteMenu_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var badId = await _context.Menus.CountAsync() + 1;

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + badId);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [DataTestMethod]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public async Task DeleteMenu_HasRelations_ShouldReturnBadRequest(bool isCap, bool isFirst)
        {
            // Arrange
            var menuId = 0;
            if (!isCap && !isFirst) menuId = 2;
            else
            {
                if (isFirst) menuId = GetFirstMenuId;
                else menuId = await _context.Menus.CountAsync();
            }

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + menuId);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task DeleteMenu_NotLastNoRel_ShouldMigrateRelations()
        {
            // Arrange
            var menuId = await _context.Menus.CountAsync() + 1;
            var dishId = await _context.Dishes.CountAsync() + 1;
            var itemId = await _context.MenuItems.CountAsync() + 1;
            var locId = await _context.Locations.CountAsync() + 1;

            var delMenu = new Menu();
            var swapMenu = new Menu();
            var loc = new Location
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "XX",
                Country = "Delete Me",
                State = "None",
                City = "None",
                Street = "None",
                ScheduleId = 1,
                MenuId = menuId+1,
                OpenSince = new DateTime()
            };
            var dish = new Dish { Name = "Berry Tart" };
            var item = new MenuItem
            {
                MenuId = menuId+1,
                DishId = dishId
            };

            await _context.Menus.AddRangeAsync(new Menu[] { delMenu, swapMenu });
            await _context.Dishes.AddAsync(dish);
            await _context.SaveChangesAsync();

            await _context.Locations.AddAsync(loc);
            await _context.MenuItems.AddAsync(item);
            await _context.SaveChangesAsync();

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + menuId);

            var getLocRes = await _client.GetAsync("http://localhost/api/locations/" +
                locId + "/basic");
            var getLocCont = await getLocRes.Content.ReadAsStringAsync();
            var getLoc = JsonConvert.DeserializeObject(getLocCont,
                typeof(Location)) as Location;

            var getItmRes = await _client.GetAsync("http://localhost/api/menuitems/" +
                itemId + "/basic");
            var getItmCont = await getItmRes.Content.ReadAsStringAsync();
            var getItm = JsonConvert.DeserializeObject(getItmCont,
                typeof(MenuItem)) as MenuItem;

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            getLoc.MenuId.Should().Be(menuId);
            getItm.MenuId.Should().Be(menuId);

            // Clean-up
            await _client.DeleteAsync("http://localhost/api/menuitems/" + itemId);
            await _client.DeleteAsync("http://localhost/api/locations/" + locId);
            await _client.DeleteAsync("http://localhost/api/dishes/" + dishId);
            await _client.DeleteAsync(_client.BaseAddress + "/" + menuId);
        }

        #endregion

        #region Creates

        [TestMethod]
        public async Task CreateMenu_HappyFlow_ShouldCreateEmptyMenu()
        {
            // Arrange
            var menuId = await _context.Menus.CountAsync() + 1;
            var menu = new Menu();
            var menuCont = new StringContent(JsonConvert.SerializeObject(menu),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, menuCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();

            // Clean-up
            await _client.DeleteAsync(_client.BaseAddress + "/" + menuId);
        }

        [TestMethod]
        public async Task CreateMenu_TriesToSetId_ShouldReturnBadRequest()
        {
            // Arrange
            var menuId = await _context.Menus.CountAsync() + 1;
            var menu = new Menu { MenuId = menuId};
            var menuCont = new StringContent(JsonConvert.SerializeObject(menu),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, menuCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateMenu_MenuItemsNull_ShouldReturnBadRequest()
        {
            // Arrange
            var menu = new Menu { MenuItems=null };
            var menuCont = new StringContent(JsonConvert.SerializeObject(menu),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, menuCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateMenu_LocationsNull_ShouldReturnBadRequest()
        {
            // Arrange
            var menu = new Menu { Locations = null };
            var menuCont = new StringContent(JsonConvert.SerializeObject(menu),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, menuCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateMenu_MenuItemsNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var menu = new Menu { MenuItems = new MenuItem[] { new MenuItem() } };
            var menuCont = new StringContent(JsonConvert.SerializeObject(menu),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, menuCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }


        [TestMethod]
        public async Task CreateMenu_LocationsNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var menu = new Menu { Locations = new Location[] { new Location() } };
            var menuCont = new StringContent(JsonConvert.SerializeObject(menu),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, menuCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        #endregion

        //Helpers
        private int GenBadMenuId => _context.Menus
                .AsEnumerable()
                .Select(m => m.MenuId)
                .LastOrDefault() + 1;
        private int GetFirstMenuId => _context.Menus
            .Select(m => m.MenuId)
            .FirstOrDefault();
    }
}
