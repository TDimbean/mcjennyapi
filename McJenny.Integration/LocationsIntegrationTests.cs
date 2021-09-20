using FluentAssertions;
using McJenny.WebAPI;
using McJenny.WebAPI.Data.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace McJenny.Integration
{
    [TestClass]
    public class LocationsIntegrationTests
    {
        private static HttpClient _client;
        private static FoodChainsDbContext _context;
        private static WebApplicationFactory<Startup> _appFactory;

        [ClassInitialize]
        public static void InitClass(TestContext testContext)
        {
            _appFactory = new WebApplicationFactory<Startup>();
            _appFactory.ClientOptions.BaseAddress = new Uri("http://localhost/api/locations");
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
            _appFactory.ClientOptions.BaseAddress = new Uri("http://localhost/api/locations");
            _context = new FoodChainsDbContext();
            _client = _appFactory.CreateClient();

            //var messedDb = _context.Locations.Any(l => l.LocationId > 500);
            //if(messedDb)
            //{
            //    var fake = false;
            //    if (false) return;
            //}

            var fked = HelperFuck.MessedUpDB(_context);
            if (fked)
            {
                var fake = false;
            }
        }

        #region Gets

        #region Basic

        [TestMethod]
        public async Task GetLocations_HappyFlow_ShouldReturnLocations()
        {
            // Arrange
            var expectedObj = await _context.Locations
                .Include(l => l.Schedule)
                 .Select(l => new
                 {
                     l.LocationId,
                     l.AbreviatedCountry,
                     l.AbreviatedState,
                     l.Country,
                     l.State,
                     l.City,
                     l.Street,
                     l.MenuId,
                     OpenSince = l.OpenSince.ToShortDateString(),
                     l.Schedule.TimeTable
                 })
                .Take(20)
                .ToArrayAsync();

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
        public async Task GetLocationCheating_HappyFlow_ShouldReturnLocation()
        {
            // Arrange
            var locationId = GetFirstLocationId;
            var expectedObj = await _context.Locations
            .Include(l => l.Schedule)
            .Select(l => new
            {
                l.LocationId,
                l.AbreviatedCountry,
                l.AbreviatedState,
                l.Country,
                l.State,
                l.City,
                l.Street,
                l.Schedule.TimeTable,
                l.MenuId,
                OpenSince = l.OpenSince.ToShortDateString()
            })
            .FirstOrDefaultAsync(l => l.LocationId == locationId);
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + locationId);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetLocation_HappyFlow_ShouldReturnLocation()
        {
            // Arrange
            var locId = GetFirstLocationId;
            var expected = await _context.Locations
            .Include(l => l.Schedule)
            .Select(l => new
            {
                l.LocationId,
                l.AbreviatedCountry,
                l.AbreviatedState,
                l.Country,
                l.State,
                l.City,
                l.Street,
                l.Schedule.TimeTable,
                l.MenuId,
                OpenSince = l.OpenSince.ToShortDateString()
            })
            .FirstOrDefaultAsync(l => l.LocationId == locId);
           
            var definition = new
            {
                LocationId = 0,
                AbreviatedCountry = string.Empty,
                AbreviatedState = string.Empty,
                Country = string.Empty,
                State = string.Empty,
                City = string.Empty,
                Street = string.Empty,
                TimeTable = string.Empty,
                MenuId = 0,
                OpenSince = string.Empty
            };

            // Act
            var result = await _client.GetAsync(_client.BaseAddress + "/" + locId);
            var content = await result.Content.ReadAsStringAsync();
            var loc = JsonConvert.DeserializeAnonymousType(content, definition);

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            loc.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetLocation_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var locationId = GenBadLocationId;

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + locationId);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task GetLocationBasic_HappyFlow_ShouldReturnLocation()
        {
            // Arrange
            var locId = GetFirstLocationId;

            var expected = await _context.Locations.FindAsync(locId);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/" + locId + "/basic");
            var content = await result.Content.ReadAsStringAsync();
            var retrieved = JsonConvert.DeserializeObject(content, typeof(Location));

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            retrieved.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetLocationBasic_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var locId = GenBadLocationId;

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/" + locId + "/basic");

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task GetEmployees_HappyFlow_ShouldReturnEmployees()
        {
            // Arrange
            var locationId = GetFirstLocationId;
            var expectedObj = await _context.Employees
                .Include(e => e.Position)
                .Select(e => new
                {
                    e.LocationId,
                    e.EmployeeId,
                    e.FirstName,
                    e.LastName,
                    e.Position.Title
                })
                .Where(e => e.LocationId == locationId)
                .Select(e => new
                {
                    e.EmployeeId,
                    e.FirstName,
                    e.LastName,
                    e.Title
                })
                .ToArrayAsync();

            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(_client.BaseAddress + "/" + locationId + "/employees");
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSuppliers_HappyFlow_ShouldReturnSuppliers()
        {
            // Arrange
            var locationId = GetFirstLocationId;

            var supplierIds = await _context.SupplyLinks
                .Where(l => l.LocationId == locationId)
                .Select(l => l.SupplierId)
                .ToListAsync();

            var expectedObj = await _context.Suppliers
                .Where(s => supplierIds.Contains(s.SupplierId))
                .Select(s => new
                {
                    s.SupplierId,
                    s.Name,
                    s.AbreviatedCountry,
                    s.AbreviatedState,
                    s.City
                })
                .ToArrayAsync();

            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(_client.BaseAddress + "/" + locationId + "/suppliers");
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetLinks_HappyFlow_ShouldReturnLinks()
        {
            // Arrange
            var locationId = GetFirstLocationId;
            var links = await _context.SupplyLinks
                .Where(sl => sl.LocationId == locationId)
                .Select(sl => new { sl.SupplierId, sl.SupplyCategoryId })
                .ToArrayAsync();

            var categories = await _context.SupplyCategories
                .Select(c => new
                {
                    c.SupplyCategoryId,
                    c.Name
                })
                .ToArrayAsync();

            var supplierIds = new List<int>();
            foreach (var link in links)
                if (!supplierIds.Contains(link.SupplierId))
                    supplierIds.Add(link.SupplierId);

            var suppliers = await _context.Suppliers
                .Where(s => supplierIds.Contains(s.SupplierId))
                .Select(s => new
                {
                    s.SupplierId,
                    s.Name,
                    s.AbreviatedCountry,
                    s.AbreviatedState,
                    s.City
                })
                .ToArrayAsync();

            var expectedObj = new string[links.Length];
            for (int i = 0; i < expectedObj.Length; i++)
            {
                var supplier = suppliers.SingleOrDefault(s => s.SupplierId == links[i].SupplierId);

                expectedObj[i] = string.Format("({0}) {1} supplied by {2}, {3}, {4}{5} ({6})",
                    links[i].SupplyCategoryId,
                     categories.SingleOrDefault(c => c.SupplyCategoryId == links[i].SupplyCategoryId).Name,
                     supplier.Name, supplier.AbreviatedCountry,
                     supplier.AbreviatedState == "N/A" ? string.Empty : supplier.AbreviatedState + ", ",
                     supplier.City, links[i].SupplierId
                    );
            }

            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(_client.BaseAddress + "/" + locationId + "/links");
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetItems_HappyFlow_ShouldReturnItems()
        {
            // Arrange
            var locationId = GetFirstLocationId;
            var menuId = (await _context.Locations
                .Select(l => new { l.LocationId, l.MenuId })
                .FirstOrDefaultAsync(l => l.LocationId == locationId))
                .MenuId;

            var dishIds = await _context.MenuItems
                .Where(i => i.MenuId == menuId).Select(i => i.DishId).ToArrayAsync();

            var expectedObj = await _context.Dishes
                .Where(l => dishIds
                    .Contains(l.DishId))
                .Select(l => l.Name)
                .ToArrayAsync();

            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(_client.BaseAddress + "/" + locationId + "/menu");
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }


        #endregion

        #region Advanced

        [TestMethod]
        public async Task GetLocationsFiltered_AbrCountryMatch_ShouldReturnFiltered()
        {
            // Arrange
            var filter = await _context.Locations
                .Select(l => l.AbreviatedCountry)
                .FirstOrDefaultAsync();

            var expectedObj = await _context.Locations
                 .Include(l => l.Schedule)
                 .Select(l => new
                 {
                     l.LocationId,
                     l.AbreviatedCountry,
                     l.AbreviatedState,
                     l.Country,
                     l.State,
                     l.City,
                     l.Street,
                     l.MenuId,
                     OpenSince = l.OpenSince.ToShortDateString(),
                     l.Schedule.TimeTable
                 })
                 .Where(l =>
                    l.AbreviatedCountry.ToUpper()
                        .Contains(filter.ToUpper()) ||
                    l.AbreviatedState.ToUpper()
                        .Contains(filter.ToUpper()) ||
                    l.Country.ToUpper()
                        .Contains(filter.ToUpper()) ||
                    l.State.ToUpper()
                        .Contains(filter.ToUpper()) ||
                    l.City.ToUpper()
                        .Contains(filter.ToUpper()) ||
                    l.Street.ToUpper()
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
        public async Task GetLocationsFiltered_AbrStateMatch_ShouldReturnFiltered()
        {
            // Arrange
            var filter = await _context.Locations
                .Select(l => l.AbreviatedState)
                .Where(l=>l!="N/A")
                .FirstOrDefaultAsync();

            var expectedObj = await _context.Locations
                 .Include(l => l.Schedule)
                 .Select(l => new
                 {
                     l.LocationId,
                     l.AbreviatedCountry,
                     l.AbreviatedState,
                     l.Country,
                     l.State,
                     l.City,
                     l.Street,
                     l.MenuId,
                     OpenSince = l.OpenSince.ToShortDateString(),
                     l.Schedule.TimeTable
                 })
                 .Where(l =>
                    l.AbreviatedCountry.ToUpper()
                        .Contains(filter.ToUpper()) ||
                    l.AbreviatedState.ToUpper()
                        .Contains(filter.ToUpper()) ||
                    l.Country.ToUpper()
                        .Contains(filter.ToUpper()) ||
                    l.State.ToUpper()
                        .Contains(filter.ToUpper()) ||
                    l.City.ToUpper()
                        .Contains(filter.ToUpper()) ||
                    l.Street.ToUpper()
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
        public async Task GetLocationsFiltered_CountryMatch_ShouldReturnFiltered()
        {
            // Arrange
            var filter = await _context.Locations
                .Select(l => l.Country)
                .FirstOrDefaultAsync();

            var expectedObj = await _context.Locations
                 .Include(l => l.Schedule)
                 .Select(l => new
                 {
                     l.LocationId,
                     l.AbreviatedCountry,
                     l.AbreviatedState,
                     l.Country,
                     l.State,
                     l.City,
                     l.Street,
                     l.MenuId,
                     OpenSince = l.OpenSince.ToShortDateString(),
                     l.Schedule.TimeTable
                 })
                 .Where(l => l.Country.ToUpper()
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
        public async Task GetLocationsFiltered_StateMatch_ShouldReturnFiltered()
        {
            // Arrange
            var filter = await _context.Locations
                .Select(l => l.State)
                .Where(l=>l!="N/A")
                .FirstOrDefaultAsync();

            var expectedObj = await _context.Locations
                 .Include(l => l.Schedule)
                 .Select(l => new
                 {
                     l.LocationId,
                     l.AbreviatedCountry,
                     l.AbreviatedState,
                     l.Country,
                     l.State,
                     l.City,
                     l.Street,
                     l.MenuId,
                     OpenSince = l.OpenSince.ToShortDateString(),
                     l.Schedule.TimeTable
                 })
                 .Where(l => l.State.ToUpper()
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
        public async Task GetLocationsFiltered_CityMatch_ShouldReturnFiltered()
        {
            // Arrange
            var filter = await _context.Locations
                .Select(l => l.City)
                .FirstOrDefaultAsync();

            var expectedObj = await _context.Locations
                 .Include(l => l.Schedule)
                 .Select(l => new
                 {
                     l.LocationId,
                     l.AbreviatedCountry,
                     l.AbreviatedState,
                     l.Country,
                     l.State,
                     l.City,
                     l.Street,
                     l.MenuId,
                     OpenSince = l.OpenSince.ToShortDateString(),
                     l.Schedule.TimeTable
                 })
                 .Where(l => l.City.ToUpper()
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
        public async Task GetLocationsFiltered_StreetMatch_ShouldReturnFiltered()
        {
            // Arrange
            var filter = await _context.Locations
                .Select(l => l.Street)
                .FirstOrDefaultAsync();

            var expectedObj = await _context.Locations
                 .Include(l => l.Schedule)
                 .Select(l => new
                 {
                     l.LocationId,
                     l.AbreviatedCountry,
                     l.AbreviatedState,
                     l.Country,
                     l.State,
                     l.City,
                     l.Street,
                     l.MenuId,
                     OpenSince = l.OpenSince.ToShortDateString(),
                     l.Schedule.TimeTable
                 })
                 .Where(l => l.Street.ToUpper()
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
        public async Task GetLocationsPaged_HapyFlow_ShouldReturnPaged()
        {
            // Arrange
            var pgSz = 2;
            var pgInd = 1;

            var expectedObj = await _context.Locations
                 .Include(l => l.Schedule)
                 .Select(l => new
                 {
                     l.LocationId,
                     l.AbreviatedCountry,
                     l.AbreviatedState,
                     l.Country,
                     l.State,
                     l.City,
                     l.Street,
                     l.MenuId,
                     OpenSince = l.OpenSince.ToShortDateString(),
                     l.Schedule.TimeTable
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
        public async Task GetLocationsPaged_EndPageIncomplete_ShouldReturnPaged()
        {
            // Arrange
            var locationCount = await _context.Locations.CountAsync();
            var pgSz = 1;

            while (true)
            {
                if (locationCount % pgSz == 0) pgSz++;
                else break;
            }

            var pgInd = locationCount / (pgSz - 1);

            var expectedObj = await _context.Locations
                 .Include(l => l.Schedule)
                 .Select(l => new
                 {
                     l.LocationId,
                     l.AbreviatedCountry,
                     l.AbreviatedState,
                     l.Country,
                     l.State,
                     l.City,
                     l.Street,
                     l.MenuId,
                     OpenSince = l.OpenSince.ToShortDateString(),
                     l.Schedule.TimeTable
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
        public async Task GetLocationsPaged_SizeOver_ShouldReturnPaged()
        {
            // Arrange
            var locationCount = await _context.Locations.CountAsync();
            var pgSz = locationCount + 1;
            var pgInd = 1;

            var expectedObj = await _context.Locations
                 .Include(l => l.Schedule)
                 .Select(l => new
                 {
                     l.LocationId,
                     l.AbreviatedCountry,
                     l.AbreviatedState,
                     l.Country,
                     l.State,
                     l.City,
                     l.Street,
                     l.MenuId,
                     OpenSince = l.OpenSince.ToShortDateString(),
                     l.Schedule.TimeTable
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
        public async Task GetLocationsPaged_IndexOver_ShouldReturnPaged()
        {
            // Arrange
            var locationCount = await _context.Locations.CountAsync();
            var pgInd = locationCount + 1;
            var pgSz = 1;

            var expectedObj = await _context.Locations
                 .Include(l => l.Schedule)
                 .Select(l => new
                 {
                     l.LocationId,
                     l.AbreviatedCountry,
                     l.AbreviatedState,
                     l.Country,
                     l.State,
                     l.City,
                     l.Street,
                     l.MenuId,
                     OpenSince = l.OpenSince.ToShortDateString(),
                     l.Schedule.TimeTable
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
        public async Task GetLocationsPaged_NegativeValues_ShouldReturnPaged(int pgSz, int pgInd)
        {
            // Arrange
            var absPgSz = Math.Abs(pgSz);
            var absPgInd = Math.Abs(pgInd);

            var expectedObj = await _context.Locations
                 .Include(l => l.Schedule)
                 .Select(l => new
                 {
                     l.LocationId,
                     l.AbreviatedCountry,
                     l.AbreviatedState,
                     l.Country,
                     l.State,
                     l.City,
                     l.Street,
                     l.MenuId,
                     OpenSince = l.OpenSince.ToShortDateString(),
                     l.Schedule.TimeTable
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
        public async Task GetLocationsSorted_ByCountry_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "country";

            var expectedObj = (object)await _context.Locations
                            .Include(l => l.Schedule)
                            .Select(l => new
                            {
                                l.LocationId,
                                l.AbreviatedCountry,
                                l.AbreviatedState,
                                l.Country,
                                l.State,
                                l.City,
                                l.Street,
                                l.MenuId,
                                OpenSince = l.OpenSince.ToShortDateString(),
                                l.Schedule.TimeTable
                            })
                            .OrderBy(l => l.AbreviatedCountry)
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
        public async Task GetLocationsSorted_ByState_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "state";

            var expectedObj = (object)await _context.Locations
                            .Include(l => l.Schedule)
                            .Select(l => new
                            {
                                l.LocationId,
                                l.AbreviatedCountry,
                                l.AbreviatedState,
                                l.Country,
                                l.State,
                                l.City,
                                l.Street,
                                l.MenuId,
                                OpenSince = l.OpenSince.ToShortDateString(),
                                l.Schedule.TimeTable
                            })
                            .OrderBy(l => l.AbreviatedState)
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
        public async Task GetLocationsSorted_ByCity_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "city";

            var expectedObj = (object)await _context.Locations
                            .Include(l => l.Schedule)
                            .Select(l => new
                            {
                                l.LocationId,
                                l.AbreviatedCountry,
                                l.AbreviatedState,
                                l.Country,
                                l.State,
                                l.City,
                                l.Street,
                                l.MenuId,
                                OpenSince = l.OpenSince.ToShortDateString(),
                                l.Schedule.TimeTable
                            })
                            .OrderBy(l => l.City)
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
        public async Task GetLocationsSorted_ByStreet_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "street";

            var expectedObj = (object)await _context.Locations
                            .Include(l => l.Schedule)
                            .Select(l => new
                            {
                                l.LocationId,
                                l.AbreviatedCountry,
                                l.AbreviatedState,
                                l.Country,
                                l.State,
                                l.City,
                                l.Street,
                                l.MenuId,
                                OpenSince = l.OpenSince.ToShortDateString(),
                                l.Schedule.TimeTable
                            })
                            .OrderBy(l => l.Street)
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
        public async Task GetLocationsSorted_ByMenu_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "menu";

            var expectedObj = (object)await _context.Locations
                            .Include(l => l.Schedule)
                            .Select(l => new
                            {
                                l.LocationId,
                                l.AbreviatedCountry,
                                l.AbreviatedState,
                                l.Country,
                                l.State,
                                l.City,
                                l.Street,
                                l.MenuId,
                                OpenSince = l.OpenSince.ToShortDateString(),
                                l.Schedule.TimeTable
                            })
                            .OrderBy(l => l.MenuId)
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
        public async Task GetLocationsSorted_GarbleText_ShouldReturnSortedByID()
        {
            // Arrange
            var sortBy = "hbhbhjas";

            var expectedObj = (object)await _context.Locations
                            .Include(l => l.Schedule)
                            .Select(l => new
                            {
                                l.LocationId,
                                l.AbreviatedCountry,
                                l.AbreviatedState,
                                l.Country,
                                l.State,
                                l.City,
                                l.Street,
                                l.MenuId,
                                OpenSince = l.OpenSince.ToShortDateString(),
                                l.Schedule.TimeTable
                            })
                            .OrderBy(l => l.LocationId)
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
        public async Task GetLocationsSorted_Descending_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "city";

            var expectedObj = (object)await _context.Locations
                            .Include(l => l.Schedule)
                            .Select(l => new
                            {
                                l.LocationId,
                                l.AbreviatedCountry,
                                l.AbreviatedState,
                                l.Country,
                                l.State,
                                l.City,
                                l.Street,
                                l.MenuId,
                                OpenSince = l.OpenSince.ToShortDateString(),
                                l.Schedule.TimeTable
                            })
                            .OrderByDescending(l => l.City)
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
        public async Task GetLocationsOpen_Before_ShouldReturnBefore()
        {
            // Arrange
            var date = await _context.Locations
                .Select(e => e.OpenSince)
                .OrderBy(e => e)
                .Skip(1)
                .FirstOrDefaultAsync();

            var expectedObj = (object)await _context.Locations
                .Include(l => l.Schedule)
                 .Select(l => new
                 {
                     l.LocationId,
                     l.AbreviatedCountry,
                     l.AbreviatedState,
                     l.Country,
                     l.State,
                     l.City,
                     l.Street,
                     l.MenuId,
                     l.OpenSince,
                     l.Schedule.TimeTable
                 })
                 .Where(l => l.OpenSince < date)
                 .Select(l => new
                 {
                     l.LocationId,
                     l.AbreviatedCountry,
                     l.AbreviatedState,
                     l.Country,
                     l.State,
                     l.City,
                     l.Street,
                     l.MenuId,
                     OpenSince = l.OpenSince.ToShortDateString(),
                     l.TimeTable
                 }).ToArrayAsync();
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/query:before=true&open=" + date.ToShortDateString().Replace('/', '-'));
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetLocationsOpen_After_ShouldReturnSince()
        {
            // Arrange
            var date = await _context.Locations
                .Select(e => e.OpenSince)
                .OrderBy(e => e)
                .Skip(1)
                .FirstOrDefaultAsync();

            var expectedObj = (object)await _context.Locations
                .Include(l => l.Schedule)
                 .Select(l => new
                 {
                     l.LocationId,
                     l.AbreviatedCountry,
                     l.AbreviatedState,
                     l.Country,
                     l.State,
                     l.City,
                     l.Street,
                     l.MenuId,
                     l.OpenSince,
                     l.Schedule.TimeTable
                 })
                 .Where(l => l.OpenSince >= date)
                 .Select(l => new
                 {
                     l.LocationId,
                     l.AbreviatedCountry,
                     l.AbreviatedState,
                     l.Country,
                     l.State,
                     l.City,
                     l.Street,
                     l.MenuId,
                     OpenSince = l.OpenSince.ToShortDateString(),
                     l.TimeTable
                 }).ToArrayAsync();
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/query:open=" + date.ToShortDateString().Replace('/', '-'));
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetLocationsQueried_FilteredAndSorted_ShouldReturnFilteredAndSorted()
        {
            // Arrange
            var sortBy = "city";
            var filter = await GetCommonCountry();

            var filtered = await _context.Locations
                 .Include(l => l.Schedule)
                 .Select(l => new
                 {
                     l.LocationId,
                     l.AbreviatedCountry,
                     l.AbreviatedState,
                     l.Country,
                     l.State,
                     l.City,
                     l.Street,
                     l.MenuId,
                     OpenSince = l.OpenSince.ToShortDateString(),
                     l.Schedule.TimeTable
                 })
                 .Where(l =>
                    l.Country.ToUpper()
                        .Contains(filter.ToUpper()))
                 .ToArrayAsync();

            var expectedObj = filtered
                            .OrderByDescending(l => l.City)
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
        public async Task GetLocationsQueried_FilteredAndPaged_ShouldReturnFilteredAndPaged()
        {
            // Arrange
            int pgSz = 2;
            int pgInd = 1;
            var filter = await GetCommonCountry();

            var filtered = await _context.Locations
                 .Include(l => l.Schedule)
                 .Select(l => new
                 {
                     l.LocationId,
                     l.AbreviatedCountry,
                     l.AbreviatedState,
                     l.Country,
                     l.State,
                     l.City,
                     l.Street,
                     l.MenuId,
                     OpenSince = l.OpenSince.ToShortDateString(),
                     l.Schedule.TimeTable
                 })
                 .Where(l =>
                    l.AbreviatedCountry.ToUpper()
                        .Contains(filter.ToUpper()) ||
                    l.AbreviatedState.ToUpper()
                        .Contains(filter.ToUpper()) ||
                    l.Country.ToUpper()
                        .Contains(filter.ToUpper()) ||
                    l.State.ToUpper()
                        .Contains(filter.ToUpper()) ||
                    l.City.ToUpper()
                        .Contains(filter.ToUpper()) ||
                    l.Street.ToUpper()
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
        public async Task GetLocationsQueried_SortedAndPaged_ShouldReturnSortedAndPaged()
        {
            // Arrange
            int pgSz = 2;
            int pgInd = 1;
            var sortBy = "city";

            var sorted = await _context.Locations
                            .Include(l => l.Schedule)
                            .Select(l => new
                            {
                                l.LocationId,
                                l.AbreviatedCountry,
                                l.AbreviatedState,
                                l.Country,
                                l.State,
                                l.City,
                                l.Street,
                                l.MenuId,
                                OpenSince = l.OpenSince.ToShortDateString(),
                                l.Schedule.TimeTable
                            })
                            .OrderByDescending(l => l.City)
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
        public async Task GetLocationsQueried_FilteredSortedAndPaged_ShouldReturnFilteredSortedAndPaged()
        {
            // Arrange
            int pgSz = 2;
            int pgInd = 1;
            var sortBy = "city";
            var filter = await GetCommonCountry();

            var filtered = await _context.Locations
                 .Include(l => l.Schedule)
                 .Select(l => new
                 {
                     l.LocationId,
                     l.AbreviatedCountry,
                     l.AbreviatedState,
                     l.Country,
                     l.State,
                     l.City,
                     l.Street,
                     l.MenuId,
                     OpenSince = l.OpenSince.ToShortDateString(),
                     l.Schedule.TimeTable
                 })
                 .Where(l =>
                    l.Country.ToUpper()
                        .Contains(filter.ToUpper()))
                 .ToArrayAsync();

            var sorted = filtered
                            .OrderByDescending(l => l.City)
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

        [TestMethod]
        public async Task GetLocationsQueried_OpenedFilteredAndSorted_ShouldReturnOpenedFilteredAndSorted()
        {
            // Arrange
            var sortBy = "city";
            var count = await _context.Locations.CountAsync();
            var date = await _context.Locations
                .Select(e => e.OpenSince)
                .OrderBy(e => e)
                .Skip(count/2)
                .FirstOrDefaultAsync();

            var open = await _context.Locations
                .Include(l => l.Schedule)
                 .Select(l => new
                 {
                     l.LocationId,
                     l.AbreviatedCountry,
                     l.AbreviatedState,
                     l.Country,
                     l.State,
                     l.City,
                     l.Street,
                     l.MenuId,
                     l.OpenSince,
                     l.Schedule.TimeTable
                 })
                 .Where(l => l.OpenSince < date)
                 .Select(l => new
                 {
                     l.LocationId,
                     l.AbreviatedCountry,
                     l.AbreviatedState,
                     l.Country,
                     l.State,
                     l.City,
                     l.Street,
                     l.MenuId,
                     OpenSince = l.OpenSince.ToShortDateString(),
                     l.TimeTable
                 }).ToArrayAsync();

            var filter = open.Select(l => l.Country).FirstOrDefault();

            var filtered = open
                 .Where(l => l.Country.ToUpper()
                        .Contains(filter.ToUpper()))
                 .ToArray();

            var expectedObj = filtered
                            .OrderByDescending(l => l.City)
                            .ToArray();
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/query:desc=true&before=true&filter=" + filter + 
                "&sortby=" + sortBy +
                "&open=" + date.ToShortDateString().Replace('/','-'));
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetLocationsQueried_OpenedFilteredAndPaged_ShouldReturnOpenedFilteredAndPaged()
        {
            // Arrange
            int pgSz = 2;
            int pgInd = 1;
            var count = await _context.Locations.CountAsync();
            var date = await _context.Locations
                .Select(e => e.OpenSince)
                .OrderBy(e => e)
                .Skip(count / 2)
                .FirstOrDefaultAsync();

            var open = await _context.Locations
                .Include(l => l.Schedule)
                 .Select(l => new
                 {
                     l.LocationId,
                     l.AbreviatedCountry,
                     l.AbreviatedState,
                     l.Country,
                     l.State,
                     l.City,
                     l.Street,
                     l.MenuId,
                     l.OpenSince,
                     l.Schedule.TimeTable
                 })
                 .Where(l => l.OpenSince < date)
                 .Select(l => new
                 {
                     l.LocationId,
                     l.AbreviatedCountry,
                     l.AbreviatedState,
                     l.Country,
                     l.State,
                     l.City,
                     l.Street,
                     l.MenuId,
                     OpenSince = l.OpenSince.ToShortDateString(),
                     l.TimeTable
                 }).ToArrayAsync();

            var filter = open.Select(l => l.Country).FirstOrDefault();

            var filtered = open
                 .Where(l => l.Country.ToUpper()
                        .Contains(filter.ToUpper()))
                 .ToArray();

            var expectedObj = filtered
                .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArray();
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/query:before=true&filter=" + filter + 
                "&pgsz=" + pgSz + "&pgind=" + pgInd +
                "&open=" + date.ToShortDateString().Replace('/', '-'));

            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetLocationsQueried_OpenedSortedAndPaged_ShouldReturnOpenedSortedAndPaged()
        {
            // Arrange
            int pgSz = 2;
            int pgInd = 1;
            var sortBy = "city";

            var count = await _context.Locations.CountAsync();
            var date = await _context.Locations
                .Select(e => e.OpenSince)
                .OrderBy(e => e)
                .Skip(count / 2)
                .FirstOrDefaultAsync();

            var open = await _context.Locations
                .Include(l => l.Schedule)
                 .Select(l => new
                 {
                     l.LocationId,
                     l.AbreviatedCountry,
                     l.AbreviatedState,
                     l.Country,
                     l.State,
                     l.City,
                     l.Street,
                     l.MenuId,
                     l.OpenSince,
                     l.Schedule.TimeTable
                 })
                 .Where(l => l.OpenSince < date)
                 .Select(l => new
                 {
                     l.LocationId,
                     l.AbreviatedCountry,
                     l.AbreviatedState,
                     l.Country,
                     l.State,
                     l.City,
                     l.Street,
                     l.MenuId,
                     OpenSince = l.OpenSince.ToShortDateString(),
                     l.TimeTable
                 }).ToArrayAsync();

            var sorted = open
                .OrderByDescending(l => l.City)
                .ToArray();

            var expectedObj = sorted
                .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArray();
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/query:desc=true&before=true&sortby=" + sortBy +
                "&pgsz=" + pgSz + "&pgind=" + pgInd +
                "&open=" + date.ToShortDateString().Replace('/', '-'));

            var content = await result.Content.ReadAsStringAsync();

            // Assert
            
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetLocationsQueried_OpenedFilteredSortedAndPaged_ShouldReturnOpenedFilteredSortedAndPaged()
        {
            // Arrange
            int pgSz = 2;
            int pgInd = 1;
            var sortBy = "city";
            var count = await _context.Locations.CountAsync();
            var date = await _context.Locations
                .Select(e => e.OpenSince)
                .OrderBy(e => e)
                .Skip(count / 2)
                .FirstOrDefaultAsync();

            var open = await _context.Locations
                .Include(l => l.Schedule)
                 .Select(l => new
                 {
                     l.LocationId,
                     l.AbreviatedCountry,
                     l.AbreviatedState,
                     l.Country,
                     l.State,
                     l.City,
                     l.Street,
                     l.MenuId,
                     l.OpenSince,
                     l.Schedule.TimeTable
                 })
                 .Where(l => l.OpenSince < date)
                 .Select(l => new
                 {
                     l.LocationId,
                     l.AbreviatedCountry,
                     l.AbreviatedState,
                     l.Country,
                     l.State,
                     l.City,
                     l.Street,
                     l.MenuId,
                     OpenSince = l.OpenSince.ToShortDateString(),
                     l.TimeTable
                 }).ToArrayAsync();

            var filter = open.Select(l=>l.Country).FirstOrDefault();

            var filtered = open
                 .Where(l =>
                    l.AbreviatedCountry.ToUpper()
                        .Contains(filter.ToUpper()) ||
                    l.AbreviatedState.ToUpper()
                        .Contains(filter.ToUpper()) ||
                    l.Country.ToUpper()
                        .Contains(filter.ToUpper()) ||
                    l.State.ToUpper()
                        .Contains(filter.ToUpper()) ||
                    l.City.ToUpper()
                        .Contains(filter.ToUpper()) ||
                    l.Street.ToUpper()
                        .Contains(filter.ToUpper()))
                 .ToArray();

            var sorted = filtered
                            .OrderByDescending(l => l.City)
                            .ToArray();

            var expectedObj = sorted
                .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArray();
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/query:before=true&desc=true&sortby=" + sortBy +
                "&pgsz=" + pgSz + "&pgind=" + pgInd +
                "&filter=" + filter + 
                "&open=" + date.ToShortDateString().Replace('/', '-'));

            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        #endregion

        #endregion

        #region Updates

        [TestMethod]
        public async Task UpdateLocation_HappyFlow_ShouldUpdateAndReturnLocation()
        {
            // Arrange
            var locId = GetFirstLocationId;

            /* Save a copy of the original entry to compare against*/
            var oldLoc = await _context.Locations.FindAsync(locId);
            var oldLocCopy = new Location
            {
                LocationId = oldLoc.LocationId,
                Schedule=oldLoc.Schedule,
                ScheduleId=oldLoc.ScheduleId,
                State=oldLoc.State,
                AbreviatedCountry=oldLoc.AbreviatedCountry,
                Street=oldLoc.Street,
                SupplyLinks=oldLoc.SupplyLinks,
                AbreviatedState=oldLoc.AbreviatedState,
                City=oldLoc.City,
                OpenSince=oldLoc.OpenSince,
                Country=oldLoc.Country,
                Employees=oldLoc.Employees,
                Managements=oldLoc.Managements,
                Menu=oldLoc.Menu,
                MenuId=oldLoc.MenuId
            };

            var loc = new Location
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "YY",
                City = "Newtopia",
                Street = "808 Upstream Lane",
                Country = "Updaya",
                State = "Freshens",
                OpenSince = oldLoc.OpenSince.AddDays(-10),
                MenuId = await _context.Menus
                    .Select(m => m.MenuId)
                    .FirstOrDefaultAsync(m=>m!=oldLoc.MenuId),
                ScheduleId = await _context.Schedules
                    .Select(s => s.ScheduleId)
                    .FirstOrDefaultAsync(s=>s!=oldLoc.ScheduleId)
            };
            var locContent = new StringContent(JsonConvert.SerializeObject(loc),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + locId, locContent);

            /* Retrieve the (allegedly) updated loc*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + locId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newLoc = JsonConvert.DeserializeObject(newCont, typeof(Location));

            /* Complete the loc we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            loc.Managements = oldLocCopy.Managements;
            loc.Menu = oldLocCopy.Menu;
            loc.Schedule = oldLocCopy.Schedule;
            loc.SupplyLinks = oldLocCopy.SupplyLinks;
            loc.Employees = oldLocCopy.Employees;
            loc.LocationId = oldLocCopy.LocationId;

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            newLoc.Should().NotBeEquivalentTo(oldLocCopy);
            newLoc.Should().BeEquivalentTo(loc);

            // Clean-up
            loc = new Location 
            {
                AbreviatedCountry = oldLocCopy.AbreviatedCountry,
                AbreviatedState = oldLocCopy.AbreviatedState,
                City = oldLocCopy.City,
                Street = oldLocCopy.Street,
                Country = oldLocCopy.Country,
                State = oldLocCopy.State,
                OpenSince = oldLocCopy.OpenSince,
                MenuId = oldLocCopy.MenuId,
                ScheduleId = oldLocCopy.ScheduleId
            };
            locContent = new StringContent(JsonConvert.SerializeObject(loc),
                Encoding.UTF8, "application/json");
            result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + locId, locContent);
            updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + locId + "/basic");
            newCont = await updCall.Content.ReadAsStringAsync();
            newLoc = JsonConvert.DeserializeObject(newCont, typeof(Location));

            result.IsSuccessStatusCode.Should().BeTrue();
            newLoc.Should().BeEquivalentTo(oldLocCopy);
        }

        [TestMethod]
        public async Task UpdateLocation_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var locId = GetFirstLocationId;

            /* Save a copy of the original entry to compare against*/
            var oldLoc = await _context.Locations.FindAsync(locId);
            var oldLocCopy = new Location
            {
                LocationId = oldLoc.LocationId,
                Schedule = oldLoc.Schedule,
                ScheduleId = oldLoc.ScheduleId,
                State = oldLoc.State,
                AbreviatedCountry = oldLoc.AbreviatedCountry,
                Street = oldLoc.Street,
                SupplyLinks = oldLoc.SupplyLinks,
                AbreviatedState = oldLoc.AbreviatedState,
                City = oldLoc.City,
                OpenSince = oldLoc.OpenSince,
                Country = oldLoc.Country,
                Employees = oldLoc.Employees,
                Managements = oldLoc.Managements,
                Menu = oldLoc.Menu,
                MenuId = oldLoc.MenuId
            };

            var loc = new Location
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "YY",
                City = "Newtopia",
                Street = "808 Upstream Lane",
                Country = "Updaya",
                State = "Freshens",
                OpenSince = oldLoc.OpenSince.AddDays(-10),
                MenuId = await _context.Menus
                    .Select(m => m.MenuId)
                    .FirstOrDefaultAsync(m => m != oldLoc.MenuId),
                ScheduleId = await _context.Schedules
                    .Select(s => s.ScheduleId)
                    .FirstOrDefaultAsync(s => s != oldLoc.ScheduleId)
            };
            var locContent = new StringContent(JsonConvert.SerializeObject(loc),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + 
                GenBadLocationId, locContent);

            /* Retrieve the (allegedly) updated loc*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + locId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newLoc = JsonConvert.DeserializeObject(newCont, typeof(Location));

            /* Complete the loc we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            loc.Managements = oldLocCopy.Managements;
            loc.Menu = oldLocCopy.Menu;
            loc.Schedule = oldLocCopy.Schedule;
            loc.SupplyLinks = oldLocCopy.SupplyLinks;
            loc.Employees = oldLocCopy.Employees;
            loc.LocationId = oldLocCopy.LocationId;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
            newLoc.Should().BeEquivalentTo(oldLocCopy);
            newLoc.Should().NotBeEquivalentTo(loc);
        }

        [TestMethod]
        public async Task UpdateLocation_TriesToSetId_ShouldReturnBadRequest()
        {
            // Arrange
            var locId = GetFirstLocationId;

            /* Save a copy of the original entry to compare against*/
            var oldLoc = await _context.Locations.FindAsync(locId);
            var oldLocCopy = new Location
            {
                LocationId = oldLoc.LocationId,
                Schedule = oldLoc.Schedule,
                ScheduleId = oldLoc.ScheduleId,
                State = oldLoc.State,
                AbreviatedCountry = oldLoc.AbreviatedCountry,
                Street = oldLoc.Street,
                SupplyLinks = oldLoc.SupplyLinks,
                AbreviatedState = oldLoc.AbreviatedState,
                City = oldLoc.City,
                OpenSince = oldLoc.OpenSince,
                Country = oldLoc.Country,
                Employees = oldLoc.Employees,
                Managements = oldLoc.Managements,
                Menu = oldLoc.Menu,
                MenuId = oldLoc.MenuId
            };

            var loc = new Location
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "YY",
                City = "Newtopia",
                Street = "808 Upstream Lane",
                Country = "Updaya",
                State = "Freshens",
                OpenSince = oldLoc.OpenSince.AddDays(-10),
                MenuId = await _context.Menus
                    .Select(m => m.MenuId)
                    .FirstOrDefaultAsync(m => m != oldLoc.MenuId),
                ScheduleId = await _context.Schedules
                    .Select(s => s.ScheduleId)
                    .FirstOrDefaultAsync(s => s != oldLoc.ScheduleId),
                LocationId = GenBadLocationId
            };
            var locContent = new StringContent(JsonConvert.SerializeObject(loc),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + locId, locContent);

            /* Retrieve the (allegedly) updated loc*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + locId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newLoc = JsonConvert.DeserializeObject(newCont, typeof(Location));

            /* Complete the loc we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            loc.Managements = oldLocCopy.Managements;
            loc.Menu = oldLocCopy.Menu;
            loc.Schedule = oldLocCopy.Schedule;
            loc.SupplyLinks = oldLocCopy.SupplyLinks;
            loc.Employees = oldLocCopy.Employees;
            loc.LocationId = oldLocCopy.LocationId;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newLoc.Should().BeEquivalentTo(oldLocCopy);
            newLoc.Should().NotBeEquivalentTo(loc);
        }

        [TestMethod]
        [Ignore]
        public async Task UpdateLocation_Fixes()
        {
            var loc = await _context.Locations.FirstOrDefaultAsync();

            loc.AbreviatedCountry = "IE";
            loc.AbreviatedState = "N/A";
            loc.City = "Coolock";
            loc.Street = "421 Tennessee Circle";
            loc.Country = "Ireland";
            loc.State = "N/A";
            loc.OpenSince = new DateTime(2018, 2, 27);
            loc.MenuId = 1;
            loc.ScheduleId = 1;

            await _context.SaveChangesAsync();

            bool fake = false;
            fake.Should().BeFalse();
        }

        [DataTestMethod]
        #region Rows
        [DataRow("FRAAA", "B8", "Digne-les-Bains", "279 Corben Terrace", "France", "Provence-Alpes-Côte d'Azur")]
        [DataRow(" ", "B8", "Digne-les-Bains", "279 Corben Terrace", "France", "Provence-Alpes-Côte d'Azur")]
        [DataRow("FR", "B8AAA", "Digne-les-Bains", "279 Corben Terrace", "France", "Provence-Alpes-Côte d'Azur")]
        [DataRow("FR", " ", "Digne-les-Bains", "279 Corben Terrace", "France", "Provence-Alpes-Côte d'Azur")]
        [DataRow("FR", "B8", "012345678911234567892123456789312345678941234567895",
             "279 Corben Terrace", "France", "Provence-Alpes-Côte d'Azur")]
        [DataRow("FR", "B8", " ", "279 Corben Terrace", "France", "Provence-Alpes-Côte d'Azur")]
        [DataRow("FR", "B8", "Digne-les-Bains", "012345678911234567892123456789312345678941234567895",
             "France", "Provence-Alpes-Côte d'Azur")]
        [DataRow("FR", "B8", "Digne-les-Bains", " ", "France", "Provence-Alpes-Côte d'Azur")]
        [DataRow("FR", "B8", "Digne-les-Bains", "279 Corben Terrace", "012345678911234567892123456789312345678941234567895",
             "Provence -Alpes-Côte d'Azur")]
        [DataRow("FR", "B8", "Digne-les-Bains", "279 Corben Terrace", " ", "Provence -Alpes-Côte d'Azur")]
        [DataRow("FR", "B8", "Digne-les-Bains", "279 Corben Terrace", "France",
             "012345678911234567892123456789312345678941234567895")]
        [DataRow("FR", "B8", "Digne-les-Bains", "279 Corben Terrace", "France", " ")]
        #endregion
        public async Task UpdateLocation_BadAddresses_ShouldReturnBadRequest
             (string abvCount, string abvState, string city, string str, string country, string state)
        {
            // Arrange
            var locId = GetFirstLocationId;

            /* Save a copy of the original entry to compare against*/
            var oldLoc = await _context.Locations.FindAsync(locId);
            var oldLocCopy = new Location
            {
                LocationId = oldLoc.LocationId,
                Schedule = oldLoc.Schedule,
                ScheduleId = oldLoc.ScheduleId,
                State = oldLoc.State,
                AbreviatedCountry = oldLoc.AbreviatedCountry,
                Street = oldLoc.Street,
                SupplyLinks = oldLoc.SupplyLinks,
                AbreviatedState = oldLoc.AbreviatedState,
                City = oldLoc.City,
                OpenSince = oldLoc.OpenSince,
                Country = oldLoc.Country,
                Employees = oldLoc.Employees,
                Managements = oldLoc.Managements,
                Menu = oldLoc.Menu,
                MenuId = oldLoc.MenuId
            };

            var loc = new Location
            {
                OpenSince = oldLoc.OpenSince.AddDays(-10),
                MenuId = await _context.Menus
                    .Select(m => m.MenuId)
                    .FirstOrDefaultAsync(m => m != oldLoc.MenuId),
                ScheduleId = await _context.Schedules
                    .Select(s => s.ScheduleId)
                    .FirstOrDefaultAsync(s => s != oldLoc.ScheduleId),
                AbreviatedCountry = abvCount,
                AbreviatedState = abvState,
                City = city,
                Street = str,
                Country = country,
                State = state
            };
            var locContent = new StringContent(JsonConvert.SerializeObject(loc),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + locId, locContent);

            /* Retrieve the (allegedly) updated loc*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + locId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newLoc = JsonConvert.DeserializeObject(newCont, typeof(Location));

            /* Complete the loc we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            loc.Managements = oldLocCopy.Managements;
            loc.Menu = oldLocCopy.Menu;
            loc.Schedule = oldLocCopy.Schedule;
            loc.SupplyLinks = oldLocCopy.SupplyLinks;
            loc.Employees = oldLocCopy.Employees;
            loc.LocationId = oldLocCopy.LocationId;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newLoc.Should().BeEquivalentTo(oldLocCopy);
            newLoc.Should().NotBeEquivalentTo(loc);
        }

        [TestMethod]
        public async Task UpdateLocation_DuplicateAddress_ShouldReturnBadRequest()
        {
            // Arrange
            var locId = GetFirstLocationId;

            var dupe = (await _context.Locations
                .Select(l => new
                {
                    l.LocationId,
                    res = new
                    {
                        l.AbreviatedCountry,
                        l.AbreviatedState,
                        l.City,
                        l.Country,
                        l.State,
                        l.Street
                    }
                })
                .FirstOrDefaultAsync(l => l.LocationId != locId))
                .res;

            var stop = await _context.Locations.AnyAsync(l =>
                        l.AbreviatedCountry == dupe.AbreviatedCountry &&
                        l.AbreviatedState == dupe.AbreviatedState &&
                        l.City == dupe.City &&
                        l.Street == dupe.Street);

            /* Save a copy of the original entry to compare against*/
            var oldLoc = await _context.Locations.FindAsync(locId);
            var oldLocCopy = new Location
            {
                LocationId = oldLoc.LocationId,
                Schedule = oldLoc.Schedule,
                ScheduleId = oldLoc.ScheduleId,
                State = oldLoc.State,
                AbreviatedCountry = oldLoc.AbreviatedCountry,
                Street = oldLoc.Street,
                SupplyLinks = oldLoc.SupplyLinks,
                AbreviatedState = oldLoc.AbreviatedState,
                City = oldLoc.City,
                OpenSince = oldLoc.OpenSince,
                Country = oldLoc.Country,
                Employees = oldLoc.Employees,
                Managements = oldLoc.Managements,
                Menu = oldLoc.Menu,
                MenuId = oldLoc.MenuId
            };

            var loc = new Location
            {
                OpenSince = oldLoc.OpenSince.AddDays(-10),
                MenuId = await _context.Menus
                    .Select(m => m.MenuId)
                    .FirstOrDefaultAsync(m => m != oldLoc.MenuId),
                ScheduleId = await _context.Schedules
                    .Select(s => s.ScheduleId)
                    .FirstOrDefaultAsync(s => s != oldLoc.ScheduleId),
                AbreviatedCountry = dupe.AbreviatedCountry,
                AbreviatedState = dupe.AbreviatedState,
                City = dupe.City,
                Street = dupe.Street,
                Country = dupe.Country,
                State = dupe.State
            };
            var locContent = new StringContent(JsonConvert.SerializeObject(loc),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + locId, locContent);

            /* Retrieve the (allegedly) updated loc*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + locId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newLoc = JsonConvert.DeserializeObject(newCont, typeof(Location));

            /* Complete the loc we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            loc.Managements = oldLocCopy.Managements;
            loc.Menu = oldLocCopy.Menu;
            loc.Schedule = oldLocCopy.Schedule;
            loc.SupplyLinks = oldLocCopy.SupplyLinks;
            loc.Employees = oldLocCopy.Employees;
            loc.LocationId = oldLocCopy.LocationId;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newLoc.Should().BeEquivalentTo(oldLocCopy);
            newLoc.Should().NotBeEquivalentTo(loc);
        }

        [TestMethod]
        public async Task UpdateLocation_BadMenuId_ShouldReturnBadRequest()
        {
            // Arrange
            var locId = GetFirstLocationId;

            /* Save a copy of the original entry to compare against*/
            var oldLoc = await _context.Locations.FindAsync(locId);
            var oldLocCopy = new Location
            {
                LocationId = oldLoc.LocationId,
                Schedule = oldLoc.Schedule,
                ScheduleId = oldLoc.ScheduleId,
                State = oldLoc.State,
                AbreviatedCountry = oldLoc.AbreviatedCountry,
                Street = oldLoc.Street,
                SupplyLinks = oldLoc.SupplyLinks,
                AbreviatedState = oldLoc.AbreviatedState,
                City = oldLoc.City,
                OpenSince = oldLoc.OpenSince,
                Country = oldLoc.Country,
                Employees = oldLoc.Employees,
                Managements = oldLoc.Managements,
                Menu = oldLoc.Menu,
                MenuId = oldLoc.MenuId
            };
            
            var loc = new Location
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "YY",
                City = "Newtopia",
                Street = "808 Upstream Lane",
                Country = "Updaya",
                State = "Freshens",
                OpenSince = oldLoc.OpenSince.AddDays(-10),
                ScheduleId = await _context.Schedules
                    .Select(s => s.ScheduleId)
                    .FirstOrDefaultAsync(s => s != oldLoc.ScheduleId),
                MenuId = (await _context.Menus
                    .Select(m=>m.MenuId)
                    .ToArrayAsync())
                    .LastOrDefault() + 1
            };
            var locContent = new StringContent(JsonConvert.SerializeObject(loc),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + locId, locContent);

            /* Retrieve the (allegedly) updated loc*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + locId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newLoc = JsonConvert.DeserializeObject(newCont, typeof(Location));

            /* Complete the loc we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            loc.Managements = oldLocCopy.Managements;
            loc.Menu = oldLocCopy.Menu;
            loc.Schedule = oldLocCopy.Schedule;
            loc.SupplyLinks = oldLocCopy.SupplyLinks;
            loc.Employees = oldLocCopy.Employees;
            loc.LocationId = oldLocCopy.LocationId;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newLoc.Should().BeEquivalentTo(oldLocCopy);
            newLoc.Should().NotBeEquivalentTo(loc);
        }

        [TestMethod]
        public async Task UpdateLocation_BadScheduleId_ShouldReturnBadRequest()
        {
            // Arrange
            var locId = GetFirstLocationId;

            /* Save a copy of the original entry to compare against*/
            var oldLoc = await _context.Locations.FindAsync(locId);
            var oldLocCopy = new Location
            {
                LocationId = oldLoc.LocationId,
                Schedule = oldLoc.Schedule,
                ScheduleId = oldLoc.ScheduleId,
                State = oldLoc.State,
                AbreviatedCountry = oldLoc.AbreviatedCountry,
                Street = oldLoc.Street,
                SupplyLinks = oldLoc.SupplyLinks,
                AbreviatedState = oldLoc.AbreviatedState,
                City = oldLoc.City,
                OpenSince = oldLoc.OpenSince,
                Country = oldLoc.Country,
                Employees = oldLoc.Employees,
                Managements = oldLoc.Managements,
                Menu = oldLoc.Menu,
                MenuId = oldLoc.MenuId
            };

            var loc = new Location
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "YY",
                City = "Newtopia",
                Street = "808 Upstream Lane",
                Country = "Updaya",
                State = "Freshens",
                OpenSince = oldLoc.OpenSince.AddDays(-10),
                MenuId = await _context.Menus
                    .Select(m => m.MenuId)
                    .FirstOrDefaultAsync(m => m!= oldLoc.MenuId),
                ScheduleId = (await _context.Schedules
                    .Select(s => s.ScheduleId)
                    .ToArrayAsync())
                    .LastOrDefault() + 1
            };
            var locContent = new StringContent(JsonConvert.SerializeObject(loc),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + locId, locContent);

            /* Retrieve the (allegedly) updated loc*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + locId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newLoc = JsonConvert.DeserializeObject(newCont, typeof(Location));

            /* Complete the loc we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            loc.Managements = oldLocCopy.Managements;
            loc.Menu = oldLocCopy.Menu;
            loc.Schedule = oldLocCopy.Schedule;
            loc.SupplyLinks = oldLocCopy.SupplyLinks;
            loc.Employees = oldLocCopy.Employees;
            loc.LocationId = oldLocCopy.LocationId;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newLoc.Should().BeEquivalentTo(oldLocCopy);
            newLoc.Should().NotBeEquivalentTo(loc);
        }

        [TestMethod]
        public async Task UpdateLocation_HasMenu_ShouldReturnBadRequest()
        {
            // Arrange
            var locId = GetFirstLocationId;

            /* Save a copy of the original entry to compare against*/
            var oldLoc = await _context.Locations.FindAsync(locId);
            var oldLocCopy = new Location
            {
                LocationId = oldLoc.LocationId,
                Schedule = oldLoc.Schedule,
                ScheduleId = oldLoc.ScheduleId,
                State = oldLoc.State,
                AbreviatedCountry = oldLoc.AbreviatedCountry,
                Street = oldLoc.Street,
                SupplyLinks = oldLoc.SupplyLinks,
                AbreviatedState = oldLoc.AbreviatedState,
                City = oldLoc.City,
                OpenSince = oldLoc.OpenSince,
                Country = oldLoc.Country,
                Employees = oldLoc.Employees,
                Managements = oldLoc.Managements,
                Menu = oldLoc.Menu,
                MenuId = oldLoc.MenuId
            };

            var loc = new Location
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "YY",
                City = "Newtopia",
                Street = "808 Upstream Lane",
                Country = "Updaya",
                State = "Freshens",
                OpenSince = oldLoc.OpenSince.AddDays(-10),
                MenuId = await _context.Menus
                    .Select(m => m.MenuId)
                    .FirstOrDefaultAsync(m => m != oldLoc.MenuId),
                ScheduleId = await _context.Schedules
                    .Select(s => s.ScheduleId)
                    .FirstOrDefaultAsync(s => s != oldLoc.ScheduleId),
                Menu = new Menu()
            };
            var locContent = new StringContent(JsonConvert.SerializeObject(loc),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + locId, locContent);

            /* Retrieve the (allegedly) updated loc*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + locId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newLoc = JsonConvert.DeserializeObject(newCont, typeof(Location));

            /* Complete the loc we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            loc.Managements = oldLocCopy.Managements;
            loc.Menu = oldLocCopy.Menu;
            loc.Schedule = oldLocCopy.Schedule;
            loc.SupplyLinks = oldLocCopy.SupplyLinks;
            loc.Employees = oldLocCopy.Employees;
            loc.LocationId = oldLocCopy.LocationId;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newLoc.Should().BeEquivalentTo(oldLocCopy);
            newLoc.Should().NotBeEquivalentTo(loc);
        }

        [TestMethod]
        public async Task UpdateLocation_HasSchedule_ShouldReturnBadRequest()
        {
            // Arrange
            var locId = GetFirstLocationId;

            /* Save a copy of the original entry to compare against*/
            var oldLoc = await _context.Locations.FindAsync(locId);
            var oldLocCopy = new Location
            {
                LocationId = oldLoc.LocationId,
                Schedule = oldLoc.Schedule,
                ScheduleId = oldLoc.ScheduleId,
                State = oldLoc.State,
                AbreviatedCountry = oldLoc.AbreviatedCountry,
                Street = oldLoc.Street,
                SupplyLinks = oldLoc.SupplyLinks,
                AbreviatedState = oldLoc.AbreviatedState,
                City = oldLoc.City,
                OpenSince = oldLoc.OpenSince,
                Country = oldLoc.Country,
                Employees = oldLoc.Employees,
                Managements = oldLoc.Managements,
                Menu = oldLoc.Menu,
                MenuId = oldLoc.MenuId
            };

            var loc = new Location
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "YY",
                City = "Newtopia",
                Street = "808 Upstream Lane",
                Country = "Updaya",
                State = "Freshens",
                OpenSince = oldLoc.OpenSince.AddDays(-10),
                MenuId = await _context.Menus
                    .Select(m => m.MenuId)
                    .FirstOrDefaultAsync(m => m != oldLoc.MenuId),
                ScheduleId = await _context.Schedules
                    .Select(s => s.ScheduleId)
                    .FirstOrDefaultAsync(s => s != oldLoc.ScheduleId),
                Schedule = new Schedule()
            };
            var locContent = new StringContent(JsonConvert.SerializeObject(loc),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + locId, locContent);

            /* Retrieve the (allegedly) updated loc*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + locId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newLoc = JsonConvert.DeserializeObject(newCont, typeof(Location));

            /* Complete the loc we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            loc.Managements = oldLocCopy.Managements;
            loc.Menu = oldLocCopy.Menu;
            loc.Schedule = oldLocCopy.Schedule;
            loc.SupplyLinks = oldLocCopy.SupplyLinks;
            loc.Employees = oldLocCopy.Employees;
            loc.LocationId = oldLocCopy.LocationId;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newLoc.Should().BeEquivalentTo(oldLocCopy);
            newLoc.Should().NotBeEquivalentTo(loc);
        }

        [TestMethod]
        public async Task UpdateLocation_ManagementsNull_ShouldReturnBadRequest()
        {
            // Arrange
            var locId = GetFirstLocationId;

            /* Save a copy of the original entry to compare against*/
            var oldLoc = await _context.Locations.FindAsync(locId);
            var oldLocCopy = new Location
            {
                LocationId = oldLoc.LocationId,
                Schedule = oldLoc.Schedule,
                ScheduleId = oldLoc.ScheduleId,
                State = oldLoc.State,
                AbreviatedCountry = oldLoc.AbreviatedCountry,
                Street = oldLoc.Street,
                SupplyLinks = oldLoc.SupplyLinks,
                AbreviatedState = oldLoc.AbreviatedState,
                City = oldLoc.City,
                OpenSince = oldLoc.OpenSince,
                Country = oldLoc.Country,
                Employees = oldLoc.Employees,
                Managements = oldLoc.Managements,
                Menu = oldLoc.Menu,
                MenuId = oldLoc.MenuId
            };

            var loc = new Location
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "YY",
                City = "Newtopia",
                Street = "808 Upstream Lane",
                Country = "Updaya",
                State = "Freshens",
                OpenSince = oldLoc.OpenSince.AddDays(-10),
                MenuId = await _context.Menus
                    .Select(m => m.MenuId)
                    .FirstOrDefaultAsync(m => m != oldLoc.MenuId),
                ScheduleId = await _context.Schedules
                    .Select(s => s.ScheduleId)
                    .FirstOrDefaultAsync(s => s != oldLoc.ScheduleId),
                Managements = null
            };
            var locContent = new StringContent(JsonConvert.SerializeObject(loc),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + locId, locContent);

            /* Retrieve the (allegedly) updated loc*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + locId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newLoc = JsonConvert.DeserializeObject(newCont, typeof(Location));

            /* Complete the loc we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            loc.Managements = oldLocCopy.Managements;
            loc.Menu = oldLocCopy.Menu;
            loc.Schedule = oldLocCopy.Schedule;
            loc.SupplyLinks = oldLocCopy.SupplyLinks;
            loc.Employees = oldLocCopy.Employees;
            loc.LocationId = oldLocCopy.LocationId;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newLoc.Should().BeEquivalentTo(oldLocCopy);
            newLoc.Should().NotBeEquivalentTo(loc);
        }

        [TestMethod]
        public async Task UpdateLocation_SupplyLinksNull_ShouldReturnBadRequest()
        {
            // Arrange
            var locId = GetFirstLocationId;

            /* Save a copy of the original entry to compare against*/
            var oldLoc = await _context.Locations.FindAsync(locId);
            var oldLocCopy = new Location
            {
                LocationId = oldLoc.LocationId,
                Schedule = oldLoc.Schedule,
                ScheduleId = oldLoc.ScheduleId,
                State = oldLoc.State,
                AbreviatedCountry = oldLoc.AbreviatedCountry,
                Street = oldLoc.Street,
                SupplyLinks = oldLoc.SupplyLinks,
                AbreviatedState = oldLoc.AbreviatedState,
                City = oldLoc.City,
                OpenSince = oldLoc.OpenSince,
                Country = oldLoc.Country,
                Employees = oldLoc.Employees,
                Managements = oldLoc.Managements,
                Menu = oldLoc.Menu,
                MenuId = oldLoc.MenuId
            };

            var loc = new Location
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "YY",
                City = "Newtopia",
                Street = "808 Upstream Lane",
                Country = "Updaya",
                State = "Freshens",
                OpenSince = oldLoc.OpenSince.AddDays(-10),
                MenuId = await _context.Menus
                    .Select(m => m.MenuId)
                    .FirstOrDefaultAsync(m => m != oldLoc.MenuId),
                ScheduleId = await _context.Schedules
                    .Select(s => s.ScheduleId)
                    .FirstOrDefaultAsync(s => s != oldLoc.ScheduleId),
                SupplyLinks = null
            };
            var locContent = new StringContent(JsonConvert.SerializeObject(loc),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + locId, locContent);

            /* Retrieve the (allegedly) updated loc*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + locId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newLoc = JsonConvert.DeserializeObject(newCont, typeof(Location));

            /* Complete the loc we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            loc.Managements = oldLocCopy.Managements;
            loc.Menu = oldLocCopy.Menu;
            loc.Schedule = oldLocCopy.Schedule;
            loc.SupplyLinks = oldLocCopy.SupplyLinks;
            loc.Employees = oldLocCopy.Employees;
            loc.LocationId = oldLocCopy.LocationId;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newLoc.Should().BeEquivalentTo(oldLocCopy);
            newLoc.Should().NotBeEquivalentTo(loc);
        }

        [TestMethod]
        public async Task UpdateLocation_ManagementsNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var locId = GetFirstLocationId;

            /* Save a copy of the original entry to compare against*/
            var oldLoc = await _context.Locations.FindAsync(locId);
            var oldLocCopy = new Location
            {
                LocationId = oldLoc.LocationId,
                Schedule = oldLoc.Schedule,
                ScheduleId = oldLoc.ScheduleId,
                State = oldLoc.State,
                AbreviatedCountry = oldLoc.AbreviatedCountry,
                Street = oldLoc.Street,
                SupplyLinks = oldLoc.SupplyLinks,
                AbreviatedState = oldLoc.AbreviatedState,
                City = oldLoc.City,
                OpenSince = oldLoc.OpenSince,
                Country = oldLoc.Country,
                Employees = oldLoc.Employees,
                Managements = oldLoc.Managements,
                Menu = oldLoc.Menu,
                MenuId = oldLoc.MenuId
            };

            var loc = new Location
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "YY",
                City = "Newtopia",
                Street = "808 Upstream Lane",
                Country = "Updaya",
                State = "Freshens",
                OpenSince = oldLoc.OpenSince.AddDays(-10),
                MenuId = await _context.Menus
                    .Select(m => m.MenuId)
                    .FirstOrDefaultAsync(m => m != oldLoc.MenuId),
                ScheduleId = await _context.Schedules
                    .Select(s => s.ScheduleId)
                    .FirstOrDefaultAsync(s => s != oldLoc.ScheduleId),
                Managements = new Management[] {new Management()}
            };
            var locContent = new StringContent(JsonConvert.SerializeObject(loc),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + locId, locContent);

            /* Retrieve the (allegedly) updated loc*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + locId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newLoc = JsonConvert.DeserializeObject(newCont, typeof(Location));

            /* Complete the loc we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            loc.Managements = oldLocCopy.Managements;
            loc.Menu = oldLocCopy.Menu;
            loc.Schedule = oldLocCopy.Schedule;
            loc.SupplyLinks = oldLocCopy.SupplyLinks;
            loc.Employees = oldLocCopy.Employees;
            loc.LocationId = oldLocCopy.LocationId;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newLoc.Should().BeEquivalentTo(oldLocCopy);
            newLoc.Should().NotBeEquivalentTo(loc);
        }

        [TestMethod]
        public async Task UpdateLocation_SupplyLinksNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var locId = GetFirstLocationId;

            /* Save a copy of the original entry to compare against*/
            var oldLoc = await _context.Locations.FindAsync(locId);
            var oldLocCopy = new Location
            {
                LocationId = oldLoc.LocationId,
                Schedule = oldLoc.Schedule,
                ScheduleId = oldLoc.ScheduleId,
                State = oldLoc.State,
                AbreviatedCountry = oldLoc.AbreviatedCountry,
                Street = oldLoc.Street,
                SupplyLinks = oldLoc.SupplyLinks,
                AbreviatedState = oldLoc.AbreviatedState,
                City = oldLoc.City,
                OpenSince = oldLoc.OpenSince,
                Country = oldLoc.Country,
                Employees = oldLoc.Employees,
                Managements = oldLoc.Managements,
                Menu = oldLoc.Menu,
                MenuId = oldLoc.MenuId
            };

            var loc = new Location
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "YY",
                City = "Newtopia",
                Street = "808 Upstream Lane",
                Country = "Updaya",
                State = "Freshens",
                OpenSince = oldLoc.OpenSince.AddDays(-10),
                MenuId = await _context.Menus
                    .Select(m => m.MenuId)
                    .FirstOrDefaultAsync(m => m != oldLoc.MenuId),
                ScheduleId = await _context.Schedules
                    .Select(s => s.ScheduleId)
                    .FirstOrDefaultAsync(s => s != oldLoc.ScheduleId),
                SupplyLinks = new SupplyLink[] {new SupplyLink()}
            };
            var locContent = new StringContent(JsonConvert.SerializeObject(loc),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + locId, locContent);

            /* Retrieve the (allegedly) updated loc*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + locId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newLoc = JsonConvert.DeserializeObject(newCont, typeof(Location));

            /* Complete the loc we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            loc.Managements = oldLocCopy.Managements;
            loc.Menu = oldLocCopy.Menu;
            loc.Schedule = oldLocCopy.Schedule;
            loc.SupplyLinks = oldLocCopy.SupplyLinks;
            loc.Employees = oldLocCopy.Employees;
            loc.LocationId = oldLocCopy.LocationId;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newLoc.Should().BeEquivalentTo(oldLocCopy);
            newLoc.Should().NotBeEquivalentTo(loc);
        }

        #endregion

        #region Deletes

        [TestMethod]
        public async Task DeleteLocation_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var badId = await _context.Locations.CountAsync() + 1;

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + badId);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task DeleteLocation_LastWithNoRelations_ShouldDelete()
        {
            // Arrange
            var locId = await _context.Locations.CountAsync() + 1;
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
            await _context.Locations.AddAsync(loc);
            await _context.SaveChangesAsync();

            var delCountry = loc.Country;

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + locId);
            var locExists = await _context.Locations.AnyAsync(l => l.Country==delCountry);

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            locExists.Should().BeFalse();
        }

        [DataTestMethod]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public async Task DeleteLocation_HasRelations_ShouldTurnToBlank(bool isCap, bool isFirst)
        {
            // Arrange
            var locId = 0;
            if (!isCap && !isFirst) locId = 2;
            else
            {
                if (isFirst) locId = GetFirstLocationId;
                else locId = await _context.Locations.CountAsync();
            }

            if (locId == 0) return;

            var location = await _context.Locations.FindAsync(locId);
            var locCopy = new Location
            {
                AbreviatedCountry = location.AbreviatedCountry,
                AbreviatedState = location.AbreviatedState,
                Country = location.Country,
                State = location.State,
                City = location.City,
                Street = location.Street,
                ScheduleId = location.ScheduleId,
                MenuId = location.MenuId,
                OpenSince = location.OpenSince
            };
            location = null;

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + locId);
            var getRes = await _client.GetAsync(_client.BaseAddress + "/" + locId + "/basic");
            var getCont = await getRes.Content.ReadAsStringAsync();
            var getLoc = JsonConvert.DeserializeObject(getCont, typeof(Location)) as Location;


            var fixLoc = new StringContent(JsonConvert.SerializeObject(locCopy),
                    Encoding.UTF8, "application/json");
            var fixRes = (HttpResponseMessage)null;
            // Assert
            try
            {
                result.IsSuccessStatusCode.Should().BeTrue();
                getLoc.AbreviatedCountry.Should().Be("XX");
                getLoc.AbreviatedState.Should().Be("XX");
                getLoc.Country.Should().Contain("Empty");
                getLoc.State.Should().Be("None");
                getLoc.City.Should().Be("None");
                getLoc.Street.Should().Be("None");
                getLoc.MenuId.Should().Be(1);
                getLoc.ScheduleId.Should().Be(1);
                getLoc.OpenSince.Should().Be(new DateTime());
            }

            catch (Exception)
            {
                Debug.Write("Failed");
            }
                
            finally
            {
                //Clean-up
                fixRes = await _client.PutAsync(_client.BaseAddress + "/" + locId, fixLoc);

                //Check-up
                fixRes.IsSuccessStatusCode.Should().BeTrue();
            }

            ////Clean-up
            ////var fixLoc = new StringContent(JsonConvert.SerializeObject(locCopy),
            //    //Encoding.UTF8, "application/json");
            //fixRes = await _client.PutAsync(_client.BaseAddress + "/" + locId, fixLoc);

            ////Check-up
            //fixRes.IsSuccessStatusCode.Should().BeTrue();
        }

        [TestMethod]
        public async Task DeleteLocation_NotLastWithNoRelations_ShouldDelete()
        {
            // Arrange
            var locId = await _context.Locations.CountAsync() + 1;
            var delLoc = new Location 
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
            var emptyLoc = new Location 
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "XX",
                Country = "Replacementania",
                State = "None",
                City = "None",
                Street = "None",
                ScheduleId = 1,
                MenuId = 1,
                OpenSince = new DateTime()
            };
            var newCountry = emptyLoc.Country;

            await _context.Locations.AddRangeAsync(new Location[] { delLoc, emptyLoc });
            await _context.SaveChangesAsync();

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + locId);
            var getRes = await _client.GetAsync(_client.BaseAddress + "/" + locId + "/basic");
            var getCont = await getRes.Content.ReadAsStringAsync();
            var getLoc = JsonConvert.DeserializeObject(getCont, typeof(Location)) as Location;

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            getLoc.Country.Should().BeEquivalentTo(newCountry);

            // Clean-up
            await _client.DeleteAsync(_client.BaseAddress + "/" + locId);
        }

        [TestMethod]
        public async Task DeleteLocation_NotLastNoRel_ShouldMigrateRelations()
        {
            // Arrange
            var empId = await _context.Employees.CountAsync() + 1;
            var manId = await _context.Managements.CountAsync() + 1;
            var locId = await _context.Locations.CountAsync() + 1;

            var delLoc = new Location 
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
            var swapLoc = new Location 
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "XX",
                Country = "Replacetania",
                State = "None",
                City = "None",
                Street = "None",
                ScheduleId = 1,
                MenuId = 1,
                OpenSince = new DateTime()
            };
            var emp = new Employee
            {
                FirstName = "Delete",
                LastName = "Me",
                WeeklyHours = 30,
                LocationId = locId + 1,
                StartedOn = new DateTime(),
                PositionId = 1
            };
            var man = new Management { ManagerId = empId, LocationId = locId + 1 };

            await _context.Locations.AddRangeAsync(new Location[] { delLoc, swapLoc });
            await _context.SaveChangesAsync();

            await _context.Employees.AddAsync(emp);
            await _context.SaveChangesAsync();
            
            await _context.Managements.AddAsync(man);
            await _context.SaveChangesAsync();

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + locId);

            var getManRes = await _client.GetAsync("http://localhost/api/managements/" +
                manId + "/basic");
            var getManCont = await getManRes.Content.ReadAsStringAsync();
            var getMan = JsonConvert.DeserializeObject(getManCont,
                typeof(Management)) as Management;

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            getMan.LocationId.Should().Be(locId);

            // Clean-up
            await _client.DeleteAsync("http://localhost/api/managements/" + manId);
            await _client.DeleteAsync("http://localhost/api/employees/" + empId);
            await _client.DeleteAsync(_client.BaseAddress + "/" + locId);
        }

        #endregion

        #region Creates

        [TestMethod]
        public async Task CreateLocation_HappyFlow_ShouldCreateAndReturnLocation()
        {
            // Arrange
            var location = new Location
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "YY",
                City = "Newtonville",
                Street = "12 Bight Post",
                Country = "Postania",
                State = "New County",
                OpenSince = DateTime.Today.AddDays(-4),
                MenuId = 1,
                ScheduleId = 1
            };
            var locCont = new StringContent(JsonConvert.SerializeObject(location),
                    Encoding.UTF8, "application/json");
            var locId = await _context.Locations.CountAsync() + 1;

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, locCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();

            // Clean-up
            await _client.DeleteAsync(_client.BaseAddress + "/" + locId);
        }

        [TestMethod]
        public async Task CreateLocation_TriesToSetId_ShouldReturnBadRequest()
        {
            // Arrange
            var locId = await _context.Locations.CountAsync() + 1;
            var location = new Location
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "YY",
                City = "Newtonville",
                Street = "12 Bight Post",
                Country = "Postania",
                State = "New County",
                OpenSince = DateTime.Today.AddDays(-4),
                MenuId = 1,
                ScheduleId = 1,
                LocationId=locId
            };
            var locCont = new StringContent(JsonConvert.SerializeObject(location),
                    Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, locCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [DataTestMethod]
        #region Rows
        [DataRow(null, "B8", "Digne-les-Bains", "279 Corben Terrace", "France", "Provence-Alpes-Côte d'Azur")]
        [DataRow("FR", null, "Digne-les-Bains", "279 Corben Terrace", "France", "Provence-Alpes-Côte d'Azur")]
        [DataRow("FR", "B8", null, "279 Corben Terrace", "France", "Provence-Alpes-Côte d'Azur")]
        [DataRow("FR", "B8", "Digne-les-Bains", null, "France", "Provence-Alpes-Côte d'Azur")]
        [DataRow("FR", "B8", "Digne-les-Bains", "279 Corben Terrace", null, "Provence-Alpes-Côte d'Azur")]
        [DataRow("FR", "B8", "Digne-les-Bains", "279 Corben Terrace", "France", null)]
        [DataRow("", "B8", "Digne-les-Bains", "279 Corben Terrace", "France", "Provence-Alpes-Côte d'Azur")]
        [DataRow("FR", "", "Digne-les-Bains", "279 Corben Terrace", "France", "Provence-Alpes-Côte d'Azur")]
        [DataRow("FR", "B8", "", "279 Corben Terrace", "France", "Provence-Alpes-Côte d'Azur")]
        [DataRow("FR", "B8", "Digne-les-Bains", "", "France", "Provence-Alpes-Côte d'Azur")]
        [DataRow("FR", "B8", "Digne-les-Bains", "279 Corben Terrace", "", "Provence-Alpes-Côte d'Azur")]
        [DataRow("FR", "B8", "Digne-les-Bains", "279 Corben Terrace", "France", "")]
        [DataRow(" ", "B8", "Digne-les-Bains", "279 Corben Terrace", "France", "Provence-Alpes-Côte d'Azur")]
        [DataRow("FR", " ", "Digne-les-Bains", "279 Corben Terrace", "France", "Provence-Alpes-Côte d'Azur")]
        [DataRow("FR", "B8", " ", "279 Corben Terrace", "France", "Provence-Alpes-Côte d'Azur")]
        [DataRow("FR", "B8", "Digne-les-Bains", " ", "France", "Provence-Alpes-Côte d'Azur")]
        [DataRow("FR", "B8", "Digne-les-Bains", "279 Corben Terrace", " ", "Provence-Alpes-Côte d'Azur")]
        [DataRow("FR", "B8", "Digne-les-Bains", "279 Corben Terrace", "France", " ")]
        [DataRow("FRAAA", "B8", "Digne-les-Bains", "279 Corben Terrace", "France", "Provence-Alpes-Côte d'Azur")]
        [DataRow("FR", "B8AAA", "Digne-les-Bains", "279 Corben Terrace", "France", "Provence-Alpes-Côte d'Azur")]
        [DataRow("FR", "B8", "012345678911234567892123456789312345678941234567895",
            "279 Corben Terrace", "France", "Provence-Alpes-Côte d'Azur")]
        [DataRow("FR", "B8", "Digne-les-Bains", "012345678911234567892123456789312345678941234567895",
            "France", "Provence-Alpes-Côte d'Azur")]
        [DataRow("FR", "B8", "Digne-les-Bains", "279 Corben Terrace", "012345678911234567892123456789312345678941234567895",
            "Provence -Alpes-Côte d'Azur")]
        [DataRow("FR", "B8", "Digne-les-Bains", "279 Corben Terrace", "France",
            "012345678911234567892123456789312345678941234567895")]
        #endregion
        public async Task CreateLocation_BadAddresses_ShouldReturnBadRequest
            (string abvCount, string abvState, string city, string str, string country, string state)
        {
            // Arrange
            var locId = await _context.Locations.CountAsync() + 1;
            var location = new Location
            {
                AbreviatedCountry = abvCount,
                AbreviatedState = abvState,
                City = city,
                Street = str,
                Country = country,
                State = state,
                OpenSince = DateTime.Today.AddDays(-4),
                MenuId = 1,
                ScheduleId = 1
            };
            var locCont = new StringContent(JsonConvert.SerializeObject(location),
                    Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, locCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateLocation_DuplicateAddress_ShouldReturnBadRequest()
        {
            // Arrange
            var dupe = await _context.Locations.FirstOrDefaultAsync();

            var locId = await _context.Locations.CountAsync() + 1;
        var location = new Location
        {
            AbreviatedCountry = dupe.AbreviatedCountry,
            AbreviatedState = dupe.AbreviatedState,
            City = dupe.City,
            Street = dupe.Street,
            Country = "Postania",
            State = "New County",
            OpenSince = DateTime.Today.AddDays(-4),
            MenuId = 1,
            ScheduleId = 1
        };
        var locCont = new StringContent(JsonConvert.SerializeObject(location),
                Encoding.UTF8, "application/json");

        // Act
        var result = await _client.PostAsync(_client.BaseAddress, locCont);

        // Assert
        result.IsSuccessStatusCode.Should().BeFalse();
        result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
    }

        [TestMethod]
        public async Task CreateLocation_NoOpeningDate_ShouldReturnBadRequest()
        {
            // Arrange
            var locId = await _context.Locations.CountAsync() + 1;
            var location = new Location
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "YY",
                City = "Newtonville",
                Street = "12 Bight Post",
                Country = "Postania",
                State = "New County",
                MenuId = 1,
                ScheduleId = 1
            };
            var locCont = new StringContent(JsonConvert.SerializeObject(location),
                    Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, locCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateLocation_BadMenuId_ShouldReturnBadRequest()
        {
            // Arrange
            var badMenuId = await _context.Menus.CountAsync() + 1;
            var locId = await _context.Locations.CountAsync() + 1;
            var location = new Location
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "YY",
                City = "Newtonville",
                Street = "12 Bight Post",
                Country = "Postania",
                State = "New County",
                OpenSince = DateTime.Today.AddDays(-4),
                ScheduleId = 1,
                MenuId = badMenuId
            };
            var locCont = new StringContent(JsonConvert.SerializeObject(location),
                    Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, locCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateLocation_BadScheduleId_ShouldReturnBadRequest()
        {
            // Arrange
            var badSchId = await _context.Schedules.CountAsync() + 1;
            var locId = await _context.Locations.CountAsync() + 1;
            var location = new Location
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "YY",
                City = "Newtonville",
                Street = "12 Bight Post",
                Country = "Postania",
                State = "New County",
                OpenSince = DateTime.Today.AddDays(-4),
                MenuId = 1,
                ScheduleId = badSchId
            };
            var locCont = new StringContent(JsonConvert.SerializeObject(location),
                    Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, locCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateLocation_HasMenu_ShouldReturnBadRequest()
        {
            // Arrange
            var locId = await _context.Locations.CountAsync() + 1;
            var location = new Location
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "YY",
                City = "Newtonville",
                Street = "12 Bight Post",
                Country = "Postania",
                State = "New County",
                OpenSince = DateTime.Today.AddDays(-4),
                MenuId = 1,
                ScheduleId = 1,
                Menu = new Menu()
            };
            var locCont = new StringContent(JsonConvert.SerializeObject(location),
                    Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, locCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateLocation_HasSchedule_ShouldReturnBadRequest()
        {
            // Arrange
            var locId = await _context.Locations.CountAsync() + 1;
            var location = new Location
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "YY",
                City = "Newtonville",
                Street = "12 Bight Post",
                Country = "Postania",
                State = "New County",
                OpenSince = DateTime.Today.AddDays(-4),
                MenuId = 1,
                ScheduleId = 1,
                Schedule = new Schedule()
            };
            var locCont = new StringContent(JsonConvert.SerializeObject(location),
                    Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, locCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateLocation_ManagementsNull_ShouldReturnBadRequest()
        {
            // Arrange
            var locId = await _context.Locations.CountAsync() + 1;
            var location = new Location
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "YY",
                City = "Newtonville",
                Street = "12 Bight Post",
                Country = "Postania",
                State = "New County",
                OpenSince = DateTime.Today.AddDays(-4),
                MenuId = 1,
                ScheduleId = 1,
                Managements = null
            };
            var locCont = new StringContent(JsonConvert.SerializeObject(location),
                    Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, locCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateLocation_SupplyLinksNull_ShouldReturnBadRequest()
        {
            // Arrange
            var locId = await _context.Locations.CountAsync() + 1;
            var location = new Location
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "YY",
                City = "Newtonville",
                Street = "12 Bight Post",
                Country = "Postania",
                State = "New County",
                OpenSince = DateTime.Today.AddDays(-4),
                MenuId = 1,
                ScheduleId = 1,
                SupplyLinks = null
            };
            var locCont = new StringContent(JsonConvert.SerializeObject(location),
                    Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, locCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateLocation_SupplyLinksNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var locId = await _context.Locations.CountAsync() + 1;
            var location = new Location
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "YY",
                City = "Newtonville",
                Street = "12 Bight Post",
                Country = "Postania",
                State = "New County",
                OpenSince = DateTime.Today.AddDays(-4),
                MenuId = 1,
                ScheduleId = 1,
                SupplyLinks = new SupplyLink[] {new SupplyLink()}
            };
            var locCont = new StringContent(JsonConvert.SerializeObject(location),
                    Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, locCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateLocation_ManagementsNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var locId = await _context.Locations.CountAsync() + 1;
            var location = new Location
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "YY",
                City = "Newtonville",
                Street = "12 Bight Post",
                Country = "Postania",
                State = "New County",
                OpenSince = DateTime.Today.AddDays(-4),
                MenuId = 1,
                ScheduleId = 1,
                Managements = new Management[] {new Management()}
            };
            var locCont = new StringContent(JsonConvert.SerializeObject(location),
                    Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, locCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        #endregion

        //Helpers

        [TestMethod]
        [Ignore]
        public async Task GetCommonCountryWorks()
        {
            var result = await GetCommonCountry();

            result.Should().NotBeEquivalentTo("ERROR");
        }

        private int GenBadLocationId => _context.Locations
                .AsEnumerable()
                .Select(l => l.LocationId)
                .LastOrDefault() + 1;

        private int GetFirstLocationId
            => _context.Locations.Select(l => l.LocationId).FirstOrDefault();

        private async Task<string> GetCommonCountry()
        {
            var countries = new List<(string country, int count)>();
            var i = 0;
            while(true)
            {
                var loc = await _context.Locations
                    .Select(l => l.Country)
                    .Skip(i)
                    .FirstOrDefaultAsync();
                if (loc == null) break;
                i++;
                if (countries.Any(c => c.country == loc))
                {
                    var existing = countries.SingleOrDefault(c => c.country == loc);
                    if (existing.count == 2) return loc;
                    countries.Add((loc, existing.count + 1));
                    countries.Remove(existing);
                }
                else countries.Add((loc, 1));
            }
            return "ERROR";
        }
    }
}
