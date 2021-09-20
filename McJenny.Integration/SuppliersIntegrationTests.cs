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
    public class SuppliersIntegrationTests
    {
        private static HttpClient _client;
        private static FoodChainsDbContext _context;
        private static WebApplicationFactory<Startup> _appFactory;

        [ClassInitialize]
        public static void InitClass(TestContext testContext)
        {
            _appFactory = new WebApplicationFactory<Startup>();
            _appFactory.ClientOptions.BaseAddress = new Uri("http://localhost/api/suppliers");
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
            _appFactory.ClientOptions.BaseAddress = new Uri("http://localhost/api/suppliers");
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
        public async Task GetSuppliers_HappyFlow_ShouldReturnSuppliers()
        {
            // Arrange
            var expectedObj = await _context.Suppliers
            .Take(20)
            .Select(s =>
            string.Format("({0}) {1}, {2}{3}, {4}",
                s.SupplierId, s.Name, s.AbreviatedCountry,
                s.AbreviatedState == "N/A" ? string.Empty : ", " + s.AbreviatedState,
                s.City
                ))
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
        public async Task GetSupplierCheating_HappyFlow_ShouldReturnSupplier()
        {
            // Arrange
            var supId = GetFirstSupplierId;

            var expectedObj = await _context.Suppliers
                .Select(s => new
                {
                    s.SupplierId,
                    s.Name,
                    s.AbreviatedCountry,
                    s.AbreviatedState,
                    s.City
                })
                .FirstOrDefaultAsync(s => s.SupplierId == supId);
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + supId);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSupplier_HappyFlow_ShouldReturnSupplier()
        {
            // Arrange
            var supId = GetFirstSupplierId;
            var supplier = await _context.Suppliers.FindAsync(supId);
            var expected = new
            {
                supplier.SupplierId,
                supplier.Name,
                supplier.AbreviatedCountry,
                supplier.AbreviatedState,
                supplier.City
            };
            var definition = new
            {
                SupplierId = 0,
                Name = string.Empty,
                AbreviatedCountry = string.Empty,
                AbreviatedState = string.Empty,
                City = string.Empty
            };

            // Act
            var result = await _client.GetAsync(_client.BaseAddress + "/" + supId);
            var content = await result.Content.ReadAsStringAsync();
            var sup = JsonConvert.DeserializeAnonymousType(content, definition);

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            sup.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSupplier_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var supId = GenBadSupplierId;

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + supId);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task GetLocations_HappyFlow_ShouldReturnLocations()
        {
            // Arrange
            var supId = GetFirstSupplierId;

            var locIds = await _context.SupplyLinks
                .Where(l => l.SupplierId == supId)
                .Select(l => l.LocationId)
                .ToArrayAsync();

            var expectedObj = await _context.Locations
                .Where(l => locIds
                    .Contains(l.LocationId))
                .Select(l => string.Format("({0}), {1}, {2}{3}, {4}",
                    l.LocationId,
                    l.AbreviatedCountry,
                    l.AbreviatedState == "N/A" ? string.Empty :
                    l.AbreviatedState + ", ",
                    l.City,
                    l.Street))
                .ToArrayAsync();

            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + supId + "/locations");
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetLocations_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var supId = GenBadSupplierId;

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + supId + "/locations");
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task GetSupplierBasic_HappyFlow_ShouldReturnSupplier()
        {
            // Arrange
            var supId = GetFirstSupplierId;

            var expected = await _context.Suppliers.FindAsync(supId);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/" + supId + "/basic");
            var content = await result.Content.ReadAsStringAsync();
            var retrieved = JsonConvert.DeserializeObject(content, typeof(Supplier));

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            retrieved.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSupplierBasic_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var supId = GenBadSupplierId;

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/" + supId + "/basic");

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task GetSupplyCategories_HappyFlow_ShouldReturnSupplyCategories()
        {
            // Arrange
            var supId = GetFirstSupplierId;

            var catIds = await _context.SupplierStocks
                .Where(ss => ss.SupplierId == supId)
                .Select(ss => ss.SupplyCategoryId)
                .ToArrayAsync();

            var expectedObj = await _context.SupplyCategories
                .Where(c => catIds.Contains(c.SupplyCategoryId))
                .Select(c => new { c.SupplyCategoryId, c.Name })
                .ToArrayAsync();

            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + supId + "/stocks");
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSupplyCategories_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var supId = GenBadSupplierId;

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + supId + "/stocks");
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task GetSupplierLinks_HappyFlow_ShouldReturnLinks()
        {
            // Arrange
            var supId = GetFirstSupplierId;

            var supplyLinks = await _context.SupplyLinks
                .Where(l => l.SupplierId == supId)
                .ToArrayAsync();

            var catIds = new List<int>();
            var locIds = new List<int>();

            foreach (var link in supplyLinks)
            {
                catIds.Add(link.SupplyCategoryId);
                locIds.Add(link.LocationId);
            };

            catIds = catIds.Distinct().ToList();
            locIds = locIds.Distinct().ToList();

            var cats = await _context.SupplyCategories
                .Select(c => new
                {
                    c.SupplyCategoryId,
                    c.Name
                })
                .Where(c => catIds.Contains(c.SupplyCategoryId))
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
                var cat = cats.FirstOrDefault(c => c.SupplyCategoryId == supplyLinks[i].SupplyCategoryId);
                var loc = locations.FirstOrDefault(l => l.LocationId == supplyLinks[i].LocationId);

                expected[i] = string.Format("(Supply Link: {0}): ({1}) {2} " +
                    "supplied to ({3}) {4}, {5}{6}, {7}.",
                    i,
                    cat.SupplyCategoryId,
                    cat.Name,
                    loc.LocationId,
                    loc.AbreviatedCountry,
                    loc.AbreviatedState,
                    loc.City,
                    loc.Street);
            }

            // Act
            var result = await _client.GetAsync(_client.BaseAddress + "/" + supId + "/links");
            var cont = await result.Content.ReadAsStringAsync();
            var links = JsonConvert.DeserializeObject(cont, typeof(IEnumerable<string>)) as IEnumerable<string>;

            // Assert
            links.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSupplierLinks_InexistentId_ShouldReturnNotFound()
        {
            // Act
            var result = await _client.GetAsync(_client.BaseAddress + "/" + GenBadSupplierId + "/links");

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        #endregion

        #region Advanced

        [TestMethod]
        public async Task GetSuppliersFiltered_NameMatch_ShouldReturnFiltered()
        {
            // Arrange
            var filter = await _context.Suppliers
                .Select(s => s.Name)
                .FirstOrDefaultAsync();

            var expectedObj = await _context.Suppliers
                 .Select(s => new
                 {
                     s.SupplierId,
                     s.Name,
                     s.AbreviatedCountry,
                     s.AbreviatedState,
                     s.Country,
                     s.State,
                     s.City
                 })
                 .Where(s => s.Name.ToUpper()
                     .Contains(filter.ToUpper()))
                 .ToArrayAsync();

            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/query:filter=" + filter);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSuppliersFiltered_AbrCountryMatch_ShouldReturnFiltered()
        {
            // Arrange
            var filter = await _context.Suppliers
                .Select(s => s.AbreviatedCountry)
                .FirstOrDefaultAsync(s=>s!="N/A");

            var expectedObj = await _context.Suppliers
                 .Select(s => new
                 {
                     s.SupplierId,
                     s.Name,
                     s.AbreviatedCountry,
                     s.AbreviatedState,
                     s.Country,
                     s.State,
                     s.City
                 })
                 .Where(s => s.Name.ToUpper()
                         .Contains(filter.ToUpper()) ||
                        s.AbreviatedCountry.ToUpper()
                            .Contains(filter.ToUpper()) ||
                        s.AbreviatedState.ToUpper()
                            .Contains(filter.ToUpper()) ||
                        s.Country.ToUpper()
                            .Contains(filter.ToUpper()) ||
                        s.State.ToUpper()
                            .Contains(filter.ToUpper()) ||
                        s.City.ToUpper()
                            .Contains(filter.ToUpper()))
                 .ToArrayAsync();

            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/query:filter=" + filter);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSuppliersFiltered_AbrStateMatch_ShouldReturnFiltered()
        {
            // Arrange
            var filter = await _context.Suppliers
                .Select(s => s.AbreviatedState)
                .FirstOrDefaultAsync(s => s != "N/A");

            var expectedObj = await _context.Suppliers
                 .Select(s => new
                 {
                     s.SupplierId,
                     s.Name,
                     s.AbreviatedCountry,
                     s.AbreviatedState,
                     s.Country,
                     s.State,
                     s.City
                 })
                 .Where(s => s.Name.ToUpper()
                         .Contains(filter.ToUpper()) ||
                        s.AbreviatedCountry.ToUpper()
                            .Contains(filter.ToUpper()) ||
                        s.AbreviatedState.ToUpper()
                            .Contains(filter.ToUpper()) ||
                        s.Country.ToUpper()
                            .Contains(filter.ToUpper()) ||
                        s.State.ToUpper()
                            .Contains(filter.ToUpper()) ||
                        s.City.ToUpper()
                            .Contains(filter.ToUpper()))
                 .ToArrayAsync();

            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/query:filter=" + filter);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSuppliersFiltered_CountryMatch_ShouldReturnFiltered()
        {
            // Arrange
            var filter = await _context.Suppliers
                .Select(s => s.Country)
                .FirstOrDefaultAsync();

            var expectedObj = await _context.Suppliers
                 .Select(s => new
                 {
                     s.SupplierId,
                     s.Name,
                     s.AbreviatedCountry,
                     s.AbreviatedState,
                     s.Country,
                     s.State,
                     s.City
                 })
                 .Where(s => s.Country.ToUpper()
                            .Contains(filter.ToUpper()))
                 .ToArrayAsync();

            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/query:filter=" + filter);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSuppliersFiltered_StateMatch_ShouldReturnFiltered()
        {
            // Arrange
            var filter = await _context.Suppliers
                .Select(s => s.State)
                .FirstOrDefaultAsync(s=>s!="N/A");

            var expectedObj = await _context.Suppliers
                 .Select(s => new
                 {
                     s.SupplierId,
                     s.Name,
                     s.AbreviatedCountry,
                     s.AbreviatedState,
                     s.Country,
                     s.State,
                     s.City
                 })
                 .Where(s => s.State.ToUpper()
                            .Contains(filter.ToUpper()))
                 .ToArrayAsync();

            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/query:filter=" + filter);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSuppliersFiltered_CityMatch_ShouldReturnFiltered()
        {
            // Arrange
            var filter = await _context.Suppliers
                .Select(s => s.City)
                .FirstOrDefaultAsync();

            var expectedObj = await _context.Suppliers
                 .Select(s => new
                 {
                     s.SupplierId,
                     s.Name,
                     s.AbreviatedCountry,
                     s.AbreviatedState,
                     s.Country,
                     s.State,
                     s.City
                 })
                 .Where(s => s.City.ToUpper()
                            .Contains(filter.ToUpper()))
                 .ToArrayAsync();

            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/query:filter=" + filter);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSuppliersPaged_HapyFlow_ShouldReturnPaged()
        {
            // Arrange
            var pgSz = 2;
            var pgInd = 1;

            var expectedObj = await _context.Suppliers
                .Select(s => new
                {
                    s.SupplierId,
                    s.Name,
                    s.AbreviatedCountry,
                    s.AbreviatedState,
                    s.Country,
                    s.State,
                    s.City
                })
                .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArrayAsync();

            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/query:pgSz=" + pgSz + "&pgInd=" + pgInd);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSuppliersPaged_EndPageIncomplete_ShouldReturnPaged()
        {
            // Arrange
            var supplierCount = await _context.Suppliers.CountAsync();
            var pgSz = 1;

            while (true)
            {
                if (supplierCount % pgSz == 0) pgSz++;
                else break;
            }

            var pgInd = supplierCount / (pgSz - 1);

            var expectedObj = await _context.Suppliers
                .Select(s => new
                {
                    s.SupplierId,
                    s.Name,
                    s.AbreviatedCountry,
                    s.AbreviatedState,
                    s.Country,
                    s.State,
                    s.City
                })
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
        public async Task GetSuppliersPaged_SizeOver_ShouldReturnPaged()
        {
            // Arrange
            var supplierCount = await _context.Suppliers.CountAsync();
            var pgSz = supplierCount + 1;
            var pgInd = 1;

            var expectedObj = await _context.Suppliers
                .Select(s => new
                {
                    s.SupplierId,
                    s.Name,
                    s.AbreviatedCountry,
                    s.AbreviatedState,
                    s.Country,
                    s.State,
                    s.City
                })
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
        public async Task GetSuppliersPaged_IndexOver_ShouldReturnPaged()
        {
            // Arrange
            var supplierCount = await _context.Suppliers.CountAsync();
            var pgInd = supplierCount + 1;
            var pgSz = 1;

            var expectedObj = await _context.Suppliers
                .Select(s => new
                {
                    s.SupplierId,
                    s.Name,
                    s.AbreviatedCountry,
                    s.AbreviatedState,
                    s.Country,
                    s.State,
                    s.City
                })
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
        public async Task GetSuppliersPaged_HapyFlow_ShouldReturnPaged(int pgSz, int pgInd)
        {
            // Arrange
            var absPgSz = Math.Abs(pgSz);
            var absPgInd = Math.Abs(pgInd);

            var expectedObj = await _context.Suppliers
                .Select(s => new
                {
                    s.SupplierId,
                    s.Name,
                    s.AbreviatedCountry,
                    s.AbreviatedState,
                    s.Country,
                    s.State,
                    s.City
                })
                 .Skip(absPgSz * (absPgInd - 1))
                .Take(absPgSz)
                .ToArrayAsync();

            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + 
                "/query:pgSz=" + pgSz + "&pgInd=" + pgInd);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSuppliersSorted_ByName_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "name";

            var expectedObj = await _context.Suppliers
                            .Select(s => new
                            {
                                s.SupplierId,
                                s.Name,
                                s.AbreviatedCountry,
                                s.AbreviatedState,
                                s.Country,
                                s.State,
                                s.City
                            })
                            .OrderBy(s => s.Name)
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
        public async Task GetSuppliersSorted_ByCountry_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "country";

            var expectedObj = await _context.Suppliers
                            .Select(s => new
                            {
                                s.SupplierId,
                                s.Name,
                                s.AbreviatedCountry,
                                s.AbreviatedState,
                                s.Country,
                                s.State,
                                s.City
                            })
                            .OrderBy(s => s.AbreviatedCountry)
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
        public async Task GetSuppliersSorted_ByState_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "state";

            var expectedObj = await _context.Suppliers
                            .Select(s => new
                            {
                                s.SupplierId,
                                s.Name,
                                s.AbreviatedCountry,
                                s.AbreviatedState,
                                s.Country,
                                s.State,
                                s.City
                            })
                            .OrderBy(s => s.AbreviatedState)
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
        public async Task GetSuppliersSorted_ByCity_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "city";

            var expectedObj = await _context.Suppliers
                            .Select(s => new
                            {
                                s.SupplierId,
                                s.Name,
                                s.AbreviatedCountry,
                                s.AbreviatedState,
                                s.Country,
                                s.State,
                                s.City
                            })
                            .OrderBy(s => s.City)
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
        public async Task GetSuppliersSorted_GarbleText_ShouldReturnSortedByID()
        {
            // Arrange
            var sortBy = "frgfc";

            var expectedObj = await _context.Suppliers
                            .Select(s => new
                            {
                                s.SupplierId,
                                s.Name,
                                s.AbreviatedCountry,
                                s.AbreviatedState,
                                s.Country,
                                s.State,
                                s.City
                            })
                            .OrderBy(s => s.SupplierId)
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
        public async Task GetSuppliersSorted_Desc_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "name";

            var expectedObj = await _context.Suppliers
                            .Select(s => new
                            {
                                s.SupplierId,
                                s.Name,
                                s.AbreviatedCountry,
                                s.AbreviatedState,
                                s.Country,
                                s.State,
                                s.City
                            })
                            .OrderByDescending(s => s.Name)
                            .ToArrayAsync();
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/query:desc=true&sortby=" + sortBy);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSuppliersQueried_FilteredAndSorted_ShouldReturnFilteredAndSorted()
        {
            // Arrange
            var sortBy = "name";
            var filter = "FR";

            var filtered = await _context.Suppliers
                     .Select(s => new
                     {
                         s.SupplierId,
                         s.Name,
                         s.AbreviatedCountry,
                         s.AbreviatedState,
                         s.Country,
                         s.State,
                         s.City
                     })
                     .Where(s => s.Name.ToUpper()
                         .Contains(filter.ToUpper()) ||
                        s.AbreviatedCountry.ToUpper()
                            .Contains(filter.ToUpper()) ||
                        s.AbreviatedState.ToUpper()
                            .Contains(filter.ToUpper()) ||
                        s.Country.ToUpper()
                            .Contains(filter.ToUpper()) ||
                        s.State.ToUpper()
                            .Contains(filter.ToUpper()) ||
                        s.City.ToUpper()
                            .Contains(filter.ToUpper()))
                     .ToArrayAsync();

            var expectedObj = filtered
                            .Select(s => new
                            {
                                s.SupplierId,
                                s.Name,
                                s.AbreviatedCountry,
                                s.AbreviatedState,
                                s.Country,
                                s.State,
                                s.City
                            })
                            .OrderByDescending(s => s.Name)
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
        public async Task GetSuppliersQueried_FilteredAndPaged_ShouldReturnFilteredAndPaged()
        {
            // Arrange
            int pgSz = 2;
            int pgInd = 1;
            var filter = "FR";

            var filtered = await _context.Suppliers
                     .Select(s => new
                     {
                         s.SupplierId,
                         s.Name,
                         s.AbreviatedCountry,
                         s.AbreviatedState,
                         s.Country,
                         s.State,
                         s.City
                     })
                     .Where(s => s.Name.ToUpper()
                         .Contains(filter.ToUpper()) ||
                        s.AbreviatedCountry.ToUpper()
                            .Contains(filter.ToUpper()) ||
                        s.AbreviatedState.ToUpper()
                            .Contains(filter.ToUpper()) ||
                        s.Country.ToUpper()
                            .Contains(filter.ToUpper()) ||
                        s.State.ToUpper()
                            .Contains(filter.ToUpper()) ||
                        s.City.ToUpper()
                            .Contains(filter.ToUpper()))
                     .ToArrayAsync();

            var expectedObj = filtered
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
        public async Task GetSuppliersQueried_SortedAndPaged_ShouldReturnSortedAndPaged()
        {
            // Arrange
            int pgSz = 2;
            int pgInd = 1;
            var sortBy = "name";

            var sorted = await _context.Suppliers
                            .Select(s => new
                            {
                                s.SupplierId,
                                s.Name,
                                s.AbreviatedCountry,
                                s.AbreviatedState,
                                s.Country,
                                s.State,
                                s.City
                            })
                            .OrderByDescending(s => s.Name)
                            .ToArrayAsync();

            var expectedObj = sorted
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
        public async Task GetSuppliersQueried_FilteredSortedAndPaged_ShouldReturnFilteredSortedAndPaged()
        {
            // Arrange
            int pgSz = 2;
            int pgInd = 1;
            var sortBy = "name";
            var filter = "FR";

            var filtered = await _context.Suppliers
                     .Select(s => new
                     {
                         s.SupplierId,
                         s.Name,
                         s.AbreviatedCountry,
                         s.AbreviatedState,
                         s.Country,
                         s.State,
                         s.City
                     })
                     .Where(s => s.Name.ToUpper()
                         .Contains(filter.ToUpper()) ||
                        s.AbreviatedCountry.ToUpper()
                            .Contains(filter.ToUpper()) ||
                        s.AbreviatedState.ToUpper()
                            .Contains(filter.ToUpper()) ||
                        s.Country.ToUpper()
                            .Contains(filter.ToUpper()) ||
                        s.State.ToUpper()
                            .Contains(filter.ToUpper()) ||
                        s.City.ToUpper()
                            .Contains(filter.ToUpper()))
                     .ToArrayAsync();

            var sorted = filtered
                            .OrderByDescending(s => s.Name)
                            .ToArray();

            var expectedObj = sorted
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
        public async Task UpdateSupplier_HappyFlow_ShouldUpdateAndReturnSupplier()
        {
            // Arrange
            var supId = GetFirstSupplierId;

            /* Save a copy of the original entry to compare against*/
            var oldSupplier = await _context.Suppliers.FindAsync(supId);
            var oldSupplierCopy = new Supplier
            {
                SupplierId = oldSupplier.SupplierId,
                Name = oldSupplier.Name,
                State=oldSupplier.State,
                SupplierStocks=oldSupplier.SupplierStocks,
                SupplyLinks=oldSupplier.SupplyLinks,
                AbreviatedState=oldSupplier.AbreviatedState,
                AbreviatedCountry=oldSupplier.AbreviatedCountry,
                City=oldSupplier.City,
                Country=oldSupplier.Country
            };

            var sup = new Supplier
            {
                Name = "The New Potato",
                AbreviatedCountry = "SP",
                AbreviatedState = "CN",
                City = "Palmas De Gran Canaria",
                Country = "Spain",
                State = "Canarias"
            };
            var supContent = new StringContent(JsonConvert.SerializeObject(sup),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + supId, supContent);

            /* Retrieve the (allegedly) updated sup*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + supId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newSupplier = JsonConvert.DeserializeObject(newCont, typeof(Supplier));

            /* Complete the sup we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            sup.SupplierId = oldSupplierCopy.SupplierId;
            sup.SupplierStocks = oldSupplierCopy.SupplierStocks;
            sup.SupplyLinks = oldSupplierCopy.SupplyLinks;

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            newSupplier.Should().NotBeEquivalentTo(oldSupplierCopy);
            newSupplier.Should().BeEquivalentTo(sup);

            // Clean-up
            sup = new Supplier
            {
                Name = oldSupplierCopy.Name,
                AbreviatedCountry = oldSupplierCopy.AbreviatedCountry,
                AbreviatedState = oldSupplierCopy.AbreviatedState,
                City = oldSupplierCopy.City,
                Country = oldSupplierCopy.Country,
                State = oldSupplierCopy.State
            };
            supContent = new StringContent(JsonConvert.SerializeObject(sup),
                Encoding.UTF8, "application/json");
            result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + supId, supContent);
            updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + supId + "/basic");
            newCont = await updCall.Content.ReadAsStringAsync();
            newSupplier = JsonConvert.DeserializeObject(newCont, typeof(Supplier));

            result.IsSuccessStatusCode.Should().BeTrue();
            newSupplier.Should().BeEquivalentTo(oldSupplierCopy);
        }

        [TestMethod]
        public async Task UpdateSupplier_InexistendId_ShouldReturnNotFound()
        {
            // Arrange
            var supId = GetFirstSupplierId;

            /* Save a copy of the original entry to compare against*/
            var oldSupplier = await _context.Suppliers.FindAsync(supId);
            var oldSupplierCopy = new Supplier
            {
                SupplierId = oldSupplier.SupplierId,
                Name = oldSupplier.Name,
                State = oldSupplier.State,
                SupplierStocks = oldSupplier.SupplierStocks,
                SupplyLinks = oldSupplier.SupplyLinks,
                AbreviatedState = oldSupplier.AbreviatedState,
                AbreviatedCountry = oldSupplier.AbreviatedCountry,
                City = oldSupplier.City,
                Country = oldSupplier.Country
            };

            var sup = new Supplier
            {
                Name = "The New Potato",
                AbreviatedCountry = "SP",
                AbreviatedState = "CN",
                City = "Palmas De Gran Canaria",
                Country = "Spain",
                State = "Canarias"
            };
            var supContent = new StringContent(JsonConvert.SerializeObject(sup),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + 
                GenBadSupplierId, supContent);

            /* Retrieve the (allegedly) updated sup*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + supId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newSupplier = JsonConvert.DeserializeObject(newCont, typeof(Supplier));

            /* Complete the sup we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            sup.SupplierId = oldSupplierCopy.SupplierId;
            sup.SupplierStocks = oldSupplierCopy.SupplierStocks;
            sup.SupplyLinks = oldSupplierCopy.SupplyLinks;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
            newSupplier.Should().BeEquivalentTo(oldSupplierCopy);
            newSupplier.Should().NotBeEquivalentTo(sup);
        }

        [TestMethod]
        public async Task UpdateSupplier_TriesToSetId_ShouldReturnBadRequest()
        {
            // Arrange
            var supId = GetFirstSupplierId;

            /* Save a copy of the original entry to compare against*/
            var oldSupplier = await _context.Suppliers.FindAsync(supId);
            var oldSupplierCopy = new Supplier
            {
                SupplierId = oldSupplier.SupplierId,
                Name = oldSupplier.Name,
                State = oldSupplier.State,
                SupplierStocks = oldSupplier.SupplierStocks,
                SupplyLinks = oldSupplier.SupplyLinks,
                AbreviatedState = oldSupplier.AbreviatedState,
                AbreviatedCountry = oldSupplier.AbreviatedCountry,
                City = oldSupplier.City,
                Country = oldSupplier.Country
            };

            var sup = new Supplier
            {
                Name = "The New Potato",
                AbreviatedCountry = "SP",
                AbreviatedState = "CN",
                City = "Palmas De Gran Canaria",
                Country = "Spain",
                State = "Canarias",
                SupplierId = GenBadSupplierId
            };
            var supContent = new StringContent(JsonConvert.SerializeObject(sup),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + supId, supContent);

            /* Retrieve the (allegedly) updated sup*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + supId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newSupplier = JsonConvert.DeserializeObject(newCont, typeof(Supplier));

            /* Complete the sup we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            sup.SupplierId = oldSupplierCopy.SupplierId;
            sup.SupplierStocks = oldSupplierCopy.SupplierStocks;
            sup.SupplyLinks = oldSupplierCopy.SupplyLinks;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newSupplier.Should().BeEquivalentTo(oldSupplierCopy);
            newSupplier.Should().NotBeEquivalentTo(sup);
        }

        [TestMethod]
        #region Rows
        [DataRow(" ", "FR", "B7", "Niort", "France", "Poitou-Charentes")]
        [DataRow("Mertz and Sons", " ", "B7", "Niort", "France", "Poitou-Charentes")]
        [DataRow("Mertz and Sons", "FR", " ", "Niort", "France", "Poitou-Charentes")]
        [DataRow("Mertz and Sons", "FR", "B7", " ", "France", "Poitou-Charentes")]
        [DataRow("Mertz and Sons", "FR", "B7", "Niort", " ", "Poitou-Charentes")]
        [DataRow("Mertz and Sons", "FR", "B7", "Niort", "France", " ")]
        [DataRow("012345678912345678911234567892123456789312345678941",
            "FR", "B7", "Niort", "France", "Poitou-Charentes")]
        [DataRow("Mertz and Sons", "FR345", "B7", "Niort", "France", "Poitou-Charentes")]
        [DataRow("Mertz and Sons", "FR", "B7345", "Niort", "France", "Poitou-Charentes")]
        [DataRow("Mertz and Sons", "FR", "B7",
            "012345678912345678911234567892123456789312345678941", "France", "Poitou-Charentes")]
        [DataRow("Mertz and Sons", "FR", "B7", "Niort",
            "012345678912345678911234567892123456789312345678941", "Poitou-Charentes")]
        [DataRow("Mertz and Sons", "FR", "B7", "Niort", "France",
            "012345678912345678911234567892123456789312345678941")]
        #endregion
        public async Task UpdateSupplier_BadAddresses_ShouldReturnBadRequest
            (string name, string abrCountry, string abrState, string city, string country, string state)
        {
            // Arrange
            var supId = GetFirstSupplierId;

            /* Save a copy of the original entry to compare against*/
            var oldSupplier = await _context.Suppliers.FindAsync(supId);
            var oldSupplierCopy = new Supplier
            {
                SupplierId = oldSupplier.SupplierId,
                Name = oldSupplier.Name,
                State = oldSupplier.State,
                SupplierStocks = oldSupplier.SupplierStocks,
                SupplyLinks = oldSupplier.SupplyLinks,
                AbreviatedState = oldSupplier.AbreviatedState,
                AbreviatedCountry = oldSupplier.AbreviatedCountry,
                City = oldSupplier.City,
                Country = oldSupplier.Country
            };

            var sup = new Supplier
            {
                Name = name,
                AbreviatedCountry = abrCountry,
                AbreviatedState = abrState,
                City = city,
                Country = country,
                State = state,
            };
            var supContent = new StringContent(JsonConvert.SerializeObject(sup),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + supId, supContent);

            /* Retrieve the (allegedly) updated sup*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + supId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newSupplier = JsonConvert.DeserializeObject(newCont, typeof(Supplier));

            /* Complete the sup we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            sup.SupplierId = oldSupplierCopy.SupplierId;
            sup.SupplierStocks = oldSupplierCopy.SupplierStocks;
            sup.SupplyLinks = oldSupplierCopy.SupplyLinks;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newSupplier.Should().BeEquivalentTo(oldSupplierCopy);
            newSupplier.Should().NotBeEquivalentTo(sup);
        }

        [TestMethod]
        public async Task UpdateSupplier_DuplicateName_ShouldReturnBadRequest()
        {
            // Arrange
            var supId = GetFirstSupplierId;

            var dupe = (await _context.Suppliers
                .Select(s => new { s.SupplierId, s.Name })
                .FirstOrDefaultAsync(s => s.SupplierId != supId))
                .Name;

            /* Save a copy of the original entry to compare against*/
            var oldSupplier = await _context.Suppliers.FindAsync(supId);
            var oldSupplierCopy = new Supplier
            {
                SupplierId = oldSupplier.SupplierId,
                Name = oldSupplier.Name,
                State = oldSupplier.State,
                SupplierStocks = oldSupplier.SupplierStocks,
                SupplyLinks = oldSupplier.SupplyLinks,
                AbreviatedState = oldSupplier.AbreviatedState,
                AbreviatedCountry = oldSupplier.AbreviatedCountry,
                City = oldSupplier.City,
                Country = oldSupplier.Country
            };

            var sup = new Supplier
            {
                AbreviatedCountry = "SP",
                AbreviatedState = "CN",
                City = "Palmas De Gran Canaria",
                Country = "Spain",
                State = "Canarias",
                Name = dupe
            };
            var supContent = new StringContent(JsonConvert.SerializeObject(sup),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + supId, supContent);

            /* Retrieve the (allegedly) updated sup*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + supId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newSupplier = JsonConvert.DeserializeObject(newCont, typeof(Supplier));

            /* Complete the sup we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            sup.SupplierId = oldSupplierCopy.SupplierId;
            sup.SupplierStocks = oldSupplierCopy.SupplierStocks;
            sup.SupplyLinks = oldSupplierCopy.SupplyLinks;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newSupplier.Should().BeEquivalentTo(oldSupplierCopy);
            newSupplier.Should().NotBeEquivalentTo(sup);
        }

        [TestMethod]
        public async Task UpdateSupplier_SupplierStocksNull_ShouldReturnBadRequest()
        {
            // Arrange
            var supId = GetFirstSupplierId;

            /* Save a copy of the original entry to compare against*/
            var oldSupplier = await _context.Suppliers.FindAsync(supId);
            var oldSupplierCopy = new Supplier
            {
                SupplierId = oldSupplier.SupplierId,
                Name = oldSupplier.Name,
                State = oldSupplier.State,
                SupplierStocks = oldSupplier.SupplierStocks,
                SupplyLinks = oldSupplier.SupplyLinks,
                AbreviatedState = oldSupplier.AbreviatedState,
                AbreviatedCountry = oldSupplier.AbreviatedCountry,
                City = oldSupplier.City,
                Country = oldSupplier.Country
            };

            var sup = new Supplier
            {
                Name = "The New Potato",
                AbreviatedCountry = "SP",
                AbreviatedState = "CN",
                City = "Palmas De Gran Canaria",
                Country = "Spain",
                State = "Canarias",
                SupplierStocks = null
            };
            var supContent = new StringContent(JsonConvert.SerializeObject(sup),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + supId, supContent);

            /* Retrieve the (allegedly) updated sup*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + supId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newSupplier = JsonConvert.DeserializeObject(newCont, typeof(Supplier));

            /* Complete the sup we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            sup.SupplierId = oldSupplierCopy.SupplierId;
            sup.SupplierStocks = oldSupplierCopy.SupplierStocks;
            sup.SupplyLinks = oldSupplierCopy.SupplyLinks;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newSupplier.Should().BeEquivalentTo(oldSupplierCopy);
            newSupplier.Should().NotBeEquivalentTo(sup);
        }

        [TestMethod]
        public async Task UpdateSupplier_SupplierStocksNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var supId = GetFirstSupplierId;

            /* Save a copy of the original entry to compare against*/
            var oldSupplier = await _context.Suppliers.FindAsync(supId);
            var oldSupplierCopy = new Supplier
            {
                SupplierId = oldSupplier.SupplierId,
                Name = oldSupplier.Name,
                State = oldSupplier.State,
                SupplierStocks = oldSupplier.SupplierStocks,
                SupplyLinks = oldSupplier.SupplyLinks,
                AbreviatedState = oldSupplier.AbreviatedState,
                AbreviatedCountry = oldSupplier.AbreviatedCountry,
                City = oldSupplier.City,
                Country = oldSupplier.Country
            };

            var sup = new Supplier
            {
                Name = "The New Potato",
                AbreviatedCountry = "SP",
                AbreviatedState = "CN",
                City = "Palmas De Gran Canaria",
                Country = "Spain",
                State = "Canarias",
                SupplierStocks = new SupplierStock[] {new SupplierStock()}
            };
            var supContent = new StringContent(JsonConvert.SerializeObject(sup),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + supId, supContent);

            /* Retrieve the (allegedly) updated sup*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + supId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newSupplier = JsonConvert.DeserializeObject(newCont, typeof(Supplier));

            /* Complete the sup we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            sup.SupplierId = oldSupplierCopy.SupplierId;
            sup.SupplierStocks = oldSupplierCopy.SupplierStocks;
            sup.SupplyLinks = oldSupplierCopy.SupplyLinks;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newSupplier.Should().BeEquivalentTo(oldSupplierCopy);
            newSupplier.Should().NotBeEquivalentTo(sup);
        }

        [TestMethod]
        public async Task UpdateSupplier_SuppllyLinksNull_ShouldReturnBadRequest()
        {
            // Arrange
            var supId = GetFirstSupplierId;

            /* Save a copy of the original entry to compare against*/
            var oldSupplier = await _context.Suppliers.FindAsync(supId);
            var oldSupplierCopy = new Supplier
            {
                SupplierId = oldSupplier.SupplierId,
                Name = oldSupplier.Name,
                State = oldSupplier.State,
                SupplierStocks = oldSupplier.SupplierStocks,
                SupplyLinks = oldSupplier.SupplyLinks,
                AbreviatedState = oldSupplier.AbreviatedState,
                AbreviatedCountry = oldSupplier.AbreviatedCountry,
                City = oldSupplier.City,
                Country = oldSupplier.Country
            };

            var sup = new Supplier
            {
                Name = "The New Potato",
                AbreviatedCountry = "SP",
                AbreviatedState = "CN",
                City = "Palmas De Gran Canaria",
                Country = "Spain",
                State = "Canarias",
                SupplyLinks = null
            };
            var supContent = new StringContent(JsonConvert.SerializeObject(sup),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + supId, supContent);

            /* Retrieve the (allegedly) updated sup*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + supId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newSupplier = JsonConvert.DeserializeObject(newCont, typeof(Supplier));

            /* Complete the sup we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            sup.SupplierId = oldSupplierCopy.SupplierId;
            sup.SupplierStocks = oldSupplierCopy.SupplierStocks;
            sup.SupplyLinks = oldSupplierCopy.SupplyLinks;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newSupplier.Should().BeEquivalentTo(oldSupplierCopy);
            newSupplier.Should().NotBeEquivalentTo(sup);
        }

        [TestMethod]
        public async Task UpdateSupplier_SuppllyLinksNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var supId = GetFirstSupplierId;

            /* Save a copy of the original entry to compare against*/
            var oldSupplier = await _context.Suppliers.FindAsync(supId);
            var oldSupplierCopy = new Supplier
            {
                SupplierId = oldSupplier.SupplierId,
                Name = oldSupplier.Name,
                State = oldSupplier.State,
                SupplierStocks = oldSupplier.SupplierStocks,
                SupplyLinks = oldSupplier.SupplyLinks,
                AbreviatedState = oldSupplier.AbreviatedState,
                AbreviatedCountry = oldSupplier.AbreviatedCountry,
                City = oldSupplier.City,
                Country = oldSupplier.Country
            };

            var sup = new Supplier
            {
                Name = "The New Potato",
                AbreviatedCountry = "SP",
                AbreviatedState = "CN",
                City = "Palmas De Gran Canaria",
                Country = "Spain",
                State = "Canarias",
                SupplyLinks = new SupplyLink[] {new SupplyLink()}
            };
            var supContent = new StringContent(JsonConvert.SerializeObject(sup),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + supId, supContent);

            /* Retrieve the (allegedly) updated sup*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + supId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newSupplier = JsonConvert.DeserializeObject(newCont, typeof(Supplier));

            /* Complete the sup we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            sup.SupplierId = oldSupplierCopy.SupplierId;
            sup.SupplierStocks = oldSupplierCopy.SupplierStocks;
            sup.SupplyLinks = oldSupplierCopy.SupplyLinks;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newSupplier.Should().BeEquivalentTo(oldSupplierCopy);
            newSupplier.Should().NotBeEquivalentTo(sup);
        }

        #endregion

        #region Deletes

        [TestMethod]
        public async Task DeleteSupplier_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var badId = await _context.Suppliers.CountAsync() + 1;

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + badId);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task DeleteSupplier_LastWithNoRelations_ShouldDelete()
        {
            // Arrange
            var supId = await _context.Suppliers.CountAsync() + 1;
            var sup = new Supplier 
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "XX",
                Name = "Delete Me",
                State = "None",
                City = "None",
                Country = "None"
            };
            await _context.Suppliers.AddAsync(sup);
            await _context.SaveChangesAsync();
            var delName = sup.Name;

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + supId);
            var supExists = await _context.Suppliers.AnyAsync(s => s.Name == delName);

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            supExists.Should().BeFalse();
        }

        [DataTestMethod]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public async Task DeleteSupplier_HasRelations_ShouldTurnToBlank(bool isCap, bool isFirst)
        {
            // Arrange
            var supId = 0;
            if (!isCap && !isFirst) supId = 2;
            else
            {
                if (isFirst) supId = GetFirstSupplierId;
                else supId = await _context.Suppliers.CountAsync();
            }
            var sup = await _context.Suppliers.FindAsync(supId);
            var supCopy = new Supplier
            {
                AbreviatedCountry = sup.AbreviatedCountry,
                AbreviatedState = sup.AbreviatedState,
                Country = sup.Country,
                State = sup.State,
                City = sup.City,
                Name = sup.Name
            };
            sup = null;

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + supId);
            var getRes = await _client.GetAsync(_client.BaseAddress + "/" + supId + "/basic");
            var getCont = await getRes.Content.ReadAsStringAsync();
            var getSup = JsonConvert.DeserializeObject(getCont, typeof(Supplier)) as Supplier;

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            getSup.AbreviatedCountry.Should().Be("XX");
            getSup.AbreviatedState.Should().Be("XX");
            getSup.Name.Should().Contain("Empty");
            getSup.Country.Should().Be("None");
            getSup.State.Should().Be("None");
            getSup.City.Should().Be("None");

            //Clean-up
            var fixSup = new StringContent(JsonConvert.SerializeObject(supCopy),
                Encoding.UTF8, "application/json");
            var fixRes = await _client.PutAsync(_client.BaseAddress + "/" + supId, fixSup);

            //Check-up
            fixRes.IsSuccessStatusCode.Should().BeTrue();
        }

        [TestMethod]
        public async Task DeleteSupplier_NotLastWithNoRelations_ShouldDelete()
        {
            // Arrange
            var supId = await _context.Suppliers.CountAsync() + 1;
            var delSup = new Supplier
            {
                Name = "Delete Me",
                AbreviatedCountry = "XX",
                AbreviatedState = "XX",
                Country = "None",
                State = "None",
                City = "None"
            };
            var emptySup = new Supplier
            {
                Name = "Replacement Co",
                AbreviatedCountry = "XX",
                AbreviatedState = "XX",
                Country = "None",
                State = "None",
                City = "None"
            };
            var newName = emptySup.Name;

            await _context.Suppliers.AddRangeAsync(new Supplier[] { delSup, emptySup });
            await _context.SaveChangesAsync();

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + supId);
            var getRes = await _client.GetAsync(_client.BaseAddress + "/" + supId + "/basic");
            var getCont = await getRes.Content.ReadAsStringAsync();
            var getSup = JsonConvert.DeserializeObject(getCont, typeof(Supplier)) as Supplier;

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            getSup.Name.Should().BeEquivalentTo(newName);

            // Clean-up
            await _client.DeleteAsync(_client.BaseAddress + "/" + supId);
        }

        [TestMethod]
        public async Task DeleteSupplier_NotLastNoRel_ShouldMigrateRelations()
        {
            // Arrange
            var linkId = await _context.SupplyLinks.CountAsync() + 1;
            var locId = await _context.Locations.CountAsync() + 1;
            var catId = await _context.SupplyCategories.CountAsync() + 1;
            var stockId = await _context.SupplierStocks.CountAsync() + 1;
            var supId = await _context.Suppliers.CountAsync() + 1;

            var delSup = new Supplier
            {
                Name = "Delete Me",
                AbreviatedCountry = "XX",
                AbreviatedState = "XX",
                Country = "None",
                State = "None",
                City = "None"
            };
            var swapSup = new Supplier
            {
                Name = "Swap and Co",
                AbreviatedCountry = "XX",
                AbreviatedState = "XX",
                Country = "None",
                State = "None",
                City = "None"
            };
            var loc = new Location
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "XX",
                Country = "Delete Me",
                State = "None",
                City = "None",
                Street = "None",
                ScheduleId = 1,
                MenuId = 1,
                OpenSince = new DateTime()
            };
            var cat = new SupplyCategory { Name = "Fresh Berries" };
            var link = new SupplyLink 
            {
                LocationId = locId,
                SupplyCategoryId = catId,
                SupplierId = supId + 1
            };
            var stock = new SupplierStock
            {
                SupplyCategoryId = catId,
                SupplierId = supId + 1
            };

            await _context.Suppliers.AddRangeAsync(new Supplier[] { delSup, swapSup });
            await _context.SupplyCategories.AddAsync(cat);
            await _context.Locations.AddAsync(loc);
            await _context.SaveChangesAsync();

            await _context.SupplyLinks.AddAsync(link);
            await _context.SupplierStocks.AddAsync(stock);
            await _context.SaveChangesAsync();

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + supId);

            var getLinkRes = await _client.GetAsync("http://localhost/api/supplylinks/" +
                linkId + "/basic");
            var getLinkCont = await getLinkRes.Content.ReadAsStringAsync();
            var getLink = JsonConvert.DeserializeObject(getLinkCont,
                typeof(SupplyLink)) as SupplyLink;

            var getStkRes = await _client.GetAsync("http://localhost/api/supplierstocks/" +
                stockId + "/basic");
            var getStkCont = await getStkRes.Content.ReadAsStringAsync();
            var getStock = JsonConvert.DeserializeObject(getStkCont,
                typeof(SupplierStock)) as SupplierStock;

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            getStock.SupplierId.Should().Be(supId);
            getLink.SupplierId.Should().Be(supId);

            // Clean-up
            await _client.DeleteAsync("http://localhost/api/supplierstocks/" + stockId);
            await _client.DeleteAsync("http://localhost/api/supplylinks/" + linkId);
            await _client.DeleteAsync("http://localhost/api/supplycategories/" + catId);
            await _client.DeleteAsync("http://localhost/api/locations/" + locId);
            await _client.DeleteAsync(_client.BaseAddress + "/" + supId);
        }

        #endregion

        #region Creates

        [TestMethod]
        public async Task CreateSupplier_HappyFlow_ShouldCreateAndReturnSupplier()
        {
            // Arrange
            var supId = await _context.Suppliers.CountAsync() + 1;
            var supplier = new Supplier
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "YY",
                Country = "Creatania",
                State = "Crudton",
                City = "Newtonville",
                Name = "Delete Me"
            };
            var supCont = new StringContent(JsonConvert.SerializeObject(supplier),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, supCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();

            // Clean-up
            await _client.DeleteAsync(_client.BaseAddress + "/" + supId);
        }

        [TestMethod]
        public async Task CreateSupplier_TriesToSetId_ShouldReturnBadRequest()
        {
            // Arrange
            var supId = await _context.Suppliers.CountAsync() + 1;
            var supplier = new Supplier
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "YY",
                Country = "Creatania",
                State = "Crudton",
                City = "Newtonville",
                Name = "Delete Me",
                SupplierId = supId
            };
            var supCont = new StringContent(JsonConvert.SerializeObject(supplier),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, supCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [DataTestMethod]
        #region Rows
        [DataRow("", "FR", "B7", "Niort", "France", "Poitou-Charentes")]
        [DataRow("Mertz and Sons", "", "B7", "Niort", "France", "Poitou-Charentes")]
        [DataRow("Mertz and Sons", "FR", "", "Niort", "France", "Poitou-Charentes")]
        [DataRow("Mertz and Sons", "FR", "B7", "", "France", "Poitou-Charentes")]
        [DataRow("Mertz and Sons", "FR", "B7", "Niort", "", "Poitou-Charentes")]
        [DataRow("Mertz and Sons", "FR", "B7", "Niort", "France", "")]
        [DataRow(" ", "FR", "B7", "Niort", "France", "Poitou-Charentes")]
        [DataRow("Mertz and Sons", " ", "B7", "Niort", "France", "Poitou-Charentes")]
        [DataRow("Mertz and Sons", "FR", " ", "Niort", "France", "Poitou-Charentes")]
        [DataRow("Mertz and Sons", "FR", "B7", " ", "France", "Poitou-Charentes")]
        [DataRow("Mertz and Sons", "FR", "B7", "Niort", " ", "Poitou-Charentes")]
        [DataRow("Mertz and Sons", "FR", "B7", "Niort", "France", " ")]
        [DataRow(null, "FR", "B7", "Niort", "France", "Poitou-Charentes")]
        [DataRow("Mertz and Sons", null, "B7", "Niort", "France", "Poitou-Charentes")]
        [DataRow("Mertz and Sons", "FR", null, "Niort", "France", "Poitou-Charentes")]
        [DataRow("Mertz and Sons", "FR", "B7", null, "France", "Poitou-Charentes")]
        [DataRow("Mertz and Sons", "FR", "B7", "Niort", null, "Poitou-Charentes")]
        [DataRow("Mertz and Sons", "FR", "B7", "Niort", "France", null)]
        [DataRow("012345678912345678911234567892123456789312345678941",
            "FR", "B7", "Niort", "France", "Poitou-Charentes")]
        [DataRow("Mertz and Sons", "FR345", "B7", "Niort", "France", "Poitou-Charentes")]
        [DataRow("Mertz and Sons", "FR", "B7345", "Niort", "France", "Poitou-Charentes")]
        [DataRow("Mertz and Sons", "FR", "B7",
            "012345678912345678911234567892123456789312345678941", "France", "Poitou-Charentes")]
        [DataRow("Mertz and Sons", "FR", "B7", "Niort",
            "012345678912345678911234567892123456789312345678941", "Poitou-Charentes")]
        [DataRow("Mertz and Sons", "FR", "B7", "Niort", "France",
            "012345678912345678911234567892123456789312345678941")]
        #endregion
        public async Task CreateSupplier_BadStrings_ShouldReturnBadRequest
            (string name, string abrCountry, string abrState, string city, string country, string state)
        {
            // Arrange
            var supplier = new Supplier
            {
                AbreviatedCountry = abrCountry,
                AbreviatedState = abrState,
                Country = country,
                State = state,
                City = city,
                Name = name
            };
            var supCont = new StringContent(JsonConvert.SerializeObject(supplier),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, supCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateSupplier_DuplicateName_ShouldReturnBadRequest()
        {
            // Arrange
            var dupe = await _context.Suppliers.FirstOrDefaultAsync();

            var supplier = new Supplier
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "YY",
                Country = "Creatania",
                State = "Crudton",
                City = "Newtonville",
                Name = dupe.Name
            };
            var supCont = new StringContent(JsonConvert.SerializeObject(supplier),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, supCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateSupplier_SupplierStocksNull_ShouldReturnBadRequest()
        {
            // Arrange
            var supplier = new Supplier
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "YY",
                Country = "Creatania",
                State = "Crudton",
                City = "Newtonville",
                Name = "Delete Me",
                SupplierStocks = null
            };
            var supCont = new StringContent(JsonConvert.SerializeObject(supplier),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, supCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateSupplier_SupplyLinksNull_ShouldReturnBadRequest()
        {
            // Arrange
            var supplier = new Supplier
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "YY",
                Country = "Creatania",
                State = "Crudton",
                City = "Newtonville",
                Name = "Delete Me",
                SupplyLinks = null
            };
            var supCont = new StringContent(JsonConvert.SerializeObject(supplier),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, supCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateSupplier_SupplierStocksNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var supplier = new Supplier
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "YY",
                Country = "Creatania",
                State = "Crudton",
                City = "Newtonville",
                Name = "Delete Me",
                SupplierStocks = new SupplierStock[] {new SupplierStock()}
            };
            var supCont = new StringContent(JsonConvert.SerializeObject(supplier),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, supCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateSupplier_SupplyLinks_ShouldReturnBadRequest()
        {
            // Arrange
            var supplier = new Supplier
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "YY",
                Country = "Creatania",
                State = "Crudton",
                City = "Newtonville",
                Name = "Delete Me",
                SupplyLinks = new SupplyLink[] {new SupplyLink()}
            };
            var supCont = new StringContent(JsonConvert.SerializeObject(supplier),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, supCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        #endregion

        //Helpers
        private int GenBadSupplierId => _context.Suppliers
                .AsEnumerable()
                .Select(s => s.SupplierId)
                .LastOrDefault() + 1;
        private int GetFirstSupplierId => _context.Suppliers
            .Select(s => s.SupplierId)
            .FirstOrDefault();
    }
}
