using FluentAssertions;
using McJenny.WebAPI;
using McJenny.WebAPI.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace McJenny.Integration
{
    [TestClass]
    public class PositionsIntegrationTests
    {
        private static HttpClient _client;
        private static FoodChainsDbContext _context;
        private static WebApplicationFactory<Startup> _appFactory;

        [ClassInitialize]
        public static void InitClass(TestContext testContext)
        {
            _appFactory = new WebApplicationFactory<Startup>();
            _appFactory.ClientOptions.BaseAddress = new Uri("http://localhost/api/positions");
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
            _appFactory.ClientOptions.BaseAddress = new Uri("http://localhost/api/positions");
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
        public async Task GetPositions_HappyFlow_ShouldReturnPositions()
        {
            // Arrange
            var expectedObj = await _context.Positions
                .Select(p => new
                {
                    ID = p.PositionId,
                    p.Title,
                    HourlyWage = string.Format("{0:C}", p.Wage)
                })
                .Take(20)
                .ToListAsync();
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
        public async Task GetPositionCheating_HappyFlow_ShouldReturnPosition()
        {
            // Arrange
            var posId = 2;

            var expectedObj = (await _context.Positions
                .Select(p => new
                {
                    p.PositionId,
                    res = new
                    {
                        p.Title,
                        HourlyWage = string.Format("{0:C}", p.Wage)
                    }
                })
                .FirstOrDefaultAsync(p => p.PositionId == posId)).res;
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + posId);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetPosition_HappyFlow_ShouldReturnPosition()
        {
            // Arrange
            var posId = GetFirstPositionId;
            var position = await _context.Positions.FindAsync(posId);   
            var expected = new
            {
                position.Title,
                HourlyWage = string.Format("{0:C}", position.Wage)
            };
            var definition = new
            {
                Title = string.Empty,
                HourlyWage = string.Empty
            };

            // Act
            var result = await _client.GetAsync(_client.BaseAddress + "/" + posId);
            var content = await result.Content.ReadAsStringAsync();
            var pos = JsonConvert.DeserializeAnonymousType(content, definition);

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            pos.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetPosition_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var posId = GenBadPositionId;

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + posId);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task GetPositionBasic_HappyFlow_ShouldReturnPosition()
        {
            // Arrange
            var posId = GetFirstPositionId;

            var expected = await _context.Positions.FindAsync(posId);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/" + posId + "/basic");
            var content = await result.Content.ReadAsStringAsync();
            var retrieved = JsonConvert.DeserializeObject(content, typeof(Position));

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            retrieved.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetPositionBasic_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var posId = GenBadPositionId;

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/" + posId + "/basic");

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task GetEmployees_HappyFlow_ShouldReturnPositions()
        {
            // Arrange
            var posId = GetFirstPositionId;

            var expectedObj = await _context.Employees
                .Where(e => e.PositionId == posId)
                .Select(e => string.Format("({0}) {1}, {2}",
                    e.EmployeeId, e.LastName, e.FirstName))
                .ToArrayAsync();
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(_client.BaseAddress + "/" + posId + "/employees");
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetEmployees_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var posId = GenBadPositionId;

            // Act
            var result = await _client.GetAsync(_client.BaseAddress + "/" + posId +"/employees");

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        #endregion

        #region Advanced

        [TestMethod]
        public async Task GetPositionsFiltered_NameMatch_ShouldReturnFiltered()
        {
            // Arrange
            var filter = await _context.Positions.Select(p => p.Title).FirstOrDefaultAsync();

            var expectedObj = await _context.Positions
                 .Select(p => new { p.PositionId, p.Title, p.Wage })
                 .Where(p => p.Title.ToUpper()
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
        public async Task GetPositionsPaged_HapyFlow_ShouldReturnPaged()
        {
            // Arrange
            var pgSz = 2;
            var pgInd = 1;

            var expectedObj = await _context.Positions
                 .Select(p => new { p.PositionId, p.Title, p.Wage })
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
        public async Task GetPositionsPaged_EndPageIncomplete_ShouldReturnPaged()
        {
            // Arrange
            var dishCount = await _context.Positions.CountAsync();
            var pgSz = 1;

            while (true)
            {
                if (dishCount % pgSz == 0) pgSz++;
                else break;
            }

            var pgInd = dishCount / (pgSz - 1);

            var expectedObj = await _context.Positions
                 .Select(p => new { p.PositionId, p.Title, p.Wage })
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
        public async Task GetPositionsPaged_SizeOver_ShouldReturnPaged()
        {
            // Arrange
            var dishCount = await _context.Positions.CountAsync();
            var pgSz = dishCount + 1;
            var pgInd = 1;

            var expectedObj = await _context.Positions
                 .Select(p => new { p.PositionId, p.Title, p.Wage })
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
        public async Task GetPositionsPaged_IndexOver_ShouldReturnPaged()
        {
            // Arrange
            var dishCount = await _context.Positions.CountAsync();
            var pgInd = dishCount + 1;
            var pgSz = 1;

            var expectedObj = await _context.Positions
                 .Select(p => new { p.PositionId, p.Title, p.Wage })
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

        [DataTestMethod]
        [DataRow(-2, 1)]
        [DataRow(2, -1)]
        public async Task GetPositionsPaged_HapyFlow_ShouldReturnPaged(int pgSz, int pgInd)
        {
            // Arrange
            var absPgSz = Math.Abs(pgSz);
            var absPgInd = Math.Abs(pgInd);

            var expectedObj = await _context.Positions
                 .Select(p => new { p.PositionId, p.Title, p.Wage })
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
        public async Task GetPositionsSorted_ByTitle_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "title";

            var expectedObj = await _context.Positions
                            .Select(p => new { p.PositionId, p.Title, p.Wage })
                            .OrderBy(p => p.Title)
                            .ToArrayAsync();
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + 
                "/query:sortby=" + sortBy);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetPositionsSorted_ByWage_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "wage";

            var expectedObj = await _context.Positions
                            .Select(p => new { p.PositionId, p.Title, p.Wage })
                            .OrderBy(p => p.Wage)
                            .ToArrayAsync();
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/query:sortby=" + sortBy);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetPositionsSorted_GarbledText_ShouldReturnSortedByID()
        {
            // Arrange
            var sortBy = "gytxfd";

            var expectedObj = await _context.Positions
                            .Select(p => new { p.PositionId, p.Title, p.Wage })
                            //.OrderBy(p => p.PositionId)
                            .ToArrayAsync();
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/query:sortby=" + sortBy);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetPositionsSorted_Descending_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "wage";

            var expectedObj = await _context.Positions
                            .Select(p => new { p.PositionId, p.Title, p.Wage })
                            .OrderByDescending(p => p.Wage)
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
        public async Task GetPositionsQueried_FilteredAndSorted_ShouldReturnFilteredAndSorted()
        {
            // Arrange
            var sortBy = "wage";
            var filter = "er";

            var filtered = await _context.Positions
                 .Select(p => new { p.PositionId, p.Title, p.Wage })
                 .Where(p => p.Title.ToUpper()
                     .Contains(filter.ToUpper()))
                 .ToArrayAsync();

            var expectedObj = filtered
                            .OrderByDescending(p => p.Wage)
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
        public async Task GetPositionsQueried_FilteredAndPaged_ShouldReturnFilteredAndPaged()
        {
            // Arrange
            int pgSz = 2;
            int pgInd = 1;
            var filter = "er";

            var filtered = await _context.Positions
                 .Select(p => new { p.PositionId, p.Title, p.Wage })
                 .Where(p => p.Title.ToUpper()
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
        public async Task GetPositionsQueried_SortedAndPaged_ShouldReturnSortedAndPaged()
        {
            // Arrange
            int pgSz = 2;
            int pgInd = 1;
            var sortBy = "wage";

            var sorted = await _context.Positions
                            .Select(p => new { p.PositionId, p.Title, p.Wage })
                            .OrderByDescending(p => p.Wage)
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
        public async Task GetPositionsQueried_FilteredSortedAndPaged_ShouldReturnFilteredSortedAndPaged()
        {
            // Arrange
            int pgSz = 2;
            int pgInd = 1;
            var sortBy = "wage";
            var filter = "er";

            var filtered = await _context.Positions
                 .Select(p => new { p.PositionId, p.Title, p.Wage })
                 .Where(p => p.Title.ToUpper()
                     .Contains(filter.ToUpper()))
                 .ToArrayAsync();

            var sorted = filtered
                .OrderByDescending(p => p.Wage)
                .ToArray();

            var expectedObj = sorted
                .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArray();
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/query:desc=true&sortby=" + sortBy + "&pgsz=" + 
                pgSz + "&pgind=" + pgInd + "&filter=" + filter);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        #endregion

        #endregion

        #region Updates

        [TestCategory("Update")]
        [TestMethod]
        public async Task UpdatePosition_HappyFlow_ShouldUpdateAndReturnPosition()
        {
            // Arrange
            var posId = GetFirstPositionId;

            /* Save a copy of the original entry to compare against*/
            var oldPosition = await _context.Positions.FindAsync(posId);
            var oldPositionCopy = new Position
            {
                PositionId = oldPosition.PositionId,
                Employees = oldPosition.Employees,
                Title=oldPosition.Title,
                Wage=oldPosition.Wage
            };

            var pos = new Position 
            {
                Title = "Planner",
                Wage = oldPositionCopy.Wage + 3.50m
            };
            var posContent = new StringContent(JsonConvert.SerializeObject(pos),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + posId, posContent);

            /* Retrieve the (allegedly) updated pos*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + posId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newPosition = JsonConvert.DeserializeObject(newCont, typeof(Position));

            /* Complete the pos we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            pos.PositionId = oldPositionCopy.PositionId;
            pos.Employees = oldPositionCopy.Employees;

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            newPosition.Should().NotBeEquivalentTo(oldPositionCopy);
            newPosition.Should().BeEquivalentTo(pos);

            // Clean-up
            pos = new Position 
            { 
                Title = oldPositionCopy.Title,
                Wage=oldPositionCopy.Wage
            };
            posContent = new StringContent(JsonConvert.SerializeObject(pos),
                Encoding.UTF8, "application/json");
            result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + posId, posContent);
            updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + posId + "/basic");
            newCont = await updCall.Content.ReadAsStringAsync();
            newPosition = JsonConvert.DeserializeObject(newCont, typeof(Position));

            result.IsSuccessStatusCode.Should().BeTrue();
            newPosition.Should().BeEquivalentTo(oldPositionCopy);
        }

        [TestMethod]
        public async Task UpdatePosition_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var posId = GetFirstPositionId;

            /* Save a copy of the original entry to compare against*/
            var oldPosition = await _context.Positions.FindAsync(posId);
            var oldPositionCopy = new Position
            {
                PositionId = oldPosition.PositionId,
                Employees = oldPosition.Employees,
                Title = oldPosition.Title,
                Wage = oldPosition.Wage
            };

            var pos = new Position
            {
                Title = "Planner",
                Wage = oldPositionCopy.Wage + 3.50m
            };
            var posContent = new StringContent(JsonConvert.SerializeObject(pos),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + 
                GenBadPositionId, posContent);

            /* Retrieve the (allegedly) updated pos*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + posId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newPosition = JsonConvert.DeserializeObject(newCont, typeof(Position));

            /* Complete the pos we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            pos.PositionId = oldPositionCopy.PositionId;
            pos.Employees = oldPositionCopy.Employees;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
            newPosition.Should().BeEquivalentTo(oldPositionCopy);
            newPosition.Should().NotBeEquivalentTo(pos);
        }

        [TestMethod]
        public async Task UpdatePosition_TriesToSetId_ShouldReturnBadRequest()
        {
            // Arrange
            var posId = GetFirstPositionId;

            /* Save a copy of the original entry to compare against*/
            var oldPosition = await _context.Positions.FindAsync(posId);
            var oldPositionCopy = new Position
            {
                PositionId = oldPosition.PositionId,
                Employees = oldPosition.Employees,
                Title = oldPosition.Title,
                Wage = oldPosition.Wage
            };

            var pos = new Position
            {
                Title = "Planner",
                Wage = oldPositionCopy.Wage + 3.50m,
                PositionId = GenBadPositionId
            };
            var posContent = new StringContent(JsonConvert.SerializeObject(pos),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + posId, posContent);

            /* Retrieve the (allegedly) updated pos*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + posId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newPosition = JsonConvert.DeserializeObject(newCont, typeof(Position));

            /* Complete the pos we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            pos.PositionId = oldPositionCopy.PositionId;
            pos.Employees = oldPositionCopy.Employees;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newPosition.Should().BeEquivalentTo(oldPositionCopy);
            newPosition.Should().NotBeEquivalentTo(pos);
        }

        [DataTestMethod]
        [DataRow(" ")]
        [DataRow("123456789012345678911234567892123456789412345678941")]
        public async Task UpdatePosition_BadTitles_ShouldReturnBadRequest(string title)
        {
            // Arrange
            var posId = GetFirstPositionId;

            /* Save a copy of the original entry to compare against*/
            var oldPosition = await _context.Positions.FindAsync(posId);
            var oldPositionCopy = new Position
            {
                PositionId = oldPosition.PositionId,
                Employees = oldPosition.Employees,
                Title = oldPosition.Title,
                Wage = oldPosition.Wage
            };

            var pos = new Position
            {
                Wage = oldPositionCopy.Wage + 3.50m,
                Title = title
            };
            var posContent = new StringContent(JsonConvert.SerializeObject(pos),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + posId, posContent);

            /* Retrieve the (allegedly) updated pos*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + posId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newPosition = JsonConvert.DeserializeObject(newCont, typeof(Position));

            /* Complete the pos we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            pos.PositionId = oldPositionCopy.PositionId;
            pos.Employees = oldPositionCopy.Employees;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newPosition.Should().BeEquivalentTo(oldPositionCopy);
            newPosition.Should().NotBeEquivalentTo(pos);
        }

        [TestMethod]
        public async Task UpdatePosition_DuplicateTitle_ShouldReturnBadRequest()
        {
            // Arrange
            var posId = GetFirstPositionId;
            var dupe = (await _context.Positions
                .FirstOrDefaultAsync(p => p.PositionId != posId)).Title;

            /* Save a copy of the original entry to compare against*/
            var oldPosition = await _context.Positions.FindAsync(posId);
            var oldPositionCopy = new Position
            {
                PositionId = oldPosition.PositionId,
                Employees = oldPosition.Employees,
                Title = oldPosition.Title,
                Wage = oldPosition.Wage
            };

            var pos = new Position
            {
                Wage = oldPositionCopy.Wage + 3.50m,
                Title = dupe
            };
            var posContent = new StringContent(JsonConvert.SerializeObject(pos),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + posId, posContent);

            /* Retrieve the (allegedly) updated pos*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + posId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newPosition = JsonConvert.DeserializeObject(newCont, typeof(Position));

            /* Complete the pos we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            pos.PositionId = oldPositionCopy.PositionId;
            pos.Employees = oldPositionCopy.Employees;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newPosition.Should().BeEquivalentTo(oldPositionCopy);
            newPosition.Should().NotBeEquivalentTo(pos);
        }

        [TestMethod]
        public async Task UpdatePosition_WageTooLow_ShouldReturnBadRequest()
        {
            // Arrange
            var posId = GetFirstPositionId;

            /* Save a copy of the original entry to compare against*/
            var oldPosition = await _context.Positions.FindAsync(posId);
            var oldPositionCopy = new Position
            {
                PositionId = oldPosition.PositionId,
                Employees = oldPosition.Employees,
                Title = oldPosition.Title,
                Wage = oldPosition.Wage
            };

            var pos = new Position
            {
                Title = "Planner",
                Wage = -1m
            };
            var posContent = new StringContent(JsonConvert.SerializeObject(pos),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + posId, posContent);

            /* Retrieve the (allegedly) updated pos*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + posId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newPosition = JsonConvert.DeserializeObject(newCont, typeof(Position));

            /* Complete the pos we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            pos.PositionId = oldPositionCopy.PositionId;
            pos.Employees = oldPositionCopy.Employees;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newPosition.Should().BeEquivalentTo(oldPositionCopy);
            newPosition.Should().NotBeEquivalentTo(pos);
        }

        [TestMethod]
        public async Task UpdatePosition_EmployeesNull_ShouldReturnBadRequest()
        {
            // Arrange
            var posId = GetFirstPositionId;

            /* Save a copy of the original entry to compare against*/
            var oldPosition = await _context.Positions.FindAsync(posId);
            var oldPositionCopy = new Position
            {
                PositionId = oldPosition.PositionId,
                Employees = oldPosition.Employees,
                Title = oldPosition.Title,
                Wage = oldPosition.Wage
            };

            var pos = new Position
            {
                Title = "Planner",
                Wage = oldPositionCopy.Wage + 3.50m,
                Employees = null
            };
            var posContent = new StringContent(JsonConvert.SerializeObject(pos),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + posId, posContent);

            /* Retrieve the (allegedly) updated pos*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + posId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newPosition = JsonConvert.DeserializeObject(newCont, typeof(Position));

            /* Complete the pos we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            pos.PositionId = oldPositionCopy.PositionId;
            pos.Employees = oldPositionCopy.Employees;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newPosition.Should().BeEquivalentTo(oldPositionCopy);
            newPosition.Should().NotBeEquivalentTo(pos);
        }

        [TestMethod]
        public async Task UpdatePosition_EmployeesNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var posId = GetFirstPositionId;

            /* Save a copy of the original entry to compare against*/
            var oldPosition = await _context.Positions.FindAsync(posId);
            var oldPositionCopy = new Position
            {
                PositionId = oldPosition.PositionId,
                Employees = oldPosition.Employees,
                Title = oldPosition.Title,
                Wage = oldPosition.Wage
            };

            var pos = new Position
            {
                Title = "Planner",
                Wage = oldPositionCopy.Wage + 3.50m,
                Employees = new Employee[] { new Employee()}
            };
            var posContent = new StringContent(JsonConvert.SerializeObject(pos),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + posId, posContent);

            /* Retrieve the (allegedly) updated pos*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + posId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newPosition = JsonConvert.DeserializeObject(newCont, typeof(Position));

            /* Complete the pos we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            pos.PositionId = oldPositionCopy.PositionId;
            pos.Employees = oldPositionCopy.Employees;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newPosition.Should().BeEquivalentTo(oldPositionCopy);
            newPosition.Should().NotBeEquivalentTo(pos);
        }

        #endregion

        #region Deletes

        [DataTestMethod]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public async Task DeletePosition_HasEmployees_ShouldTurnToUnassigned(bool isEnd, bool isFirst)
        {
            // Arrange
            var posId = 0;
            if (!isEnd && !isFirst) posId = 2;
            else
            {
                if (isFirst) posId = GetFirstPositionId;
                else posId = await _context.Positions.CountAsync();
                /*Don't ask why but the test shits itself if I ask it to get p=>p.PositionId.LastOrDef, so I just use GenBad*/
            }

            var pos = await _context.Positions.FindAsync(posId);
            var posCopy = new Position
            {
                Title = pos.Title,
                Wage = pos.Wage
            };
            pos = null;

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + posId);
            var newPosGet = await _client.GetAsync(_client.BaseAddress + "/" + posId+"/basic");
            var newPosCall = await newPosGet.Content.ReadAsStringAsync();
            var newPosCont = JsonConvert.DeserializeObject(newPosCall, typeof(Position)) as Position;

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            newPosCont.Title.Should().Contain("Unassigned");
            newPosCont.Wage.Should().Be(0m);

            // Clean-up V2
            var fixPos = new StringContent(JsonConvert.SerializeObject(posCopy),
                Encoding.UTF8, "application/json");
            var fixRes = await _client.PutAsync(_client.BaseAddress + "/" + posId, fixPos);

            /*Check-Up*/
            pos = await _context.Positions.FindAsync(posId);
            pos.Title.Should().Be(posCopy.Title);
            pos.Wage.Should().Be(posCopy.Wage);
        }

        [TestMethod]
        public async Task DeletePosition_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var posId = await _context.Positions.CountAsync()+1;

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + posId);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task DeletePosition_LastPositionNoEmployees_ShouldDelete()
        {
            // Arrange
            var posId = await _context.Positions.CountAsync()+1;
            var pos = new Position
            {
                Title = "Baker",
                Wage = 16.65m
            };
            await _context.Positions.AddAsync(pos);
            await _context.SaveChangesAsync();

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + posId);
            var newPosGet = await _client.GetAsync(_client.BaseAddress + "/" + posId + "/basic");

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            newPosGet.IsSuccessStatusCode.Should().BeFalse();
            newPosGet.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestCategory("Deletes")]
        [TestMethod]
        public async Task DeletePosition_NotLastNoEmployees_ShouldTurnIntoLastPosAndDeleteThat()
        {
            // Arrange
            var posId = GetFirstPositionId;
            var lastPosId = await _context.Positions.CountAsync()+1;
            var pos = await _context.Positions.FindAsync(posId);

            var emptyPos = new Position
            {
                Title = pos.Title,
                Wage = pos.Wage
            };
            await _context.AddAsync(emptyPos);
            await _context.SaveChangesAsync();

            var posEmployees = await _context.Employees
                .Where(e => e.PositionId == posId)
                .ToArrayAsync();

            foreach (var emp in posEmployees) emp.PositionId = lastPosId;
            pos.Title = "Baker";
            pos.Wage = 16.65m;
            await _context.SaveChangesAsync();

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + posId);
            
            var posCount = await _context.Positions.CountAsync();

            await _context.DisposeAsync();
            var context = new FoodChainsDbContext();
            var newPos = await context.Positions.FindAsync(posId);
            var delPosExists = await context.Positions.AnyAsync(p => p.Title == "Baker");

            //Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            posCount.Should().Be(lastPosId-1);
            newPos.Title.Should().BeEquivalentTo(emptyPos.Title);
            newPos.Wage.Should().Be(emptyPos.Wage);
            delPosExists.Should().BeFalse();
        }

        [TestMethod]
        public async Task DeletePosition_NotLastNoRel_ShouldMigrateRelations()
        {
            // Arrange
            var posId = await _context.Positions.CountAsync() + 1;
            var empId = await _context.Employees.CountAsync() + 1;

            var delPos = new Position
            { 
                Title = "Delete Me",
                Wage = 0m
            };
            var swapPos = new Position
            {
                Title = "Plumber",
                Wage = 16.65m
            };
            var emp = new Employee
            {
                FirstName = "Delete",
                LastName = "Me",
                LocationId = 1,
                PositionId = posId + 1,
                StartedOn = DateTime.Today.AddDays(-5),
                WeeklyHours = 24
            };

            await _context.Positions.AddRangeAsync(new Position[] { delPos, swapPos });
            await _context.SaveChangesAsync();

            await _context.Employees.AddAsync(emp);
            await _context.SaveChangesAsync();

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + posId);

            var getEmpRes = await _client.GetAsync("http://localhost/api/employees/" +
                empId + "/basic");
            var getEmpCont = await getEmpRes.Content.ReadAsStringAsync();
            var getEmp = JsonConvert.DeserializeObject(getEmpCont,
                typeof(Employee)) as Employee;

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            getEmp.PositionId.Should().Be(posId);

            // Clean-up
            await _client.DeleteAsync("http://localhost/api/employees/" + empId);
            await _client.DeleteAsync(_client.BaseAddress + "/" + posId);
        }

        #endregion

        #region Creates

        [TestMethod]
        public async Task CreatePosition_HappyFlow_ShouldCreateAndReturnPosition()
        {
            // Arrange
            var posId = await _context.Positions.CountAsync() + 1;
            var position = new Position
            {
                Title = "Baker",
                Wage = 14.38m
            };
            var posCont = new StringContent(JsonConvert.SerializeObject(position),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, posCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();

            // Clean-up
            await _client.DeleteAsync(_client.BaseAddress + "/" + posId);
        }

        [TestMethod]
        public async Task CreatePosition_TriesToSetId_ShouldReturnBadRequest()
        {
            // Arrange
            var posId = await _context.Positions.CountAsync() + 1;
            var position = new Position
            {
                Title = "Baker",
                Wage = 14.38m,
                PositionId = posId
            };
            var posCont = new StringContent(JsonConvert.SerializeObject(position),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, posCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [DataTestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow(null)]
        [DataRow("123456789012345678911234567892123456789412345678941")]
        public async Task CreatePosition_BadTitles_ShouldReturnBadRequest(string title)
        {
            // Arrange
            var position = new Position
            {
                Title = title,
                Wage = 14.38m
            };
            var posCont = new StringContent(JsonConvert.SerializeObject(position),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, posCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreatePosition_DuplicateTitle_ShouldReturnBadRequest()
        {
            // Arrange
            var dupe = await _context.Positions.FirstAsync();

            var position = new Position
            {
                Title = dupe.Title,
                Wage = 14.38m
            };
            var posCont = new StringContent(JsonConvert.SerializeObject(position),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, posCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreatePosition_WageTooLow_ShouldReturnBadRequest()
        {
            // Arrange
            var position = new Position
            {
                Title = "Baker",
                Wage = -1m
            };
            var posCont = new StringContent(JsonConvert.SerializeObject(position),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, posCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreatePosition_EmployeesNull_ShouldReturnBadRequest()
        {
            // Arrange
            var posId = await _context.Positions.CountAsync() + 1;
            var position = new Position
            {
                Title = "Baker",
                Wage = 14.38m,
                Employees = null
            };
            var posCont = new StringContent(JsonConvert.SerializeObject(position),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, posCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreatePosition_EmployeesNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var posId = await _context.Positions.CountAsync() + 1;
            var position = new Position
            {
                Title = "Baker",
                Wage = 14.38m,
                Employees = new Employee[] {new Employee()}
            };
            var posCont = new StringContent(JsonConvert.SerializeObject(position),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, posCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        #endregion

        //Helpers

        private int GenBadPositionId => _context.Positions
            .AsEnumerable()
            .Select(p=>p.PositionId)
            .LastOrDefault()+1;

        private int GetFirstPositionId => _context.Positions
            .Select(p => p.PositionId)
            .FirstOrDefault();

        private int GetLastPositionId => ( _context.Positions
           .Select(p => p.PositionId).ToArray())
            .LastOrDefault();
    }
}
