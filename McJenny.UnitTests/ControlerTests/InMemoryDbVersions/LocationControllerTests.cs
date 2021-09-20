using FluentAssertions;
using McJenny.WebAPI.Controllers;
using McJenny.WebAPI.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace McJenny.UnitTests.ControlerTests.InMemoryDbVersions
{
    [TestClass]
    public class LocationControllerTests
    {
        private static FoodChainsDbContext _context;
        private static LocationsController _controller;
        private static ILogger<LocationsController> _logger;

        [ClassInitialize]
        public static void Init(TestContext testContext)
        {
            _logger = (new Mock<ILogger<LocationsController>>()).Object;
            Setup();
        }

        [TestInitialize]
        public void InitTest() => Setup();

        #region Gets

        #region Basic

        [TestMethod]
        public async Task GetLocations_HappyFlow_ShouldReturnAllLocations()
        {
            // Arrange
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
                     l.MenuId,
                     OpenSince = l.OpenSince.ToShortDateString(),
                     l.Schedule.TimeTable
                 })
                .Take(20)
                .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetLocations()).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetLocation_HappyFlow_ShouldReturnLocation()
        {
            // Arrange
            var expected =  await _context.Locations
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
            .FirstOrDefaultAsync(l => l.LocationId == GetFirstLocationId);

            // Act
            var result = (object)(await _controller.GetLocation(GetFirstLocationId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetLocation_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var badId = GenBadLocationId;

            // Act
            var result = (await _controller.GetLocation(badId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetLocationBasic_HappyFlow_ShouldReturnLocation()
        {
            // Arrange
            var expected = await GetFirstLocation();

            // Act
            var result = (await _controller.GetLocationBasic(GetFirstLocationId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetLocationBasic_InexistentId_ShouldReturnNotFound()
        {
            // Act
            var result = (await _controller.GetLocationBasic(GenBadLocationId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetEmployees_HappyFlow_ShouldReturnLocationsEmployees()
        {
            // Arrange
                        var location = await GetFirstLocation();

            var positions = await _context.Positions.Select(p => new { p.PositionId, p.Title }).ToArrayAsync();

            var employees = await _context.Employees.Where(e => e.LocationId == location.LocationId).ToArrayAsync();

            var expected = (object) employees.Select(e => new
            {
                e.EmployeeId,
                e.FirstName,
                e.LastName,
                positions.FirstOrDefault(p => p.PositionId == e.PositionId).Title
            }).ToArray();

            // Act
            var result = (object)(await _controller.GetEmployees(location.LocationId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetEmployees_InexistentId_ShouldReturnNotFound()
        {
            // Act
            var result = (await _controller.GetEmployees(GenBadLocationId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetSuppliers_HappyFlow_ShouldReturnLocationSuppliers()
        {
            // Arrange
                        var location = await GetFirstLocation();

            var supplierIds = await _context.SupplyLinks.Where(l => l.LocationId == location.LocationId).Select(l => l.SupplierId).ToListAsync();
            var expected = (object) await _context.Suppliers.Where(s => supplierIds.Contains(s.SupplierId))
                .Select(s => new
                {
                    s.SupplierId,
                    s.Name,
                    s.AbreviatedCountry,
                    s.AbreviatedState,
                    s.City
                })
                .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetSuppliers(location.LocationId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSuppliers_InexistentId_ShouldReturnNotFoind()
        {
            // Act
            var result = (await _controller.GetSuppliers(GenBadLocationId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetLinks_HappyFlow_ShouldRetunLocationLinks()
        {
            // Arrange
                        var location = await GetFirstLocation();

            var links = await _context.SupplyLinks
                .Where(sl => sl.LocationId == location.LocationId)
                .Select(sl => new { sl.SupplierId, sl.SupplyCategoryId })
                .ToArrayAsync();

            var categories = await _context.SupplyCategories.Select(c => new
            {
                c.SupplyCategoryId,
                c.Name
            }).ToArrayAsync();

            var supplierIds = new List<int>();
            foreach (var link in links) if (!supplierIds.Contains(link.SupplierId)) supplierIds.Add(link.SupplierId);

            var suppliers = await _context.Suppliers.Where(s => supplierIds.Contains(s.SupplierId))
                .Select(s => new
                {
                    s.SupplierId,
                    s.Name,
                    s.AbreviatedCountry,
                    s.AbreviatedState,
                    s.City
                }).ToArrayAsync();

            var query = new string[links.Length];
            for (int i = 0; i < query.Length; i++)
            {
                var supplier = suppliers.SingleOrDefault(s => s.SupplierId == links[i].SupplierId);

                query[i] = string.Format("({0}) {1} supplied by {2}, {3}, {4}{5} ({6})",
                    links[i].SupplyCategoryId,
                     categories.SingleOrDefault(c => c.SupplyCategoryId == links[i].SupplyCategoryId).Name,
                     supplier.Name, supplier.AbreviatedCountry,
                     supplier.AbreviatedState == "N/A" ? string.Empty : supplier.AbreviatedState + ", ",
                     supplier.City, links[i].SupplierId
                    );
            }

            var expected = (object)query;

            // Act
            var result = (object)(await _controller.GetLinks(location.LocationId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetLinks_InexistentId_ShouldReturnNotFound()
        {
            // Act
            var result = (await _controller.GetLinks(GenBadLocationId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetItems_HappyFlow_ShouldReturnLocationItems()
        {
            // Arrange
            var location = await GetFirstLocation();

            var dishIds = await _context.MenuItems
                .Where(i => i.MenuId == location.MenuId)
                .Select(i => i.DishId)
                .ToArrayAsync();

            var expected = (object)(await _context.Dishes.Where(d => dishIds.Contains(d.DishId)).Select(d => d.Name).ToArrayAsync());

            // Act
            var result = (object)(await _controller.GetItems(location.LocationId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetItems_InexistentId_ShouldReturnNotFound()
        {
            // Act
            var result = (await _controller.GetItems(GenBadLocationId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        #endregion


        #region Advanced

        [TestMethod]
        public async Task GetLocationsFiltered_AbrCountryMatch_ShouldReturnFiltered()
        {
            // Arrange
            var filter = await _context.Locations.Select(l => l.AbreviatedCountry).FirstOrDefaultAsync();

            var expected = (object)await _context.Locations
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
                 .Where(l => l.AbreviatedCountry.ToUpper()
                        .Contains(filter.ToUpper()))
                 .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetLocationsFiltered(filter)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetLocationsFiltered_AbrStateMatch_ShouldReturnFiltered()
        {
            // Arrange
            var filter = await _context.Locations.Select(l => l.AbreviatedState).FirstOrDefaultAsync();

            var expected = (object)await _context.Locations
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
                 .Where(l => l.AbreviatedState.ToUpper()
                        .Contains(filter.ToUpper()))
                 .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetLocationsFiltered(filter)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetLocationsFiltered_CountryMatch_ShouldReturnFiltered()
        {
            // Arrange
            var filter = await _context.Locations.Select(l => l.Country).FirstOrDefaultAsync();

            var expected = (object)await _context.Locations
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

            // Act
            var result = (object)(await _controller.GetLocationsFiltered(filter)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetLocationsFiltered_StateMatch_ShouldReturnFiltered()
        {
            // Arrange
            var filter = await _context.Locations.Select(l => l.State).FirstOrDefaultAsync();

            var expected = (object)await _context.Locations
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

            // Act
            var result = (object)(await _controller.GetLocationsFiltered(filter)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetLocationsFiltered_CityMatch_ShouldReturnFiltered()
        {
            // Arrange
            var filter = await _context.Locations.Select(l => l.City).FirstOrDefaultAsync();

            var expected = (object)await _context.Locations
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
                 .Where(l =>l.City.ToUpper()
                        .Contains(filter.ToUpper()))
                 .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetLocationsFiltered(filter)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetLocationsFiltered_StreetMatch_ShouldReturnFiltered()
        {
            // Arrange
            var filter = await _context.Locations.Select(l => l.Street).FirstOrDefaultAsync();

            var expected = (object)await _context.Locations
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

            // Act
            var result = (object)(await _controller.GetLocationsFiltered(filter)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetLocationsFiltered_NoMatch_ShouldReturnEmpty()
        {
            // Arrange
            var filter = "fgdg";

            var expected = (object)new dynamic[0];

            // Act
            var result = (object)(await _controller.GetLocationsFiltered(filter)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetLocationsPaged_HappyFlow_ShouldReturnPaged()
        {
            // Arrange
            var pgSz = 2;
            var pgInd = 1;

            var expected = (object)await _context.Locations
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

            // Act
            var result = (object)(await _controller.GetLocationsPaged(pgInd, pgSz)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetLocationsPaged_EndPageIncomplete_ShouldReturnPaged()
        {
            // Arrange
            var LocationCount = await _context.Locations.CountAsync();
            var pgSz = 1;

            while (true)
            {
                if (LocationCount % pgSz == 0) pgSz++;
                else break;
            }

            var pgInd = LocationCount / (pgSz - 1);

            var expected = (object)await _context.Locations
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

            // Act
            var call = (await _controller.GetLocationsPaged(pgInd, pgSz)).Value;
            var result = (object)call;
            var resCount = (result as IEnumerable<dynamic>).Count();

            // Assert
            result.Should().BeEquivalentTo(expected);
            resCount.Should().BeLessThan(pgSz);
        }

        [TestMethod]
        public async Task GetLocationsPaged_SizeOver_ShouldReturnPaged()
        {
            // Arrange
            var LocationCount = await _context.Locations.CountAsync();
            var pgSz = LocationCount + 1;
            var pgInd = 1;

            var expected = (object)await _context.Locations
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

            // Act
            var result = (object)(await _controller.GetLocationsPaged(pgInd, pgSz)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetLocationsPaged_IndexOver_ShouldReturnPaged()
        {
            // Arrange
            var LocationCount = await _context.Locations.CountAsync();
            var pgInd = LocationCount + 1;
            var pgSz = 1;

            var expected = (object)await _context.Locations
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

            // Act
            var result = (object)(await _controller.GetLocationsPaged(pgInd, pgSz)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetLocationsSorted_ByCountry_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "country";

            var expected = (object)await _context.Locations
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

            // Act
            var result = (object)(await _controller.GetLocationsSorted(sortBy, false)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetLocationsSorted_ByState_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "state";

            var expected = (object)await _context.Locations
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

            // Act
            var result = (object)(await _controller.GetLocationsSorted(sortBy, false)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetLocationsSorted_ByCity_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "city";

            var expected = (object)await _context.Locations
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

            // Act
            var result = (object)(await _controller.GetLocationsSorted(sortBy, false)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetLocationsSorted_ByStreet_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "street";

            var expected = (object)await _context.Locations
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

            // Act
            var result = (object)(await _controller.GetLocationsSorted(sortBy, false)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetLocationsSorted_ByMenu_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "menu";

            var expected = (object)await _context.Locations
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

            // Act
            var result = (object)(await _controller.GetLocationsSorted(sortBy, false)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetLocationsSorted_Desc_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "country";

            var expected = (object)await _context.Locations
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
                        .OrderByDescending(l => l.AbreviatedCountry)
                        .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetLocationsSorted(sortBy, true)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetLocationsSorted_GarbledText_ShouldDefaultoIdSort()
        {
            // Arrange
            var sortBy = "asd";

            var expected = (object)await _context.Locations
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

            // Act
            var result = (object)(await _controller.GetLocationsSorted(sortBy, false)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetLocationsOpen_Before_ShouldOpenedBefore()
        {
            // Arrange
            var date = await _context.Locations
                .Select(l => l.OpenSince)
                .OrderBy(l => l)
                .Skip(1)
                .FirstOrDefaultAsync();

            var expected = (object)await _context.Locations
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
                        .Where(l=>l.OpenSince<date)
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
                        })
                        .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetLocationsOpen(date, true)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetLocationsOpen_After_ShouldOpenedSince()
        {
            // Arrange
            var date = await _context.Locations
                .Select(l => l.OpenSince)
                .OrderBy(l => l)
                .Skip(1)
                .FirstOrDefaultAsync();

            var expected = (object)await _context.Locations
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
                        })
                        .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetLocationsOpen(date, false)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        #endregion

        #endregion

        #region Creates

        [TestMethod]
        public async Task CreateLocation_HappyFlow_ShouldCreateAndReturnLocation()
        {
            // Arrange
            var location = new Location
            {
                AbreviatedCountry = "FR",
                AbreviatedState = "B8",
                City = "Digne-les-Bains",
                Street = "279 Corben Terrace",
                Country = "France",
                State = "Provence-Alpes-Côte d'Azur",
                OpenSince = new DateTime(2018, 1, 29),
                MenuId = await _context.Menus.Select(m => m.MenuId).FirstOrDefaultAsync(),
                ScheduleId = await _context.Schedules.Select(s => s.ScheduleId).FirstOrDefaultAsync()
            };

            // Act
            await _controller.CreateLocation(location);
            var result = (object)(await _controller.GetLocationBasic(location.LocationId)).Value;

            var expected = (object)location;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task CreateLocation_TriesToSetId_ShouldReturnBadRequest()
        {
            // Arrange
            var location = new Location
            {
                LocationId = await _context.Locations.Select(l=>l.LocationId).LastOrDefaultAsync()+1,
                AbreviatedCountry = "FR",
                AbreviatedState = "B8",
                City = "Digne-les-Bains",
                Street = "279 Corben Terrace",
                Country = "France",
                State = "Provence-Alpes-Côte d'Azur",
                OpenSince = new DateTime(2018, 1, 29),
                MenuId = await _context.Menus.Select(m => m.MenuId).FirstOrDefaultAsync(),
                ScheduleId = await _context.Schedules.Select(s => s.ScheduleId).FirstOrDefaultAsync()
            };

            // Act
            var result = (await _controller.CreateLocation(location)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [DataTestMethod]
        #region Rows
        [DataRow( null, "B8", "Digne-les-Bains", "279 Corben Terrace", "France", "Provence-Alpes-Côte d'Azur")]
        [DataRow( "FR", null, "Digne-les-Bains", "279 Corben Terrace", "France", "Provence-Alpes-Côte d'Azur")]
        [DataRow( "FR", "B8", null, "279 Corben Terrace", "France", "Provence-Alpes-Côte d'Azur")]
        [DataRow( "FR", "B8", "Digne-les-Bains", null, "France", "Provence-Alpes-Côte d'Azur")]
        [DataRow( "FR", "B8", "Digne-les-Bains", "279 Corben Terrace", null, "Provence-Alpes-Côte d'Azur")]
        [DataRow( "FR", "B8", "Digne-les-Bains", "279 Corben Terrace", "France", null)]
        [DataRow( "", "B8", "Digne-les-Bains", "279 Corben Terrace", "France", "Provence-Alpes-Côte d'Azur")]
        [DataRow( "FR", "", "Digne-les-Bains", "279 Corben Terrace", "France", "Provence-Alpes-Côte d'Azur")]
        [DataRow( "FR", "B8", "", "279 Corben Terrace", "France", "Provence-Alpes-Côte d'Azur")]
        [DataRow( "FR", "B8", "Digne-les-Bains", "", "France", "Provence-Alpes-Côte d'Azur")]
        [DataRow( "FR", "B8", "Digne-les-Bains", "279 Corben Terrace", "", "Provence-Alpes-Côte d'Azur")]
        [DataRow( "FR", "B8", "Digne-les-Bains", "279 Corben Terrace", "France", "")]
        [DataRow( " ", "B8", "Digne-les-Bains", "279 Corben Terrace", "France", "Provence-Alpes-Côte d'Azur")]
        [DataRow( "FR", " ", "Digne-les-Bains", "279 Corben Terrace", "France", "Provence-Alpes-Côte d'Azur")]
        [DataRow( "FR", "B8", " ", "279 Corben Terrace", "France", "Provence-Alpes-Côte d'Azur")]
        [DataRow( "FR", "B8", "Digne-les-Bains", " ", "France", "Provence-Alpes-Côte d'Azur")]
        [DataRow( "FR", "B8", "Digne-les-Bains", "279 Corben Terrace", " ", "Provence-Alpes-Côte d'Azur")]
        [DataRow( "FR", "B8", "Digne-les-Bains", "279 Corben Terrace", "France", " ")]
        [DataRow( "FRAAA", "B8", "Digne-les-Bains", "279 Corben Terrace", "France", "Provence-Alpes-Côte d'Azur")]
        [DataRow( "FR", "B8AAA", "Digne-les-Bains", "279 Corben Terrace", "France", "Provence-Alpes-Côte d'Azur")]
        [DataRow( "FR", "B8", "012345678911234567892123456789312345678941234567895",
            "279 Corben Terrace", "France", "Provence-Alpes-Côte d'Azur")]
        [DataRow( "FR", "B8", "Digne-les-Bains", "012345678911234567892123456789312345678941234567895",
            "France", "Provence-Alpes-Côte d'Azur")]
        [DataRow( "FR", "B8", "Digne-les-Bains", "279 Corben Terrace", "012345678911234567892123456789312345678941234567895", 
            "Provence -Alpes-Côte d'Azur")]
        [DataRow( "FR", "B8", "Digne-les-Bains", "279 Corben Terrace", "France",
            "012345678911234567892123456789312345678941234567895")]
        #endregion
        public async Task CreateLocation_BadAddresses_ShouldReturnBadRequest
            (string abvCount, string abvState, string city, string str, string country, string state)
        {
            // Arrange
            var location = new Location
            {
                AbreviatedCountry = abvCount,
                AbreviatedState = abvState,
                City = city,
                Street = str,
                Country = country,
                State = state,
                OpenSince = new DateTime(2018, 1, 29),
                MenuId = await _context.Menus.Select(m => m.MenuId).FirstOrDefaultAsync(),
                ScheduleId = await _context.Schedules.Select(s => s.ScheduleId).FirstOrDefaultAsync()
            };

            // Act
            var result = (await _controller.CreateLocation(location)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateLocation_DuplicateAddress_ShouldReturnBadRequest()
        {
            // Arrange
            var exAdr = await _context.Locations.Select(l => new
            { l.AbreviatedCountry, l.AbreviatedState, l.City, l.Street })
                .FirstOrDefaultAsync();

            var location = new Location
            {
                AbreviatedCountry = exAdr.AbreviatedCountry,
                AbreviatedState = exAdr.AbreviatedState,
                City = exAdr.City,
                Street = exAdr.Street,
                Country = "France",
                State = "Provence-Alpes-Côte d'Azur",
                OpenSince = new DateTime(2018, 1, 29),
                MenuId = await _context.Menus.Select(m => m.MenuId).FirstOrDefaultAsync(),
                ScheduleId = await _context.Schedules.Select(s => s.ScheduleId).FirstOrDefaultAsync()
            };

            // Act
            var result = (await _controller.CreateLocation(location)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateLocation_NoOpeningDate_ShouldReturnBadRequest()
        {
            // Arrange
            var location = new Location
            {
                AbreviatedCountry = "FR",
                AbreviatedState = "B8",
                City = "Digne-les-Bains",
                Street = "279 Corben Terrace",
                Country = "France",
                State = "Provence-Alpes-Côte d'Azur",
                MenuId = await _context.Menus.Select(m => m.MenuId).FirstOrDefaultAsync(),
                ScheduleId = await _context.Schedules.Select(s => s.ScheduleId).FirstOrDefaultAsync()
            };

            // Act
            var result = (await _controller.CreateLocation(location)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateLocation_BadMenuId_ShouldReturnBadRequest()
        {
            // Arrange
            var location = new Location
            {
                AbreviatedCountry = "FR",
                AbreviatedState = "B8",
                City = "Digne-les-Bains",
                Street = "279 Corben Terrace",
                Country = "France",
                State = "Provence-Alpes-Côte d'Azur",
                OpenSince = new DateTime(2018, 1, 29),
                MenuId = await _context.Menus.Select(m => m.MenuId).LastOrDefaultAsync()+1,
                ScheduleId = await _context.Schedules.Select(s => s.ScheduleId).FirstOrDefaultAsync()
            };

            // Act
            var result = (await _controller.CreateLocation(location)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateLocation_BadScheduleId_ShouldReturnBadRequest()
        {
            // Arrange
            var location = new Location
            {
                AbreviatedCountry = "FR",
                AbreviatedState = "B8",
                City = "Digne-les-Bains",
                Street = "279 Corben Terrace",
                Country = "France",
                State = "Provence-Alpes-Côte d'Azur",
                OpenSince = new DateTime(2018, 1, 29),
                MenuId = await _context.Menus.Select(m => m.MenuId).FirstOrDefaultAsync(),
                ScheduleId = await _context.Schedules.Select(s => s.ScheduleId).LastOrDefaultAsync()+1
            };

            // Act
            var result = (await _controller.CreateLocation(location)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateLocation_HasMenu_ShouldReturnBadRequest()
        {
            // Arrange
            var location = new Location
            {
                AbreviatedCountry = "FR",
                AbreviatedState = "B8",
                City = "Digne-les-Bains",
                Street = "279 Corben Terrace",
                Country = "France",
                State = "Provence-Alpes-Côte d'Azur",
                OpenSince = new DateTime(2018, 1, 29),
                Menu = new Menu(),
                MenuId = await _context.Menus.Select(m => m.MenuId).FirstOrDefaultAsync(),
                ScheduleId = await _context.Schedules.Select(s => s.ScheduleId).FirstOrDefaultAsync()
            };

            // Act
            var result = (await _controller.CreateLocation(location)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateLocation_HasSchedule_ShouldReturnBadRequest()
        {
            // Arrange
            var location = new Location
            {
                AbreviatedCountry = "FR",
                AbreviatedState = "B8",
                City = "Digne-les-Bains",
                Street = "279 Corben Terrace",
                Country = "France",
                State = "Provence-Alpes-Côte d'Azur",
                OpenSince = new DateTime(2018, 1, 29),
                MenuId = await _context.Menus.Select(m => m.MenuId).FirstOrDefaultAsync(),
                Schedule = new Schedule(),
                ScheduleId = await _context.Schedules.Select(s => s.ScheduleId).FirstOrDefaultAsync()
            };

            // Act
            var result = (await _controller.CreateLocation(location)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateLocation_ManagementsNull_ShouldReturnBadRequest()
        {
            // Arrange
            var location = new Location
            {
                AbreviatedCountry = "FR",
                AbreviatedState = "B8",
                City = "Digne-les-Bains",
                Street = "279 Corben Terrace",
                Country = "France",
                State = "Provence-Alpes-Côte d'Azur",
                OpenSince = new DateTime(2018, 1, 29),
                MenuId = await _context.Menus.Select(m => m.MenuId).FirstOrDefaultAsync(),
                ScheduleId = await _context.Schedules.Select(s => s.ScheduleId).FirstOrDefaultAsync(),
                Managements = null
            };

            // Act
            var result = (await _controller.CreateLocation(location)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateLocation_SupplyLinksNull_ShouldReturnBadRequest()
        {
            // Arrange
            var location = new Location
            {
                AbreviatedCountry = "FR",
                AbreviatedState = "B8",
                City = "Digne-les-Bains",
                Street = "279 Corben Terrace",
                Country = "France",
                State = "Provence-Alpes-Côte d'Azur",
                OpenSince = new DateTime(2018, 1, 29),
                MenuId = await _context.Menus.Select(m => m.MenuId).FirstOrDefaultAsync(),
                ScheduleId = await _context.Schedules.Select(s => s.ScheduleId).FirstOrDefaultAsync(),
                SupplyLinks = null
            };

            // Act
            var result = (await _controller.CreateLocation(location)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateLocation_SupplyLinksNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var location = new Location
            {
                AbreviatedCountry = "FR",
                AbreviatedState = "B8",
                City = "Digne-les-Bains",
                Street = "279 Corben Terrace",
                Country = "France",
                State = "Provence-Alpes-Côte d'Azur",
                OpenSince = new DateTime(2018, 1, 29),
                MenuId = await _context.Menus.Select(m => m.MenuId).FirstOrDefaultAsync(),
                ScheduleId = await _context.Schedules.Select(s => s.ScheduleId).FirstOrDefaultAsync(),
                SupplyLinks = new SupplyLink[] {new SupplyLink()}
            };

            // Act
            var result = (await _controller.CreateLocation(location)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateLocation_ManagementsNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var location = new Location
            {
                AbreviatedCountry = "FR",
                AbreviatedState = "B8",
                City = "Digne-les-Bains",
                Street = "279 Corben Terrace",
                Country = "France",
                State = "Provence-Alpes-Côte d'Azur",
                OpenSince = new DateTime(2018, 1, 29),
                MenuId = await _context.Menus.Select(m => m.MenuId).FirstOrDefaultAsync(),
                ScheduleId = await _context.Schedules.Select(s => s.ScheduleId).FirstOrDefaultAsync(),
                Managements = new Management[] {new Management()}
            };

            // Act
            var result = (await _controller.CreateLocation(location)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        #endregion

        #region Update

        [TestMethod]
        public async Task UpdateLocation_HappyFlow_ShouldCreateAndReturnLocation()
        {
            // Arrange
            var oldLoc = await _context.Locations.FindAsync(GetFirstLocationId);
            var oldLocCopy = new Location
            {
                LocationId = oldLoc.LocationId,
                Schedule = oldLoc.Schedule,
                ScheduleId = oldLoc.ScheduleId,
                State = oldLoc.State,
                Street = oldLoc.Street,
                SupplyLinks = oldLoc.SupplyLinks,
                AbreviatedState = oldLoc.AbreviatedState,
                OpenSince = oldLoc.OpenSince,
                AbreviatedCountry = oldLoc.AbreviatedCountry,
                City = oldLoc.City,
                Country = oldLoc.Country,
                Employees = oldLoc.Employees,
                Managements = oldLoc.Managements,
                Menu =oldLoc.Menu,
                MenuId = oldLoc.MenuId
            };

            var location = new Location
            {
                AbreviatedCountry = "FR",
                AbreviatedState = "B8",
                City = "Digne-les-Bains",
                Street = "279 Corben Terrace",
                Country = "France",
                State = "Provence-Alpes-Côte d'Azur",
                OpenSince = new DateTime(2018, 1, 29),
                MenuId = await _context.Menus.Select(m => m.MenuId).FirstOrDefaultAsync(),
                ScheduleId = await _context.Schedules.Select(s => s.ScheduleId).FirstOrDefaultAsync()
            };

            // Act
            await _controller.UpdateLocation(oldLoc.LocationId, location);
            var result = (await _controller.GetLocationBasic(oldLoc.LocationId)).Value;

            location.LocationId = oldLoc.LocationId;
            location.Employees = await _context.Employees
                .Where(e => e.LocationId == oldLoc.LocationId)
                .ToArrayAsync();
            location.Managements = await _context.Managements
                .Where(m => m.LocationId == oldLoc.LocationId)
                .ToArrayAsync();
            location.Menu = await _context.Menus.FindAsync(location.MenuId);
            location.Schedule = await _context.Schedules
                .FindAsync(location.ScheduleId);
            location.SupplyLinks = await _context.SupplyLinks
                .Where(l => l.LocationId == location.LocationId)
                .ToArrayAsync();

            // Assert
            result.Should().BeEquivalentTo(location);
            result.Should().NotBeEquivalentTo(oldLocCopy);
        }

        [TestMethod]
        public async Task UpdateLocation_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var oldLoc = await _context.Locations.FindAsync(GetFirstLocationId);
            var oldLocCopy = new Location
            {
                LocationId = oldLoc.LocationId,
                Schedule = oldLoc.Schedule,
                ScheduleId = oldLoc.ScheduleId,
                State = oldLoc.State,
                Street = oldLoc.Street,
                SupplyLinks = oldLoc.SupplyLinks,
                AbreviatedState = oldLoc.AbreviatedState,
                OpenSince = oldLoc.OpenSince,
                AbreviatedCountry = oldLoc.AbreviatedCountry,
                City = oldLoc.City,
                Country = oldLoc.Country,
                Employees = oldLoc.Employees,
                Managements = oldLoc.Managements,
                Menu = oldLoc.Menu,
                MenuId = oldLoc.MenuId
            };

            var location = new Location
            {
                AbreviatedCountry = "FR",
                AbreviatedState = "B8",
                City = "Digne-les-Bains",
                Street = "279 Corben Terrace",
                Country = "France",
                State = "Provence-Alpes-Côte d'Azur",
                OpenSince = new DateTime(2018, 1, 29),
                MenuId = await _context.Menus.Select(m => m.MenuId).FirstOrDefaultAsync(),
                ScheduleId = await _context.Schedules.Select(s => s.ScheduleId).FirstOrDefaultAsync()
            };

            // Act
            var result = (await _controller.UpdateLocation(GenBadLocationId, location));
            var updatedLoc = (await _controller.GetLocationBasic(oldLoc.LocationId)).Value;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
            updatedLoc.Should().BeEquivalentTo(oldLocCopy);
        }

        [TestMethod]
        public async Task UpdateLocation_TriesToSetId_ShouldReturnBadRequest()
        {
            // Arrange
            var oldLoc = await _context.Locations.FindAsync(GetFirstLocationId);
            var oldLocCopy = new Location
            {
                LocationId = oldLoc.LocationId,
                Schedule = oldLoc.Schedule,
                ScheduleId = oldLoc.ScheduleId,
                State = oldLoc.State,
                Street = oldLoc.Street,
                SupplyLinks = oldLoc.SupplyLinks,
                AbreviatedState = oldLoc.AbreviatedState,
                OpenSince = oldLoc.OpenSince,
                AbreviatedCountry = oldLoc.AbreviatedCountry,
                City = oldLoc.City,
                Country = oldLoc.Country,
                Employees = oldLoc.Employees,
                Managements = oldLoc.Managements,
                Menu = oldLoc.Menu,
                MenuId = oldLoc.MenuId
            };

            var location = new Location
            {
                LocationId = GenBadLocationId,
                AbreviatedCountry = "FR",
                AbreviatedState = "B8",
                City = "Digne-les-Bains",
                Street = "279 Corben Terrace",
                Country = "France",
                State = "Provence-Alpes-Côte d'Azur",
                OpenSince = new DateTime(2018, 1, 29),
                MenuId = await _context.Menus.Select(m => m.MenuId).FirstOrDefaultAsync(),
                ScheduleId = await _context.Schedules.Select(s => s.ScheduleId).FirstOrDefaultAsync()
            };

            // Act
            var result = await _controller.UpdateLocation(oldLoc.LocationId, location);
            var updatedLoc = await _context.Locations.FindAsync(GetFirstLocationId);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedLoc.Should().BeEquivalentTo(oldLocCopy);
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
            var oldLoc = await _context.Locations.FindAsync(GetFirstLocationId);
            var oldLocCopy = new Location
            {
                LocationId = oldLoc.LocationId,
                Schedule = oldLoc.Schedule,
                ScheduleId = oldLoc.ScheduleId,
                State = oldLoc.State,
                Street = oldLoc.Street,
                SupplyLinks = oldLoc.SupplyLinks,
                AbreviatedState = oldLoc.AbreviatedState,
                OpenSince = oldLoc.OpenSince,
                AbreviatedCountry = oldLoc.AbreviatedCountry,
                City = oldLoc.City,
                Country = oldLoc.Country,
                Employees = oldLoc.Employees,
                Managements = oldLoc.Managements,
                Menu = oldLoc.Menu,
                MenuId = oldLoc.MenuId
            };

            var location = new Location
            {
                AbreviatedCountry = abvCount,
                AbreviatedState = abvState,
                City = city,
                Street = str,
                Country = country,
                State = state,
                OpenSince = new DateTime(2018, 1, 29),
                MenuId = await _context.Menus.Select(m => m.MenuId).FirstOrDefaultAsync(),
                ScheduleId = await _context.Schedules.Select(s => s.ScheduleId).FirstOrDefaultAsync()
            };

            // Act
            var result = await _controller.UpdateLocation(oldLoc.LocationId, location);
            var updatedLoc = await _context.Locations.FindAsync(GetFirstLocationId);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedLoc.Should().BeEquivalentTo(oldLocCopy);
        }

        [TestMethod]
        public async Task UpdateLocation_DuplicateAddress_ShouldReturnBadRequest()
        {
            // Arrange
            var dupeSource = await _context.Locations
                .Select(l => new
                {
                    l.AbreviatedCountry,
                    l.AbreviatedState,
                    l.City,
                    l.Street
                }).Skip(1).FirstOrDefaultAsync();

            var oldLoc = await _context.Locations.FindAsync(GetFirstLocationId);
            var oldLocCopy = new Location
            {
                LocationId = oldLoc.LocationId,
                Schedule = oldLoc.Schedule,
                ScheduleId = oldLoc.ScheduleId,
                State = oldLoc.State,
                Street = oldLoc.Street,
                SupplyLinks = oldLoc.SupplyLinks,
                AbreviatedState = oldLoc.AbreviatedState,
                OpenSince = oldLoc.OpenSince,
                AbreviatedCountry = oldLoc.AbreviatedCountry,
                City = oldLoc.City,
                Country = oldLoc.Country,
                Employees = oldLoc.Employees,
                Managements = oldLoc.Managements,
                Menu = oldLoc.Menu,
                MenuId = oldLoc.MenuId
            };

            var location = new Location
            {
                AbreviatedCountry = dupeSource.AbreviatedCountry,
                AbreviatedState = dupeSource.AbreviatedState,
                City = dupeSource.City,
                Street = dupeSource.Street,
                Country = "France",
                State = "Provence-Alpes-Côte d'Azur",
                OpenSince = new DateTime(2018, 1, 29),
                MenuId = await _context.Menus.Select(m => m.MenuId).LastOrDefaultAsync(),
                ScheduleId = await _context.Schedules.Select(s => s.ScheduleId).LastOrDefaultAsync()
            };

            // Act
            var result = await _controller.UpdateLocation(oldLoc.LocationId, location);
            var updatedLoc = await _context.Locations.FindAsync(GetFirstLocationId);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedLoc.Should().BeEquivalentTo(oldLocCopy);
        }

        [TestMethod]
        public async Task UpdateLocation_BadMenuId_ShouldReturnBadRequest()
        {
            // Arrange
            var oldLoc = await _context.Locations.FindAsync(GetFirstLocationId);
            var oldLocCopy = new Location
            {
                LocationId = oldLoc.LocationId,
                Schedule = oldLoc.Schedule,
                ScheduleId = oldLoc.ScheduleId,
                State = oldLoc.State,
                Street = oldLoc.Street,
                SupplyLinks = oldLoc.SupplyLinks,
                AbreviatedState = oldLoc.AbreviatedState,
                OpenSince = oldLoc.OpenSince,
                AbreviatedCountry = oldLoc.AbreviatedCountry,
                City = oldLoc.City,
                Country = oldLoc.Country,
                Employees = oldLoc.Employees,
                Managements = oldLoc.Managements,
                Menu = oldLoc.Menu,
                MenuId = oldLoc.MenuId
            };

            var location = new Location
            {
                AbreviatedCountry = "FR",
                AbreviatedState = "B8",
                City = "Digne-les-Bains",
                Street = "279 Corben Terrace",
                Country = "France",
                State = "Provence-Alpes-Côte d'Azur",
                OpenSince = new DateTime(2018, 1, 29),
                MenuId = await _context.Menus.Select(m => m.MenuId).LastOrDefaultAsync()+1,
                ScheduleId = await _context.Schedules.Select(s => s.ScheduleId).LastOrDefaultAsync()
            };

            // Act
            var result = await _controller.UpdateLocation(oldLoc.LocationId, location);
            var updatedLoc = await _context.Locations.FindAsync(GetFirstLocationId);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedLoc.Should().BeEquivalentTo(oldLocCopy);
        }

        [TestMethod]
        public async Task UpdateLocation_BadScheduleId_ShouldReturnBadRequest()
        {
            // Arrange
            var oldLoc = await _context.Locations.FindAsync(GetFirstLocationId);
            var oldLocCopy = new Location
            {
                LocationId = oldLoc.LocationId,
                Schedule = oldLoc.Schedule,
                ScheduleId = oldLoc.ScheduleId,
                State = oldLoc.State,
                Street = oldLoc.Street,
                SupplyLinks = oldLoc.SupplyLinks,
                AbreviatedState = oldLoc.AbreviatedState,
                OpenSince = oldLoc.OpenSince,
                AbreviatedCountry = oldLoc.AbreviatedCountry,
                City = oldLoc.City,
                Country = oldLoc.Country,
                Employees = oldLoc.Employees,
                Managements = oldLoc.Managements,
                Menu = oldLoc.Menu,
                MenuId = oldLoc.MenuId
            };

            var location = new Location
            {
                AbreviatedCountry = "FR",
                AbreviatedState = "B8",
                City = "Digne-les-Bains",
                Street = "279 Corben Terrace",
                Country = "France",
                State = "Provence-Alpes-Côte d'Azur",
                OpenSince = new DateTime(2018, 1, 29),
                MenuId = await _context.Menus.Select(m => m.MenuId).LastOrDefaultAsync(),
                ScheduleId = await _context.Schedules.Select(s => s.ScheduleId).LastOrDefaultAsync()+1
            };

            // Act
            var result = await _controller.UpdateLocation(oldLoc.LocationId, location);
            var updatedLoc = await _context.Locations.FindAsync(GetFirstLocationId);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedLoc.Should().BeEquivalentTo(oldLocCopy);
        }

        [TestMethod]
        public async Task UpdateLocation_HasMenu_ShouldReturnBadRequest()
        {
            // Arrange
            var oldLoc = await _context.Locations.FindAsync(GetFirstLocationId);
            var oldLocCopy = new Location
            {
                LocationId = oldLoc.LocationId,
                Schedule = oldLoc.Schedule,
                ScheduleId = oldLoc.ScheduleId,
                State = oldLoc.State,
                Street = oldLoc.Street,
                SupplyLinks = oldLoc.SupplyLinks,
                AbreviatedState = oldLoc.AbreviatedState,
                OpenSince = oldLoc.OpenSince,
                AbreviatedCountry = oldLoc.AbreviatedCountry,
                City = oldLoc.City,
                Country = oldLoc.Country,
                Employees = oldLoc.Employees,
                Managements = oldLoc.Managements,
                Menu = oldLoc.Menu,
                MenuId = oldLoc.MenuId
            };

            var location = new Location
            {
                AbreviatedCountry = "FR",
                AbreviatedState = "B8",
                City = "Digne-les-Bains",
                Street = "279 Corben Terrace",
                Country = "France",
                State = "Provence-Alpes-Côte d'Azur",
                OpenSince = new DateTime(2018, 1, 29),
                MenuId = await _context.Menus.Select(m => m.MenuId).LastOrDefaultAsync(),
                ScheduleId = await _context.Schedules.Select(s => s.ScheduleId).LastOrDefaultAsync(),
                Menu = new Menu()
            };

            // Act
            var result = await _controller.UpdateLocation(oldLoc.LocationId, location);
            var updatedLoc = await _context.Locations.FindAsync(GetFirstLocationId);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedLoc.Should().BeEquivalentTo(oldLocCopy);
        }

        [TestMethod]
        public async Task UpdateLocation_HasSchedule_ShouldReturnBadRequest()
        {
            // Arrange
            var oldLoc = await _context.Locations.FindAsync(GetFirstLocationId);
            var oldLocCopy = new Location
            {
                LocationId = oldLoc.LocationId,
                Schedule = oldLoc.Schedule,
                ScheduleId = oldLoc.ScheduleId,
                State = oldLoc.State,
                Street = oldLoc.Street,
                SupplyLinks = oldLoc.SupplyLinks,
                AbreviatedState = oldLoc.AbreviatedState,
                OpenSince = oldLoc.OpenSince,
                AbreviatedCountry = oldLoc.AbreviatedCountry,
                City = oldLoc.City,
                Country = oldLoc.Country,
                Employees = oldLoc.Employees,
                Managements = oldLoc.Managements,
                Menu = oldLoc.Menu,
                MenuId = oldLoc.MenuId
            };

            var location = new Location
            {
                AbreviatedCountry = "FR",
                AbreviatedState = "B8",
                City = "Digne-les-Bains",
                Street = "279 Corben Terrace",
                Country = "France",
                State = "Provence-Alpes-Côte d'Azur",
                OpenSince = new DateTime(2018, 1, 29),
                MenuId = await _context.Menus.Select(m => m.MenuId).LastOrDefaultAsync(),
                ScheduleId = await _context.Schedules.Select(s => s.ScheduleId).LastOrDefaultAsync(),
                Schedule = new Schedule()
            };

            // Act
            var result = await _controller.UpdateLocation(oldLoc.LocationId, location);
            var updatedLoc = await _context.Locations.FindAsync(GetFirstLocationId);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedLoc.Should().BeEquivalentTo(oldLocCopy);
        }

        [TestMethod]
        public async Task UpdateLocation_ManagementsNull_ShouldReturnBadRequest()
        {
            // Arrange
            var oldLoc = await _context.Locations.FindAsync(GetFirstLocationId);
            var oldLocCopy = new Location
            {
                LocationId = oldLoc.LocationId,
                Schedule = oldLoc.Schedule,
                ScheduleId = oldLoc.ScheduleId,
                State = oldLoc.State,
                Street = oldLoc.Street,
                SupplyLinks = oldLoc.SupplyLinks,
                AbreviatedState = oldLoc.AbreviatedState,
                OpenSince = oldLoc.OpenSince,
                AbreviatedCountry = oldLoc.AbreviatedCountry,
                City = oldLoc.City,
                Country = oldLoc.Country,
                Employees = oldLoc.Employees,
                Managements = oldLoc.Managements,
                Menu = oldLoc.Menu,
                MenuId = oldLoc.MenuId
            };

            var location = new Location
            {
                AbreviatedCountry = "FR",
                AbreviatedState = "B8",
                City = "Digne-les-Bains",
                Street = "279 Corben Terrace",
                Country = "France",
                State = "Provence-Alpes-Côte d'Azur",
                OpenSince = new DateTime(2018, 1, 29),
                MenuId = await _context.Menus.Select(m => m.MenuId).LastOrDefaultAsync(),
                ScheduleId = await _context.Schedules.Select(s => s.ScheduleId).LastOrDefaultAsync(),
                Managements = null
            };

            // Act
            var result = await _controller.UpdateLocation(oldLoc.LocationId, location);
            var updatedLoc = await _context.Locations.FindAsync(GetFirstLocationId);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedLoc.Should().BeEquivalentTo(oldLocCopy);
        }

        [TestMethod]
        public async Task UpdateLocation_SupplyLinksNull_ShouldReturnBadRequest()
        {
            // Arrange
            var oldLoc = await _context.Locations.FindAsync(GetFirstLocationId);
            var oldLocCopy = new Location
            {
                LocationId = oldLoc.LocationId,
                Schedule = oldLoc.Schedule,
                ScheduleId = oldLoc.ScheduleId,
                State = oldLoc.State,
                Street = oldLoc.Street,
                SupplyLinks = oldLoc.SupplyLinks,
                AbreviatedState = oldLoc.AbreviatedState,
                OpenSince = oldLoc.OpenSince,
                AbreviatedCountry = oldLoc.AbreviatedCountry,
                City = oldLoc.City,
                Country = oldLoc.Country,
                Employees = oldLoc.Employees,
                Managements = oldLoc.Managements,
                Menu = oldLoc.Menu,
                MenuId = oldLoc.MenuId
            };

            var location = new Location
            {
                AbreviatedCountry = "FR",
                AbreviatedState = "B8",
                City = "Digne-les-Bains",
                Street = "279 Corben Terrace",
                Country = "France",
                State = "Provence-Alpes-Côte d'Azur",
                OpenSince = new DateTime(2018, 1, 29),
                MenuId = await _context.Menus.Select(m => m.MenuId).LastOrDefaultAsync(),
                ScheduleId = await _context.Schedules.Select(s => s.ScheduleId).LastOrDefaultAsync(),
                SupplyLinks = null
            };

            // Act
            var result = await _controller.UpdateLocation(oldLoc.LocationId, location);
            var updatedLoc = await _context.Locations.FindAsync(GetFirstLocationId);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedLoc.Should().BeEquivalentTo(oldLocCopy);
        }

        [TestMethod]
        public async Task UpdateLocation_SupplyLinksNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var oldLoc = await _context.Locations.FindAsync(GetFirstLocationId);
            var oldLocCopy = new Location
            {
                LocationId = oldLoc.LocationId,
                Schedule = oldLoc.Schedule,
                ScheduleId = oldLoc.ScheduleId,
                State = oldLoc.State,
                Street = oldLoc.Street,
                SupplyLinks = oldLoc.SupplyLinks,
                AbreviatedState = oldLoc.AbreviatedState,
                OpenSince = oldLoc.OpenSince,
                AbreviatedCountry = oldLoc.AbreviatedCountry,
                City = oldLoc.City,
                Country = oldLoc.Country,
                Employees = oldLoc.Employees,
                Managements = oldLoc.Managements,
                Menu = oldLoc.Menu,
                MenuId = oldLoc.MenuId
            };

            var location = new Location
            {
                AbreviatedCountry = "FR",
                AbreviatedState = "B8",
                City = "Digne-les-Bains",
                Street = "279 Corben Terrace",
                Country = "France",
                State = "Provence-Alpes-Côte d'Azur",
                OpenSince = new DateTime(2018, 1, 29),
                MenuId = await _context.Menus.Select(m => m.MenuId).LastOrDefaultAsync(),
                ScheduleId = await _context.Schedules.Select(s => s.ScheduleId).LastOrDefaultAsync(),
                SupplyLinks = new SupplyLink[] {new SupplyLink()}
            };

            // Act
            var result = await _controller.UpdateLocation(oldLoc.LocationId, location);
            var updatedLoc = await _context.Locations.FindAsync(GetFirstLocationId);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedLoc.Should().BeEquivalentTo(oldLocCopy);
        }

        [TestMethod]
        public async Task UpdateLocation_ManagementsNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var oldLoc = await _context.Locations.FindAsync(GetFirstLocationId);
            var oldLocCopy = new Location
            {
                LocationId = oldLoc.LocationId,
                Schedule = oldLoc.Schedule,
                ScheduleId = oldLoc.ScheduleId,
                State = oldLoc.State,
                Street = oldLoc.Street,
                SupplyLinks = oldLoc.SupplyLinks,
                AbreviatedState = oldLoc.AbreviatedState,
                OpenSince = oldLoc.OpenSince,
                AbreviatedCountry = oldLoc.AbreviatedCountry,
                City = oldLoc.City,
                Country = oldLoc.Country,
                Employees = oldLoc.Employees,
                Managements = oldLoc.Managements,
                Menu = oldLoc.Menu,
                MenuId = oldLoc.MenuId
            };

            var location = new Location
            {
                AbreviatedCountry = "FR",
                AbreviatedState = "B8",
                City = "Digne-les-Bains",
                Street = "279 Corben Terrace",
                Country = "France",
                State = "Provence-Alpes-Côte d'Azur",
                OpenSince = new DateTime(2018, 1, 29),
                MenuId = await _context.Menus.Select(m => m.MenuId).LastOrDefaultAsync(),
                ScheduleId = await _context.Schedules.Select(s => s.ScheduleId).LastOrDefaultAsync(),
                Managements = new Management[] {new Management()}
            };

            // Act
            var result = await _controller.UpdateLocation(oldLoc.LocationId, location);
            var updatedLoc = await _context.Locations.FindAsync(GetFirstLocationId);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedLoc.Should().BeEquivalentTo(oldLocCopy);
        }

        #endregion

        [TestMethod]
        public void LocationExists_ItDoes_ShoudReturnTrue()
        {
            //Arrange
            var methodInfo = typeof(LocationsController).GetMethod("LocationExists", BindingFlags.NonPublic | BindingFlags.Instance);
            var parameters = new object[] { GetFirstLocationId };

            // Act
            var result = ((methodInfo.Invoke(_controller, parameters)) as Task<bool>).Result;

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public void LocationExists_ItDoesNot_ShoudReturnFalse()
        {
            //Arrange
            var methodInfo = typeof(LocationsController).GetMethod("LocationExists", BindingFlags.NonPublic | BindingFlags.Instance);
            var parameters = new object[] { GenBadLocationId };

            // Act
            var result = (methodInfo.Invoke(_controller, parameters) as Task<bool>).Result;

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
            _controller = new LocationsController(_context, _logger);
        }

        /// Helpers
        int GetFirstLocationId => _context.Locations.Select(l=>l.LocationId).OrderBy(id => id).FirstOrDefault();
        int GenBadLocationId => _context.Locations.Select(l=>l.LocationId).ToArray().OrderBy(id => id).LastOrDefault() + 1;
        async Task<Location> GetFirstLocation() => await _context.Locations.FirstOrDefaultAsync();

        private static void Setup()
        {
            _context = InMemoryHelpers.GetContext();
            _controller = new LocationsController(_context, _logger);
        }
    }
}
