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
    public class SupplierStockControllerTests
    {
        private static SupplierStocksController _controller;
        private static FoodChainsDbContext _context;
        private static ILogger<SupplierStocksController> _logger;

        [ClassInitialize]
        public static void Init(TestContext testContext)
        {
            _logger = (new Mock<ILogger<SupplierStocksController>>()).Object;
            Setup();
        }

        [TestInitialize]
        public void InitTest() => Setup();

        #region Gets

        #region Basic

        [TestMethod]
        public async Task GetSupplierStocks_HappyFlow_ShouldReturnAllSupplierStocks()
        {
            // Arrange
            var stocks = await _context.SupplierStocks
                            .Select(s => new { s.SupplierStockId, s.SupplierId, s.SupplyCategoryId })
                            .ToArrayAsync();

            var catIds = stocks.Select(s => s.SupplyCategoryId).Distinct().ToArray();
            var supIds = stocks.Select(s => s.SupplierId).Distinct().ToArray();

            var cats = await _context.SupplyCategories
                .Where(c => catIds.Contains(c.SupplyCategoryId))
                .Select(c => new { c.SupplyCategoryId, c.Name })
                .ToArrayAsync();

            var sups = await _context.Suppliers
                .Where(s => supIds.Contains(s.SupplierId))
                .Select(s => new {
                    s.SupplierId,
                    s.AbreviatedCountry,
                    s.AbreviatedState,
                    s.City,
                    s.Name
                })
                .ToArrayAsync();

            var strings = new string[stocks.Length];
            for (int i = 0; i < stocks.Length; i++)
            {
                var cat = cats.SingleOrDefault(c => c.SupplyCategoryId == stocks[i].SupplyCategoryId).Name;
                var sup = sups.SingleOrDefault(s => s.SupplierId == stocks[i].SupplierId);

                strings[i] = string.Format("Stock [{0}]: ({1}) {2}, {3}, {4}{5} stocks ({6}) {7}",
                    stocks[i].SupplierStockId,
                    stocks[i].SupplierId,
                    sup.Name, sup.AbreviatedCountry,
                    sup.AbreviatedState == "N/A" ? string.Empty :
                    sup.AbreviatedState + ", ",
                    sup.City,
                    stocks[i].SupplyCategoryId,
                    cat);
            }

            var expected = (object)strings;

            // Act
            var result = (object)(await _controller.GetSupplierStocks()).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSupplierStock_HappyFlow_ShouldReturnSupplierStock()
        {
            // Arrange
            var stock = await _context.SupplierStocks
                            .Select(s => new { s.SupplierStockId, s.SupplierId, s.SupplyCategoryId })
                            .FirstOrDefaultAsync(s => s.SupplierStockId == GetFirstSupplierStockId);

            var cat = (await _context.SupplyCategories.FindAsync(stock.SupplyCategoryId)).Name;
            var sup = await _context.Suppliers
                .Select(s => new
                {
                    s.SupplierId,
                    s.AbreviatedCountry,
                    s.AbreviatedState,
                    s.City,
                    s.Name
                })
                .FirstOrDefaultAsync(s => s.SupplierId == stock.SupplierId);

            var expected = (object) string.Format("({0}) {1}, {2}, {3}{4} stocks ({5}) {6}",
                    sup.SupplierId,
                    sup.Name, sup.AbreviatedCountry,
                    sup.AbreviatedState == "N/A" ? string.Empty :
                    sup.AbreviatedState + ", ",
                    sup.City,
                    stock.SupplyCategoryId,
                    cat);

            // Act
            var result = (object)(await _controller
                .GetSupplierStock(GetFirstSupplierStockId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSupplierStock_InexistentId_ShouldReturnNotFound()
        {
            // Act
            var result = (object)(await _controller
                .GetSupplierStock(GenBadSupplierStockId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetSupplierStockBasic_HappyFlow_ShouldReturnRequirement()
        {
            // Arrange
            var expected = (object)(await _context.SupplierStocks.FirstOrDefaultAsync());

            // Act
            var result = (object)(await _controller
                .GetSupplierStockBasic(GetFirstSupplierStockId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSupplierStockBasic_InexistentId_ShouldReturnNotFound()
        {
            // Act
            var result = (object)(await _controller
                .GetSupplierStockBasic(GenBadSupplierStockId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        #endregion

        #endregion

        #region Creates

        [TestMethod]
        public async Task CreateSupplierStock_HappyFlow_ShouldCreateAndReturnSupplierStock()
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

            var stock = new SupplierStock
            {
                SupplierId = supplier.SupplierId,
                SupplyCategoryId = await _context.SupplyCategories
                    .Select(c=>c.SupplyCategoryId)
                    .FirstOrDefaultAsync()
            };

            // Act
            await _controller.CreateSupplierStock(stock);
            var result = (object)(await _controller.GetSupplierStockBasic(stock.SupplierStockId)).Value;

            var expected = (object)stock;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task CreateSupplierStock_TriesToSetId_ShouldReturnBadRequest()
        {
            var supplier = new Supplier
            {
                AbreviatedCountry = "IT",
                AbreviatedState = "LZ",
                City = "Roma",
                Country = "Italy",
                State = "Lazio",
                Name = "La Verdura-Roma"
            } ;
            await _context.Suppliers.AddAsync(supplier);

            var stock = new SupplierStock
            {
                SupplierStockId = await _context.SupplierStocks
                    .Select(s=>s.SupplierStockId)
                    .LastOrDefaultAsync()+1,
                SupplierId = supplier.SupplierId,
                SupplyCategoryId = await _context.SupplyCategories
                    .Select(c => c.SupplyCategoryId)
                    .FirstOrDefaultAsync()
            };

            // Act
            var result = (object)(await _controller.CreateSupplierStock(stock)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateSupplierStock_HasSupplier_ShouldReturnBadRequest()
        {
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

            var stock = new SupplierStock
            {
                Supplier = new Supplier(),
                SupplierId = supplier.SupplierId,
                SupplyCategoryId = await _context.SupplyCategories
                    .Select(c => c.SupplyCategoryId)
                    .FirstOrDefaultAsync()
            };

            // Act
            var result = (object)(await _controller.CreateSupplierStock(stock)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateSupplierStock_HasSupplyCategory_ShouldReturnBadRequest()
        {
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

            var stock = new SupplierStock
            {
                SupplyCategory = new SupplyCategory(),
                SupplierId = supplier.SupplierId,
                SupplyCategoryId = await _context.SupplyCategories
                    .Select(c => c.SupplyCategoryId)
                    .FirstOrDefaultAsync()
            };

            // Act
            var result = (object)(await _controller.CreateSupplierStock(stock)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateSupplierStock_InexistentSupplyCategoryId_ShouldReturnBadRequest()
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

            var stock = new SupplierStock
            {
                SupplierId = supplier.SupplierId,
                SupplyCategoryId = await _context.SupplyCategories
                    .Select(c => c.SupplyCategoryId)
                    .LastOrDefaultAsync() + 1
            };

            // Act
            var result = (object)(await _controller.CreateSupplierStock(stock)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateSupplierStock_InexistentSupplierId_ShouldReturnBadRequest()
        {
            var stock = new SupplierStock
            {
                SupplierId = await _context.Suppliers
                    .Select(s=>s.SupplierId)
                    .LastOrDefaultAsync()+1,
                SupplyCategoryId = await _context.SupplyCategories
                    .Select(c => c.SupplyCategoryId)
                    .FirstOrDefaultAsync()
            };

            // Act
            var result = (object)(await _controller.CreateSupplierStock(stock)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateSupplierStock_Dupe_ShouldReturnBadRequest()
        {
            //Arrange
            var source = await _context.SupplierStocks
                .Select(s => new { s.SupplierId, s.SupplyCategoryId })
                .FirstOrDefaultAsync();

            var stock = new SupplierStock
            {
                SupplierId = source.SupplierId,
                SupplyCategoryId = source.SupplyCategoryId
            };

            // Act
            var result = (object)(await _controller.CreateSupplierStock(stock)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        #endregion

        [TestMethod]
        public void SupplierStockExists_ItDoes_ShoudReturnTrue()
        {
            //Arrange
            var methodInfo = typeof(SupplierStocksController).GetMethod("SupplierStockExists",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var parameters = new object[] { GetFirstSupplierStockId };

            // Act
            var result = ((methodInfo.Invoke(_controller, parameters)) as Task<bool>).Result;

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public void SupplierStockExists_ItDoesNot_ShoudReturnFalse()
        {
            //Arrange
            var methodInfo = typeof(SupplierStocksController).GetMethod("SupplierStockExists",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var parameters = new object[] { GenBadSupplierStockId };

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
            _controller = new SupplierStocksController(_context, _logger);
        }

        /// Helpers

        int GetFirstSupplierStockId => _context.SupplierStocks
            .Select(r => r.SupplierStockId)
            .OrderBy(id => id)
            .FirstOrDefault();
        int GenBadSupplierStockId => _context.SupplierStocks
            .Select(r => r.SupplierStockId)
            .ToArray()
            .OrderBy(id => id)
            .LastOrDefault() + 1;

        private static void Setup()
        {
            _context = InMemoryHelpers.GetContext();
            _controller = new SupplierStocksController(_context, _logger);
        }
    }
}
