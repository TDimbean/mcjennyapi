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
    public class SchedulesControllerTests
    {
        private static SchedulesController _controller;
        private static FoodChainsDbContext _context;
        private static ILogger<SchedulesController> _logger;

        [ClassInitialize]
        public static void Init(TestContext testContext)
        {
            _logger = (new Mock<ILogger<SchedulesController>>()).Object;
            Setup();
        }

        [TestInitialize]
        public void InitTest() => Setup();

        #region Gets

        #region Basic

        [TestMethod]
        public async Task GetSchedules_HappyFlow_ShouldReturnAllSchedules()
        {
            // Arrange
            var expected = (object)(await _context.Schedules
                .Select(s => new { s.ScheduleId, s.TimeTable })
                .ToArrayAsync());

            // Act
            var result = (object)(await _controller.GetSchedules()).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSchedule_HappyFlow_ShouldReturnSchedule()
        {
            // Arrange
            var schedule = (await _context.Schedules.FindAsync(GetFirstScheduleId));
            var expected = (object)new { schedule.ScheduleId, schedule.TimeTable };

            // Act
            var result = (object)(await _controller.GetSchedule(schedule.ScheduleId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSchedule_InexistentId_ShouldReturnNotFound()
        {
            // Act
            var result = (await _controller.GetSchedule(GenBadScheduleId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetScheduleBasic_HappyFlow_ShouldReturnSchedule()
        {
            // Arrange
            var expected = await _context.Schedules.FindAsync(GetFirstScheduleId);

            // Act
            var result = (await _controller.GetScheduleBasic(GetFirstScheduleId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetScheduleBasic_InexistentId_ShouldReturnNotFound()
        {
            // Act
            var result = (await _controller.GetScheduleBasic(GenBadScheduleId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetLocations_HappyFlow_ShouldReturnLocationsFollowingSchedule()
        {
            // Arrange
            //var scheduleId = await _context.Schedules.FindAsync(GetFirstScheduleId);
            var scheduleId = GetFirstScheduleId;

            var dbLocations = await _context.Locations.Where(l => l.ScheduleId == scheduleId).ToArrayAsync();
            var locations = new dynamic[dbLocations.Length];

            for (int i = 0; i < dbLocations.Length; i++)
                locations[i] = new
                {
                    dbLocations[i].LocationId,
                    dbLocations[i].AbreviatedCountry,
                    dbLocations[i].AbreviatedState,
                    dbLocations[i].City,
                    dbLocations[i].Street
                };

            var expected = (object)locations;

            // Act
            var result = (object)(await _controller.GetLocations(scheduleId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetLocations_InexistentId_ShouldReturnNotFound()
        {
            // Act
            var result = (await _controller.GetLocations(GenBadScheduleId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        #endregion

        #endregion

        #region Creates

        [TestMethod]
        public async Task CreateSchedule_HappyFlow_ShouldCreateAndReturnSchedule()
        {
            // Arrange
            var schedule = new Schedule
            {
                TimeTable = "Mon/Fri: 8:00~22:00; Sat: 10:00~22:00; Sun:Closed"
            };

            // Act
            await _controller.CreateSchedule(schedule);
            var result = (object)(await _controller.GetScheduleBasic(schedule.ScheduleId)).Value;

            var expected = (object)schedule;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task CreateSchedule_TriesToSetId_ShouldReturnBadRequest()
        {
            // Arrange
            var schedule = new Schedule
            {
                ScheduleId = await _context.Schedules.Select(s => s.ScheduleId).LastOrDefaultAsync() + 1,
                TimeTable = "Mon/Fri: 8:00~22:00; Sat: 10:00~22:00; Sun:Closed"
            };

            // Act
            var result = (await _controller.CreateSchedule(schedule)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
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

            // Act
            var result = (await _controller.CreateSchedule(schedule)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateSchedule_LocationsNull_ShouldReturnBadRequest()
        {
            // Arrange
            var schedule = new Schedule
            {
                TimeTable = "Mon/Fri: 8:00~22:00; Sat: 10:00~22:00; Sun:Closed",
                Locations = null
            };

            // Act
            var result = (await _controller.CreateSchedule(schedule)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateSchedule_LocationsNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var schedule = new Schedule
            {
                TimeTable = "Mon/Fri: 8:00~22:00; Sat: 10:00~22:00; Sun:Closed",
                Locations = new Location[] { new Location() }
            };

            // Act
            var result = (await _controller.CreateSchedule(schedule)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        #endregion

        #region Updates

        [TestMethod]
        public async Task UpdateSchedule_HappyFlow_ShouldCreateAndReturnSchedule()
        {
            // Arrange
            var oldSch = await _context.Schedules.FindAsync(GetFirstScheduleId);
            var oldLocs = await _context.Locations
                .Where(l => l.ScheduleId == oldSch.ScheduleId)
                .ToArrayAsync();
            oldSch.Locations = oldLocs;
            
            var oldSchCopy = new Schedule
            {
                ScheduleId = oldSch.ScheduleId,
                Locations = oldSch.Locations,
                TimeTable = oldSch.TimeTable
            };

            var schedule = new Schedule
            {
                TimeTable = "Mon/Fri: 8:00~22:00; Sat: 10:00~22:00; Sun:Closed"
            };

            // Act
            await _controller.UpdateSchedule(oldSch.ScheduleId, schedule);
            var result = (await _controller.GetScheduleBasic(oldSch.ScheduleId)).Value;

            schedule.ScheduleId = oldSchCopy.ScheduleId;
            schedule.Locations = await _context.Locations
                .Where(l => l.ScheduleId == schedule.ScheduleId)
                .ToArrayAsync();

            var resLocs = await _context.Locations
                .Where(l => l.ScheduleId == result.ScheduleId)
                .ToArrayAsync();
            result.Locations = resLocs;

            // Assert
            result.Should().BeEquivalentTo(schedule);
            result.Should().NotBeEquivalentTo(oldSchCopy);
        }

        [TestMethod]
        public async Task UpdateSchedule_InexistentId_ShouldCreateAndReturnSchedule()
        {
            // Arrange
            var oldSch = await _context.Schedules.FindAsync(GetFirstScheduleId);
            var oldSchCopy = new Schedule
            {
                ScheduleId = oldSch.ScheduleId,
                Locations = oldSch.Locations,
                TimeTable = oldSch.TimeTable
            };

            var schedule = new Schedule
            {
                TimeTable = "Mon/Fri: 8:00~22:00; Sat: 10:00~22:00; Sun:Closed"
            };

            // Act
            var result = await _controller.UpdateSchedule(GenBadScheduleId, schedule);
            var updSch = (await _controller.GetScheduleBasic(oldSch.ScheduleId)).Value;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
            updSch.Should().BeEquivalentTo(oldSchCopy);
        }

        [TestMethod]
        public async Task UpdateSchedule_TriesToSetId_ShouldReturnBadRequest()
        {
            // Arrange
            var oldSch = await _context.Schedules.FindAsync(GetFirstScheduleId);
            var oldSchCopy = new Schedule
            {
                ScheduleId = oldSch.ScheduleId,
                Locations = oldSch.Locations,
                TimeTable = oldSch.TimeTable
            };

            var schedule = new Schedule
            {
                TimeTable = "Mon/Fri: 8:00~22:00; Sat: 10:00~22:00; Sun:Closed",
                ScheduleId = GenBadScheduleId
            };

            // Act
            var result = await _controller.UpdateSchedule(oldSch.ScheduleId, schedule);
            var updatedSch = await _context.Schedules.FindAsync(GetFirstScheduleId);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedSch.Should().BeEquivalentTo(oldSchCopy);
        }

        [DataTestMethod]
        [DataRow(" ")]
        [DataRow
("uyajggurltxtlrqojjtxuxshkpjqsizoogweypmkqtjkzdekbnruwydofwoemuxpdicxnemdhrwgmgnkmvxhsswuwaerqisfeszapjmdxgurbnpbsfzxybryblzofjsivyiuijhyendgsyyzfaxwalgcaznlzlysvpijonpjuzmqktjkwlbcsitxduytjjhnabmoobmec")]
        public async Task UpdateSchedule_BadTimeTable_ShouldReturnBadRequest(string timeTable)
        {
            // Arrange
            var oldSch = await _context.Schedules.FindAsync(GetFirstScheduleId);
            var oldSchCopy = new Schedule
            {
                ScheduleId = oldSch.ScheduleId,
                Locations = oldSch.Locations,
                TimeTable = oldSch.TimeTable
            };

            var schedule = new Schedule
            {
                TimeTable = timeTable
            };

            // Act
            var result = await _controller.UpdateSchedule(oldSch.ScheduleId, schedule);
            var updatedSch = await _context.Schedules.FindAsync(GetFirstScheduleId);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedSch.Should().BeEquivalentTo(oldSchCopy);
        }

        [TestMethod]
        public async Task UpdateSchedule_LocationsNull_ShouldReturnBadRequest()
        {
            // Arrange
            var oldSch = await _context.Schedules.FindAsync(GetFirstScheduleId);
            var oldSchCopy = new Schedule
            {
                ScheduleId = oldSch.ScheduleId,
                Locations = oldSch.Locations,
                TimeTable = oldSch.TimeTable
            };

            var schedule = new Schedule
            {
                TimeTable = "Mon/Fri: 8:00~22:00; Sat: 10:00~22:00; Sun:Closed",
                Locations = null
            };

            // Act
            var result = await _controller.UpdateSchedule(oldSch.ScheduleId, schedule);
            var updatedSch = await _context.Schedules.FindAsync(GetFirstScheduleId);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedSch.Should().BeEquivalentTo(oldSchCopy);
        }

        [TestMethod]
        public async Task UpdateSchedule_LocationsNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var oldSch = await _context.Schedules.FindAsync(GetFirstScheduleId);
            var oldSchCopy = new Schedule
            {
                ScheduleId = oldSch.ScheduleId,
                Locations = oldSch.Locations,
                TimeTable = oldSch.TimeTable
            };

            var schedule = new Schedule
            {
                TimeTable = "Mon/Fri: 8:00~22:00; Sat: 10:00~22:00; Sun:Closed",
                Locations = new Location[] { new Location() }
            };

            // Act
            var result = await _controller.UpdateSchedule(oldSch.ScheduleId, schedule);
            var updatedSch = await _context.Schedules.FindAsync(GetFirstScheduleId);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedSch.Should().BeEquivalentTo(oldSchCopy);
        }

        #endregion

        [TestMethod]
        public void ScheduleExists_ItDoes_ShoudReturnTrue()
        {
            //Arrange
            var methodInfo = typeof(SchedulesController).GetMethod("ScheduleExists", BindingFlags.NonPublic | BindingFlags.Instance);
            var parameters = new object[] { GetFirstScheduleId };

            // Act
            var result = ((methodInfo.Invoke(_controller, parameters)) as Task<bool>).Result;

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public void ScheduleExists_ItDoesNot_ShoudReturnFalse()
        {
            //Arrange
            var methodInfo = typeof(SchedulesController).GetMethod("ScheduleExists", BindingFlags.NonPublic | BindingFlags.Instance);
            var parameters = new object[] { GenBadScheduleId };

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
            _controller = new SchedulesController(_context, _logger);
        }

        /// Helpers
        int GetFirstScheduleId => _context.Schedules.Select(s => s.ScheduleId).OrderBy(id => id).FirstOrDefault();
        int GenBadScheduleId => _context.Schedules.Select(s => s.ScheduleId).ToArray().OrderBy(id => id).LastOrDefault() + 1;

        private static void Setup()
        {
            var schedules = FakeRepo.Schedules;
            var locations = FakeRepo.Locations;

            var DummyOptions = new DbContextOptionsBuilder<FoodChainsDbContext>().Options;

            var dbContextMock = new DbContextMock<FoodChainsDbContext>(DummyOptions);
            var schedulesDbSetMock = dbContextMock.CreateDbSetMock(x => x.Schedules, schedules);
            var locationsDbSetMock = dbContextMock.CreateDbSetMock(x => x.Locations, locations);
            dbContextMock.Setup(m => m.Schedules).Returns(schedulesDbSetMock.Object);
            dbContextMock.Setup(m => m.Locations).Returns(locationsDbSetMock.Object);
            _context = dbContextMock.Object;
            _controller = new SchedulesController(_context, _logger);
        }
    }
}
