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
using System.Text;
using System.Threading.Tasks;

namespace McJenny.UnitTests.ControlerTests.InMemoryDbVersions
{
    [TestClass]
    public class SupplyLinkControllerTests
    {
        private static SupplyLinksController _controller;
        private static FoodChainsDbContext _context;
        private static ILogger<SupplyLinksController> _logger;

        [ClassInitialize]
        public static void Init(TestContext testContext)
        {
            _logger = (new Mock<ILogger<SupplyLinksController>>()).Object;
            Setup();
        }

        [TestInitialize]
        public void InitTest() => Setup();

        #region Gets

        [TestMethod]
        public async Task GetSupplyLinks_HappyFlow_ShouldReturnAllSupplyLinks()
        {
            // Arrange
            var links = await _context.SupplyLinks.
                            Select(l => new { l.SupplyLinkId, l.LocationId, l.SupplierId, l.SupplyCategoryId })
                            .ToArrayAsync();

            var locIds = links.Select(l => l.LocationId).ToArray();
            var supIds = links.Select(l => l.SupplierId).ToArray();
            var catIds = links.Select(l => l.SupplyCategoryId).ToArray();

            var locs = await _context.Locations
                .Where(l => locIds.Contains(l.LocationId))
                .Select(l => new
                {
                    l.LocationId,
                    l.AbreviatedCountry,
                    l.AbreviatedState,
                    l.City,
                    l.Street
                })
                .ToArrayAsync();

            var sups = await _context.Suppliers
                .Where(s => supIds.Contains(s.SupplierId))
                .Select(s => new
                {
                    s.SupplierId,
                    s.AbreviatedCountry,
                    s.AbreviatedState,
                    s.City,
                    s.Name
                })
                .ToArrayAsync();

            var cats = await _context.SupplyCategories
                .Where(c => catIds.Contains(c.SupplyCategoryId))
                .Select(c => new { c.SupplyCategoryId, c.Name })
                .ToArrayAsync();

            var strings = new string[links.Length];
            for (int i = 0; i < links.Length; i++)
            {
                var loc = locs.SingleOrDefault(l => l.LocationId == links[i].LocationId);
                var sup = sups.SingleOrDefault(s => s.SupplierId == links[i].SupplierId);
                var cat = cats.SingleOrDefault(c => c.SupplyCategoryId == links[i].SupplyCategoryId).Name;

                strings[i] = string.Format("Link [{0}]: ({1}) {2}, {3}{4}, {5} supplies" +
                    "({6}) {7}, {8}{9}, {10} with ({11}) {12}",
                    links[i].SupplyLinkId,
                    links[i].SupplierId,
                    sup.AbreviatedCountry,
                    sup.AbreviatedState == "N/A" ? string.Empty :
                    sup.AbreviatedState + ", ",
                    sup.City,
                    sup.Name,
                    links[i].LocationId,
                    loc.AbreviatedCountry,
                    loc.AbreviatedState == "N/A" ? string.Empty :
                    loc.AbreviatedState + ", ",
                    loc.City,
                    loc.Street,
                    links[i].SupplyCategoryId, cat);
            }

            var expected = (object)strings;

            // Act
            var result = (object)(await _controller.GetSupplyLinks()).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSupplyLink_HappyFlow_ShouldReturnSupplyLink()
        {
            // Arrange
            var link = await _context.SupplyLinks
                .Select(l => new { l.SupplyLinkId, l.LocationId, l.SupplierId, l.SupplyCategoryId })
                .FirstOrDefaultAsync(l => l.SupplyLinkId == GetFirstSupplyLinkId);

            var loc = await _context.Locations
                .Select(l => new
                {
                    l.LocationId,
                    l.AbreviatedCountry,
                    l.AbreviatedState,
                    l.City,
                    l.Street
                })
                .SingleOrDefaultAsync(l => l.LocationId == link.LocationId);

            var sup = await _context.Suppliers
                .Select(s => new
                {
                    s.SupplierId,
                    s.AbreviatedCountry,
                    s.AbreviatedState,
                    s.City,
                    s.Name
                })
                .SingleOrDefaultAsync(s => s.SupplierId == link.SupplierId);

            var cat = (await _context.SupplyCategories
                .FindAsync(link.SupplyCategoryId)).Name;

            var expected = (object) string.Format("({0}) {1}, {2}{3}, {4} supplies" +
                    "({5}) {6}, {7}{8}, {9} with ({10}) {11}",
                    sup.SupplierId,
                    sup.AbreviatedCountry,
                    sup.AbreviatedState == "N/A" ? string.Empty :
                    sup.AbreviatedState + ", ",
                    sup.City,
                    sup.Name,
                    loc.LocationId,
                    loc.AbreviatedCountry,
                    loc.AbreviatedState == "N/A" ? string.Empty :
                    loc.AbreviatedState + ", ",
                    loc.City,
                    loc.Street,
                    link.SupplyCategoryId, cat);

            // Act
            var result = (object)(await _controller
                .GetSupplyLink(GetFirstSupplyLinkId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSupplyLink_InexistentId_ShouldReturnNotFound()
        {
            // Act
            var result = (object)(await _controller
                .GetSupplyLink(GenBadSupplyLinkId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetSupplyLinkBasic_HappyFlow_ShouldReturnRequirement()
        {
            // Arrange
            var expected = (object)(await _context.SupplyLinks.FirstOrDefaultAsync());

            // Act
            var result = (object)(await _controller
                .GetSupplyLinkBasic(GetFirstSupplyLinkId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSupplyLinkBasic_InexistentId_ShouldReturnNotFound()
        {
            // Act
            var result = (object)(await _controller
                .GetSupplyLinkBasic(GenBadSupplyLinkId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        #region Basic

        #endregion

        #endregion

        #region Creates

        [TestMethod]
        public async Task CreateSupplyLink_HappyFlow_ShouldCreateAndReturnSupplyLink()
        {
            // Arrange
            var supplier = new Supplier
            {
                AbreviatedCountry = "IT",
                AbreviatedState = "LZ",
                City = "Roma",
                Country = "Italy",
                State = "Lazio",
                Name = "La Verdura-Roma"
            };
            await _context.Suppliers.AddAsync(supplier);

            var location = new Location
            {
                AbreviatedCountry = "IT",
                AbreviatedState = "LZ",
                City = "Roma",
                Street = "58 Continental Drive",
                Country = "Italy",
                State = "Lazio",
                OpenSince = new DateTime(2018, 10, 08),
                MenuId = await _context.Menus.Select(m => m.MenuId).FirstOrDefaultAsync(),
                ScheduleId = await _context.Schedules.Select(s => s.ScheduleId).FirstOrDefaultAsync()
            };
            await _context.Locations.AddAsync(location);

            var catId = await _context.SupplyCategories
                    .Select(c => c.SupplyCategoryId)
                    .FirstOrDefaultAsync();

            var stock = new SupplierStock
            {
                SupplierId = supplier.SupplierId,
                SupplyCategoryId = catId
            };
            await _context.SupplierStocks.AddAsync(stock);
            await _context.SaveChangesAsync();


            var link = new SupplyLink
            {
                LocationId = location.LocationId,
                SupplierId = supplier.SupplierId,
                SupplyCategoryId = catId
            };

            // Act
            await _controller.CreateSupplyLink(link);
            var result = (object)(await _controller.GetSupplyLinkBasic(link.SupplyLinkId)).Value;

            var expected = (object)link;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task CreateSupplyLink_TriesToSetId_ShouldReturnBadRequest()
        {
            // Arrange
            var supplier = new Supplier
            {
                AbreviatedCountry = "IT",
                AbreviatedState = "LZ",
                City = "Roma",
                Country = "Italy",
                State = "Lazio",
                Name = "La Verdura-Roma"
            };
            await _context.Suppliers.AddAsync(supplier);

            var location = new Location
            {
                AbreviatedCountry = "IT",
                AbreviatedState = "LZ",
                City = "Roma",
                Street = "58 Continental Drive",
                Country = "Italy",
                State = "Lazio",
                OpenSince = new DateTime(2018, 10, 08),
                MenuId = await _context.Menus.Select(m => m.MenuId).FirstOrDefaultAsync(),
                ScheduleId = await _context.Schedules.Select(s => s.ScheduleId).FirstOrDefaultAsync()
            };
            await _context.Locations.AddAsync(location);

            var catId = await _context.SupplyCategories
                    .Select(c => c.SupplyCategoryId)
                    .FirstOrDefaultAsync();

            var stock = new SupplierStock
            {
                SupplierId = supplier.SupplierId,
                SupplyCategoryId = catId
            };
            await _context.SupplierStocks.AddAsync(stock);

            var link = new SupplyLink
            {
                SupplyLinkId = await _context.SupplyLinks
                    .Select(l=>l.SupplyLinkId)
                    .LastOrDefaultAsync()+1,
                LocationId = location.LocationId,
                SupplierId = supplier.SupplierId,
                SupplyCategoryId = catId
            };

            // Act
            var result = (object)(await _controller.CreateSupplyLink(link)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateSupplyLink_HasSupplier_ShouldReturnBadRequest()
        {
            // Arrange
            var supplier = new Supplier
            {
                AbreviatedCountry = "IT",
                AbreviatedState = "LZ",
                City = "Roma",
                Country = "Italy",
                State = "Lazio",
                Name = "La Verdura-Roma"
            };
            await _context.Suppliers.AddAsync(supplier);

            var location = new Location
            {
                AbreviatedCountry = "IT",
                AbreviatedState = "LZ",
                City = "Roma",
                Street = "58 Continental Drive",
                Country = "Italy",
                State = "Lazio",
                OpenSince = new DateTime(2018, 10, 08),
                MenuId = await _context.Menus.Select(m => m.MenuId).FirstOrDefaultAsync(),
                ScheduleId = await _context.Schedules.Select(s => s.ScheduleId).FirstOrDefaultAsync()
            };
            await _context.Locations.AddAsync(location);

            var catId = await _context.SupplyCategories
                    .Select(c => c.SupplyCategoryId)
                    .FirstOrDefaultAsync();

            var stock = new SupplierStock
            {
                SupplierId = supplier.SupplierId,
                SupplyCategoryId = catId
            };
            await _context.SupplierStocks.AddAsync(stock);

            var link = new SupplyLink
            {
                Supplier = new Supplier(),
                LocationId = location.LocationId,
                SupplierId = supplier.SupplierId,
                SupplyCategoryId = catId
            };

            // Act
            var result = (object)(await _controller.CreateSupplyLink(link)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateSupplyLink_HasLocation_ShouldReturnBadRequest()
        {
            // Arrange
            var supplier = new Supplier
            {
                AbreviatedCountry = "IT",
                AbreviatedState = "LZ",
                City = "Roma",
                Country = "Italy",
                State = "Lazio",
                Name = "La Verdura-Roma"
            };
            await _context.Suppliers.AddAsync(supplier);

            var location = new Location
            {
                AbreviatedCountry = "IT",
                AbreviatedState = "LZ",
                City = "Roma",
                Street = "58 Continental Drive",
                Country = "Italy",
                State = "Lazio",
                OpenSince = new DateTime(2018, 10, 08),
                MenuId = await _context.Menus.Select(m => m.MenuId).FirstOrDefaultAsync(),
                ScheduleId = await _context.Schedules.Select(s => s.ScheduleId).FirstOrDefaultAsync()
            };
            await _context.Locations.AddAsync(location);

            var catId = await _context.SupplyCategories
                    .Select(c => c.SupplyCategoryId)
                    .FirstOrDefaultAsync();

            var stock = new SupplierStock
            {
                SupplierId = supplier.SupplierId,
                SupplyCategoryId = catId
            };
            await _context.SupplierStocks.AddAsync(stock);

            var link = new SupplyLink
            {
                Location=new Location(),
                LocationId = location.LocationId,
                SupplierId = supplier.SupplierId,
                SupplyCategoryId = catId
            };

            // Act
            var result = (object)(await _controller.CreateSupplyLink(link)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateSupplyLink_HasSupplyCategory_ShouldReturnBadRequest()
        {
            // Arrange
            var supplier = new Supplier
            {
                AbreviatedCountry = "IT",
                AbreviatedState = "LZ",
                City = "Roma",
                Country = "Italy",
                State = "Lazio",
                Name = "La Verdura-Roma"
            };
            await _context.Suppliers.AddAsync(supplier);

            var location = new Location
            {
                AbreviatedCountry = "IT",
                AbreviatedState = "LZ",
                City = "Roma",
                Street = "58 Continental Drive",
                Country = "Italy",
                State = "Lazio",
                OpenSince = new DateTime(2018, 10, 08),
                MenuId = await _context.Menus.Select(m => m.MenuId).FirstOrDefaultAsync(),
                ScheduleId = await _context.Schedules.Select(s => s.ScheduleId).FirstOrDefaultAsync()
            };
            await _context.Locations.AddAsync(location);

            var catId = await _context.SupplyCategories
                    .Select(c => c.SupplyCategoryId)
                    .FirstOrDefaultAsync();

            var stock = new SupplierStock
            {
                SupplierId = supplier.SupplierId,
                SupplyCategoryId = catId
            };
            await _context.SupplierStocks.AddAsync(stock);

            var link = new SupplyLink
            {
                SupplyCategory = new SupplyCategory(),
                LocationId = location.LocationId,
                SupplierId = supplier.SupplierId,
                SupplyCategoryId = catId
            };

            // Act
            var result = (object)(await _controller.CreateSupplyLink(link)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateSupplyLink_InexistentLocationId_ShouldReturnBadRequest()
        {
            // Arrange
            var supplier = new Supplier
            {
                AbreviatedCountry = "IT",
                AbreviatedState = "LZ",
                City = "Roma",
                Country = "Italy",
                State = "Lazio",
                Name = "La Verdura-Roma"
            };
            await _context.Suppliers.AddAsync(supplier);

            var catId = await _context.SupplyCategories
                    .Select(c => c.SupplyCategoryId)
                    .FirstOrDefaultAsync();

            var stock = new SupplierStock
            {
                SupplierId = supplier.SupplierId,
                SupplyCategoryId = catId
            };
            await _context.SupplierStocks.AddAsync(stock);

            var link = new SupplyLink
            {
                LocationId = await _context.Locations
                    .Select(l=>l.LocationId)
                    .LastOrDefaultAsync()+1,
                SupplierId = supplier.SupplierId,
                SupplyCategoryId = catId
            };

            // Act
            var result = (object)(await _controller.CreateSupplyLink(link)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateSupplyLink_InexistentSupplierId_ShouldReturnBadRequest()
        {
            // Arrange
            var location = new Location
            {
                AbreviatedCountry = "IT",
                AbreviatedState = "LZ",
                City = "Roma",
                Street = "58 Continental Drive",
                Country = "Italy",
                State = "Lazio",
                OpenSince = new DateTime(2018, 10, 08),
                MenuId = await _context.Menus.Select(m => m.MenuId).FirstOrDefaultAsync(),
                ScheduleId = await _context.Schedules.Select(s => s.ScheduleId).FirstOrDefaultAsync()
            };
            await _context.Locations.AddAsync(location);

            var catId = await _context.SupplyCategories
                    .Select(c => c.SupplyCategoryId)
                    .FirstOrDefaultAsync();

            var link = new SupplyLink
            {
                LocationId = location.LocationId,
                SupplierId = await _context.Suppliers
                    .Select(s=>s.SupplierId)
                    .LastOrDefaultAsync()+1,
                SupplyCategoryId = catId
            };

            // Act
            var result = (object)(await _controller.CreateSupplyLink(link)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateSupplyLink_InexistentSupplyCategoryId_ShouldReturnBadRequest()
        {
            // Arrange
            var supplier = new Supplier
            {
                AbreviatedCountry = "IT",
                AbreviatedState = "LZ",
                City = "Roma",
                Country = "Italy",
                State = "Lazio",
                Name = "La Verdura-Roma"
            };
            await _context.Suppliers.AddAsync(supplier);

            var location = new Location
            {
                AbreviatedCountry = "IT",
                AbreviatedState = "LZ",
                City = "Roma",
                Street = "58 Continental Drive",
                Country = "Italy",
                State = "Lazio",
                OpenSince = new DateTime(2018, 10, 08),
                MenuId = await _context.Menus.Select(m => m.MenuId).FirstOrDefaultAsync(),
                ScheduleId = await _context.Schedules.Select(s => s.ScheduleId).FirstOrDefaultAsync()
            };
            await _context.Locations.AddAsync(location);

            var catId = await _context.SupplyCategories
                    .Select(c => c.SupplyCategoryId)
                    .FirstOrDefaultAsync();

            var stock = new SupplierStock
            {
                SupplierId = supplier.SupplierId,
                SupplyCategoryId = catId
            };
            await _context.SupplierStocks.AddAsync(stock);

            var link = new SupplyLink
            {
                LocationId = location.LocationId,
                SupplierId = supplier.SupplierId,
                SupplyCategoryId = catId + 1
            };

            // Act
            var result = (object)(await _controller.CreateSupplyLink(link)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateSupplyLink_DuplicateLink_ShouldReturnBadRequest()
        {
            // Arrange
            var source = await _context.SupplyLinks
                .Select(l => new { l.LocationId, l.SupplierId, l.SupplyCategoryId })
                .FirstOrDefaultAsync();

            var link = new SupplyLink
            {
                LocationId = source.LocationId,
                SupplierId = source.SupplierId,
                SupplyCategoryId = source.SupplyCategoryId
            };

            // Act
            var result = (object)(await _controller.CreateSupplyLink(link)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateSupplyLink_SupplierDoesntStockSupplyCategory_ShouldReturnBadRequest()
        {
            // Arrange
            var supplier = new Supplier
            {
                AbreviatedCountry = "IT",
                AbreviatedState = "LZ",
                City = "Roma",
                Country = "Italy",
                State = "Lazio",
                Name = "La Verdura-Roma"
            };
            await _context.Suppliers.AddAsync(supplier);

            var location = new Location
            {
                AbreviatedCountry = "IT",
                AbreviatedState = "LZ",
                City = "Roma",
                Street = "58 Continental Drive",
                Country = "Italy",
                State = "Lazio",
                OpenSince = new DateTime(2018, 10, 08),
                MenuId = await _context.Menus.Select(m => m.MenuId).FirstOrDefaultAsync(),
                ScheduleId = await _context.Schedules.Select(s => s.ScheduleId).FirstOrDefaultAsync()
            };
            await _context.Locations.AddAsync(location);

            var catId = await _context.SupplyCategories
                    .Select(c => c.SupplyCategoryId)
                    .FirstOrDefaultAsync();

            var link = new SupplyLink
            {
                LocationId = location.LocationId,
                SupplierId = supplier.SupplierId,
                SupplyCategoryId = catId
            };

            // Act
            var result = (object)(await _controller.CreateSupplyLink(link)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        #endregion

        [TestMethod]
        public void SupplyLinkExists_ItDoes_ShoudReturnTrue()
        {
            //Arrange
            var methodInfo = typeof(SupplyLinksController).GetMethod("SupplyLinkExists",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var parameters = new object[] { GetFirstSupplyLinkId };

            // Act
            var result = ((methodInfo.Invoke(_controller, parameters)) as Task<bool>).Result;

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public void SupplyLinkExists_ItDoesNot_ShoudReturnFalse()
        {
            //Arrange
            var methodInfo = typeof(SupplyLinksController).GetMethod("SupplyLinkExists",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var parameters = new object[] { GenBadSupplyLinkId };

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
            _controller = new SupplyLinksController(_context, _logger);
        }

        /// Helpers

        int GetFirstSupplyLinkId => _context.SupplyLinks
            .Select(r => r.SupplyLinkId)
            .OrderBy(id => id)
            .FirstOrDefault();
        int GenBadSupplyLinkId => _context.SupplyLinks
            .Select(r => r.SupplyLinkId)
            .ToArray()
            .OrderBy(id => id)
            .LastOrDefault() + 1;
        private static void Setup()
        {
            _context = InMemoryHelpers.GetContext();
            _controller = new SupplyLinksController(_context, _logger);
        }
    }
}
