using FluentAssertions;
using McJenny.WebAPI;
using McJenny.WebAPI.Data.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace McJenny.Integration
{
    [TestClass]
    public class MenuItemsIntegrationTests
    {
        private static HttpClient _client;
        private static FoodChainsDbContext _context;
        private static WebApplicationFactory<Startup> _appFactory;

        [ClassInitialize]
        public static void InitClass(TestContext testContext)
        {
            _appFactory = new WebApplicationFactory<Startup>();
            _appFactory.ClientOptions.BaseAddress = new Uri("http://localhost/api/menuitems");
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
            _appFactory.ClientOptions.BaseAddress = new Uri("http://localhost/api/menuitems");
            _client = _appFactory.CreateClient();

            var fked = HelperFuck.MessedUpDB(_context);
            if (fked)
            {
                var fake = false;
            }
        }

        #region Deletes

        [TestMethod]
        public async Task DeleteItem_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var itemId = GenBadItemId;

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + itemId);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task DeleteItem_Last_ShouldDelete()
        {
            // Arrange
            var itemId = GenBadItemId;

            var dishCount = await _context.Dishes.CountAsync();
            var menuCount = await _context.Menus.CountAsync();

            var unused = new Tuple<int, int>(0, 0);

            for (int i = 1; i <= dishCount; i++)
            {
                for (int j = 1; j <= menuCount; j++)
                {
                    if(!await _context.MenuItems.AnyAsync(itm=>
                        itm.DishId==i&&
                        itm.MenuId==j))
                    {
                        unused = new Tuple<int, int>(i, j);
                        break;
                    }
                }
                if (unused.Item1 != 0) break;
            }

            var item = new MenuItem
            {
                DishId = unused.Item1,
                MenuId = unused.Item2
            };
            await _context.MenuItems.AddAsync(item);
            await _context.SaveChangesAsync();

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + itemId);
            var getRes = await _client.GetAsync(_client.BaseAddress + "/" + itemId);

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            getRes.IsSuccessStatusCode.Should().BeFalse();
            getRes.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task DeleteItem_NotLast_ShouldSwitchWithLastThenDelThat()
        {
            // Arrange
            var itemId = GenBadItemId;

            var dishCount = await _context.Dishes.CountAsync();
            var menuCount = await _context.Menus.CountAsync();

            var unused1 = new Tuple<int, int>(0, 0);
            var unused2 = new Tuple<int, int>(0, 0);

            for (int i = 1; i <= dishCount; i++)
            {
                for (int j = 1; j <= menuCount; j++)
                {
                    if (!await _context.MenuItems.AnyAsync(itm =>
                         itm.DishId == i &&
                         itm.MenuId == j))
                    {
                        if (unused1.Item1==0)
                            unused1 = new Tuple<int, int>(i, j);
                      else
                        {
                            unused2 = new Tuple<int, int>(i, j);
                            break;
                        }
                    }
                }
                if (unused1.Item1 != 0&& unused2.Item1!=0) break;
            }

            var item1 = new MenuItem
            {
                DishId = unused1.Item1,
                MenuId = unused1.Item2
            };
            var item2 = new MenuItem
            {
                DishId = unused2.Item1,
                MenuId = unused2.Item2
            };
            await _context.MenuItems.AddRangeAsync(new MenuItem[] { item1, item2 });
            await _context.SaveChangesAsync();

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + itemId);
            var getRes = await _client.GetAsync(_client.BaseAddress + "/" + itemId+"/basic");
            var getCont = await getRes.Content.ReadAsStringAsync();
            var getItm = JsonConvert.DeserializeObject(getCont, typeof(MenuItem)) as MenuItem;

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            getItm.DishId.Should().Be(item2.DishId);
            getItm.MenuId.Should().Be(item2.MenuId);

            // Clean-up
            await _client.DeleteAsync(_client.BaseAddress + "/" + itemId);
        }

        #endregion

        #region Creates

        [TestMethod]
        public async Task CreateMenuItem_HappyFlow_ShouldCreateAndReturnMenuItem()
        {
            // Arrange
            var dishCount = await _context.Dishes.CountAsync() + 1;
            var menuCount = await _context.Menus.CountAsync() + 1;
            var itemId = await _context.MenuItems.CountAsync() + 1;

            var itemIds = new Tuple<int, int>(0, 0);

            for (int i = 1; i <= dishCount; i++)
            {
                for (int j = 1; j <= menuCount; j++)
                {
                    if(!await _context.MenuItems.AnyAsync(itm=>itm.DishId==i&&itm.MenuId==j))
                    {
                        itemIds = new Tuple<int, int>(i, j);
                        break;
                    }
                }
                if (itemIds.Item1 != 0) break;
            }

            var item = new MenuItem
            {
                DishId = itemIds.Item1,
                MenuId = itemIds.Item2
            };
            var itemCont = new StringContent(JsonConvert.SerializeObject(item),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, itemCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();

            // Clean-up
            await _client.DeleteAsync(_client.BaseAddress + "/" + itemId);
        }

        [TestMethod]
        public async Task CreateMenuItem_TriesToSetId_ShouldReturnBadRequest()
        {
            // Arrange
            var dishCount = await _context.Dishes.CountAsync() + 1;
            var menuCount = await _context.Menus.CountAsync() + 1;
            var itemId = await _context.MenuItems.CountAsync() + 1;

            var itemIds = new Tuple<int, int>(0, 0);

            for (int i = 1; i <= dishCount; i++)
            {
                for (int j = 1; j <= menuCount; j++)
                {
                    if (!await _context.MenuItems.AnyAsync(itm => itm.DishId == i && itm.MenuId == j))
                    {
                        itemIds = new Tuple<int, int>(i, j);
                        break;
                    }
                }
                if (itemIds.Item1 != 0) break;
            }

            var item = new MenuItem
            {
                DishId = itemIds.Item1,
                MenuId = itemIds.Item2,
                MenuItemId = itemId
            };
            var itemCont = new StringContent(JsonConvert.SerializeObject(item),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, itemCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateMenuItem_HasMenu_ShouldReturnBadRequest()
        {
            // Arrange
            var dishCount = await _context.Dishes.CountAsync() + 1;
            var menuCount = await _context.Menus.CountAsync() + 1;
            var itemId = await _context.MenuItems.CountAsync() + 1;

            var itemIds = new Tuple<int, int>(0, 0);

            for (int i = 1; i <= dishCount; i++)
            {
                for (int j = 1; j <= menuCount; j++)
                {
                    if (!await _context.MenuItems.AnyAsync(itm => itm.DishId == i && itm.MenuId == j))
                    {
                        itemIds = new Tuple<int, int>(i, j);
                        break;
                    }
                }
                if (itemIds.Item1 != 0) break;
            }

            var item = new MenuItem
            {
                DishId = itemIds.Item1,
                MenuId = itemIds.Item2,
                Menu = new Menu()
            };
            var itemCont = new StringContent(JsonConvert.SerializeObject(item),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, itemCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateMenuItem_HasDish_ShouldReturnBadRequest()
        {
            // Arrange
            var dishCount = await _context.Dishes.CountAsync() + 1;
            var menuCount = await _context.Menus.CountAsync() + 1;
            var itemId = await _context.MenuItems.CountAsync() + 1;

            var itemIds = new Tuple<int, int>(0, 0);

            for (int i = 1; i <= dishCount; i++)
            {
                for (int j = 1; j <= menuCount; j++)
                {
                    if (!await _context.MenuItems.AnyAsync(itm => itm.DishId == i && itm.MenuId == j))
                    {
                        itemIds = new Tuple<int, int>(i, j);
                        break;
                    }
                }
                if (itemIds.Item1 != 0) break;
            }

            var item = new MenuItem
            {
                DishId = itemIds.Item1,
                MenuId = itemIds.Item2,
                Dish = new Dish()
            };
            var itemCont = new StringContent(JsonConvert.SerializeObject(item),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, itemCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateMenuItem_InexistentMenuId_ShouldReturnBadRequest()
        {
            // Arrange
            var dishCount = await _context.Dishes.CountAsync() + 1;
            var menuCount = await _context.Menus.CountAsync() + 1;
            var itemId = await _context.MenuItems.CountAsync() + 1;

            var itemIds = new Tuple<int, int>(0, 0);

            for (int i = 1; i <= dishCount; i++)
            {
                for (int j = 1; j <= menuCount; j++)
                {
                    if (!await _context.MenuItems.AnyAsync(itm => itm.DishId == i && itm.MenuId == j))
                    {
                        itemIds = new Tuple<int, int>(i, j);
                        break;
                    }
                }
                if (itemIds.Item1 != 0) break;
            }

            var item = new MenuItem
            {
                DishId = itemIds.Item1,
                MenuId = menuCount+1
            };
            var itemCont = new StringContent(JsonConvert.SerializeObject(item),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, itemCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateMenuItem_InexistentDishId_ShouldReturnBadRequest()
        {
            // Arrange
            var dishCount = await _context.Dishes.CountAsync() + 1;
            var menuCount = await _context.Menus.CountAsync() + 1;
            var itemId = await _context.MenuItems.CountAsync() + 1;

            var itemIds = new Tuple<int, int>(0, 0);

            for (int i = 1; i <= dishCount; i++)
            {
                for (int j = 1; j <= menuCount; j++)
                {
                    if (!await _context.MenuItems.AnyAsync(itm => itm.DishId == i && itm.MenuId == j))
                    {
                        itemIds = new Tuple<int, int>(i, j);
                        break;
                    }
                }
                if (itemIds.Item1 != 0) break;
            }

            var item = new MenuItem
            {
                DishId = dishCount+1,
                MenuId = itemIds.Item2
            };
            var itemCont = new StringContent(JsonConvert.SerializeObject(item),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, itemCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateMenuItem_Duplicate_ShouldReturnBadRequest()
        {
            // Arrange
            var dupe = await _context.MenuItems.FirstOrDefaultAsync();

            var item = new MenuItem
            {
                DishId = dupe.DishId,
                MenuId = dupe.MenuId
            };
            var itemCont = new StringContent(JsonConvert.SerializeObject(item),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, itemCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        #endregion

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

            var expected = new string[items.Length];
            for (int i = 0; i < items.Length; i++)
                expected[i] = string.Format("Item [{0}]: Dish({1}) \"{2}\" available on Menu: {3}",
                    items[i],
                    items[i].DishId,
                    dishes.SingleOrDefault(d => d.DishId == items[i].DishId).Name,
                    items[i].MenuId);

            // Act
            var result = await _client.GetAsync(string.Empty);
            var content = await result.Content.ReadAsStringAsync();
            var getItems = JsonConvert.DeserializeObject(content, typeof(List<string>)) as List<string>;

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            getItems.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetMenuItem_HappyFlow_ShouldReturnMenuItem()
        {
            // Arrange
            var itmId = GetFirstMenuItemId;
            var menuItem = await _context.MenuItems.FindAsync(itmId);

            var dish = await _context.Dishes
                .Select(d => new { d.DishId, d.Name })
                .FirstAsync(d => d.DishId == menuItem.DishId);

            var expected = string.Format("(MenuItem {0}): Dish {1} \"{2}\" available on Menu {3}",
                    menuItem.MenuItemId,
                    menuItem.DishId,
                    dish.Name,
                    menuItem.MenuId);

            // Act
            var result = await _client.GetAsync(_client.BaseAddress + "/" + itmId);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetMenuItem_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var badId = GenBadItemId;

            // Act
            var result = await _client.GetAsync(_client.BaseAddress + "/" + badId);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task GetMenuItemBasic_HappyFlow_ShouldReturnRequirement()
        {
            // Arrange
            var itmId = GetFirstMenuItemId;
            var expected = await _context.MenuItems.FindAsync(itmId);

            // Act
            var result = await _client.GetAsync(_client.BaseAddress + "/" + itmId + "/basic");
            var content = await result.Content.ReadAsStringAsync();
            var item = JsonConvert.DeserializeObject(content, typeof(MenuItem)) as MenuItem;

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            item.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetMenuItemBasic_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var badId = GenBadItemId;

            // Act
            var result = await _client.GetAsync(_client.BaseAddress + "/" + badId + "/basic");

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        #endregion

        //Helpers
        private int GenBadItemId => _context.MenuItems.Count() + 1;

        private int GetFirstMenuItemId => _context.MenuItems.First().MenuId;
    }
}
