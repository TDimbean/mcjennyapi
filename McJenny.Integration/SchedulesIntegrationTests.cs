using FluentAssertions;
using McJenny.WebAPI;
using McJenny.WebAPI.Data.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace McJenny.Integration
{
    [TestClass]
    public class SchedulesIntegrationTests
    {
        private static HttpClient _client;
        private static FoodChainsDbContext _context;
        private static WebApplicationFactory<Startup> _appFactory;

        [ClassInitialize]
        public static void InitClass(TestContext testContext)
        {
            _appFactory = new WebApplicationFactory<Startup>();
            _appFactory.ClientOptions.BaseAddress = new Uri("http://localhost/api/schedules");
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
            _appFactory.ClientOptions.BaseAddress = new Uri("http://localhost/api/schedules");
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
        public async Task GetSchedules_HappyFlow_ShouldReturnSchedules()
        {
            // Arrange
            var expectedObj = await _context.Schedules
            .Select(s => new { s.ScheduleId, s.TimeTable })
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
        public async Task GetScheduleCheating_HappyFlow_ShouldReturnSchedule()
        {
            // Arrange
            var schId = GetFirstScheduleId;

            var expectedObj = await _context.Schedules
                .Select(s => new { s.ScheduleId, s.TimeTable })
                .FirstOrDefaultAsync(s => s.ScheduleId == schId);
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + schId);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSchedule_HappyFlow_ShouldReturnSchedule()
        {
            // Arrange
            var schId = GetFirstScheduleId;
            var schedule = await _context.Schedules.FindAsync(schId);
            var expected = new { schedule.ScheduleId, schedule.TimeTable };
            var definition = new 
            { 
                ScheduleId = 0, 
                TimeTable = string.Empty
            };

            // Act
            var result = await _client.GetAsync(_client.BaseAddress + "/" + schId);
            var content = await result.Content.ReadAsStringAsync();
            var sch = JsonConvert.DeserializeAnonymousType(content, definition);

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            sch.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSchedule_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var schId = GenBadScheduleId;

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + schId);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task GetScheduleBasic_HappyFlow_ShouldReturnSchedule()
        {
            // Arrange
            var schId = GetFirstScheduleId;

            var expected = await _context.Schedules.FindAsync(schId);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/" + schId + "/basic");
            var content = await result.Content.ReadAsStringAsync();
            var retrieved = JsonConvert.DeserializeObject(content, typeof(Schedule));

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            retrieved.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetScheduleBasic_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var schId = GenBadScheduleId;

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/" + schId + "/basic");

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task GetLocations_HappyFlow_ShouldReturnLocations()
        {
            // Arrange
            var schId = 2;

            var expectedObj = await _context.Locations
                .Select(l => new
                {
                    l.ScheduleId,
                    res = new
                    {
                        l.LocationId,
                        l.AbreviatedCountry,
                        l.AbreviatedState,
                        l.City,
                        l.Street
                    }
                })
                .Where(l => l.ScheduleId == schId)
                .Select(l => l.res)
                .ToArrayAsync();

            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + schId + "/locations");
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetLocations_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var schId = GenBadScheduleId;

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + schId + "/locations");
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        #endregion

        #endregion

        #region Updates

        [TestMethod]
        public async Task UpdateSchedule_HappyFlow_ShouldUpdateAndReturnSchedule()
        {
            // Arrange
            var schId = GetFirstScheduleId;

            /* Save a copy of the original entry to compare against*/
            var oldSchedule = await _context.Schedules.FindAsync(schId);
            var oldScheduleCopy = new Schedule
            {
                ScheduleId = oldSchedule.ScheduleId,
                Locations = oldSchedule.Locations,
                TimeTable = oldSchedule.TimeTable
            };

            var sch = new Schedule { TimeTable = "7:39~15:00 until lockdown ends" };
            var schContent = new StringContent(JsonConvert.SerializeObject(sch),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + schId, schContent);

            /* Retrieve the (allegedly) updated sch*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + schId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newSchedule = JsonConvert.DeserializeObject(newCont, typeof(Schedule));

            /* Complete the sch we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            sch.ScheduleId = oldScheduleCopy.ScheduleId;
            sch.Locations = oldScheduleCopy.Locations;

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            newSchedule.Should().NotBeEquivalentTo(oldScheduleCopy);
            newSchedule.Should().BeEquivalentTo(sch);

            // Clean-up
            sch = new Schedule { TimeTable = oldScheduleCopy.TimeTable };
            schContent = new StringContent(JsonConvert.SerializeObject(sch),
                Encoding.UTF8, "application/json");
            result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + schId, schContent);
            updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + schId + "/basic");
            newCont = await updCall.Content.ReadAsStringAsync();
            newSchedule = JsonConvert.DeserializeObject(newCont, typeof(Schedule));

            result.IsSuccessStatusCode.Should().BeTrue();
            newSchedule.Should().BeEquivalentTo(oldScheduleCopy);
        }

        [TestMethod]
        public async Task UpdateSchedule_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var schId = GetFirstScheduleId;

            /* Save a copy of the original entry to compare against*/
            var oldSchedule = await _context.Schedules.FindAsync(schId);
            var oldScheduleCopy = new Schedule
            {
                ScheduleId = oldSchedule.ScheduleId,
                Locations = oldSchedule.Locations,
                TimeTable = oldSchedule.TimeTable
            };

            var sch = new Schedule { TimeTable = "7:39~15:00 until lockdown ends" };
            var schContent = new StringContent(JsonConvert.SerializeObject(sch),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + 
                GenBadScheduleId, schContent);

            /* Retrieve the (allegedly) updated sch*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + schId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newSchedule = JsonConvert.DeserializeObject(newCont, typeof(Schedule));

            /* Complete the sch we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            sch.ScheduleId = oldScheduleCopy.ScheduleId;
            sch.Locations = oldScheduleCopy.Locations;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
            newSchedule.Should().BeEquivalentTo(oldScheduleCopy);
            newSchedule.Should().NotBeEquivalentTo(sch);
        }

        [TestMethod]
        public async Task UpdateSchedule_TriesToSetId_ShouldReturnBadRequest()
        {
            // Arrange
            var schId = GetFirstScheduleId;

            /* Save a copy of the original entry to compare against*/
            var oldSchedule = await _context.Schedules.FindAsync(schId);
            var oldScheduleCopy = new Schedule
            {
                ScheduleId = oldSchedule.ScheduleId,
                Locations = oldSchedule.Locations,
                TimeTable = oldSchedule.TimeTable
            };

            var sch = new Schedule 
            { 
                TimeTable = "7:39~15:00 until lockdown ends",
                ScheduleId = GenBadScheduleId
            };
            var schContent = new StringContent(JsonConvert.SerializeObject(sch),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + schId, schContent);

            /* Retrieve the (allegedly) updated sch*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + schId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newSchedule = JsonConvert.DeserializeObject(newCont, typeof(Schedule));

            /* Complete the sch we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            sch.ScheduleId = oldScheduleCopy.ScheduleId;
            sch.Locations = oldScheduleCopy.Locations;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newSchedule.Should().BeEquivalentTo(oldScheduleCopy);
            newSchedule.Should().NotBeEquivalentTo(sch);
        }

        [DataTestMethod]
        #region Rows
        [DataRow("")]
        [DataRow(" ")]
        [DataRow(null)]
        [DataRow
("uyajggurltxtlrqojjtxuxshkpjqsizoogweypmkqtjkzdekbnruwydofwoemuxpdicxnemdhrwgmgnkmvxhsswuwaerqisfeszapjmdxgurbnpbsfzxybryblzofjsivyiuijhyendgsyyzfaxwalgcaznlzlysvpijonpjuzmqktjkwlbcsitxduytjjhnabmoobmec")]
        #endregion
        public async Task UpdateSchedule_BadTimeTables_ShouldReturnBadRequest(string timeTable)
        {
            // Arrange
            var schId = GetFirstScheduleId;

            /* Save a copy of the original entry to compare against*/
            var oldSchedule = await _context.Schedules.FindAsync(schId);
            var oldScheduleCopy = new Schedule
            {
                ScheduleId = oldSchedule.ScheduleId,
                Locations = oldSchedule.Locations,
                TimeTable = oldSchedule.TimeTable
            };

            var sch = new Schedule { TimeTable = timeTable };
            var schContent = new StringContent(JsonConvert.SerializeObject(sch),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + schId, schContent);

            /* Retrieve the (allegedly) updated sch*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + schId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newSchedule = JsonConvert.DeserializeObject(newCont, typeof(Schedule));

            /* Complete the sch we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            sch.ScheduleId = oldScheduleCopy.ScheduleId;
            sch.Locations = oldScheduleCopy.Locations;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newSchedule.Should().BeEquivalentTo(oldScheduleCopy);
            newSchedule.Should().NotBeEquivalentTo(sch);
        }

        [TestMethod]
        public async Task UpdateSchedule_LocationsNull_ShouldReturnBadRequest()
        {
            // Arrange
            var schId = GetFirstScheduleId;

            /* Save a copy of the original entry to compare against*/
            var oldSchedule = await _context.Schedules.FindAsync(schId);
            var oldScheduleCopy = new Schedule
            {
                ScheduleId = oldSchedule.ScheduleId,
                Locations = oldSchedule.Locations,
                TimeTable = oldSchedule.TimeTable
            };

            var sch = new Schedule 
            { 
                TimeTable = "7:39~15:00 until lockdown ends",
                Locations = null
            };
            var schContent = new StringContent(JsonConvert.SerializeObject(sch),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + schId, schContent);

            /* Retrieve the (allegedly) updated sch*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + schId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newSchedule = JsonConvert.DeserializeObject(newCont, typeof(Schedule));

            /* Complete the sch we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            sch.ScheduleId = oldScheduleCopy.ScheduleId;
            sch.Locations = oldScheduleCopy.Locations;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newSchedule.Should().BeEquivalentTo(oldScheduleCopy);
            newSchedule.Should().NotBeEquivalentTo(sch);
        }

        [TestMethod]
        public async Task UpdateSchedule_LocationsNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var schId = GetFirstScheduleId;

            /* Save a copy of the original entry to compare against*/
            var oldSchedule = await _context.Schedules.FindAsync(schId);
            var oldScheduleCopy = new Schedule
            {
                ScheduleId = oldSchedule.ScheduleId,
                Locations = oldSchedule.Locations,
                TimeTable = oldSchedule.TimeTable
            };

            var sch = new Schedule
            {
                TimeTable = "7:39~15:00 until lockdown ends",
                Locations = new Location[] {new Location()}
            };
            var schContent = new StringContent(JsonConvert.SerializeObject(sch),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + schId, schContent);

            /* Retrieve the (allegedly) updated sch*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + schId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newSchedule = JsonConvert.DeserializeObject(newCont, typeof(Schedule));

            /* Complete the sch we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            sch.ScheduleId = oldScheduleCopy.ScheduleId;
            sch.Locations = oldScheduleCopy.Locations;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newSchedule.Should().BeEquivalentTo(oldScheduleCopy);
            newSchedule.Should().NotBeEquivalentTo(sch);
        }

        #endregion

        #region Creates

        [TestMethod]
        public async Task CreateSchedule_HappyFlow_ShouldCreateAndReturnSchedule()
        {
            // Arrange
            var schId = await _context.Schedules.CountAsync() + 1;
            var schedule = new Schedule
            {
                TimeTable = "Mon~Thu: 11:00~19:35; Sat: 12:00~21:30; Closed on Sundays"
            };
            var schCont = new StringContent(JsonConvert.SerializeObject(schedule),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, schCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();

            // Clean-up
            await _client.DeleteAsync(_client.BaseAddress + "/" + schId);
        }

        [TestMethod]
        public async Task CreateSchedule_TriesToSetId_ShouldReturnBadRequest()
        {
            // Arrange
            var schId = await _context.Schedules.CountAsync() + 1;
            var schedule = new Schedule
            {
                TimeTable = "Mon~Thu: 11:00~19:35; Sat: 12:00~21:30; Closed on Sundays",
                ScheduleId = schId
            };
            var schCont = new StringContent(JsonConvert.SerializeObject(schedule),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, schCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [DataTestMethod]
        #region Rows
        [DataRow("")]
        [DataRow(" ")]
        [DataRow(null)]
        [DataRow
("uyajggurltxtlrqojjtxuxshkpjqsizoogweypmkqtjkzdekbnruwydofwoemuxpdicxnemdhrwgmgnkmvxhsswuwaerqisfeszapjmdxgurbnpbsfzxybryblzofjsivyiuijhyendgsyyzfaxwalgcaznlzlysvpijonpjuzmqktjkwlbcsitxduytjjhnabmoobmec")]
        #endregion
        public async Task CreateSchedule_BadTimeTable_ShouldReturnBadRequest(string timeTable)
        {
            // Arrange
            var schedule = new Schedule
            {
                TimeTable = timeTable
            };
            var schCont = new StringContent(JsonConvert.SerializeObject(schedule),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, schCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateSchedule_LocationsNull_ShouldReturnBadRequest()
        {
            // Arrange
            var schedule = new Schedule
            {
                TimeTable = "Mon~Thu: 11:00~19:35; Sat: 12:00~21:30; Closed on Sundays",
                Locations = null
            };
            var schCont = new StringContent(JsonConvert.SerializeObject(schedule),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, schCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateSchedule_LocationsNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var schedule = new Schedule
            {
                TimeTable = "Mon~Thu: 11:00~19:35; Sat: 12:00~21:30; Closed on Sundays",
                Locations = new Location[] {new Location()}
            };
            var schCont = new StringContent(JsonConvert.SerializeObject(schedule),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, schCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        #endregion

        //Helpers
        private int GenBadScheduleId => _context.Schedules
                .AsEnumerable()
                .Select(s => s.ScheduleId)
                .LastOrDefault() + 1;
        private int GetFirstScheduleId => _context.Schedules
                .Select(s => s.ScheduleId)
                .FirstOrDefault();
    }
}
