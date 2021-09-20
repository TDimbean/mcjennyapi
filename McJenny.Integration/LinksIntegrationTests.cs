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
    public class LinksIntegrationTests
    {
        private static HttpClient _client;
        private static FoodChainsDbContext _context;
        private static WebApplicationFactory<Startup> _appFactory;

        [ClassInitialize]
        public static void InitClass(TestContext testContext)
        {
            _appFactory = new WebApplicationFactory<Startup>();
            _appFactory.ClientOptions.BaseAddress = new Uri("http://localhost/api/supplylinks");
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
            _appFactory.ClientOptions.BaseAddress = new Uri("http://localhost/api/supplylinks");
            _client = _appFactory.CreateClient();

            //var messedDb = _context.Locations.Any(l => l.LocationId > 500);
            //if (messedDb)
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

        #region Deletes
    
        [TestMethod]
        public async Task DeleteLink_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var linkId = GenBadSupplyLinkId;

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + linkId);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task DeleteLink_Last_ShouldDelete()
        {
            // Arrange
            var linkId = GenBadSupplyLinkId;

            var locCount = await _context.Locations.CountAsync();
            var supCount = await _context.Suppliers.CountAsync();
            var catCount = await _context.SupplyCategories.CountAsync();

            var unused = (LocationId: 0, SupplierId: 0, CategoryId: 0);

            for (int i = 1; i <= locCount; i++)
            {
                for (int j = 1; j <= supCount; j++)
                {
                    for (int k = 1; k <= catCount; k++)
                    {
                        if (!await _context.SupplyLinks.AnyAsync(l =>
                             l.LocationId == i &&
                             l.SupplierId == j &&
                             l.SupplyCategoryId==k))
                        {
                            unused = (i, j, k);
                            break;
                        }
                    }
                    if (unused.LocationId != 0) break;
                }
                if (unused.LocationId != 0) break;
            }

            var link = new SupplyLink
            {
                LocationId = unused.LocationId,
                SupplierId = unused.SupplierId,
                SupplyCategoryId = unused.CategoryId
            };
            await _context.SupplyLinks.AddAsync(link);
            await _context.SaveChangesAsync();

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + linkId);
            var getRes = await _client.GetAsync(_client.BaseAddress + "/" + linkId);

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            getRes.IsSuccessStatusCode.Should().BeFalse();
            getRes.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task DeleteLink_ShouldSwitchWithLastThenDelThat()
        {
            // Arrange
            var linkId = GenBadSupplyLinkId;

            var locCount = await _context.Locations.CountAsync();
            var supCount = await _context.Suppliers.CountAsync();
            var catCount = await _context.SupplyCategories.CountAsync();


            var unused1 = (LocationId: 0, SupplierId: 0, CategoryId: 0);
            var unused2 = (LocationId: 0, SupplierId: 0, CategoryId: 0);

            for (int i = 1; i <= locCount; i++)
            {
                for (int j = 1; j <= supCount; j++)
                {
                    for (int k = 1; k <= catCount; k++)
                    {

                        if (!await _context.Managements.AnyAsync(m =>
                             m.LocationId == i &&
                             m.ManagerId == j))
                        {
                            if (unused1.LocationId == 0)
                                unused1 = (i,j,k);
                            else
                            {
                                unused2 = (i,j,k);
                                break;
                            }
                        }
                    }
                    if (unused1.LocationId != 0 && unused2.LocationId != 0) break;
                }
                if (unused1.LocationId != 0 && unused2.LocationId != 0) break;
            }

            var link1 = new SupplyLink
            {
                LocationId = unused1.LocationId,
                SupplierId = unused1.SupplierId,
                SupplyCategoryId = unused1.CategoryId
            };
            var link2 = new SupplyLink
            {
                LocationId = unused1.LocationId,
                SupplierId = unused1.SupplierId,
                SupplyCategoryId = unused1.CategoryId
            };
            await _context.SupplyLinks.AddRangeAsync(new SupplyLink[] { link1, link2 });
            await _context.SaveChangesAsync();

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + linkId);
            var getRes = await _client.GetAsync(_client.BaseAddress + "/" + linkId + "/basic");
            var getCont = await getRes.Content.ReadAsStringAsync();
            var getItm = JsonConvert.DeserializeObject(getCont, typeof(SupplyLink)) as SupplyLink;

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            getItm.LocationId.Should().Be(link2.LocationId);
            getItm.SupplierId.Should().Be(link2.SupplierId);
            getItm.SupplyCategoryId.Should().Be(link2.SupplyCategoryId);

            // Clean-up
            await _client.DeleteAsync(_client.BaseAddress + "/" + linkId);
        }

        #endregion

        #region Creates

        [TestMethod]
        public async Task CreateSupplyLink_HappyFlow_ShouldCreateAndReturnSupplyLink()
        {
            // Arrange
            var locCount = await _context.Locations.CountAsync();
            var supCount = await _context.Suppliers.CountAsync();
            var catCount = await _context.SupplyCategories.CountAsync();

            var linkIds = new Tuple<int,int,int>(0,0,0);

            for (int i = 1; i <= locCount; i++)
            {
                for (int j = 1; j <= supCount; j++)
                {
                    for (int k = 1; k <= catCount; k++)
                    {
                        if ((!await _context.SupplyLinks.AnyAsync(l =>
                        l.LocationId == i && l.SupplierId == j && l.SupplyCategoryId == k))&&
                        (await _context.SupplierStocks.AnyAsync(s=>s.SupplierId==j&&s.SupplyCategoryId==k)))
                        {
                            linkIds = new Tuple<int, int, int>(i, j, k);
                            break;
                        }
                    }
                    if (linkIds.Item1 != 0) break;
                }
                    if (linkIds.Item1 != 0) break;
            }

            var link = new SupplyLink
            {
                LocationId = linkIds.Item1,
                SupplierId = linkIds.Item2,
                SupplyCategoryId = linkIds.Item3
            };
            var linkId = await _context.SupplyLinks.CountAsync() + 1;
            var linkCont = new StringContent(JsonConvert.SerializeObject(link),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, linkCont);
            var getRes = await _client.GetAsync(_client.BaseAddress + "/" + linkId + "/basic");
            var getCont = await getRes.Content.ReadAsStringAsync();
            var getLink = JsonConvert.DeserializeObject(getCont, typeof(SupplyLink)) as SupplyLink;

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();

            // Clean-up
            await _client.DeleteAsync(_client.BaseAddress + "/" + linkId);
        }

        [TestMethod]
        public async Task CreateSupplyLink_TriesToSetId_ShouldReturnBadRequest()
        {
            // Arrange
            var locCount = await _context.Locations.CountAsync();
            var supCount = await _context.Suppliers.CountAsync();
            var catCount = await _context.SupplyCategories.CountAsync();

            var linkIds = new Tuple<int, int, int>(0, 0, 0);

            for (int i = 1; i <= locCount; i++)
            {
                for (int j = 1; j <= supCount; j++)
                {
                    for (int k = 1; k <= catCount; k++)
                    {
                        if (!await _context.SupplyLinks.AnyAsync(l =>
                        l.LocationId == i && l.SupplierId == j && l.SupplyCategoryId == k) &&
                        (await _context.SupplierStocks.AnyAsync(s => s.SupplierId == j && s.SupplyCategoryId == k)))
                        {
                            linkIds = new Tuple<int, int, int>(i, j, k);
                            break;
                        }
                    }
                    if (linkIds.Item1 != 0) break;
                }
                if (linkIds.Item1 != 0) break;
            }
            var linkId = await _context.SupplyLinks.CountAsync() + 1;

            var link = new SupplyLink
            {
                LocationId = linkIds.Item1,
                SupplierId = linkIds.Item2,
                SupplyCategoryId = linkIds.Item3,
                SupplyLinkId = linkId
            };
            var linkCont = new StringContent(JsonConvert.SerializeObject(link),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, linkCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateSupplyLink_HasSupplier_ShouldReturnBadRequest()
        {
            // Arrange
            var locCount = await _context.Locations.CountAsync();
            var supCount = await _context.Suppliers.CountAsync();
            var catCount = await _context.SupplyCategories.CountAsync();

            var linkIds = new Tuple<int, int, int>(0, 0, 0);

            for (int i = 1; i <= locCount; i++)
            {
                for (int j = 1; j <= supCount; j++)
                {
                    for (int k = 1; k <= catCount; k++)
                    {
                        if (!await _context.SupplyLinks.AnyAsync(l =>
                        l.LocationId == i && l.SupplierId == j && l.SupplyCategoryId == k) &&
                        (await _context.SupplierStocks.AnyAsync(s => s.SupplierId == j && s.SupplyCategoryId == k)))
                        {
                            linkIds = new Tuple<int, int, int>(i, j, k);
                            break;
                        }
                    }
                    if (linkIds.Item1 != 0) break;
                }
                if (linkIds.Item1 != 0) break;
            }
            var linkId = await _context.SupplyLinks.CountAsync() + 1;

            var link = new SupplyLink
            {
                LocationId = linkIds.Item1,
                SupplierId = linkIds.Item2,
                SupplyCategoryId = linkIds.Item3,
                Supplier = new Supplier()
            };
            var linkCont = new StringContent(JsonConvert.SerializeObject(link),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, linkCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateSupplyLink_HasLocation_ShouldReturnBadRequest()
        {
            // Arrange
            var locCount = await _context.Locations.CountAsync();
            var supCount = await _context.Suppliers.CountAsync();
            var catCount = await _context.SupplyCategories.CountAsync();

            var linkIds = new Tuple<int, int, int>(0, 0, 0);

            for (int i = 1; i <= locCount; i++)
            {
                for (int j = 1; j <= supCount; j++)
                {
                    for (int k = 1; k <= catCount; k++)
                    {
                        if (!await _context.SupplyLinks.AnyAsync(l =>
                        l.LocationId == i && l.SupplierId == j && l.SupplyCategoryId == k) &&
                        (await _context.SupplierStocks.AnyAsync(s => s.SupplierId == j && s.SupplyCategoryId == k)))
                        {
                            linkIds = new Tuple<int, int, int>(i, j, k);
                            break;
                        }
                    }
                    if (linkIds.Item1 != 0) break;
                }
                if (linkIds.Item1 != 0) break;
            }
            var linkId = await _context.SupplyLinks.CountAsync() + 1;

            var link = new SupplyLink
            {
                LocationId = linkIds.Item1,
                SupplierId = linkIds.Item2,
                SupplyCategoryId = linkIds.Item3,
                Location = new Location()
            };
            var linkCont = new StringContent(JsonConvert.SerializeObject(link),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, linkCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateSupplyLink_HasSupplyCategory_ShouldReturnBadRequest()
        {
            // Arrange
            var locCount = await _context.Locations.CountAsync();
            var supCount = await _context.Suppliers.CountAsync();
            var catCount = await _context.SupplyCategories.CountAsync();

            var linkIds = new Tuple<int, int, int>(0, 0, 0);

            for (int i = 1; i <= locCount; i++)
            {
                for (int j = 1; j <= supCount; j++)
                {
                    for (int k = 1; k <= catCount; k++)
                    {
                        if (!await _context.SupplyLinks.AnyAsync(l =>
                        l.LocationId == i && l.SupplierId == j && l.SupplyCategoryId == k) &&
                        (await _context.SupplierStocks.AnyAsync(s => s.SupplierId == j && s.SupplyCategoryId == k)))
                        {
                            linkIds = new Tuple<int, int, int>(i, j, k);
                            break;
                        }
                    }
                    if (linkIds.Item1 != 0) break;
                }
                if (linkIds.Item1 != 0) break;
            }
            var linkId = await _context.SupplyLinks.CountAsync() + 1;

            var link = new SupplyLink
            {
                LocationId = linkIds.Item1,
                SupplierId = linkIds.Item2,
                SupplyCategoryId = linkIds.Item3,
                SupplyCategory = new SupplyCategory()
            };
            var linkCont = new StringContent(JsonConvert.SerializeObject(link),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, linkCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateSupplyLink_InexistentLocationId_ShouldReturnBadRequest()
        {
            // Arrange
            var locCount = await _context.Locations.CountAsync();
            var supCount = await _context.Suppliers.CountAsync();
            var catCount = await _context.SupplyCategories.CountAsync();

            var linkIds = new Tuple<int, int, int>(0, 0, 0);

            for (int i = 1; i <= locCount; i++)
            {
                for (int j = 1; j <= supCount; j++)
                {
                    for (int k = 1; k <= catCount; k++)
                    {
                        if (!await _context.SupplyLinks.AnyAsync(l =>
                        l.LocationId == i && l.SupplierId == j && l.SupplyCategoryId == k) &&
                        (await _context.SupplierStocks.AnyAsync(s => s.SupplierId == j && s.SupplyCategoryId == k)))
                        {
                            linkIds = new Tuple<int, int, int>(i, j, k);
                            break;
                        }
                    }
                    if (linkIds.Item1 != 0) break;
                }
                if (linkIds.Item1 != 0) break;
            }
            var linkId = await _context.SupplyLinks.CountAsync() + 1;

            var link = new SupplyLink
            {
                SupplierId = linkIds.Item2,
                SupplyCategoryId = linkIds.Item3,
                LocationId = locCount+1
            };
            var linkCont = new StringContent(JsonConvert.SerializeObject(link),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, linkCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateSupplyLink_InexistentSupplierId_ShouldReturnBadRequest()
        {
            // Arrange
            var locCount = await _context.Locations.CountAsync();
            var supCount = await _context.Suppliers.CountAsync();
            var catCount = await _context.SupplyCategories.CountAsync();

            var linkIds = new Tuple<int, int, int>(0, 0, 0);

            for (int i = 1; i <= locCount; i++)
            {
                for (int j = 1; j <= supCount; j++)
                {
                    for (int k = 1; k <= catCount; k++)
                    {
                        if (!await _context.SupplyLinks.AnyAsync(l =>
                        l.LocationId == i && l.SupplierId == j && l.SupplyCategoryId == k) &&
                        (await _context.SupplierStocks.AnyAsync(s => s.SupplierId == j && s.SupplyCategoryId == k)))
                        {
                            linkIds = new Tuple<int, int, int>(i, j, k);
                            break;
                        }
                    }
                    if (linkIds.Item1 != 0) break;
                }
                if (linkIds.Item1 != 0) break;
            }
            var linkId = await _context.SupplyLinks.CountAsync() + 1;

            var link = new SupplyLink
            {
                SupplyCategoryId = linkIds.Item3,
                LocationId = linkIds.Item1,
                SupplierId = supCount+1
            };
            var linkCont = new StringContent(JsonConvert.SerializeObject(link),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, linkCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateSupplyLink_InexistentSupplyCategoryId_ShouldReturnBadRequest()
        {
            // Arrange
            var locCount = await _context.Locations.CountAsync();
            var supCount = await _context.Suppliers.CountAsync();
            var catCount = await _context.SupplyCategories.CountAsync();

            var linkIds = new Tuple<int, int, int>(0, 0, 0);

            for (int i = 1; i <= locCount; i++)
            {
                for (int j = 1; j <= supCount; j++)
                {
                    for (int k = 1; k <= catCount; k++)
                    {
                        if (!await _context.SupplyLinks.AnyAsync(l =>
                        l.LocationId == i && l.SupplierId == j && l.SupplyCategoryId == k))
                        {
                            linkIds = new Tuple<int, int, int>(i, j, k);
                            break;
                        }
                    }
                    if (linkIds.Item1 != 0) break;
                }
                if (linkIds.Item1 != 0) break;
            }
            var linkId = await _context.SupplyLinks.CountAsync() + 1;
            var badCatId = await _context.SupplyCategories.CountAsync() + 1;

            var link = new SupplyLink
            {
                SupplierId = linkIds.Item2,
                LocationId = linkIds.Item1,
                SupplyCategoryId = badCatId
            };
            var linkCont = new StringContent(JsonConvert.SerializeObject(link),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, linkCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateSupplyLink_DuplicateLink_ShouldReturnBadRequest()
        {
            // Arrange
            var dupe = await _context.SupplyLinks.FirstOrDefaultAsync();

            var link = new SupplyLink
            {
                SupplierId = dupe.SupplierId,
                SupplyCategoryId = dupe.SupplyCategoryId,
                LocationId = dupe.LocationId
            };
            var linkCont = new StringContent(JsonConvert.SerializeObject(link),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, linkCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateSupplyLink_SupplierDoesntStockSupplyCategory_ShouldReturnBadRequest()
        {
            // Arrange
            var locCount = await _context.Locations.CountAsync();
            var supCount = await _context.Suppliers.CountAsync();
            var catCount = await _context.SupplyCategories.CountAsync();

            var linkIds = new Tuple<int, int, int>(0, 0, 0);

            for (int i = 1; i <= locCount; i++)
            {
                for (int j = 1; j <= supCount; j++)
                {
                    for (int k = 1; k <= catCount; k++)
                    {
                        if ((!await _context.SupplyLinks.AnyAsync(l =>
                        l.LocationId == i && l.SupplierId == j && l.SupplyCategoryId == k)) &&
                        (!await _context.SupplierStocks.AnyAsync(s => s.SupplierId == j && s.SupplyCategoryId == k)))
                        {
                            linkIds = new Tuple<int, int, int>(i, j, k);
                            break;
                        }
                    }
                    if (linkIds.Item1 != 0) break;
                }
                if (linkIds.Item1 != 0) break;
            }
            var linkId = await _context.SupplyLinks.CountAsync() + 1;

            var link = new SupplyLink
            {
                SupplierId = linkIds.Item2,
                SupplyCategoryId = linkIds.Item3,
                LocationId = linkIds.Item1
            };
            var linkCont = new StringContent(JsonConvert.SerializeObject(link),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, linkCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        #endregion

        #region Gets

        [TestMethod]
        public async Task GetSupplyLinks_HappyFlow_ShouldReturnAllSupplyLinks()
        {
            // Arrange
            var links = await _context.SupplyLinks.ToArrayAsync();

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

            var expected = strings;

            // Act
            var result = await _client.GetAsync(string.Empty);
            var content = await result.Content.ReadAsStringAsync();
            var getLinks = JsonConvert.DeserializeObject(content, typeof(List<string>)) as List<string>;

            // Assert
            getLinks.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSupplyLink_HappyFlow_ShouldReturnSupplyLink()
        {
            // Arrange
            var linkId = GetFirstLinkId;

            var link = await _context.SupplyLinks
                .FindAsync(linkId);

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

            var expected = string.Format("({0}) {1}, {2}{3}, {4} supplies" +
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
            var result = await _client.GetAsync(_client.BaseAddress + "/" + linkId);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSupplyLink_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var badId = await _context.SupplyLinks.CountAsync() + 1;

            // Act
            var result = await _client.GetAsync(_client.BaseAddress + "/" + badId);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task GetSupplyLinkBasic_HappyFlow_ShouldReturnRequirement()
        {
            // Arrange
            var linkId = GetFirstLinkId;
            var expected = await _context.SupplyLinks.FindAsync(linkId);

            // Act
            var result = await _client.GetAsync(_client.BaseAddress + "/" + linkId+"/basic");
            var content = await result.Content.ReadAsStringAsync();
            var link = JsonConvert.DeserializeObject(content, typeof(SupplyLink)) as SupplyLink;

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            link.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSupplyLinkBasic_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var badID = GenBadSupplyLinkId;

            // Act
            var result = await _client.GetAsync(_client.BaseAddress + "/" + GenBadSupplyLinkId + "/basic");

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        #endregion

        // Helpers

        private int GenBadSupplyLinkId => _context.SupplyLinks.Count() + 1;

        private int GetFirstLinkId => _context.SupplyLinks.First().SupplyLinkId;
    }
}
