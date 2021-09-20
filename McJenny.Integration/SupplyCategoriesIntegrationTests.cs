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
    public class SupplyCategoriesIntegrationTests
    {
        private static HttpClient _client;
        private static FoodChainsDbContext _context;
        private static WebApplicationFactory<Startup> _appFactory;

        [ClassInitialize]
        public static void InitClass(TestContext testContext)
        {
            _appFactory = new WebApplicationFactory<Startup>();
            _appFactory.ClientOptions.BaseAddress = new Uri("http://localhost/api/supplycategories");
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
            _appFactory.ClientOptions.BaseAddress = new Uri("http://localhost/api/supplycategories");
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
        public async Task GetSupplyCategories_HappyFlow_ShouldReturnSupplyCategories()
        {
            // Arrange
            var expectedObj = await _context.SupplyCategories
            .Select(c => new { c.SupplyCategoryId, c.Name })
            .ToListAsync();
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
        [Ignore]
        public async Task GetSupplyCategoryCheating_HappyFlow_ShouldReturnSupplyCategory()
        {
            // Arrange
            var supCatId = GetFirstSupplyCategoryId;

            var expected = (await _context.SupplyCategories.FindAsync(supCatId)).Name;
            //var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + supCatId);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSupplyCategory_HappyFlow_ShouldReturnSupplyCategory()
        {
            // Arrange
            var catId = GetFirstSupplyCategoryId;
            var cat = await _context.SupplyCategories.FindAsync(catId);
            var expected = cat.Name;

            // Act
            var result = await _client.GetAsync(_client.BaseAddress + "/" + catId);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSupplyCategory_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var supplyCategoryId = GenBadSupplyCategoryId;

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + supplyCategoryId);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task GetSupplyCategoryBasic_HappyFlow_ShouldReturnSupplyCategory()
        {
            // Arrange
            var supCatId = GetFirstSupplyCategoryId;

            var expected = await _context.SupplyCategories.FindAsync(supCatId);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/" + supCatId + "/basic");
            var content = await result.Content.ReadAsStringAsync();
            var retrieved = JsonConvert.DeserializeObject(content, typeof(SupplyCategory));

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            retrieved.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSupplyCategoryBasic_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var supCatId = GenBadSupplyCategoryId;

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/" + supCatId + "/basic");

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task GetSupplyCategoryLinks_HappyFlow_ShouldReturnSupplyLinks()
        {
            // Arrange
            var catId = GetFirstSupplyCategoryId;

            var supplyLinks = await _context.SupplyLinks
                .Where(l => l.SupplyCategoryId == catId)
                .ToArrayAsync();

            var supIds = new List<int>();
            var locIds = new List<int>();

            foreach (var link in supplyLinks)
            {
                supIds.Add(link.SupplierId);
                locIds.Add(link.LocationId);
            };

            supIds = supIds.Distinct().ToList();
            locIds = locIds.Distinct().ToList();

            var suppliers = await _context.Suppliers
                .Select(s => new
                {
                    s.SupplierId,
                    s.Name,
                    s.AbreviatedCountry,
                    s.AbreviatedState,
                    s.City
                })
                .Where(s => supIds.Contains(s.SupplierId))
                .ToArrayAsync();

            var locations = await _context.Locations
                .Select(l => new
                {
                    l.LocationId,
                    l.AbreviatedCountry,
                    l.AbreviatedState,
                    l.City,
                    l.Street
                })
                .Where(l => locIds.Contains(l.LocationId))
                .ToArrayAsync();

            var expected = new string[supplyLinks.Length];

            for (int i = 0; i < expected.Length; i++)
            {
                var sup = suppliers.FirstOrDefault(s => s.SupplierId == supplyLinks[i].SupplierId);
                var loc = locations.FirstOrDefault(l => l.LocationId == supplyLinks[i].LocationId);

                expected[i] = string.Format("(Supply Link: {0}): ({1}) {2}, {3}, {4}{5} " +
                    "supplies ({6}) {7}, {8}{9}, {10}.",
                    i,
                    sup.SupplierId,
                    sup.Name,
                    sup.AbreviatedCountry,
                    sup.AbreviatedState == "N/A" ? string.Empty :
                    sup.AbreviatedState,
                    sup.City,
                    loc.LocationId,
                    loc.AbreviatedCountry,
                    loc.AbreviatedState,
                    loc.City,
                    loc.Street);
            }

            // Act
            var call = await _client.GetAsync(_client.BaseAddress + "/" + catId + "/links");
            var cont = await call.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject(cont, typeof(IEnumerable<string>)) as IEnumerable<string>;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSupplyCategoryLinks_InexistentId_ShouldReturnBadRequest()
        {
            // Arrange
            var badId = GenBadSupplyCategoryId;

            // Act
            var result = await _client.GetAsync(_client.BaseAddress + "/" + badId + "/links");

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task GetDishes_HappyFlow_ShouldReturnDishes()
        {
            // Arrange
            var catId = GetFirstSupplyCategoryId;

            var dishRequirements = await _context.DishRequirements
               .Where(r => r.SupplyCategoryId == catId)
               .ToArrayAsync();

            var dishIds = new List<int>();

            foreach (var req in dishRequirements)
                dishIds.Add(req.DishId);

            dishIds = dishIds.Distinct().ToList();

            var expected = await _context.Dishes
                .Select(d => new
                {
                    d.DishId,
                    d.Name
                })
                .Where(d => dishIds.Contains(d.DishId))
                .ToArrayAsync();
            var definition = new[] { new { DishId = 0, Name = string.Empty } };

            // Act
            var call = await _client.GetAsync(_client.BaseAddress + "/" + catId + "/dishes");
            var cont = await call.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeAnonymousType(cont, definition);

            // Assert
            call.IsSuccessStatusCode.Should().BeTrue();
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetDishes_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var badId = GenBadSupplyCategoryId;

            // Act
            var result = await _client.GetAsync(_client.BaseAddress + "/" + badId + "/dishes");

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task GetSuppliers_HappyFlow_ShouldReturnSuppliers()
        {
            // Arrange
            var catId = GetFirstSupplyCategoryId;

            var supplierStocks = await _context.SupplierStocks
                .Where(r => r.SupplyCategoryId == catId)
                .ToArrayAsync();

            var supIds = new List<int>();

            foreach (var stock in supplierStocks)
                supIds.Add(stock.SupplierId);

            supIds = supIds.Distinct().ToList();

            var expected = await _context.Suppliers
                .Select(s => new
                {
                    s.SupplierId,
                    s.Name,
                    s.AbreviatedCountry,
                    s.AbreviatedState,
                    s.City
                })
                .Where(s => supIds.Contains(s.SupplierId))
                .ToArrayAsync();
            var definition = new[] {new
            {
                SupplierId=0,
                Name = string.Empty,
                AbreviatedCountry = string.Empty,
                AbreviatedState = string.Empty,
                City = string.Empty
            }};

            // Act
            var call = await _client.GetAsync(_client.BaseAddress + "/" + catId + "/suppliers");
            var cont = await call.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeAnonymousType(cont, definition);

            // Assert
            call.IsSuccessStatusCode.Should().BeTrue();
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSuppliers_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var badId = GenBadSupplyCategoryId;

            // Act
            var result = await _client.GetAsync(_client.BaseAddress + "/" + badId + "/suppliers");

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        #endregion

        #region Advanced

        [TestMethod]
        public async Task GetSupplyCategoriesFiltered_NameMatch_ShouldReturnFiltered()
        {
            // Arrange
            var filter = await _context.SupplyCategories.Select(c => c.Name).FirstOrDefaultAsync();

            var expectedObj = await _context.SupplyCategories
                 .Select(c => new { c.SupplyCategoryId, c.Name })
                 .Where(c => c.Name.ToUpper()
                     .Contains(filter.ToUpper()))
                 .ToArrayAsync();

            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/query:filter=" + filter);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSupplyCategoriesPaged_HapyFlow_ShouldReturnPaged()
        {
            // Arrange
            var pgSz = 2;
            var pgInd = 1;

            var expectedObj = await _context.SupplyCategories
                 .Select(c => new { c.SupplyCategoryId, c.Name })
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
        public async Task GetSupplyCategoriesPaged_EndPageIncomplete_ShouldReturnPaged()
        {
            // Arrange
            var dishCount = await _context.SupplyCategories.CountAsync();
            var pgSz = 1;

            while (true)
            {
                if (dishCount % pgSz == 0) pgSz++;
                else break;
            }

            var pgInd = dishCount / (pgSz - 1);

            var expectedObj = await _context.SupplyCategories
                 .Select(c => new { c.SupplyCategoryId, c.Name })
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
        public async Task GetSupplyCategoriesPaged_SizeOver_ShouldReturnPaged()
        {
            // Arrange
            var dishCount = await _context.SupplyCategories.CountAsync();
            var pgSz = dishCount + 1;
            var pgInd = 1;

            var expectedObj = await _context.SupplyCategories
                 .Select(c => new { c.SupplyCategoryId, c.Name })
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
        public async Task GetSupplyCategoriesPaged_IndexOver_ShouldReturnPaged()
        {
            // Arrange
            var dishCount = await _context.SupplyCategories.CountAsync();
            var pgInd = dishCount + 1;
            var pgSz = 1;

            var expectedObj = await _context.SupplyCategories
                 .Select(c => new { c.SupplyCategoryId, c.Name })
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
        public async Task GetSupplyCategoriesPaged_HapyFlow_ShouldReturnPaged(int pgSz, int pgInd)
        {
            // Arrange
            var absPgSz = Math.Abs(pgSz);
            var absPgInd = Math.Abs(pgInd);

            var expectedObj = await _context.SupplyCategories
                 .Select(c => new { c.SupplyCategoryId, c.Name })
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
        public async Task GetSupplyCategoriesSorted_ByName_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "name";

            var expectedObj = (object)await _context.SupplyCategories
                            .Select(c => new { c.SupplyCategoryId, c.Name })
                            .OrderBy(c => c.Name)
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
        public async Task GetSupplyCategoriesSorted_GarbleText_ShouldReturnSortedByID()
        {
            // Arrange
            var sortBy = "hbhbhjas";

            var expectedObj = (object)await _context.SupplyCategories
                            .Select(c => new { c.SupplyCategoryId, c.Name })
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
        public async Task GetSupplyCategoriesSorted_Desc_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "name";

            var expectedObj = (object)await _context.SupplyCategories
                            .Select(c => new { c.SupplyCategoryId, c.Name })
                            .OrderByDescending(c => c.Name)
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
        public async Task GetSupplyCategoriesQueried_FilteredAndSorted_ShouldReturnFilteredAndSorted()
        {
            // Arrange
            var sortBy = "name";
            var filter = "g";

            var filtered = await _context.SupplyCategories
                 .Select(c => new { c.SupplyCategoryId, c.Name })
                 .Where(c => c.Name.ToUpper()
                     .Contains(filter.ToUpper()))
                 .ToArrayAsync();

            var expectedObj = filtered
                            .Select(c => new { c.SupplyCategoryId, c.Name })
                            .OrderByDescending(c => c.Name)
                            .ToArray();
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/query:desc=true&filter=" + filter + "&sortby=" + sortBy);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSupplyCategoriesQueried_FilteredAndPaged_ShouldReturnFilteredAndPaged()
        {
            // Arrange
            int pgSz = 2;
            int pgInd = 1;
            var filter = "g";

            var filtered = await _context.SupplyCategories
                 .Select(c => new { c.SupplyCategoryId, c.Name })
                 .Where(c => c.Name.ToUpper()
                     .Contains(filter.ToUpper()))
                 .ToArrayAsync();

            var expectedObj = filtered
                .Select(c => new { c.SupplyCategoryId, c.Name })
                .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArray();
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/query:filter=" + filter + "&pgsz=" + pgSz + "&pgind=" + pgInd);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSupplyCategoriesQueried_SortedAndPaged_ShouldReturnSortedAndPaged()
        {
            // Arrange
            int pgSz = 2;
            int pgInd = 1;
            var sortBy = "name";

            var sorted = await _context.SupplyCategories
                            .Select(c => new { c.SupplyCategoryId, c.Name })
                            .OrderByDescending(c => c.Name)
                            .ToArrayAsync();

            var expectedObj = sorted
                .Select(c => new { c.SupplyCategoryId, c.Name })
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
        public async Task GetSupplyCategoriesQueried_FilteredSortedAndPaged_ShouldReturnFilteredSortedAndPaged()
        {
            // Arrange
            int pgSz = 2;
            int pgInd = 1;
            var sortBy = "name";
            var filter = "g";

            var filtered = await _context.SupplyCategories
                 .Select(c => new { c.SupplyCategoryId, c.Name })
                 .Where(c => c.Name.ToUpper()
                     .Contains(filter.ToUpper()))
                 .ToArrayAsync();

            var sorted = filtered
                            .Select(c => new { c.SupplyCategoryId, c.Name })
                            .OrderByDescending(c => c.Name)
                            .ToArray();

            var expectedObj = sorted
                .Select(c => new { c.SupplyCategoryId, c.Name })
                .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArray();
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/query:desc=true&sortby=" + sortBy + "&pgsz=" + pgSz + "&pgind=" + pgInd + "&filter=" + filter);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        #endregion

        #endregion

        #region Updates

        [TestMethod]
        public async Task UpdateSupplyCategory_HappyFlow_ShouldUpdateAndReturnSupplyCategory()
        {
            // Arrange
            var supCatId = GetFirstSupplyCategoryId;

            /* Save a copy of the original entry to compare against*/
            var oldSupCat = await _context.SupplyCategories.FindAsync(supCatId);
            var oldSupCatCopy = new SupplyCategory
            {
                SupplyCategoryId = oldSupCat.SupplyCategoryId,
                SupplyLinks = oldSupCat.SupplyLinks,
                SupplierStocks = oldSupCat.SupplierStocks,
                DishRequirements= oldSupCat.DishRequirements,
                Name = oldSupCat.Name
            };

            var supCat = new SupplyCategory { Name = "Fresh Juices" };
            var supCatContent = new StringContent(JsonConvert.SerializeObject(supCat),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + supCatId, supCatContent);

            /* Retrieve the (allegedly) updated supCat*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + supCatId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newSupCat = JsonConvert.DeserializeObject(newCont, typeof(SupplyCategory));

            /* Complete the supCat we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            supCat.SupplyCategoryId = oldSupCatCopy.SupplyCategoryId;
            supCat.SupplierStocks = oldSupCatCopy.SupplierStocks;
            supCat.SupplyLinks = oldSupCatCopy.SupplyLinks;
            supCat.DishRequirements = oldSupCatCopy.DishRequirements;

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            newSupCat.Should().NotBeEquivalentTo(oldSupCatCopy);
            newSupCat.Should().BeEquivalentTo(supCat);

            // Clean-up
            supCat = new SupplyCategory { Name = oldSupCatCopy.Name };
            supCatContent = new StringContent(JsonConvert.SerializeObject(supCat),
                Encoding.UTF8, "application/json");
            result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + supCatId, supCatContent);
            updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + supCatId + "/basic");
            newCont = await updCall.Content.ReadAsStringAsync();
            newSupCat = JsonConvert.DeserializeObject(newCont, typeof(SupplyCategory));

            result.IsSuccessStatusCode.Should().BeTrue();
            newSupCat.Should().BeEquivalentTo(oldSupCatCopy);
        }

        [TestMethod]
        public async Task UpdateSupplyCategory_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var supCatId = GetFirstSupplyCategoryId;

            /* Save a copy of the original entry to compare against*/
            var oldSupCat = await _context.SupplyCategories.FindAsync(supCatId);
            var oldSupCatCopy = new SupplyCategory
            {
                SupplyCategoryId = oldSupCat.SupplyCategoryId,
                SupplyLinks = oldSupCat.SupplyLinks,
                SupplierStocks = oldSupCat.SupplierStocks,
                DishRequirements = oldSupCat.DishRequirements,
                Name = oldSupCat.Name
            };

            var supCat = new SupplyCategory { Name = "Fresh Juices" };
            var supCatContent = new StringContent(JsonConvert.SerializeObject(supCat),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + 
                GenBadSupplyCategoryId, supCatContent);

            /* Retrieve the (allegedly) updated supCat*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + supCatId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newSupCat = JsonConvert.DeserializeObject(newCont, typeof(SupplyCategory));

            /* Complete the supCat we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            supCat.SupplyCategoryId = oldSupCatCopy.SupplyCategoryId;
            supCat.SupplierStocks = oldSupCatCopy.SupplierStocks;
            supCat.SupplyLinks = oldSupCatCopy.SupplyLinks;
            supCat.DishRequirements = oldSupCatCopy.DishRequirements;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
            newSupCat.Should().BeEquivalentTo(oldSupCatCopy);
            newSupCat.Should().NotBeEquivalentTo(supCat);
        }

        [TestMethod]
        public async Task UpdateSupplyCategory_TriesToSetId_ShouldReturnBadRequest()
        {
            // Arrange
            var supCatId = GetFirstSupplyCategoryId;

            /* Save a copy of the original entry to compare against*/
            var oldSupCat = await _context.SupplyCategories.FindAsync(supCatId);
            var oldSupCatCopy = new SupplyCategory
            {
                SupplyCategoryId = oldSupCat.SupplyCategoryId,
                SupplyLinks = oldSupCat.SupplyLinks,
                SupplierStocks = oldSupCat.SupplierStocks,
                DishRequirements = oldSupCat.DishRequirements,
                Name = oldSupCat.Name
            };

            var supCat = new SupplyCategory { Name = "Fresh Juices", SupplyCategoryId = GenBadSupplyCategoryId };
            var supCatContent = new StringContent(JsonConvert.SerializeObject(supCat),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + supCatId, supCatContent);

            /* Retrieve the (allegedly) updated supCat*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + supCatId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newSupCat = JsonConvert.DeserializeObject(newCont, typeof(SupplyCategory));

            /* Complete the supCat we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            supCat.SupplyCategoryId = oldSupCatCopy.SupplyCategoryId;
            supCat.SupplierStocks = oldSupCatCopy.SupplierStocks;
            supCat.SupplyLinks = oldSupCatCopy.SupplyLinks;
            supCat.DishRequirements = oldSupCatCopy.DishRequirements;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newSupCat.Should().BeEquivalentTo(oldSupCatCopy);
            newSupCat.Should().NotBeEquivalentTo(supCat);
        }

        [DataTestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow(null)]
        [DataRow("0123456789112345678921234567893123456789412345678951")]
        public async Task UpdateSupplyCategory_BadNames_ShouldReturnNotFound(string name)
        {
            // Arrange
            var supCatId = GetFirstSupplyCategoryId;

            /* Save a copy of the original entry to compare against*/
            var oldSupCat = await _context.SupplyCategories.FindAsync(supCatId);
            var oldSupCatCopy = new SupplyCategory
            {
                SupplyCategoryId = oldSupCat.SupplyCategoryId,
                SupplyLinks = oldSupCat.SupplyLinks,
                SupplierStocks = oldSupCat.SupplierStocks,
                DishRequirements = oldSupCat.DishRequirements,
                Name = oldSupCat.Name
            };

            var supCat = new SupplyCategory { Name = name };
            var supCatContent = new StringContent(JsonConvert.SerializeObject(supCat),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" +
                GenBadSupplyCategoryId, supCatContent);

            /* Retrieve the (allegedly) updated supCat*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + supCatId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newSupCat = JsonConvert.DeserializeObject(newCont, typeof(SupplyCategory));

            /* Complete the supCat we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            supCat.SupplyCategoryId = oldSupCatCopy.SupplyCategoryId;
            supCat.SupplierStocks = oldSupCatCopy.SupplierStocks;
            supCat.SupplyLinks = oldSupCatCopy.SupplyLinks;
            supCat.DishRequirements = oldSupCatCopy.DishRequirements;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
            newSupCat.Should().BeEquivalentTo(oldSupCatCopy);
            newSupCat.Should().NotBeEquivalentTo(supCat);
        }

        [TestMethod]
        public async Task UpdateSupplyCategory_DupicateName_ShouldReturnBadRequest()
        {
            // Arrange
            var supCatId = GetFirstSupplyCategoryId;

            var dupe = (await _context.SupplyCategories
                .Select(c => new { c.SupplyCategoryId, c.Name })
                .FirstOrDefaultAsync(c => c.SupplyCategoryId != supCatId))
                .Name;

            /* Save a copy of the original entry to compare against*/
            var oldSupCat = await _context.SupplyCategories.FindAsync(supCatId);
            var oldSupCatCopy = new SupplyCategory
            {
                SupplyCategoryId = oldSupCat.SupplyCategoryId,
                SupplyLinks = oldSupCat.SupplyLinks,
                SupplierStocks = oldSupCat.SupplierStocks,
                DishRequirements = oldSupCat.DishRequirements,
                Name = oldSupCat.Name
            };

            var supCat = new SupplyCategory { Name = dupe };
            var supCatContent = new StringContent(JsonConvert.SerializeObject(supCat),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + supCatId, supCatContent);

            /* Retrieve the (allegedly) updated supCat*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + supCatId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newSupCat = JsonConvert.DeserializeObject(newCont, typeof(SupplyCategory));

            /* Complete the supCat we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            supCat.SupplyCategoryId = oldSupCatCopy.SupplyCategoryId;
            supCat.SupplierStocks = oldSupCatCopy.SupplierStocks;
            supCat.SupplyLinks = oldSupCatCopy.SupplyLinks;
            supCat.DishRequirements = oldSupCatCopy.DishRequirements;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newSupCat.Should().BeEquivalentTo(oldSupCatCopy);
            newSupCat.Should().NotBeEquivalentTo(supCat);
        }

        [TestMethod]
        public async Task UpdateSupplyCategory_DishRequirementsNull_ShouldReturnBadRequest()
        {
            // Arrange
            var supCatId = GetFirstSupplyCategoryId;

            /* Save a copy of the original entry to compare against*/
            var oldSupCat = await _context.SupplyCategories.FindAsync(supCatId);
            var oldSupCatCopy = new SupplyCategory
            {
                SupplyCategoryId = oldSupCat.SupplyCategoryId,
                SupplyLinks = oldSupCat.SupplyLinks,
                SupplierStocks = oldSupCat.SupplierStocks,
                DishRequirements = oldSupCat.DishRequirements,
                Name = oldSupCat.Name
            };

            var supCat = new SupplyCategory 
            { 
                Name = "Fresh Juices",
                DishRequirements = null
            };
            var supCatContent = new StringContent(JsonConvert.SerializeObject(supCat),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + supCatId, supCatContent);

            /* Retrieve the (allegedly) updated supCat*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + supCatId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newSupCat = JsonConvert.DeserializeObject(newCont, typeof(SupplyCategory));

            /* Complete the supCat we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            supCat.SupplyCategoryId = oldSupCatCopy.SupplyCategoryId;
            supCat.SupplierStocks = oldSupCatCopy.SupplierStocks;
            supCat.SupplyLinks = oldSupCatCopy.SupplyLinks;
            supCat.DishRequirements = oldSupCatCopy.DishRequirements;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newSupCat.Should().BeEquivalentTo(oldSupCatCopy);
            newSupCat.Should().NotBeEquivalentTo(supCat);
        }

        [TestMethod]
        public async Task UpdateSupplyCategory_SupplyLinksNull_ShouldReturnBadRequest()
        {
            // Arrange
            var supCatId = GetFirstSupplyCategoryId;

            /* Save a copy of the original entry to compare against*/
            var oldSupCat = await _context.SupplyCategories.FindAsync(supCatId);
            var oldSupCatCopy = new SupplyCategory
            {
                SupplyCategoryId = oldSupCat.SupplyCategoryId,
                SupplyLinks = oldSupCat.SupplyLinks,
                SupplierStocks = oldSupCat.SupplierStocks,
                DishRequirements = oldSupCat.DishRequirements,
                Name = oldSupCat.Name
            };

            var supCat = new SupplyCategory
            {
                Name = "Fresh Juices",
                SupplyLinks = null
            };
            var supCatContent = new StringContent(JsonConvert.SerializeObject(supCat),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + supCatId, supCatContent);

            /* Retrieve the (allegedly) updated supCat*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + supCatId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newSupCat = JsonConvert.DeserializeObject(newCont, typeof(SupplyCategory));

            /* Complete the supCat we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            supCat.SupplyCategoryId = oldSupCatCopy.SupplyCategoryId;
            supCat.SupplierStocks = oldSupCatCopy.SupplierStocks;
            supCat.SupplyLinks = oldSupCatCopy.SupplyLinks;
            supCat.DishRequirements = oldSupCatCopy.DishRequirements;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newSupCat.Should().BeEquivalentTo(oldSupCatCopy);
            newSupCat.Should().NotBeEquivalentTo(supCat);
        }

        [TestMethod]
        public async Task UpdateSupplyCategory_SupplierStocksNull_ShouldReturnBadRequest()
        {
            // Arrange
            var supCatId = GetFirstSupplyCategoryId;

            /* Save a copy of the original entry to compare against*/
            var oldSupCat = await _context.SupplyCategories.FindAsync(supCatId);
            var oldSupCatCopy = new SupplyCategory
            {
                SupplyCategoryId = oldSupCat.SupplyCategoryId,
                SupplyLinks = oldSupCat.SupplyLinks,
                SupplierStocks = oldSupCat.SupplierStocks,
                DishRequirements = oldSupCat.DishRequirements,
                Name = oldSupCat.Name
            };

            var supCat = new SupplyCategory
            {
                Name = "Fresh Juices",
                SupplierStocks= null
            };
            var supCatContent = new StringContent(JsonConvert.SerializeObject(supCat),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + supCatId, supCatContent);

            /* Retrieve the (allegedly) updated supCat*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + supCatId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newSupCat = JsonConvert.DeserializeObject(newCont, typeof(SupplyCategory));

            /* Complete the supCat we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            supCat.SupplyCategoryId = oldSupCatCopy.SupplyCategoryId;
            supCat.SupplierStocks = oldSupCatCopy.SupplierStocks;
            supCat.SupplyLinks = oldSupCatCopy.SupplyLinks;
            supCat.DishRequirements = oldSupCatCopy.DishRequirements;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newSupCat.Should().BeEquivalentTo(oldSupCatCopy);
            newSupCat.Should().NotBeEquivalentTo(supCat);
        }

        [TestMethod]
        public async Task UpdateSupplyCategory_DishRequirementsNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var supCatId = GetFirstSupplyCategoryId;

            /* Save a copy of the original entry to compare against*/
            var oldSupCat = await _context.SupplyCategories.FindAsync(supCatId);
            var oldSupCatCopy = new SupplyCategory
            {
                SupplyCategoryId = oldSupCat.SupplyCategoryId,
                SupplyLinks = oldSupCat.SupplyLinks,
                SupplierStocks = oldSupCat.SupplierStocks,
                DishRequirements = oldSupCat.DishRequirements,
                Name = oldSupCat.Name
            };

            var supCat = new SupplyCategory
            {
                Name = "Fresh Juices",
                DishRequirements = new DishRequirement[] {new DishRequirement()}
            };
            var supCatContent = new StringContent(JsonConvert.SerializeObject(supCat),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + supCatId, supCatContent);

            /* Retrieve the (allegedly) updated supCat*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + supCatId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newSupCat = JsonConvert.DeserializeObject(newCont, typeof(SupplyCategory));

            /* Complete the supCat we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            supCat.SupplyCategoryId = oldSupCatCopy.SupplyCategoryId;
            supCat.SupplierStocks = oldSupCatCopy.SupplierStocks;
            supCat.SupplyLinks = oldSupCatCopy.SupplyLinks;
            supCat.DishRequirements = oldSupCatCopy.DishRequirements;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newSupCat.Should().BeEquivalentTo(oldSupCatCopy);
            newSupCat.Should().NotBeEquivalentTo(supCat);
        }

        [TestMethod]
        public async Task UpdateSupplyCategory_SupplyLinksNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var supCatId = GetFirstSupplyCategoryId;

            /* Save a copy of the original entry to compare against*/
            var oldSupCat = await _context.SupplyCategories.FindAsync(supCatId);
            var oldSupCatCopy = new SupplyCategory
            {
                SupplyCategoryId = oldSupCat.SupplyCategoryId,
                SupplyLinks = oldSupCat.SupplyLinks,
                SupplierStocks = oldSupCat.SupplierStocks,
                DishRequirements = oldSupCat.DishRequirements,
                Name = oldSupCat.Name
            };

            var supCat = new SupplyCategory
            {
                Name = "Fresh Juices",
                SupplyLinks = new SupplyLink[] { new SupplyLink()}
            };
            var supCatContent = new StringContent(JsonConvert.SerializeObject(supCat),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + supCatId, supCatContent);

            /* Retrieve the (allegedly) updated supCat*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + supCatId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newSupCat = JsonConvert.DeserializeObject(newCont, typeof(SupplyCategory));

            /* Complete the supCat we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            supCat.SupplyCategoryId = oldSupCatCopy.SupplyCategoryId;
            supCat.SupplierStocks = oldSupCatCopy.SupplierStocks;
            supCat.SupplyLinks = oldSupCatCopy.SupplyLinks;
            supCat.DishRequirements = oldSupCatCopy.DishRequirements;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newSupCat.Should().BeEquivalentTo(oldSupCatCopy);
            newSupCat.Should().NotBeEquivalentTo(supCat);
        }

        [TestMethod]
        public async Task UpdateSupplyCategory_SupplierStocksNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var supCatId = GetFirstSupplyCategoryId;

            /* Save a copy of the original entry to compare against*/
            var oldSupCat = await _context.SupplyCategories.FindAsync(supCatId);
            var oldSupCatCopy = new SupplyCategory
            {
                SupplyCategoryId = oldSupCat.SupplyCategoryId,
                SupplyLinks = oldSupCat.SupplyLinks,
                SupplierStocks = oldSupCat.SupplierStocks,
                DishRequirements = oldSupCat.DishRequirements,
                Name = oldSupCat.Name
            };

            var supCat = new SupplyCategory
            {
                Name = "Fresh Juices",
                SupplierStocks = new SupplierStock[] {new SupplierStock()}
            };
            var supCatContent = new StringContent(JsonConvert.SerializeObject(supCat),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + supCatId, supCatContent);

            /* Retrieve the (allegedly) updated supCat*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + supCatId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newSupCat = JsonConvert.DeserializeObject(newCont, typeof(SupplyCategory));

            /* Complete the supCat we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            supCat.SupplyCategoryId = oldSupCatCopy.SupplyCategoryId;
            supCat.SupplierStocks = oldSupCatCopy.SupplierStocks;
            supCat.SupplyLinks = oldSupCatCopy.SupplyLinks;
            supCat.DishRequirements = oldSupCatCopy.DishRequirements;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newSupCat.Should().BeEquivalentTo(oldSupCatCopy);
            newSupCat.Should().NotBeEquivalentTo(supCat);
        }

        #endregion

        #region Creates

        [TestMethod]
        public async Task CreateSupplyCategory_HappyFlow_ShouldCreateAndReturnSupplyCategory()
        {
            // Arrange
            var catId = await _context.SupplyCategories.CountAsync() + 1;
            var cat = new SupplyCategory
            {
                Name = "Greek Olives"
            };
            var catCont = new StringContent(JsonConvert.SerializeObject(cat),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, catCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();

            // Clean-up
            await _client.DeleteAsync(_client.BaseAddress + "/" + catId);
        }

        [TestMethod]
        public async Task CreateSupplyCategory_TriesToSetId_ShouldReturnBadRequest()
        {
            // Arrange
            var catId = await _context.SupplyCategories.CountAsync() + 1;
            var cat = new SupplyCategory
            {
                Name = "Greek Olives",
                SupplyCategoryId = catId
            };
            var catCont = new StringContent(JsonConvert.SerializeObject(cat),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, catCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [DataTestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow(null)]
        [DataRow("0123456789112345678921234567893123456789412345678951")]
        public async Task CreateSupplyCategory_BadNames_ShouldReturnBadRequest(string name)
        {
            // Arrange
            var cat = new SupplyCategory
            {
                Name = name
            };
            var catCont = new StringContent(JsonConvert.SerializeObject(cat),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, catCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateSupply_DuplicateName_ShouldReturnBadRequest()
        {
            // Arrange
            var dupe = await _context.SupplyCategories.FirstOrDefaultAsync();
            var cat = new SupplyCategory
            {
                Name = dupe.Name
            };
            var catCont = new StringContent(JsonConvert.SerializeObject(cat),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, catCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateSupplyCategory_DishRequirementsNull_ShouldReturnBadRequest()
        {
            // Arrange
            var cat = new SupplyCategory
            {
                Name = "Greek Olives",
                DishRequirements = null
            };
            var catCont = new StringContent(JsonConvert.SerializeObject(cat),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, catCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateSupplyCategory_SupplierStocksNull_ShouldReturnBadRequest()
        {
            // Arrange
            var cat = new SupplyCategory
            {
                Name = "Greek Olives",
                SupplierStocks = null
            };
            var catCont = new StringContent(JsonConvert.SerializeObject(cat),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, catCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateSupplyCategory_SupplyLinksNull_ShouldReturnBadRequest()
        {
            // Arrange
            var cat = new SupplyCategory
            {
                Name = "Greek Olives",
                SupplyLinks = null
            };
            var catCont = new StringContent(JsonConvert.SerializeObject(cat),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, catCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateSupplyCategory_DishRequirementsNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var cat = new SupplyCategory
            {
                Name = "Greek Olives",
                DishRequirements = new DishRequirement[] {new DishRequirement()}
            };
            var catCont = new StringContent(JsonConvert.SerializeObject(cat),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, catCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateSupplyCategory_SupplierStocksNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var cat = new SupplyCategory
            {
                Name = "Greek Olives",
                SupplierStocks = new SupplierStock[] {new SupplierStock()}
            };
            var catCont = new StringContent(JsonConvert.SerializeObject(cat),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, catCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateSupplyCategory_SupplyLinksNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var cat = new SupplyCategory
            {
                Name = "Greek Olives",
                SupplyLinks = new SupplyLink[] {new SupplyLink()}
            };
            var catCont = new StringContent(JsonConvert.SerializeObject(cat),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, catCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        #endregion

        //Helpers
        private int GenBadSupplyCategoryId => _context.SupplyCategories
                .AsEnumerable()
                .Select(c => c.SupplyCategoryId)
                .LastOrDefault() + 1;
        private int GetFirstSupplyCategoryId => _context.SupplyCategories
            .Select(c => c.SupplyCategoryId)
            .FirstOrDefault();
    }
}
