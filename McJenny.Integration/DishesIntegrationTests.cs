using FluentAssertions;
using McJenny.WebAPI;
using McJenny.WebAPI.Controllers;
using McJenny.WebAPI.Data.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace McJenny.Integration
{
    [TestClass]
    public class DishesIntegrationTests
    {
        private static WebApplicationFactory<Startup> _appFactory;
        private static HttpClient _client;
        private static FoodChainsDbContext _context;
        
        [ClassInitialize]
        public static void InitClass(TestContext testContext)
        {
            _appFactory = new WebApplicationFactory<Startup>();
            _appFactory.ClientOptions.BaseAddress = new Uri("http://localhost/api/dishes");
            _context = new FoodChainsDbContext();
            _client = _appFactory.CreateClient();
        }

        [TestCleanup]
        public void TestClean()
        {
            _appFactory.Dispose();
            _appFactory = null;
            _client.Dispose();
            _client = null;
            _context.Dispose();
            _context = null;

            _appFactory = new WebApplicationFactory<Startup>();
            _appFactory.ClientOptions.BaseAddress = new Uri("http://localhost/api/dishes");
            _context = new FoodChainsDbContext();
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
        public async Task GetDish_HappyFlow_ShouldReturnFormatted()
        {
            // Arrange
            var dishId = GetFirstDishId;
            var dish = await _context.Dishes.FindAsync(dishId);

            var supCatIds = await _context.DishRequirements
                .Where(r => r.DishId == dishId)
                .Select(r => r.SupplyCategoryId)
                .ToArrayAsync();

            var supplyCategories = await _context.SupplyCategories
                .Where(c => supCatIds.Contains(c.SupplyCategoryId))
                .Select(c => new { ID = c.SupplyCategoryId, c.Name })
                .ToArrayAsync();

            var expected = new { dish=dish.Name, supplyCategories };
            var catDefinition = new[] { new { ID = 0, Name = string.Empty } };
            var definition = new
            {
                dish = string.Empty,
                supplyCategories = new[] { new { ID = 0, Name = string.Empty } }
            };

            // Act
            var result = await _client.GetAsync(_client.BaseAddress + "/" + dishId);
            var content = await result.Content.ReadAsStringAsync();
            var getDish = JsonConvert.DeserializeAnonymousType(content, definition);

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            getDish.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetDishes_HappyFlow_ShouldReturnDishes()
        {
            // Arrange
            var expectedObj = await _context.Dishes
                .Select(d => new { ID = d.DishId, d.Name })
                .ToArrayAsync();
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(string.Empty);
            var content = await result.Content.ReadAsStringAsync();
            //var content = JsonConvert.DeserializeObject(contentString);

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetDishCheated_HappyFlow_ShouldReturnDish()
        {
            // Arrange
            var dishId = GetFirstDishId;

            var dish = (await _context.Dishes.FindAsync(dishId)).Name;

            var supCatIds = await _context.DishRequirements
                .Where(r => r.DishId == dishId)
                .Select(r => r.SupplyCategoryId)
                .ToArrayAsync();

            var supplyCategories = await _context.SupplyCategories
                .Where(c => supCatIds.Contains(c.SupplyCategoryId))
                .Select(c => new { ID = c.SupplyCategoryId, c.Name })
                .ToArrayAsync();

            var expectedObj = new { dish, supplyCategories };
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress+"/"+dishId);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetDish_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var dishId = GenBadDishId;

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + dishId);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task GetDishBasic_HappyFlow_ShouldReturnDish()
        {
            // Arrange
            var dishId = GetFirstDishId;

            var expected = await _context.Dishes.FindAsync(dishId);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + 
                "/" + dishId + "/basic");
            var content = await result.Content.ReadAsStringAsync();
            var retrieved = JsonConvert.DeserializeObject(content, typeof(Dish));

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            retrieved.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetDishBasic_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var dishId = GenBadDishId;

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/" + dishId + "/basic");

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task GetLocations_HappyFlow_ShouldReturnLocations()
        {
            // Arrange
            var dishId = 2;

            var menuIds = await _context.MenuItems
                .Where(i => i.DishId == dishId)
                .Select(i => i.MenuId)
                .Distinct()
                .ToArrayAsync();

            var expectedObj = (await _context.Locations
                .Where(l => menuIds
                    .Contains(l.MenuId))
                .ToArrayAsync())
                .Select(l => string.Format(
                  "({0}) {1}, {2}{3} {4}",
                  l.LocationId,
                  l.AbreviatedCountry,
                  l.AbreviatedState == "N/A" ? string.Empty : l.AbreviatedState + ", ",
                  l.City, l.Street)).ToArray();

            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + dishId + "/locations");
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetRequirements_HappyFlow_ShouldReturnDish()
        {
            // Arrange
            var dishId = 2;

            var catIds = await _context.DishRequirements
                .Where(r => r.DishId == dishId)
                .Select(r => r.SupplyCategoryId)
                .ToArrayAsync();

            var expectedObj = await _context.SupplyCategories
                .Where(c => catIds.Contains(c.SupplyCategoryId))
                .Select(c => string.Format("({0}) {1}", c.SupplyCategoryId, c.Name))
                .ToArrayAsync();

            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + dishId + "/requirements");
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetMenus_HappyFlow_ShouldReturnDish()
        {
            // Arrange
            var dishId = 2;

            var expectedObj = await _context.MenuItems
                .Where(i => i.DishId == dishId)
                .Select(i => i.MenuId)
                .Distinct()
                .ToArrayAsync();
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + dishId+"/menus");
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetLocations_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var dishId = GenBadDishId;

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + dishId + "/locations");
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task GetRequirements_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var dishId = GenBadDishId;

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + dishId + "/requirements");
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task GetMenus_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var dishId = GenBadDishId;

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + dishId + "/menus");
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        #endregion

        #region Advanced

        [TestMethod]
        public async Task GetDishesFiltered_NameMatch_ShouldReturnFiltered()
        {
            // Arrange
            var filter = await _context.Dishes.Select(d => d.Name).FirstOrDefaultAsync();

            var expectedObj = await _context.Dishes
                 .Select(d => new { d.DishId, d.Name })
                 .Where(d => d.Name.ToUpper()
                     .Contains(filter.ToUpper()))
                 .ToArrayAsync();

            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/query:filter="+filter);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetDishesPaged_HapyFlow_ShouldReturnPaged()
        {
            // Arrange
            var pgSz = 2;
            var pgInd = 1;

            var expectedObj = await _context.Dishes
                 .Select(d => new { d.DishId, d.Name })
                 .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArrayAsync();

            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/query:pgSz=" + pgSz+"&pgInd="+pgInd);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
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

            var expectedObj = await _context.Dishes
                 .Select(d => new { d.DishId, d.Name })
                 .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArrayAsync();
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/query:pgSz=" + pgSz + "&pgInd=" + pgInd);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetDishesPaged_SizeOver_ShouldReturnPaged()
        {
            // Arrange
            var dishCount = await _context.Dishes.CountAsync();
            var pgSz = dishCount + 1;
            var pgInd = 1;

            var expectedObj = await _context.Dishes
                 .Select(d => new { d.DishId, d.Name })
                 .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArrayAsync();
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/query:pgSz=" + pgSz + "&pgInd=" + pgInd);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetDishesPaged_IndexOver_ShouldReturnPaged()
        {
            // Arrange
            var dishCount = await _context.Dishes.CountAsync();
            var pgInd = dishCount + 1;
            var pgSz = 1;

            var expectedObj = await _context.Dishes
                 .Select(d => new { d.DishId, d.Name })
                 .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArrayAsync();
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/query:pgSz=" + pgSz + "&pgInd=" + pgInd);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [DataTestMethod]
        [DataRow(-2, 1)]
        [DataRow(2, -1)]
        public async Task GetDishesPaged_HapyFlow_ShouldReturnPaged(int pgSz, int pgInd)
        {
            // Arrange
            var absPgSz = Math.Abs(pgSz);
            var absPgInd = Math.Abs(pgInd);

            var expectedObj = await _context.Dishes
                 .Select(d => new { d.DishId, d.Name })
                 .Skip(absPgSz * (absPgInd - 1))
                .Take(absPgSz)
                .ToArrayAsync();

            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/query:pgSz=" + pgSz + "&pgInd=" + pgInd);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetDishesSorted_ByName_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "name";

            var expectedObj = (object)await _context.Dishes
                            .Select(d => new { d.DishId, d.Name })
                            .OrderBy(d => d.Name)
                            .ToArrayAsync();
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/query:sortby=" + sortBy);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetDishesSorted_GarbleText_ShouldReturnSortedByID()
        {
            // Arrange
            var sortBy = "hbhbhjas";

            var expectedObj = (object)await _context.Dishes
                            .Select(d => new { d.DishId, d.Name })
                            .ToArrayAsync();
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/query:sortby=" + sortBy);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetDishesSorted_Desc_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "name";

            var expectedObj = (object)await _context.Dishes
                            .Select(d => new { d.DishId, d.Name })
                            .OrderByDescending(d => d.Name)
                            .ToArrayAsync();
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/query:desc=true&sortby=" + sortBy);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetDishesQueried_FilteredAndSorted_ShouldReturnFilteredAndSorted()
        {
            // Arrange
            var sortBy = "name";
            var filter = "FR";

            var filtered = await _context.Dishes
                 .Select(d => new { d.DishId, d.Name })
                 .Where(d => d.Name.ToUpper()
                     .Contains(filter.ToUpper()))
                 .ToArrayAsync();

            var expectedObj = filtered
                            .Select(d => new { d.DishId, d.Name })
                            .OrderByDescending(d => d.Name)
                            .ToArray();
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/query:desc=true&filter="+filter+"&sortby=" + sortBy);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetDishesQueried_FilteredAndPaged_ShouldReturnFilteredAndPaged()
        {
            // Arrange
            int pgSz = 2;
            int pgInd = 1;
            var filter = "FR";

            var filtered = await _context.Dishes
                 .Select(d => new { d.DishId, d.Name })
                 .Where(d => d.Name.ToUpper()
                     .Contains(filter.ToUpper()))
                 .ToArrayAsync();

            var expectedObj = filtered
                .Select(d => new { d.DishId, d.Name })
                .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArray();
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/query:filter=" + filter + "&pgsz=" + pgSz+"&pgind="+pgInd);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetDishesQueried_SortedAndPaged_ShouldReturnSortedAndPaged()
        {
            // Arrange
            int pgSz = 2;
            int pgInd = 1;
            var sortBy = "name";

            var sorted = await _context.Dishes
                            .Select(d => new { d.DishId, d.Name })
                            .OrderByDescending(d => d.Name)
                            .ToArrayAsync();

            var expectedObj = sorted
                .Select(d => new { d.DishId, d.Name })
                .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArray();
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/query:desc=true&sortby=" + sortBy + "&pgsz=" + pgSz + "&pgind=" + pgInd);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetDishesQueried_FilteredSortedAndPaged_ShouldReturnFilteredSortedAndPaged()
        {
            // Arrange
            int pgSz = 2;
            int pgInd = 1;
            var sortBy = "name";
            var filter = "FR";

            var filtered = await _context.Dishes
                 .Select(d => new { d.DishId, d.Name })
                 .Where(d => d.Name.ToUpper()
                     .Contains(filter.ToUpper()))
                 .ToArrayAsync();

            var sorted = filtered
                            .Select(d => new { d.DishId, d.Name })
                            .OrderByDescending(d => d.Name)
                            .ToArray();

            var expectedObj = sorted
                .Select(d => new { d.DishId, d.Name })
                .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArray();
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/query:desc=true&sortby=" + sortBy + "&pgsz=" + pgSz + "&pgind=" + pgInd + "&filter="+filter);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        #endregion

        #endregion

        #region Updates

        [TestMethod]
        public async Task UpdateDish_HappyFlow_ShouldUpdateAndReturnDish()
        {
            // Arrange
            var dishId = GetFirstDishId;

            /* Save a copy of the original entry to compare against*/
            var oldDish = await _context.Dishes.FindAsync(dishId);
            var oldDishCopy = new Dish
            {
                DishId = oldDish.DishId,
                DishRequirements = oldDish.DishRequirements,
                MenuItems = oldDish.MenuItems,
                Name = oldDish.Name
            };

            var dish = new Dish { Name = "Cherry Pie" };
            var dishContent = new StringContent(JsonConvert.SerializeObject(dish),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + dishId, dishContent);

            /* Retrieve the (allegedly) updated dish*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + dishId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newDish = JsonConvert.DeserializeObject(newCont, typeof(Dish));

            /* Complete the dish we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            dish.DishRequirements = oldDishCopy.DishRequirements;
            dish.MenuItems = oldDishCopy.MenuItems;
            dish.DishId = oldDishCopy.DishId;

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            newDish.Should().NotBeEquivalentTo(oldDishCopy);
            newDish.Should().BeEquivalentTo(dish);

            // Clean-up
            dish = new Dish { Name = oldDishCopy.Name };
            dishContent = new StringContent(JsonConvert.SerializeObject(dish),
                Encoding.UTF8, "application/json");
            result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + dishId, dishContent);
            updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + dishId + "/basic");
            newCont = await updCall.Content.ReadAsStringAsync();
            newDish = JsonConvert.DeserializeObject(newCont, typeof(Dish));

            result.IsSuccessStatusCode.Should().BeTrue();
            newDish.Should().BeEquivalentTo(oldDishCopy);
        }

        [TestMethod]
        public async Task UpdateDish_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var dishId = GetFirstDishId;

            /* Save a copy of the original entry to compare against*/
            var oldDish = await _context.Dishes.FindAsync(dishId);
            var oldDishCopy = new Dish
            {
                DishId = oldDish.DishId,
                DishRequirements = oldDish.DishRequirements,
                MenuItems = oldDish.MenuItems,
                Name = oldDish.Name
            };

            var dish = new Dish { Name = "Cherry Pie" };
            var dishContent = new StringContent(JsonConvert.SerializeObject(dish),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress +
                "/" + GenBadDishId, dishContent);

            /* Retrieve the (allegedly) updated dish*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + dishId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newDish = JsonConvert.DeserializeObject(newCont, typeof(Dish));

            /* Complete the dish we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            dish.DishRequirements = oldDishCopy.DishRequirements;
            dish.MenuItems = oldDishCopy.MenuItems;
            dish.DishId = oldDishCopy.DishId;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
            newDish.Should().BeEquivalentTo(oldDishCopy);
            newDish.Should().NotBeEquivalentTo(dish);
        }

        [TestMethod]
        public async Task UpdateDish_TriesToSetId_ShouldReturnBadRequest()
        {
            // Arrange
            var dishId = GetFirstDishId;

            /* Save a copy of the original entry to compare against*/
            var oldDish = await _context.Dishes.FindAsync(dishId);
            var oldDishCopy = new Dish
            {
                DishId = oldDish.DishId,
                DishRequirements = oldDish.DishRequirements,
                MenuItems = oldDish.MenuItems,
                Name = oldDish.Name
            };

            var dish = new Dish { Name = "Cherry Pie", DishId = oldDishCopy.DishId };
            var dishContent = new StringContent(JsonConvert.SerializeObject(dish),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress +
                "/" + dishId, dishContent);

            /* Retrieve the (allegedly) updated dish*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + dishId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newDish = JsonConvert.DeserializeObject(newCont, typeof(Dish));

            /* Complete the dish we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            dish.DishRequirements = oldDishCopy.DishRequirements;
            dish.MenuItems = oldDishCopy.MenuItems;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newDish.Should().BeEquivalentTo(oldDishCopy);
            newDish.Should().NotBeEquivalentTo(dish);
        }

        [DataTestMethod]
        [DataRow(" ")]
        [DataRow("012345678911234567892123456789312345678941234567890")]
        public async Task UpdateDish_BadNames_ShouldReturnBadRequest(string name)
        {
            // Arrange
            var dishId = GetFirstDishId;

            /* Save a copy of the original entry to compare against*/
            var oldDish = await _context.Dishes.FindAsync(dishId);
            var oldDishCopy = new Dish
            {
                DishId = oldDish.DishId,
                DishRequirements = oldDish.DishRequirements,
                MenuItems = oldDish.MenuItems,
                Name = oldDish.Name
            };

            var dish = new Dish { Name = name };
            var dishContent = new StringContent(JsonConvert.SerializeObject(dish),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress +
                "/" + dishId, dishContent);

            /* Retrieve the (allegedly) updated dish*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + dishId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newDish = JsonConvert.DeserializeObject(newCont, typeof(Dish));

            /* Complete the dish we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            dish.DishRequirements = oldDishCopy.DishRequirements;
            dish.MenuItems = oldDishCopy.MenuItems;
            dish.DishId = oldDishCopy.DishId;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newDish.Should().BeEquivalentTo(oldDishCopy);
            newDish.Should().NotBeEquivalentTo(dish);
        }

        [TestMethod]
        public async Task UpdateDish_DuplicateName_ShouldReturnBadRequest()
        {
            // Arrange
            var dishId = GetFirstDishId;
            /* Save a copy of the original entry to compare against*/
            var oldDish = await _context.Dishes.FindAsync(dishId);
            var dupe = await _context.Dishes
                .Select(d => d.Name)
                .FirstOrDefaultAsync(d => 
                    d.ToUpper() != oldDish.Name.ToUpper());

            var oldDishCopy = new Dish
            {
                DishId = oldDish.DishId,
                DishRequirements = oldDish.DishRequirements,
                MenuItems = oldDish.MenuItems,
                Name = oldDish.Name
            };

            var dish = new Dish { Name = dupe };
            var dishContent = new StringContent(JsonConvert.SerializeObject(dish),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress +
                "/" + dishId, dishContent);

            /* Retrieve the (allegedly) updated dish*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + dishId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newDish = JsonConvert.DeserializeObject(newCont, typeof(Dish));

            /* Complete the dish we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            dish.DishRequirements = oldDishCopy.DishRequirements;
            dish.MenuItems = oldDishCopy.MenuItems;
            dish.DishId = oldDishCopy.DishId;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newDish.Should().BeEquivalentTo(oldDishCopy);
            newDish.Should().NotBeEquivalentTo(dish);
        }

        [TestMethod]
        public async Task UpdateDish_MenuItemsNull_ShouldReturnBadRequest()
        {
            // Arrange
            var dishId = GetFirstDishId;

            /* Save a copy of the original entry to compare against*/
            var oldDish = await _context.Dishes.FindAsync(dishId);
            var oldDishCopy = new Dish
            {
                DishId = oldDish.DishId,
                DishRequirements = oldDish.DishRequirements,
                MenuItems = oldDish.MenuItems,
                Name = oldDish.Name
            };

            var dish = new Dish { Name = "Cherry Pie", MenuItems = null };
            var dishContent = new StringContent(JsonConvert.SerializeObject(dish),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress +
                "/" + dishId, dishContent);

            /* Retrieve the (allegedly) updated dish*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + dishId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newDish = JsonConvert.DeserializeObject(newCont, typeof(Dish));

            /* Complete the dish we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            dish.DishRequirements = oldDishCopy.DishRequirements;
            dish.MenuItems = oldDishCopy.MenuItems;
            dish.DishId = oldDishCopy.DishId;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newDish.Should().BeEquivalentTo(oldDishCopy);
            newDish.Should().NotBeEquivalentTo(dish);
        }

        [TestMethod]
        public async Task UpdateDish_MenuItemsNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var dishId = GetFirstDishId;

            /* Save a copy of the original entry to compare against*/
            var oldDish = await _context.Dishes.FindAsync(dishId);
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
            var dishContent = new StringContent(JsonConvert.SerializeObject(dish),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress +
                "/" + dishId, dishContent);

            /* Retrieve the (allegedly) updated dish*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + dishId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newDish = JsonConvert.DeserializeObject(newCont, typeof(Dish));

            /* Complete the dish we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            dish.DishRequirements = oldDishCopy.DishRequirements;
            dish.MenuItems = oldDishCopy.MenuItems;
            dish.DishId = oldDishCopy.DishId;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newDish.Should().BeEquivalentTo(oldDishCopy);
            newDish.Should().NotBeEquivalentTo(dish);
        }

        [TestMethod]
        public async Task UpdateDish_RequirementsNull_ShouldReturnBadRequest()
        {
            // Arrange
            var dishId = GetFirstDishId;

            /* Save a copy of the original entry to compare against*/
            var oldDish = await _context.Dishes.FindAsync(dishId);
            var oldDishCopy = new Dish
            {
                DishId = oldDish.DishId,
                DishRequirements = oldDish.DishRequirements,
                MenuItems = oldDish.MenuItems,
                Name = oldDish.Name
            };

            var dish = new Dish { Name = "Cherry Pie", DishRequirements = null };
            var dishContent = new StringContent(JsonConvert.SerializeObject(dish),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress +
                "/" + dishId, dishContent);

            /* Retrieve the (allegedly) updated dish*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + dishId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newDish = JsonConvert.DeserializeObject(newCont, typeof(Dish));

            /* Complete the dish we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            dish.DishRequirements = oldDishCopy.DishRequirements;
            dish.MenuItems = oldDishCopy.MenuItems;
            dish.DishId = oldDishCopy.DishId;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newDish.Should().BeEquivalentTo(oldDishCopy);
            newDish.Should().NotBeEquivalentTo(dish);
        }

        [TestMethod]
        public async Task UpdateDish_RequirementsNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var dishId = GetFirstDishId;

            /* Save a copy of the original entry to compare against*/
            var oldDish = await _context.Dishes.FindAsync(dishId);
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
            var dishContent = new StringContent(JsonConvert.SerializeObject(dish),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress +
                "/" + dishId, dishContent);

            /* Retrieve the (allegedly) updated dish*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + dishId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newDish = JsonConvert.DeserializeObject(newCont, typeof(Dish));

            /* Complete the dish we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            dish.DishRequirements = oldDishCopy.DishRequirements;
            dish.MenuItems = oldDishCopy.MenuItems;
            dish.DishId = oldDishCopy.DishId;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newDish.Should().BeEquivalentTo(oldDishCopy);
            newDish.Should().NotBeEquivalentTo(dish);
        }

        //[TestMethod]
        //[Ignore]
        //public async Task Update2()
        //{
        //    // Clean-up
        //    var ddish = new Dish { Name = "Double Cheeseburger" };
        //    var ddishContent = new StringContent(JsonConvert.SerializeObject(ddish),
        //        Encoding.UTF8, "application/json");
        //    await _client.PutAsync(requestUri: _client.BaseAddress + "/1" , ddishContent);

        //    // Arrange
        //    var dishId = GetFirstDishId;

        //    /* Save a copy of the original entry to compare against*/
        //    var oldDish = await _context.Dishes.FindAsync(dishId);
        //    var oldDishCopy = new Dish
        //    {
        //        DishId = oldDish.DishId,
        //        DishRequirements = oldDish.DishRequirements,
        //        MenuItems = oldDish.MenuItems,
        //        Name = oldDish.Name
        //    };

        //    var dish = new Dish { Name = "Cherry Pie" };
        //    var dishContent = new StringContent(JsonConvert.SerializeObject(dish),
        //        Encoding.UTF8, "application/json");

        //    // Act
        //    var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + dishId, dishContent);

        //    /* Retrieve the (allegedly) updated dish*/
        //    var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + dishId + "/basic");
        //    var newCont = await updCall.Content.ReadAsStringAsync();
        //    var newDish = JsonConvert.DeserializeObject(newCont, typeof(Dish));

        //    /* Complete the dish we sent in the request with imutable properties 
        //     * that would've 500'd the Request, but are nonetheless necessary
        //     * for comparison to results*/
        //    dish.DishRequirements = oldDishCopy.DishRequirements;
        //    dish.MenuItems = oldDishCopy.MenuItems;
        //    dish.DishId = oldDishCopy.DishId;

        //    var oldDishCompare = JsonConvert.SerializeObject(oldDishCopy);
        //    var dishCompare = JsonConvert.SerializeObject(dish);

        //    // Assert
        //    result.IsSuccessStatusCode.Should().BeTrue();
        //    newDish.Should().NotBeEquivalentTo(oldDishCopy);
        //    newDish.Should().BeEquivalentTo(dish);

        //    // Clean-up
        //    dish = new Dish { Name = "Double Cheeseburger" };
        //    dishContent = new StringContent(JsonConvert.SerializeObject(dish),
        //        Encoding.UTF8, "application/json");
        //    result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + dishId, dishContent);
        //}


        #endregion

        #region Deletes

        [TestMethod]
        public async Task DeleteDish_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var badId = await _context.Dishes.CountAsync() + 1;

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + badId);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task DeleteDish_LastWithNoRelations_ShouldDelete()
        {
            // Arrange
            var dishId = await _context.Dishes.CountAsync() + 1;
            var dish = new Dish { Name = "Crispy Chicken Salad" };
            await _context.Dishes.AddAsync(dish);
            await _context.SaveChangesAsync();

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + dishId);
            var dishExists = await _context.Dishes.AnyAsync(d => d.Name == dish.Name);

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            dishExists.Should().BeFalse();
        }

        [DataTestMethod]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public async Task DeleteDish_HasRelations_ShouldTurnToBlank(bool isCap, bool isFirst)
        {
            // Arrange
            var dishId = 0;
            if (!isCap && !isFirst) dishId = 2;
            else
            {
                if (isFirst) dishId = GetFirstDishId;
                else dishId = await _context.Dishes.CountAsync();
            }

            var dish = await _context.Dishes.FindAsync(dishId);
            var nameCopy = dish.Name;
            dish = null;

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + dishId);
            var getRes = await _client.GetAsync(_client.BaseAddress + "/" + dishId + "/basic");
            var getCont = await getRes.Content.ReadAsStringAsync();
            var getDish = JsonConvert.DeserializeObject(getCont, typeof(Dish)) as Dish;

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            getDish.Name.Should().Contain("Unknown");

            //Clean-up
            dish = new Dish { Name = nameCopy };
            var fixDish = new StringContent(JsonConvert.SerializeObject(dish),
                Encoding.UTF8, "application/json");
            var fixRes = await _client.PutAsync(_client.BaseAddress + "/" + dishId, fixDish);

            //Check-up
            fixRes.IsSuccessStatusCode.Should().BeTrue();
        }

        [TestMethod]
        public async Task DeleteDish_NotLastWithNoRelations_ShouldDelete()
        {
            // Arrange
            var dishId = await _context.Dishes.CountAsync() + 1;
            var delDish = new Dish { Name = "DelMe" };
            var emptyDish = new Dish { Name = "Crispy Chicken Salad" };
            var newName = emptyDish.Name;

            await _context.Dishes.AddRangeAsync(new Dish[] { delDish, emptyDish });
            await _context.SaveChangesAsync();

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + dishId);
            var getRes = await _client.GetAsync(_client.BaseAddress + "/" + dishId +"/basic");
            var getCont = await getRes.Content.ReadAsStringAsync();
            var getDish = JsonConvert.DeserializeObject(getCont, typeof(Dish)) as Dish;

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            getDish.Name.Should().BeEquivalentTo(newName);

            // Clean-up
            await _client.DeleteAsync(_client.BaseAddress + "/" + dishId);
        }

        [TestMethod]
        public async Task DeleteDish_NotLastNoRel_ShouldMigrateRelations()
        {
            // Arrange
            var catId = await _context.SupplyCategories.CountAsync() + 1;
            var dishId = await _context.Dishes.CountAsync() + 1;
            var menuId = await _context.Menus.CountAsync() + 1;
            var reqId = await _context.DishRequirements.CountAsync() + 1;
            var itmId = await _context.MenuItems.CountAsync() + 1;

            var cat = new SupplyCategory { Name = "Fresh Berries" };
            var delDish = new Dish { Name = "Delete Salad" };
            var swapDish = new Dish { Name = "Berry Tart" };
            var menu = new Menu();
            var req = new DishRequirement { DishId = dishId + 1, SupplyCategoryId = catId };
            var itm = new MenuItem { MenuId = menuId, DishId = dishId + 1 };

            await _context.SupplyCategories.AddAsync(cat);
            await _context.Menus.AddAsync(menu);
            await _context.Dishes.AddRangeAsync(new Dish[] { delDish, swapDish });
            await _context.SaveChangesAsync();

            await _context.DishRequirements.AddAsync(req);
            await _context.MenuItems.AddAsync(itm);
            await _context.SaveChangesAsync();

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + dishId);

            var getReqRes = await _client.GetAsync("http://localhost/api/dishrequirements/" +
                reqId + "/basic");
            var getReqCont = await getReqRes.Content.ReadAsStringAsync();
            var getReq = JsonConvert.DeserializeObject(getReqCont, 
                typeof(DishRequirement)) as DishRequirement;

            var getItmRes = await _client.GetAsync("http://localhost/api/menuitems/" +
                itmId+ "/basic");
            var getItmCont = await getItmRes.Content.ReadAsStringAsync();
            var getItm = JsonConvert.DeserializeObject(getItmCont, typeof(MenuItem)) as MenuItem;

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            getReq.DishId.Should().Be(dishId);
            getItm.DishId.Should().Be(dishId);

            // Clean-up
            await _client.DeleteAsync("http://localhost/api/dishrequirements/" + reqId);
            await _client.DeleteAsync("http://localhost/api/menuitems/" + itmId);
            await _client.DeleteAsync("http://localhost/api/menus/" + menuId);
            await _client.DeleteAsync("http://localhost/api/supplycategories/" + catId);
            await _client.DeleteAsync(_client.BaseAddress + "/" + dishId);
        }

        #endregion

        #region Creates

        [TestMethod]
        public async Task CreateDish_HappyFlow_ShouldCreateAndReturnDish()
        {
            // Arrange
            var dish = new Dish { Name = "Cherry Pie" };
            var dishContent = new StringContent(JsonConvert.SerializeObject(dish),
                Encoding.UTF8, "application/json");
            var dishId = await _context.Dishes.CountAsync() + 1;

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, dishContent);
            var getRes = await _client.GetAsync(_client.BaseAddress + "/" + dishId + "/basic");
            var getCont = await getRes.Content.ReadAsStringAsync();
            var getDish = JsonConvert.DeserializeObject(getCont, typeof(Dish)) as Dish;

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            getDish.Name.Should().BeEquivalentTo(dish.Name);

            // Clean-up
            await _client.DeleteAsync(_client.BaseAddress + "/" + dishId);
        }

        [TestMethod]
        public async Task CreateDish_TriesToSetId_ShouldReturnBadRequest()
        {
            // Arrange
            var dish = new Dish { Name = "Cherry Pie", DishId = GenBadDishId };
            var dishContent = new StringContent(JsonConvert.SerializeObject(dish),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, dishContent);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [DataTestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow(null)]
        [DataRow("012345678911234567892123456789312345678941234567890")]
        public async Task CreateDish_BadNames_ShouldReturnBadRequest(string name)
        {
            // Arrange
            var dish = new Dish { Name = name};
            var dishContent = new StringContent(JsonConvert.SerializeObject(dish),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, dishContent);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateDish_DuplicateName_ShouldReturnBadRequest()
        {
            // Arrange
            var name = await _context.Dishes.Select(d => d.Name).FirstOrDefaultAsync();
            var dish = new Dish { Name = name };
            var dishContent = new StringContent(JsonConvert.SerializeObject(dish),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, dishContent);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateDish_MenuItemsNull_ShouldReturnBadRequest()
        {
            // Arrange
            var dish = new Dish { Name = "Cherry Pie", MenuItems = null };
            var dishContent = new StringContent(JsonConvert.SerializeObject(dish),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, dishContent);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateDish_DishRequirementsNull_ShouldReturnBadRequest()
        {
            // Arrange
            var dish = new Dish { Name = "Cherry Pie", DishRequirements=null };
            var dishContent = new StringContent(JsonConvert.SerializeObject(dish),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, dishContent);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateDish_MenuItemsNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var dish = new Dish 
            {
                Name = "Cherry Pie",
                MenuItems = new MenuItem[] { new MenuItem() }
            };
            var dishContent = new StringContent(JsonConvert.SerializeObject(dish),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, dishContent);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateDish_DishRequirementsNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var dish = new Dish
            {
                Name = "Cherry Pie",
                DishRequirements = new DishRequirement[] {new DishRequirement()}
            };
            var dishContent = new StringContent(JsonConvert.SerializeObject(dish),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, dishContent);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        #endregion

        //Helpers

        private int GenBadDishId => _context.Dishes
                .AsEnumerable()
                .Select(d => d.DishId)
                .LastOrDefault() + 1;

        private int GetFirstDishId => _context.Dishes
                .Select(d => d.DishId)
                .FirstOrDefault();
    }
}
