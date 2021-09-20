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
    public class ManagementControllerTests
    {
        private static ManagementsController _controller;
        private static FoodChainsDbContext _context;
        private static ILogger<ManagementsController> _logger;

        [ClassInitialize]
        public static void Init(TestContext testContext)
        {
            _logger = (new Mock<ILogger<ManagementsController>>()).Object;
            Setup();
        }

        [TestInitialize]
        public void InitTest() => Setup();

        #region Gets

        [TestMethod]
        public async Task GetManagements_HappyFlow_ShouldReturnAllManagements()
        {
            // Arrange
            var managements = await _context.Managements
               .Select(m => new { m.ManagementId, m.ManagerId, m.LocationId })
               .ToArrayAsync();

            var locIds = managements.Select(m => m.LocationId).Distinct().ToArray();
            var empIds = managements.Select(m => m.ManagerId).Distinct().ToArray();

            var locations = await _context.Locations
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

            var employees = await _context.Employees
                .Where(e => empIds.Contains(e.EmployeeId))
                .Select(e => new { e.EmployeeId, e.FirstName, e.LastName })
                .ToArrayAsync();

            var strings = new string[managements.Length];
            for (int i = 0; i < managements.Length; i++)
            {
                var emp = employees.SingleOrDefault(e => e.EmployeeId == managements[i].ManagerId);
                var loc = locations.SingleOrDefault(l => l.LocationId == managements[i].LocationId);

                strings[i] = string.Format("Management [{0}]: ({1}) {2}, {3} manages ({4}) {5}, {6}{7}, {8}",
                    managements[i].ManagementId,
                    managements[i].ManagerId,
                    emp.LastName, emp.FirstName,
                    managements[i].LocationId,
                    loc.AbreviatedCountry,
                    loc.AbreviatedState == "N/A" ? string.Empty : loc.AbreviatedState + ", ",
                    loc.City, loc.Street);
            }

                var expected = (object)strings;

            // Act
            var result = (object)(await _controller.GetManagements()).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetManagement_HappyFlow_ShouldReturnManagement()
        {
            // Arrange
            var management = await _context.Managements.FindAsync(GetFirstManagementId);

            var employee = await _context.Employees.FindAsync(management.ManagerId);
            var location = await _context.Locations.FindAsync(management.LocationId);

            var expected = (object)string.Format("({0}) {1}, {2} manages ({3}) {4}, {5}{6}, {7}",
                    management.ManagerId,
                    employee.LastName, employee.FirstName,
                    management.LocationId,
                    location.AbreviatedCountry,
                    location.AbreviatedState == "N/A" ? string.Empty :
                    location.AbreviatedState + ", ",
                    location.City,
                    location.Street);


            // Act
            var result = (object)(await _controller
                .GetManagement(GetFirstManagementId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetManagement_InexistentId_ShouldReturnNotFound()
        {
            // Act
            var result = (object)(await _controller
                .GetManagement(GenBadManagementId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetManagementBasic_HappyFlow_ShouldReturnRequirement()
        {
            // Arrange
            var expected = (object)(await _context.Managements.FirstOrDefaultAsync());

            // Act
            var result = (object)(await _controller
                .GetManagementBasic(GetFirstManagementId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetManagementBasic_InexistentId_ShouldReturnNotFound()
        {
            // Act
            var result = (object)(await _controller
                .GetManagementBasic(GenBadManagementId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        #region Basic

        #endregion

        #endregion

        #region Creates

        [TestMethod]
        public async Task CreateManagement_HappyFlow_ShouldCreateAndReturnManagement()
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

            var manager = new Employee
            {
                FirstName = "Stewart",
                LastName = "Gobblins",
                LocationId = location.LocationId,
                PositionId = 1,
                WeeklyHours = 32,
                StartedOn = location.OpenSince
            };
            await _context.Employees.AddAsync(manager);

            var management = new Management 
            { 
                LocationId = location.LocationId,
                ManagerId = manager.EmployeeId
            };

            // Act
            await _controller.CreateManagement(management);
            var result = (object)(await _controller.GetManagementBasic(management.ManagementId)).Value;

            var expected = (object)management;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task CreateManagement_TriesToSetId_ShouldReturnBadRequest()
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

            var manager = new Employee
            {
                FirstName = "Stewart",
                LastName = "Gobblins",
                LocationId = location.LocationId,
                PositionId = 1,
                WeeklyHours = 32,
                StartedOn = location.OpenSince
            };
            await _context.Locations.AddAsync(location);

            var management = new Management
            {
                ManagementId = await _context.Managements.Select(m=>m.ManagementId).LastOrDefaultAsync()+1,
                LocationId = location.LocationId,
                ManagerId = manager.EmployeeId
            };

            // Act
            var result = (object)(await _controller.CreateManagement(management)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateManagement_HasLocation_ShouldReturnBadRequest()
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

            var manager = new Employee
            {
                FirstName = "Stewart",
                LastName = "Gobblins",
                LocationId = location.LocationId,
                PositionId = 1,
                WeeklyHours = 32,
                StartedOn = location.OpenSince
            };
            await _context.Locations.AddAsync(location);

            var management = new Management
            {
                Location = new Location(),
                LocationId = location.LocationId,
                ManagerId = manager.EmployeeId
            };

            // Act
            var result = (object)(await _controller.CreateManagement(management)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateManagement_HasManager_ShouldReturnBadRequest()
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

            var manager = new Employee
            {
                FirstName = "Stewart",
                LastName = "Gobblins",
                LocationId = location.LocationId,
                PositionId = 1,
                WeeklyHours = 32,
                StartedOn = location.OpenSince
            };
            await _context.Locations.AddAsync(location);

            var management = new Management
            {
                Manager = new Employee(),
                LocationId = location.LocationId,
                ManagerId = manager.EmployeeId
            };

            // Act
            var result = (object)(await _controller.CreateManagement(management)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateManagement_BadLocationId_ShouldReturnBadRequest()
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

            var manager = new Employee
            {
                FirstName = "Stewart",
                LastName = "Gobblins",
                LocationId = location.LocationId,
                PositionId = 1,
                WeeklyHours = 32,
                StartedOn = location.OpenSince
            };
            await _context.Locations.AddAsync(location);

            var management = new Management
            {
                LocationId = await _context.Locations.Select(l=>l.LocationId).LastOrDefaultAsync() + 1,
                ManagerId = manager.EmployeeId
            };

            // Act
            var result = (object)(await _controller.CreateManagement(management)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateManagement_BadManagerId_ShouldReturnBadRequest()
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

            var management = new Management
            {
                LocationId = location.LocationId,
                ManagerId = await _context.Employees.Select(e=>e.EmployeeId).LastOrDefaultAsync()+1
            };

            // Act
            var result = (object)(await _controller.CreateManagement(management)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateManagement_EmployeeManagesAnotherLocation_ShouldReturnBadRequest()
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

            var management = new Management
            {
                LocationId = location.LocationId,
                ManagerId = await _context.Employees.Select(e=>e.EmployeeId).FirstOrDefaultAsync()
            };

            // Act
            var result = (object)(await _controller.CreateManagement(management)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateManagement_LocationAlreadyManaged_ShouldReturnBadRequest()
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

            var manager = new Employee
            {
                FirstName = "Stewart",
                LastName = "Gobblins",
                LocationId = location.LocationId,
                PositionId = 1,
                WeeklyHours = 32,
                StartedOn = location.OpenSince
            };
            await _context.Locations.AddAsync(location);

            var management = new Management
            {
                LocationId = await _context.Locations.Select(l => l.LocationId).FirstOrDefaultAsync(),
                ManagerId = manager.EmployeeId
            };

            // Act
            var result = (object)(await _controller.CreateManagement(management)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateManagement_ManagerWithWrongPosition_ShouldReturnBadRequest()
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

            var manager = new Employee
            {
                FirstName = "Stewart",
                LastName = "Gobblins",
                LocationId = location.LocationId,
                PositionId = 2,
                WeeklyHours = 32,
                StartedOn = location.OpenSince
            };
            await _context.Locations.AddAsync(location);

            var management = new Management
            {
                LocationId = location.LocationId,
                ManagerId = manager.EmployeeId
            };

            // Act
            var result = (object)(await _controller.CreateManagement(management)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        #endregion

        [TestMethod]
        public void ManagementExists_ItDoes_ShoudReturnTrue()
        {
            //Arrange
            var methodInfo = typeof(ManagementsController).GetMethod("ManagementExists",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var parameters = new object[] { GetFirstManagementId };

            // Act
            var result = ((methodInfo.Invoke(_controller, parameters)) as Task<bool>).Result;

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public void ManagementExists_ItDoesNot_ShoudReturnFalse()
        {
            //Arrange
            var methodInfo = typeof(ManagementsController).GetMethod("ManagementExists",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var parameters = new object[] { GenBadManagementId };

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
            _controller = new ManagementsController(_context, _logger);
        }

        /// Helpers

        int GetFirstManagementId => _context.Managements
            .Select(r => r.ManagementId)
            .OrderBy(id => id)
            .FirstOrDefault();
        int GenBadManagementId => _context.Managements
            .Select(r => r.ManagementId)
            .ToArray()
            .OrderBy(id => id)
            .LastOrDefault() + 1;

        private static void Setup()
        {
            _context = InMemoryHelpers.GetContext();
            _controller = new ManagementsController(_context, _logger);
        }
    }
}
