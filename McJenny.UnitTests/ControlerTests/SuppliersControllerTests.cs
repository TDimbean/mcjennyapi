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
    public class SuppliersControllerTests
    {
        private static SuppliersController _controller;
        private static FoodChainsDbContext _context;
        private static ILogger<SuppliersController> _logger;

        [ClassInitialize]
        public static void Init(TestContext testContext)
        {
            _logger = (new Mock<ILogger<SuppliersController>>()).Object;
            Setup();
        }

        [TestInitialize]
        public void InitTest() => Setup();

        #region Gets

        #region Basic

        [TestMethod]
        public async Task GetSuppliers_HapyFlow_ShouldReturnAllSuppliers()
        {
            // Arrange
            var expected = (object)await _context.Suppliers.Select(s =>
            string.Format("({0}) {1}, {2}{3}, {4}",
                s.SupplierId, s.Name, s.AbreviatedCountry,
                s.AbreviatedState == "N/A" ? string.Empty : ", " + s.AbreviatedState,
                s.City
                )).ToListAsync();

            // Act
            var result = (object)(await _controller.GetSuppliers()).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSupplier_HappyFlow_ShouldReturnSupplier()
        {
            // Arrange
            var expected = (object)await _context.Suppliers
                 .Select(s => new
                 {
                     s.SupplierId,
                     s.Name,
                     s.AbreviatedCountry,
                     s.AbreviatedState,
                     s.City
                 })
                 .FirstOrDefaultAsync(s => s.SupplierId == GetFirstSupplierId);

            // Act
            var result = (object)(await _controller.GetSupplier(GetFirstSupplierId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSupplier_InexistentId_ShouldReturnNotFound()
        {
            // Act
            var result = (await _controller.GetSupplier(GenBadSupplierId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetSupplierBasic_HappyFlow_ShouldReturnSupplier()
        {
            // Arrange
            var expected = await GetFirstSupplier();

            // Act
            var result = (await _controller.GetSupplierBasic(GetFirstSupplierId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSupplierBasic_InexistentId_ShouldReturnNotFound()
        {
            // Act
            var result = (await _controller.GetSupplierBasic(GenBadSupplierId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetSupplyCategories_HappyFlow_ShouldReturnSupplier()
        {
            // Arrange
            var supCatIds = await _context.SupplierStocks
                .Where(ss => ss.SupplierId == GetFirstSupplierId)
                .Select(ss => ss.SupplyCategoryId)
                .ToArrayAsync();

            var expected = (object)await _context.SupplyCategories
                .Where(c => supCatIds
                    .Contains(c.SupplyCategoryId))
                .Select(c => new { c.SupplyCategoryId, c.Name })
                .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetSupplyCategories(GetFirstSupplierId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSupplyCategories_InexistentId_ShouldReturnNotFound()
        {
            // Act
            var result = (await _controller.GetSupplyCategories(GenBadSupplierId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetLocations_HappyFlow_ShouldReturnSuppliedLocations()
        {
            // Arrange
            var supId = GetFirstSupplierId;

            var expected = (object)await _context.Locations
                .Where(l => GetSuppliedLocationIdsAsync(supId).Result.Contains(l.LocationId))
                .Select(l => string.Format("({0}), {1}, {2}{3}, {4}",
                l.LocationId, l.AbreviatedCountry,
                l.State == "N/A" ? string.Empty : l.AbreviatedState + ", ",
                l.City, l.Street))
                .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetLocations(supId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetLocations_InexistentId_ShouldReturnNotFound()
        {
            // Act
            var result = (await _controller.GetLocations(GenBadSupplierId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
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
                var result = (object)(await _controller.GetSupplierLinks(supId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSupplierLinks_InexistentId_ShouldReturnNotFound()
        {
            // Act
            var result = (await _controller.GetSupplierLinks(GenBadSupplierId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        //{
        //    [TestMethod]
        //    public async Task GetSupplyLinks_HappyFlow_ShouldReturnSupplyLinks()
        //    {
        //        // Arrange
        //        var supplier = await GetFirstSupplier();

        //        var dbLinks = await _context.SupplyLinks.ToArrayAsync();
        //        var locationIds = new List<int>();
        //        var cats = await _context.SupplyCategories.Select(c => new { c.SupplyCategoryId, c.Name }).ToArrayAsync();

        //        foreach (var dbLink in dbLinks)
        //            if (!locationIds.Contains(dbLink.LocationId)) locationIds.Add(dbLink.LocationId);

        //        var locations = await _context.Locations.Where(l => locationIds.Contains(l.LocationId)).ToArrayAsync();

        //        var statements = new string[dbLinks.Length];

        //        for (int i = 0; i < dbLinks.Length; i++)
        //        {
        //            var location = locations.SingleOrDefault(l => l.LocationId == dbLinks[i].LocationId);

        //            statements[i] = string.Format("{0} ({1}), {2}, {3}{4} supplies {5}, {6}{7}, {8} ({9}) with {10} ({11})",
        //            supplier.Name, supplier.SupplierId, supplier.AbreviatedCountry,
        //            supplier.AbreviatedState == "N/A" ? string.Empty : supplier.AbreviatedState + ", ",
        //            supplier.City, location.AbreviatedCountry,
        //            location.AbreviatedState == "N/A" ? string.Empty : location.AbreviatedState + ", ",
        //            location.City, location.Street, dbLinks[i].LocationId,
        //            cats.SingleOrDefault(c => c.SupplyCategoryId == dbLinks[0].SupplyCategoryId).Name,
        //            dbLinks[0].SupplyCategoryId);
        //        }
        //        var expected = (object)statements;

        //        // Act
        //        var result = (object)(await _controller.GetSupplyLinks(supplier.SupplierId)).Value;

        //        // Assert
        //        result.Should().BeEquivalentTo(expected);
        //    }

        //    [TestMethod]
        //    public async Task GetSupplyLinks_InexistentId_ShouldReturnNotFound()
        //    {
        //        // Act
        //        var result = (await _controller.GetSupplyLinks(GenBadSupplierId)).Result;

        //        // Assert
        //        result.Should().BeOfType<NotFoundResult>();
        //    }
        //}

        #endregion

        #region Advanced

        [TestMethod]
        public async Task GetSuppliersFiltered_NameMatch_ShouldReturnFiltered()
        {
            // Arrange
            var filter = await _context.Suppliers.Select(s => s.Name).FirstOrDefaultAsync();

            var expected = (object)await _context.Suppliers
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

            // Act
            var result = (object)(await _controller.GetSuppliersFiltered(filter)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSuppliersFiltered_AbrCountryMatch_ShouldReturnFiltered()
        {
            // Arrange
            var filter = await _context.Suppliers.Select(s => s.AbreviatedCountry).FirstOrDefaultAsync();

            var expected = (object)await _context.Suppliers
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
                 .Where(s => s.AbreviatedCountry.ToUpper()
                        .Contains(filter.ToUpper()))
                 .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetSuppliersFiltered(filter)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSuppliersFiltered_AbrStateMatch_ShouldReturnFiltered()
        {
            // Arrange
            var filter = await _context.Suppliers.Select(s => s.AbreviatedState).FirstOrDefaultAsync();

            var expected = (object)await _context.Suppliers
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
                 .Where(s => s.AbreviatedState.ToUpper()
                        .Contains(filter.ToUpper()))
                 .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetSuppliersFiltered(filter)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSuppliersFiltered_CountryMatch_ShouldReturnFiltered()
        {
            // Arrange
            var filter = await _context.Suppliers.Select(s => s.Country).FirstOrDefaultAsync();

            var expected = (object)await _context.Suppliers
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

            // Act
            var result = (object)(await _controller.GetSuppliersFiltered(filter)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSuppliersFiltered_StateMatch_ShouldReturnFiltered()
        {
            // Arrange
            var filter = await _context.Suppliers.Select(s => s.State).FirstOrDefaultAsync();

            var expected = (object)await _context.Suppliers
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

            // Act
            var result = (object)(await _controller.GetSuppliersFiltered(filter)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSuppliersFiltered_CityMatch_ShouldReturnFiltered()
        {
            // Arrange
            var filter = await _context.Suppliers.Select(s => s.City).FirstOrDefaultAsync();

            var expected = (object)await _context.Suppliers
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

            // Act
            var result = (object)(await _controller.GetSuppliersFiltered(filter)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSuppliersPaged_HappyFlow_ShouldReturnPaged()
        {
            // Arrange
            var pgSz = 2;
            var pgInd = 1;

            var expected = (object)await _context.Suppliers
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

            // Act
            var result = (object)(await _controller.GetSuppliersPaged(pgInd, pgSz)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSuppliersPaged_EndPageIncomplete_ShouldReturnPaged()
        {
            // Arrange
            var SupplierCount = await _context.Suppliers.CountAsync();
            var pgSz = 1;

            while (true)
            {
                if (SupplierCount % pgSz == 0) pgSz++;
                else break;
            }

            var pgInd = SupplierCount / (pgSz - 1);

            var expected = (object)await _context.Suppliers
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

            // Act
            var call = (await _controller.GetSuppliersPaged(pgInd, pgSz)).Value;
            var result = (object)call;
            var resCount = (result as IEnumerable<dynamic>).Count();

            // Assert
            result.Should().BeEquivalentTo(expected);
            resCount.Should().BeLessThan(pgSz);
        }

        [TestMethod]
        public async Task GetSuppliersPaged_SizeOver_ShouldReturnPaged()
        {
            // Arrange
            var SupplierCount = await _context.Suppliers.CountAsync();
            var pgSz = SupplierCount + 1;
            var pgInd = 1;

            var expected = (object)await _context.Suppliers
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

            // Act
            var result = (object)(await _controller.GetSuppliersPaged(pgInd, pgSz)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSuppliersPaged_IndexOver_ShouldReturnPaged()
        {
            // Arrange
            var SupplierCount = await _context.Suppliers.CountAsync();
            var pgInd = SupplierCount + 1;
            var pgSz = 1;

            var expected = (object)await _context.Suppliers
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

            // Act
            var result = (object)(await _controller.GetSuppliersPaged(pgInd, pgSz)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSuppliersSorted_ByName_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "name";

            var expected = (object)await _context.Suppliers
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

            // Act
            var result = (object)(await _controller.GetSuppliersSorted(sortBy, false)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSuppliersSorted_ByCountry_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "country";

            var expected = (object)await _context.Suppliers
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

            // Act
            var result = (object)(await _controller.GetSuppliersSorted(sortBy, false)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSuppliersSorted_ByState_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "state";

            var expected = (object)await _context.Suppliers
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

            // Act
            var result = (object)(await _controller.GetSuppliersSorted(sortBy, false)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSuppliersSorted_ByCity_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "city";

            var expected = (object)await _context.Suppliers
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

            // Act
            var result = (object)(await _controller.GetSuppliersSorted(sortBy, false)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSuppliersSorted_Desc_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "name";

            var expected = (object)await _context.Suppliers
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

            // Act
            var result = (object)(await _controller.GetSuppliersSorted(sortBy, true)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSuppliersSorted_GarbledText_ShouldDefaultoIdSort()
        {
            // Arrange
            var sortBy = "asd";

            var expected = (object)await _context.Suppliers
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

            // Act
            var result = (object)(await _controller.GetSuppliersSorted(sortBy, false)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        #endregion

        #endregion

        #region Creates

        [TestMethod]
        public async Task CreateSupplier_HappyFlow_ShouldCreateAndReturnSupplier()
        {
            // Arrange
            var supplier = new Supplier
            {
                Name = "Mertz and Sons",
                AbreviatedCountry = "FR",
                AbreviatedState = "B7",
                City = "Niort",
                Country = "France",
                State = "Poitou-Charentes"
            };

            // Act
            await _controller.CreateSupplier(supplier);
            var result = (object)(await _controller.GetSupplierBasic(supplier.SupplierId)).Value;

            var expected = (object)supplier;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task CreateSupplier_TriesToSetId_ShouldReturnBadRequest()
        {
            // Arrange
            var supplier = new Supplier
            {
                SupplierId = await _context.Suppliers.Select(s => s.SupplierId).LastOrDefaultAsync() + 1,
                Name = "Mertz and Sons",
                AbreviatedCountry = "FR",
                AbreviatedState = "B7",
                City = "Niort",
                Country = "France",
                State = "Poitou-Charentes"
            };

            // Act
            var result = (await _controller.CreateSupplier(supplier)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
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
                Name = name,
                AbreviatedCountry = abrCountry,
                AbreviatedState = abrState,
                City = city,
                Country = country,
                State = state
            };

            // Act
            var result = (await _controller.CreateSupplier(supplier)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateSupplier_DuplicateName_ShouldReturnBadRequest()
        {
            // Arrange
            var exName = await _context.Suppliers.Select(s => s.Name).FirstOrDefaultAsync();
            var supplier = new Supplier { Name = exName };
            var lwrSup = new Supplier { Name = exName.ToLower() };
            var uprSup = new Supplier { Name = exName.ToUpper() };

            // Act
            var result = (await _controller.CreateSupplier(supplier)).Result;
            var lwrResult = (await _controller.CreateSupplier(lwrSup)).Result;
            var uprResult = (await _controller.CreateSupplier(uprSup)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            lwrResult.Should().BeOfType<BadRequestResult>();
            uprResult.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateSupplier_SupplierStocksNull_ShouldReturnBadRequest()
        {
            // Arrange
            var supplier = new Supplier
            {
                Name = "Mertz and Sons",
                AbreviatedCountry = "FR",
                AbreviatedState = "B7",
                City = "Niort",
                Country = "France",
                State = "Poitou-Charentes",
                SupplierStocks = null
            };

            // Act
            var result = (await _controller.CreateSupplier(supplier)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateSupplier_SupplyLinksNull_ShouldReturnBadRequest()
        {
            // Arrange
            var supplier = new Supplier
            {
                Name = "Mertz and Sons",
                AbreviatedCountry = "FR",
                AbreviatedState = "B7",
                City = "Niort",
                Country = "France",
                State = "Poitou-Charentes",
                SupplyLinks = null
            };

            // Act
            var result = (await _controller.CreateSupplier(supplier)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateSupplier_SupplierStocksNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var supplier = new Supplier
            {
                Name = "Mertz and Sons",
                AbreviatedCountry = "FR",
                AbreviatedState = "B7",
                City = "Niort",
                Country = "France",
                State = "Poitou-Charentes",
                SupplierStocks = new SupplierStock[] { new SupplierStock() }
            };

            // Act
            var result = (await _controller.CreateSupplier(supplier)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateSupplier_SupplyLinks_ShouldReturnBadRequest()
        {
            // Arrange
            var supplier = new Supplier
            {
                Name = "Mertz and Sons",
                AbreviatedCountry = "FR",
                AbreviatedState = "B7",
                City = "Niort",
                Country = "France",
                State = "Poitou-Charentes",
                SupplyLinks = new SupplyLink[] { new SupplyLink() }
            };

            // Act
            var result = (await _controller.CreateSupplier(supplier)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        #endregion

        #region Updates

        [TestMethod]
        public async Task UpdateSupplier_HappyFlow_ShouldCreateAndReturnSupplier()
        {
            // Arrange
            var oldSup = await _context.Suppliers.FindAsync(GetFirstSupplierId);
            var oldSupCopy = new Supplier
            {
                SupplierId = oldSup.SupplierId,
                State = oldSup.State,
                SupplierStocks = oldSup.SupplierStocks,
                SupplyLinks = oldSup.SupplyLinks,
                AbreviatedState = oldSup.AbreviatedState,
                AbreviatedCountry = oldSup.AbreviatedCountry,
                City = oldSup.City,
                Country = oldSup.Country,
                Name = oldSup.Name
            };

            var supplier = new Supplier
            {
                Name = "El Vegetale",
                AbreviatedCountry = "SP",
                AbreviatedState = "CN",
                City = "Palmas De Gran Canaria",
                Country = "Spain",
                State = "Canarias"
            };

            // Act
            await _controller.UpdateSupplier(oldSup.SupplierId, supplier);
            var result = (await _controller.GetSupplierBasic(oldSup.SupplierId)).Value;

            supplier.SupplierId = oldSupCopy.SupplierId;

            // Assert
            result.Should().BeEquivalentTo(supplier);
            result.Should().NotBeEquivalentTo(oldSupCopy);
        }

        [TestMethod]
        public async Task UpdateSupplier_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var oldSup = await _context.Suppliers.FindAsync(GetFirstSupplierId);
            var oldSupCopy = new Supplier
            {
                SupplierId = oldSup.SupplierId,
                State = oldSup.State,
                SupplierStocks = oldSup.SupplierStocks,
                SupplyLinks = oldSup.SupplyLinks,
                AbreviatedState = oldSup.AbreviatedState,
                AbreviatedCountry = oldSup.AbreviatedCountry,
                City = oldSup.City,
                Country = oldSup.Country,
                Name = oldSup.Name
            };

            var supplier = new Supplier
            {
                Name = "El Vegetale",
                AbreviatedCountry = "SP",
                AbreviatedState = "CN",
                City = "Palmas De Gran Canaria",
                Country = "Spain",
                State = "Canarias"
            };

            // Act
            var result = await _controller.UpdateSupplier(GenBadSupplierId, supplier);
            var updatedSup = (await _controller.GetSupplierBasic(oldSup.SupplierId)).Value;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
            updatedSup.Should().BeEquivalentTo(oldSupCopy);
        }

        [TestMethod]
        public async Task UpdateSupplier_TriesToSetId_ShouldReturnBadRequest()
        {
            // Arrange
            var oldSup = await _context.Suppliers.FindAsync(GetFirstSupplierId);
            var oldSupCopy = new Supplier
            {
                SupplierId = oldSup.SupplierId,
                State = oldSup.State,
                SupplierStocks = oldSup.SupplierStocks,
                SupplyLinks = oldSup.SupplyLinks,
                AbreviatedState = oldSup.AbreviatedState,
                AbreviatedCountry = oldSup.AbreviatedCountry,
                City = oldSup.City,
                Country = oldSup.Country,
                Name = oldSup.Name
            };

            var supplier = new Supplier
            {
                Name = "El Vegetale",
                AbreviatedCountry = "SP",
                AbreviatedState = "CN",
                City = "Palmas De Gran Canaria",
                Country = "Spain",
                State = "Canarias",
                SupplierId = GenBadSupplierId
            };

            // Act
            var result = await _controller.UpdateSupplier(oldSup.SupplierId, supplier);
            var updatedSup = (await _controller.GetSupplierBasic(oldSup.SupplierId)).Value;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedSup.Should().BeEquivalentTo(oldSupCopy);
        }

        [DataTestMethod]
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
        public async Task UpdateSupplier_BadStrings_ShouldReturnBadRequest
            (string name, string abrCountry, string abrState, string city, string country, string state)
        {
            // Arrange
            var oldSup = await _context.Suppliers.FindAsync(GetFirstSupplierId);
            var oldSupCopy = new Supplier
            {
                SupplierId = oldSup.SupplierId,
                State = oldSup.State,
                SupplierStocks = oldSup.SupplierStocks,
                SupplyLinks = oldSup.SupplyLinks,
                AbreviatedState = oldSup.AbreviatedState,
                AbreviatedCountry = oldSup.AbreviatedCountry,
                City = oldSup.City,
                Country = oldSup.Country,
                Name = oldSup.Name
            };

            var supplier = new Supplier
            {
                Name = name,
                AbreviatedCountry = abrCountry,
                AbreviatedState = abrState,
                City = city,
                Country = country,
                State = state
            };

            // Act
            var result = await _controller.UpdateSupplier(oldSup.SupplierId, supplier);
            var updatedSup = (await _controller.GetSupplierBasic(oldSup.SupplierId)).Value;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedSup.Should().BeEquivalentTo(oldSupCopy);
        }

        [TestMethod]
        public async Task UpdateSupplier_DuplicateName_ShouldReturnBadRequest()
        {
            // Arrange
            var dupeSource = await _context.Suppliers
                .Select(s => s.Name)
                .Skip(1)
                .FirstOrDefaultAsync();

            var oldSup = await _context.Suppliers.FindAsync(GetFirstSupplierId);
            var oldSupCopy = new Supplier
            {
                SupplierId = oldSup.SupplierId,
                State = oldSup.State,
                SupplierStocks = oldSup.SupplierStocks,
                SupplyLinks = oldSup.SupplyLinks,
                AbreviatedState = oldSup.AbreviatedState,
                AbreviatedCountry = oldSup.AbreviatedCountry,
                City = oldSup.City,
                Country = oldSup.Country,
                Name = oldSup.Name
            };

            var supplier = new Supplier
            {
                AbreviatedCountry = "SP",
                AbreviatedState = "CN",
                City = "Palmas De Gran Canaria",
                Country = "Spain",
                State = "Canarias",
                Name = dupeSource
            };

            // Act
            var result = await _controller.UpdateSupplier(oldSup.SupplierId, supplier);
            var updatedSup = (await _controller.GetSupplierBasic(oldSup.SupplierId)).Value;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedSup.Should().BeEquivalentTo(oldSupCopy);
        }

        [TestMethod]
        public async Task UpdateSupplier_SupplierStocksNull_ShouldReturnBadRequest()
        {
            // Arrange
            var oldSup = await _context.Suppliers.FindAsync(GetFirstSupplierId);
            var oldSupCopy = new Supplier
            {
                SupplierId = oldSup.SupplierId,
                State = oldSup.State,
                SupplierStocks = oldSup.SupplierStocks,
                SupplyLinks = oldSup.SupplyLinks,
                AbreviatedState = oldSup.AbreviatedState,
                AbreviatedCountry = oldSup.AbreviatedCountry,
                City = oldSup.City,
                Country = oldSup.Country,
                Name = oldSup.Name
            };

            var supplier = new Supplier
            {
                Name = "El Vegetale",
                AbreviatedCountry = "SP",
                AbreviatedState = "CN",
                City = "Palmas De Gran Canaria",
                Country = "Spain",
                State = "Canarias",
                SupplierStocks = null
            };

            // Act
            var result = await _controller.UpdateSupplier(oldSup.SupplierId, supplier);
            var updatedSup = (await _controller.GetSupplierBasic(oldSup.SupplierId)).Value;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedSup.Should().BeEquivalentTo(oldSupCopy);
        }

        [TestMethod]
        public async Task UpdateSupplier_SupplyLinksNull_ShouldReturnBadRequest()
        {
            // Arrange
            var oldSup = await _context.Suppliers.FindAsync(GetFirstSupplierId);
            var oldSupCopy = new Supplier
            {
                SupplierId = oldSup.SupplierId,
                State = oldSup.State,
                SupplierStocks = oldSup.SupplierStocks,
                SupplyLinks = oldSup.SupplyLinks,
                AbreviatedState = oldSup.AbreviatedState,
                AbreviatedCountry = oldSup.AbreviatedCountry,
                City = oldSup.City,
                Country = oldSup.Country,
                Name = oldSup.Name
            };

            var supplier = new Supplier
            {
                Name = "El Vegetale",
                AbreviatedCountry = "SP",
                AbreviatedState = "CN",
                City = "Palmas De Gran Canaria",
                Country = "Spain",
                State = "Canarias",
                SupplyLinks = null
            };

            // Act
            var result = await _controller.UpdateSupplier(oldSup.SupplierId, supplier);
            var updatedSup = (await _controller.GetSupplierBasic(oldSup.SupplierId)).Value;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedSup.Should().BeEquivalentTo(oldSupCopy);
        }

        [TestMethod]
        public async Task UpdateSupplier_SupplyLinksNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var oldSup = await _context.Suppliers.FindAsync(GetFirstSupplierId);
            var oldSupCopy = new Supplier
            {
                SupplierId = oldSup.SupplierId,
                State = oldSup.State,
                SupplierStocks = oldSup.SupplierStocks,
                SupplyLinks = oldSup.SupplyLinks,
                AbreviatedState = oldSup.AbreviatedState,
                AbreviatedCountry = oldSup.AbreviatedCountry,
                City = oldSup.City,
                Country = oldSup.Country,
                Name = oldSup.Name
            };

            var supplier = new Supplier
            {
                Name = "El Vegetale",
                AbreviatedCountry = "SP",
                AbreviatedState = "CN",
                City = "Palmas De Gran Canaria",
                Country = "Spain",
                State = "Canarias",
                SupplyLinks = new SupplyLink[] { new SupplyLink() }
            };

            // Act
            var result = await _controller.UpdateSupplier(oldSup.SupplierId, supplier);
            var updatedSup = (await _controller.GetSupplierBasic(oldSup.SupplierId)).Value;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedSup.Should().BeEquivalentTo(oldSupCopy);
        }

        [TestMethod]
        public async Task UpdateSupplier_SupplierStocksNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var oldSup = await _context.Suppliers.FindAsync(GetFirstSupplierId);
            var oldSupCopy = new Supplier
            {
                SupplierId = oldSup.SupplierId,
                State = oldSup.State,
                SupplierStocks = oldSup.SupplierStocks,
                SupplyLinks = oldSup.SupplyLinks,
                AbreviatedState = oldSup.AbreviatedState,
                AbreviatedCountry = oldSup.AbreviatedCountry,
                City = oldSup.City,
                Country = oldSup.Country,
                Name = oldSup.Name
            };

            var supplier = new Supplier
            {
                Name = "El Vegetale",
                AbreviatedCountry = "SP",
                AbreviatedState = "CN",
                City = "Palmas De Gran Canaria",
                Country = "Spain",
                State = "Canarias",
                SupplierStocks = new SupplierStock[] { new SupplierStock() }
            };

            // Act
            var result = await _controller.UpdateSupplier(oldSup.SupplierId, supplier);
            var updatedSup = (await _controller.GetSupplierBasic(oldSup.SupplierId)).Value;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedSup.Should().BeEquivalentTo(oldSupCopy);
        }

        #endregion

        [TestMethod]
        public void SupplierExists_ItDoes_ShoudReturnTrue()
        {
            //Arrange
            var methodInfo = typeof(SuppliersController).GetMethod("SupplierExists", BindingFlags.NonPublic | BindingFlags.Instance);
            var parameters = new object[] { GetFirstSupplierId };

            // Act
            var result = ((methodInfo.Invoke(_controller, parameters)) as Task<bool>).Result;

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public void SupplierExists_ItDoesNot_ShoudReturnFalse()
        {
            //Arrange
            var methodInfo = typeof(SuppliersController).GetMethod("SupplierExists", BindingFlags.NonPublic | BindingFlags.Instance);
            var parameters = new object[] { GenBadSupplierId };

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
            _controller = new SuppliersController(_context, _logger);
        }

        /// Helpers
        int GetFirstSupplierId => _context.Suppliers.Select(s => s.SupplierId).OrderBy(id => id).FirstOrDefault();
        int GenBadSupplierId => _context.Suppliers.Select(s => s.SupplierId).ToArray().OrderBy(id => id).LastOrDefault() + 1;
        async Task<Supplier> GetFirstSupplier() => await _context.Suppliers.FirstOrDefaultAsync();
        private async Task<int[]> GetSupplyCategoryIdsAsync(int id)
       => await _context.SupplierStocks
               .Where(ss => ss.SupplierId == id)
               .Select(ss => ss.SupplyCategoryId)
               .ToArrayAsync();

        private async Task<int[]> GetSuppliedLocationIdsAsync(int id)
        => await _context.SupplyLinks
                .Where(l => l.SupplierId == id)
                .Select(l => l.LocationId)
                .ToArrayAsync();

        private static void Setup()
        {
            var suppliers = FakeRepo.Suppliers;
            var locations = FakeRepo.Locations;
            var links = FakeRepo.SupplyLinks;
            var cats = FakeRepo.SupplyCategories;
            var stocks = FakeRepo.SupplierStocks;

            var DummyOptions = new DbContextOptionsBuilder<FoodChainsDbContext>().Options;

            var dbContextMock = new DbContextMock<FoodChainsDbContext>(DummyOptions);
            var suppliersDbSetMock = dbContextMock.CreateDbSetMock(x => x.Suppliers, suppliers);
            var locationsDbSetMock = dbContextMock.CreateDbSetMock(x => x.Locations, locations);
            var linksDbSetMock = dbContextMock.CreateDbSetMock(x => x.SupplyLinks, links);
            var catsDbSetMock = dbContextMock.CreateDbSetMock(x => x.SupplyCategories, cats);
            var stocksDbSetMock = dbContextMock.CreateDbSetMock(x => x.SupplierStocks, stocks);
            dbContextMock.Setup(m => m.Suppliers).Returns(suppliersDbSetMock.Object);
            dbContextMock.Setup(m => m.Locations).Returns(locationsDbSetMock.Object);
            dbContextMock.Setup(m => m.SupplyLinks).Returns(linksDbSetMock.Object);
            dbContextMock.Setup(m => m.SupplyCategories).Returns(catsDbSetMock.Object);
            dbContextMock.Setup(m => m.SupplierStocks).Returns(stocksDbSetMock.Object);
            _context = dbContextMock.Object;
            _controller = new SuppliersController(_context, _logger);
        }
    }
}
