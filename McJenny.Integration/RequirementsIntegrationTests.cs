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
    public class RequirementsIntegrationTests
    {
        private static HttpClient _client;
        private static FoodChainsDbContext _context;
        private static WebApplicationFactory<Startup> _appFactory;

        [ClassInitialize]
        public static void InitClass(TestContext testContext)
        {
            _appFactory = new WebApplicationFactory<Startup>();
            _appFactory.ClientOptions.BaseAddress = new Uri("http://localhost/api/dishrequirements");
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
            _appFactory.ClientOptions.BaseAddress = new Uri("http://localhost/api/dishrequirements");
            _client = _appFactory.CreateClient();

            var fked = HelperFuck.MessedUpDB(_context);
            if (fked)
            {
                var fake = false;
            }
        }

        #region Deletes

        [TestMethod]
        public async Task DeleteDishRequirement_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var reqId = GenBadRequirementId;

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + reqId);
            var getRes = await _client.GetAsync(_client.BaseAddress + "/" + reqId);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            getRes.IsSuccessStatusCode.Should().BeFalse();
            getRes.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task DeleteDishRequirement_Last_ShouldDelete()
        {
            // Arrange
            var reqId = await _context.DishRequirements.CountAsync() + 1;
            
            /*Prepare a requirement to delete*/
            var dishesCount = await _context.Dishes.CountAsync();
            var catsCount = await _context.SupplyCategories.CountAsync();

            var unused = new Tuple<int, int>(0,0);
            for (int i = 1; i <= dishesCount; i++)
            {
                for (int j = 1; j <= catsCount; j++)
                {
                    if (!await _context.DishRequirements
                        .AnyAsync(r =>
                        r.DishId == i && r.SupplyCategoryId == j))

                    {
                        unused = new Tuple<int, int>(i, j);
                        break;
                    }

                }
                if (unused.Item1 != 0) break;
            }

            if (unused.Item1 == 0) return;

            var req = new DishRequirement
            {
                DishId = unused.Item1,
                SupplyCategoryId = unused.Item2
            };

            await _context.DishRequirements.AddAsync(req);
            await _context.SaveChangesAsync();

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + reqId);
            var context = new FoodChainsDbContext();
            var reqStillExists = await context.DishRequirements
                .AnyAsync(r => 
                r.DishId == unused.Item1 &&
                r.SupplyCategoryId == unused.Item2);

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            reqStillExists.Should().BeFalse();
        }

        [TestMethod]
        public async Task DeleteDishRequirement_NotLast_ShouldSwitchWithLastAndDelThat()
        {
            // Arrange
            var reqId = await _context.DishRequirements.CountAsync() + 1;

            /*Prepare a requirement to delete and another after that will be the last*/
            var dishesCount = await _context.Dishes.CountAsync();
            var catsCount = await _context.SupplyCategories.CountAsync();

            var unused1 = new Tuple<int, int>(0, 0);
            var unused2 = new Tuple<int, int>(0, 0);
            for (int i = 1; i <= dishesCount; i++)
            {
                for (int j = 1; j <= catsCount; j++)
                {
                    if (!await _context.DishRequirements
                        .AnyAsync(r =>
                        r.DishId == i && r.SupplyCategoryId == j))

                    {
                        if (unused1.Item1 == 0)
                            unused1 = new Tuple<int, int>(i, j);
                        else
                        {
                            unused2= new Tuple<int, int>(i, j);
                            break;
                        }
                    }

                }
                if (unused1.Item1 != 0 && unused2.Item1!=0) break;
            }

            if (unused1.Item1 == 0 || unused2.Item1 == 0) return;

            var req = new DishRequirement
            {
                DishId = unused1.Item1,
                SupplyCategoryId = unused1.Item2
            };

            var lastReq = new DishRequirement
            {
                DishId = unused2.Item1,
                SupplyCategoryId = unused2.Item2
            };

            await _context.DishRequirements.AddAsync(req);
            await _context.DishRequirements.AddAsync(lastReq);
            await _context.SaveChangesAsync();

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + reqId);

            var newDeletedGet = await _client.GetAsync(_client.BaseAddress + "/" + reqId+"/basic");
            var newDeletedCont = await newDeletedGet.Content.ReadAsStringAsync();
            var newDeleted = JsonConvert.DeserializeObject
                (newDeletedCont, typeof(DishRequirement)) as DishRequirement;
            
            var context = new FoodChainsDbContext();
            var reqStillExists = await context.DishRequirements
                .AnyAsync(r =>
                r.DishId == unused1.Item1 &&
                r.SupplyCategoryId == unused1.Item2);

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            reqStillExists.Should().BeFalse();
            newDeleted.DishId.Should().Be(unused2.Item1);
            newDeleted.SupplyCategoryId.Should().Be(unused2.Item2);
            
            // Clean-up
            result = await _client.DeleteAsync(_client.BaseAddress + "/" + reqId);
            result.IsSuccessStatusCode.Should().BeTrue();
        }

        #endregion

        #region Creates

        [TestMethod]
        public async Task CreateDishRequirement_HappyFlow_ShouldCreateAndReturnDishRequirement()
        {
            // Arrange
            var reqId = await _context.DishRequirements.CountAsync() + 1;
            var dishCount = await _context.Dishes.CountAsync();
            var catCount = await _context.SupplyCategories.CountAsync();

            var reqIds = new Tuple<int, int>(0, 0);
            for (int i = 1; i <= dishCount; i++)
            {
                for (int j = 1; j <= catCount; j++)
                {
                    if (!await _context.DishRequirements.AnyAsync(r=>
                            r.DishId==i&&r.SupplyCategoryId==j))
                    {
                        reqIds = new Tuple<int, int>(i, j);
                        break;
                    }
                }
                if (reqIds.Item1 != 0) break;
            }

            var req = new DishRequirement
            {
                DishId = reqIds.Item1,
                SupplyCategoryId = reqIds.Item2
            };
            var reqCont = new StringContent(JsonConvert.SerializeObject(req),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, reqCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();

            // Clean-up
            await _client.DeleteAsync(_client.BaseAddress + "/" + reqId);
        }

        [TestMethod]
        public async Task CreateDishRequirement_TriesToSetId_ShouldReturnBadRequest()
        {
            // Arrange
            var reqId = await _context.DishRequirements.CountAsync() + 1;
            var dishCount = await _context.Dishes.CountAsync();
            var catCount = await _context.SupplyCategories.CountAsync();

            var reqIds = new Tuple<int, int>(0, 0);
            for (int i = 1; i <= dishCount; i++)
            {
                for (int j = 1; j <= catCount; j++)
                {
                    if (!await _context.DishRequirements.AnyAsync(r =>
                            r.DishId == i && r.SupplyCategoryId == j))
                    {
                        reqIds = new Tuple<int, int>(i, j);
                        break;
                    }
                }
                if (reqIds.Item1 != 0) break;
            }

            var req = new DishRequirement
            {
                DishId = reqIds.Item1,
                SupplyCategoryId = reqIds.Item2,
                DishRequirementId = reqId
            };
            var reqCont = new StringContent(JsonConvert.SerializeObject(req),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, reqCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateDishRequirement_HasDish_ShouldReturnBadRequest()
        {
            // Arrange
            var dishCount = await _context.Dishes.CountAsync();
            var catCount = await _context.SupplyCategories.CountAsync();

            var reqIds = new Tuple<int, int>(0, 0);
            for (int i = 1; i <= dishCount; i++)
            {
                for (int j = 1; j <= catCount; j++)
                {
                    if (!await _context.DishRequirements.AnyAsync(r =>
                            r.DishId == i && r.SupplyCategoryId == j))
                    {
                        reqIds = new Tuple<int, int>(i, j);
                        break;
                    }
                }
                if (reqIds.Item1 != 0) break;
            }

            var req = new DishRequirement
            {
                DishId = reqIds.Item1,
                SupplyCategoryId = reqIds.Item2,
                Dish = new Dish()
            };
            var reqCont = new StringContent(JsonConvert.SerializeObject(req),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, reqCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateDishRequirement_HasSupplyCategory_ShouldReturnBadRequest()
        {
            // Arrange
            var dishCount = await _context.Dishes.CountAsync();
            var catCount = await _context.SupplyCategories.CountAsync();

            var reqIds = new Tuple<int, int>(0, 0);
            for (int i = 1; i <= dishCount; i++)
            {
                for (int j = 1; j <= catCount; j++)
                {
                    if (!await _context.DishRequirements.AnyAsync(r =>
                            r.DishId == i && r.SupplyCategoryId == j))
                    {
                        reqIds = new Tuple<int, int>(i, j);
                        break;
                    }
                }
                if (reqIds.Item1 != 0) break;
            }

            var req = new DishRequirement
            {
                DishId = reqIds.Item1,
                SupplyCategoryId = reqIds.Item2,
                SupplyCategory = new SupplyCategory()
            };
            var reqCont = new StringContent(JsonConvert.SerializeObject(req),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, reqCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateDishRequirement_InexistentDishId_ShouldReturnBadRequest()
        {
            // Arrange
            var dishCount = await _context.Dishes.CountAsync();
            var catCount = await _context.SupplyCategories.CountAsync();

            var reqIds = new Tuple<int, int>(0, 0);
            for (int i = 1; i <= dishCount; i++)
            {
                for (int j = 1; j <= catCount; j++)
                {
                    if (!await _context.DishRequirements.AnyAsync(r =>
                            r.DishId == i && r.SupplyCategoryId == j))
                    {
                        reqIds = new Tuple<int, int>(i, j);
                        break;
                    }
                }
                if (reqIds.Item1 != 0) break;
            }

            var req = new DishRequirement
            {
                SupplyCategoryId = reqIds.Item2,
                DishId = dishCount+1
            };
            var reqCont = new StringContent(JsonConvert.SerializeObject(req),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, reqCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateDishRequirement_InexistentSupplyCategoryId_ShouldReturnBadRequest()
        {
            // Arrange
            var dishCount = await _context.Dishes.CountAsync();
            var catCount = await _context.SupplyCategories.CountAsync();

            var reqIds = new Tuple<int, int>(0, 0);
            for (int i = 1; i <= dishCount; i++)
            {
                for (int j = 1; j <= catCount; j++)
                {
                    if (!await _context.DishRequirements.AnyAsync(r =>
                            r.DishId == i && r.SupplyCategoryId == j))
                    {
                        reqIds = new Tuple<int, int>(i, j);
                        break;
                    }
                }
                if (reqIds.Item1 != 0) break;
            }

            var req = new DishRequirement
            {
                DishId = reqIds.Item1,
                SupplyCategoryId = catCount+1
            };
            var reqCont = new StringContent(JsonConvert.SerializeObject(req),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, reqCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateDishRequirement_DuplicateRequirement_ShouldReturnBadRequest()
        {
            // Arrange
            var dupe = await _context.DishRequirements.FirstOrDefaultAsync();

            var req = new DishRequirement
            {
                DishId = dupe.DishId,
                SupplyCategoryId = dupe.SupplyCategoryId
            };
            var reqCont = new StringContent(JsonConvert.SerializeObject(req),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, reqCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        #endregion

        #region Gets

        #region Basic

        [TestMethod]
        public async Task GetDishRequirements_HappyFlow_ShouldReturnAllDishRequirements()
        {
            // Arrange
            var requirements = await _context.DishRequirements
               .ToArrayAsync();

            var dishIds = requirements.Select(r => r.DishId).Distinct().ToArray();
            var catIds = requirements.Select(r => r.SupplyCategoryId).Distinct().ToArray();

            var dishes = await _context.Dishes
                .Where(d => dishIds.Contains(d.DishId))
                .Select(d => new { d.Name, d.DishId })
                .ToArrayAsync();

            var cats = await _context.SupplyCategories
                .Where(c => catIds.Contains(c.SupplyCategoryId))
                .Select(c => new { c.Name, c.SupplyCategoryId })
                .ToArrayAsync();

            var expected = new string[requirements.Length];
            for (int i = 0; i < requirements.Length; i++)
                expected[i] = string.Format("Requirement [{0}]: ({1}) {2} requires ({3}) {4}",
                    requirements[i].DishRequirementId,
                    requirements[i].DishId,
                    dishes.SingleOrDefault(d => d.DishId == requirements[i].DishId).Name,
                    requirements[i].SupplyCategoryId,
                    cats.SingleOrDefault(c => c.SupplyCategoryId == requirements[i].SupplyCategoryId).Name); ;

            // Act
            var result = await _client.GetAsync(string.Empty);
            var content = await result.Content.ReadAsStringAsync();
            var reqs = JsonConvert.DeserializeObject(content, typeof(List<string>)) as List<string>;

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            reqs.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetDishRequirement_HappyFlow_ShouldReturnRequirement()
        {
            // Arrange
            var reqId = GetFirstRequirementId;
            var dishRequirement = await _context.DishRequirements.FindAsync(reqId);

            var dish = (await _context.Dishes.FindAsync(dishRequirement.DishId)).Name;
            var cat = (await _context.SupplyCategories.FindAsync(dishRequirement.SupplyCategoryId)).Name;

            var expected = dish + "requires" + cat;

            // Act
            var result = await _client.GetAsync(_client.BaseAddress + "/" + reqId);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetDishRequirement_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var badId = GenBadRequirementId;

            // Act
            var result = await _client.GetAsync(_client.BaseAddress + "/" + badId);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task GetDishRequirementBasic_HappyFlow_ShouldReturnRequirement()
        {
            // Arrange
            var reqId = GetFirstRequirementId;
            var expected = await _context.DishRequirements.FindAsync(reqId);

            // Act
            var result = await _client.GetAsync(_client.BaseAddress + "/" + reqId + "/basic");
            var content = await result.Content.ReadAsStringAsync();
            var dish = JsonConvert.DeserializeObject(content, typeof(DishRequirement)) as DishRequirement;

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            dish.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetDishRequirementBasic_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var badId = GenBadRequirementId;

            // Act
            var result = await _client.GetAsync(_client.BaseAddress + "/" + badId + "/basic");

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        #endregion

        #endregion

        //Helpers

        private int GenBadRequirementId => _context.DishRequirements.Count() + 1;

        private static int GetFirstRequirementId => _context.DishRequirements
            .Select(r => r.DishRequirementId)
            .FirstOrDefault();
    }
}
