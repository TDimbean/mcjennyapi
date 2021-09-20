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
    public class StocksIntegrationTests
    {
        private static HttpClient _client;
        private static FoodChainsDbContext _context;
        private static WebApplicationFactory<Startup> _appFactory;

        [ClassInitialize]
        public static void InitClass(TestContext testContext)
        {
            _appFactory = new WebApplicationFactory<Startup>();
            _appFactory.ClientOptions.BaseAddress = new Uri("http://localhost/api/supplierstocks");
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
            _appFactory.ClientOptions.BaseAddress = new Uri("http://localhost/api/supplierstocks");
            _client = _appFactory.CreateClient();

            var fked = HelperFuck.MessedUpDB(_context);
            if (fked)
            {
                var fake = false;
            }
        }

        #region Deletes

        [TestMethod]
        public async Task DeleteStock_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var stockId = GenBadStockId;

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + stockId);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task DeleteStock_Last_ShouldDelete()
        {
            // Arrange
            var stockId = GenBadStockId;

            var supCount = await _context.Suppliers.CountAsync();
            var catCount = await _context.SupplyCategories.CountAsync();

            var unused = new Tuple<int, int>(0, 0);

            for (int i = 1; i <= supCount; i++)
            {
                for (int j = 1; j <= catCount; j++)
                {
                    if (!await _context.SupplierStocks.AnyAsync(s =>
                         s.SupplierId == i &&
                         s.SupplyCategoryId == j))
                    {
                        unused = new Tuple<int, int>(i, j);
                        break;
                    }
                }
                if (unused.Item1 != 0) break;
            }

            var supplierStock = new SupplierStock
            {
                SupplierId = unused.Item1,
                SupplyCategoryId = unused.Item2
            };
            await _context.SupplierStocks.AddAsync(supplierStock);
            await _context.SaveChangesAsync();

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + stockId);
            var getRes = await _client.GetAsync(_client.BaseAddress + "/" + stockId);

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            getRes.IsSuccessStatusCode.Should().BeFalse();
            getRes.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task DeleteStock_NotLast_ShouldSwitchWithLastThenDelThat()
        {
            // Arrange
            var stockId = GenBadStockId;

            var supCount = await _context.Suppliers.CountAsync();
            var catCount = await _context.SupplyCategories.CountAsync();

            var unused1 = new Tuple<int, int>(0, 0);
            var unused2 = new Tuple<int, int>(0, 0);

            for (int i = 1; i <= supCount; i++)
            {
                for (int j = 1; j <= catCount; j++)
                {
                    if (!await _context.SupplierStocks.AnyAsync(s =>
                         s.SupplierId == i &&
                         s.SupplyCategoryId == j))
                    {
                        if (unused1.Item1 == 0)
                            unused1 = new Tuple<int, int>(i, j);
                        else
                        {
                            unused2 = new Tuple<int, int>(i, j);
                            break;
                        }
                    }
                }
                if (unused1.Item1 != 0 && unused2.Item1 != 0) break;
            }

            var stock1 = new SupplierStock
            {
                SupplierId = unused1.Item1,
                SupplyCategoryId = unused1.Item2
            };
            var stock2 = new SupplierStock
            {
                SupplierId = unused1.Item1,
                SupplyCategoryId = unused1.Item2
            };
            await _context.SupplierStocks.AddRangeAsync(new SupplierStock[] { stock1, stock2 });
            await _context.SaveChangesAsync();

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + stockId);
            var getRes = await _client.GetAsync(_client.BaseAddress + "/" + stockId + "/basic");
            var getCont = await getRes.Content.ReadAsStringAsync();
            var getStock = JsonConvert.DeserializeObject(getCont, typeof(SupplierStock)) as SupplierStock;

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            getStock.SupplierId.Should().Be(stock2.SupplierId);
            getStock.SupplyCategoryId.Should().Be(stock2.SupplyCategoryId);

            // Clean-up
            await _client.DeleteAsync(_client.BaseAddress + "/" + stockId);
        }

        #endregion

        #region Creates

        [TestMethod]
        public async Task CreateSupplierStock_HappyFlow_ShouldCreateAndReturnSupplierStock()
        {
            // Arrange
            var stockId = await _context.SupplierStocks.CountAsync() + 1;
            var supCount = await _context.Suppliers.CountAsync();
            var catCount = await _context.SupplyCategories.CountAsync();

            var stockIds = new Tuple<int, int>(0, 0);
            for (int i = 1; i <= supCount; i++)
            {
                for (int j = 1; j <= catCount; j++)
                {
                    if (!await _context.SupplierStocks.AnyAsync(s=>
                        s.SupplierId==i&&s.SupplyCategoryId==j))
                    {
                        stockIds = new Tuple<int, int>(i, j);
                        break;
                    }
                }
                if (stockIds.Item1 != 0) break;
            }

            var stock = new SupplierStock
            {
                SupplierId = stockIds.Item1,
                SupplyCategoryId = stockIds.Item2
            };
            var stockCont = new StringContent(JsonConvert.SerializeObject(stock),
                    Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, stockCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();

            // Clean-up
            await _client.DeleteAsync(_client.BaseAddress + "/" + stockId);
        }

        [TestMethod]
        public async Task CreateSupplierStock_TriesToSetId_ShouldReturnBadRequest()
        {
            // Arrange
            var stockId = await _context.SupplierStocks.CountAsync() + 1;
            var supCount = await _context.Suppliers.CountAsync();
            var catCount = await _context.SupplyCategories.CountAsync();

            var stockIds = new Tuple<int, int>(0, 0);
            for (int i = 1; i <= supCount; i++)
            {
                for (int j = 1; j <= catCount; j++)
                {
                    if (!await _context.SupplierStocks.AnyAsync(s =>
                        s.SupplierId == i && s.SupplyCategoryId == j))
                    {
                        stockIds = new Tuple<int, int>(i, j);
                        break;
                    }
                }
                if (stockIds.Item1 != 0) break;
            }

            var stock = new SupplierStock
            {
                SupplierId = stockIds.Item1,
                SupplyCategoryId = stockIds.Item2,
                SupplierStockId = stockId
            };
            var stockCont = new StringContent(JsonConvert.SerializeObject(stock),
                    Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, stockCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateSupplierStock_HasSupplier_ShouldReturnBadRequest()
        {
            // Arrange
            var supCount = await _context.Suppliers.CountAsync();
            var catCount = await _context.SupplyCategories.CountAsync();

            var stockIds = new Tuple<int, int>(0, 0);
            for (int i = 1; i <= supCount; i++)
            {
                for (int j = 1; j <= catCount; j++)
                {
                    if (!await _context.SupplierStocks.AnyAsync(s =>
                        s.SupplierId == i && s.SupplyCategoryId == j))
                    {
                        stockIds = new Tuple<int, int>(i, j);
                        break;
                    }
                }
                if (stockIds.Item1 != 0) break;
            }

            var stock = new SupplierStock
            {
                SupplierId = stockIds.Item1,
                SupplyCategoryId = stockIds.Item2,
                Supplier = new Supplier()
            };
            var stockCont = new StringContent(JsonConvert.SerializeObject(stock),
                    Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, stockCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateSupplierStock_HasSupplyCategory_ShouldReturnBadRequest()
        {
            // Arrange
            var supCount = await _context.Suppliers.CountAsync();
            var catCount = await _context.SupplyCategories.CountAsync();

            var stockIds = new Tuple<int, int>(0, 0);
            for (int i = 1; i <= supCount; i++)
            {
                for (int j = 1; j <= catCount; j++)
                {
                    if (!await _context.SupplierStocks.AnyAsync(s =>
                        s.SupplierId == i && s.SupplyCategoryId == j))
                    {
                        stockIds = new Tuple<int, int>(i, j);
                        break;
                    }
                }
                if (stockIds.Item1 != 0) break;
            }

            var stock = new SupplierStock
            {
                SupplierId = stockIds.Item1,
                SupplyCategoryId = stockIds.Item2,
                SupplyCategory = new SupplyCategory()
            };
            var stockCont = new StringContent(JsonConvert.SerializeObject(stock),
                    Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, stockCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateSupplierStock_InexistentSupplyCategoryId_ShouldReturnBadRequest()
        {
            // Arrange
            var supCount = await _context.Suppliers.CountAsync();
            var catCount = await _context.SupplyCategories.CountAsync();

            var stockIds = new Tuple<int, int>(0, 0);
            for (int i = 1; i <= supCount; i++)
            {
                for (int j = 1; j <= catCount; j++)
                {
                    if (!await _context.SupplierStocks.AnyAsync(s =>
                        s.SupplierId == i && s.SupplyCategoryId == j))
                    {
                        stockIds = new Tuple<int, int>(i, j);
                        break;
                    }
                }
                if (stockIds.Item1 != 0) break;
            }

            var stock = new SupplierStock
            {
                SupplierId = stockIds.Item1,
                SupplyCategoryId = catCount+1
            };
            var stockCont = new StringContent(JsonConvert.SerializeObject(stock),
                    Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, stockCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateSupplierStock_InexistentSupplierId_ShouldReturnBadRequest()
        {
            // Arrange
            var supCount = await _context.Suppliers.CountAsync();
            var catCount = await _context.SupplyCategories.CountAsync();

            var stockIds = new Tuple<int, int>(0, 0);
            for (int i = 1; i <= supCount; i++)
            {
                for (int j = 1; j <= catCount; j++)
                {
                    if (!await _context.SupplierStocks.AnyAsync(s =>
                        s.SupplierId == i && s.SupplyCategoryId == j))
                    {
                        stockIds = new Tuple<int, int>(i, j);
                        break;
                    }
                }
                if (stockIds.Item1 != 0) break;
            }

            var stock = new SupplierStock
            {
                SupplyCategoryId = stockIds.Item2,
                SupplierId = supCount+1
            };
            var stockCont = new StringContent(JsonConvert.SerializeObject(stock),
                    Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, stockCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateSupplierStock_Dupe_ShouldReturnBadRequest()
        {
            // Arrange
            var dupe = await _context.SupplierStocks.FirstOrDefaultAsync();

            var stock = new SupplierStock
            {
                SupplierId = dupe.SupplierId,
                SupplyCategoryId = dupe.SupplyCategoryId
            };
            var stockCont = new StringContent(JsonConvert.SerializeObject(stock),
                    Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, stockCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        #endregion

        #region Gets

        [TestMethod]
        public async Task GetSupplierStocks_HappyFlow_ShouldReturnAllSupplierStocks()
        {
            // Arrange
            var stocks = await _context.SupplierStocks
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

            var expected = new string[stocks.Length];
            for (int i = 0; i < stocks.Length; i++)
            {
                var cat = cats.SingleOrDefault(c => c.SupplyCategoryId == stocks[i].SupplyCategoryId).Name;
                var sup = sups.SingleOrDefault(s => s.SupplierId == stocks[i].SupplierId);

                expected[i] = string.Format("Stock [{0}]: ({1}) {2}, {3}, {4}{5} stocks ({6}) {7}",
                    stocks[i].SupplierStockId,
                    stocks[i].SupplierId,
                    sup.Name, sup.AbreviatedCountry,
                    sup.AbreviatedState == "N/A" ? string.Empty :
                    sup.AbreviatedState + ", ",
                    sup.City,
                    stocks[i].SupplyCategoryId,
                    cat);
            }

            // Act
            var result = await _client.GetAsync(string.Empty);
            var content = await result.Content.ReadAsStringAsync();
            var stks = JsonConvert.DeserializeObject(content, typeof(List<string>)) as List<string>;

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            stks.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSupplierStock_HappyFlow_ShouldReturnSupplierStock()
        {
            // Arrange
            var stockId = GetFirstStockId;
            var stock = await _context.SupplierStocks
                            .FindAsync(stockId);

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

            var expected = string.Format("({0}) {1}, {2}, {3}{4} stocks ({5}) {6}",
                    sup.SupplierId,
                    sup.Name, sup.AbreviatedCountry,
                    sup.AbreviatedState == "N/A" ? string.Empty :
                    sup.AbreviatedState + ", ",
                    sup.City,
                    stock.SupplyCategoryId,
                    cat);

            // Act
            var result = await _client.GetAsync(_client.BaseAddress + "/" + stockId);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSupplierStock_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var badId = GenBadStockId;

            // Act
            var result = await _client.GetAsync(_client.BaseAddress + "/" + badId);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task GetSupplierStockBasic_HappyFlow_ShouldReturnRequirement()
        {
            // Arrange
            var stockId = GetFirstStockId;
            var expected = await _context.SupplierStocks.FindAsync(stockId);

            // Act
            var result = await _client.GetAsync(_client.BaseAddress + "/" + stockId + "/basic");
            var content = await result.Content.ReadAsStringAsync();
            var stock = JsonConvert.DeserializeObject(content, typeof(SupplierStock)) as SupplierStock;

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            stock.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSupplierStockBasic_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var badId = GenBadStockId;

            // Act
            var result = await _client.GetAsync(_client.BaseAddress + "/" + badId + "/basic");

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        #endregion

        //Helpers

        private int GenBadStockId => _context.SupplierStocks.Count() + 1;

        private int GetFirstStockId => _context.SupplierStocks.First().SupplierStockId;
    }
}
