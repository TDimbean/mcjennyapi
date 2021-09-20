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
using System.Threading.Tasks;

namespace McJenny.UnitTests.ControlerTests.InMemoryDbVersions
{
    [TestClass]
    public class EmployeeControllerTests
    {
        private static FoodChainsDbContext _context;
        private static EmployeesController _controller;
        private static ILogger<EmployeesController> _logger;

        [ClassInitialize]
        public static void Init(TestContext testContext)
        {
            _logger = (new Mock<ILogger<EmployeesController>>()).Object;
            Setup();
        }

        [TestInitialize]
        public void InitTest() => Setup();
        #region Gets

        #region Basic

        [TestMethod]
        public async Task GetEmployees_HappyFlow_ShouldReturnAllEmpoyees()
        {
            // Arrange
            var expected = await _context.Employees
                .Include(e => e.Position)
                 .Select(e => new
                 {
                     e.EmployeeId,
                     e.FirstName,
                     e.LastName,
                     e.LocationId,
                     e.Position.Title,
                     StartedOn = e.StartedOn.ToShortDateString(),
                     e.WeeklyHours
                 })
                .Take(20)
                .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetEmployees()).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }
        
        [TestMethod]
        public async Task GetEmployee_HappyFlow_ShouldReturnEmployee()
        {
            // Arrange
            var employee = await _context.Employees.FindAsync(GetFirstEmployeeId);

            var position = await _context.Positions.SingleOrDefaultAsync(p => p.PositionId == employee.PositionId);

            var expected = (object) new
            {
                employee.EmployeeId,
                employee.FirstName,
                employee.LastName,
                employee.Location,
                Position = position.Title,
                employee.WeeklyHours,
                StartedOn = employee.StartedOn.ToShortDateString(),
                Salary = string.Format("{0:C}", position.Wage * employee.WeeklyHours)
            };

            // Act
            var result = (object)(await _controller.GetEmployee(employee.EmployeeId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetEmployeeInexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var badId = GenBadEmployeeId;

            // Act
            var result = (await _controller.GetEmployee(badId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetEmployeeBasic_HappyFlow_ShouldReturnEmployee()
        {
            // Arrange
            var expected = await _context.Employees.FirstOrDefaultAsync();

            // Act
            var result = (await _controller.GetEmployeeBasic(GetFirstEmployeeId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetEmployeeBasic_InexistenId_ShouldReturnNotFound()
        {
            // Act
            var result = (await _controller.GetEmployeeBasic(GenBadEmployeeId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async void GetSalary_HappyFlow_ShouldReturnSalary()
        {
            // Arrange
            var employee = await _context.Employees.FirstOrDefaultAsync();

            var wage = (await _context.Positions.FindAsync(employee.PositionId)).Wage;

            var expected = new
            {
                Weekly = string.Format("{0:C}", wage * employee.WeeklyHours),
                Monthly = string.Format("{0:C}", wage * employee.WeeklyHours * 4),
                Yearly = string.Format("{0:C}", wage * employee.WeeklyHours * 52)
            };

            // Act
            var result = (await _controller.GetSalary(GetFirstEmployeeId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async void GetSalary_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var badId = GenBadEmployeeId;

            // Act
            var result = (await _controller.GetSalary(badId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        #endregion

        #region Advanced

        [TestMethod]
        public async Task GetEmployeesFiltered_FirstNameMatch_ShouldReturnFiltered()
        {
            // Arrange
            var filter = await _context.Employees.Select(e => e.FirstName).FirstOrDefaultAsync();

            var expected = (object)await _context.Employees
                .Include(e => e.Position)
                 .Select(e => new
                 {
                     e.EmployeeId,
                     e.FirstName,
                     e.LastName,
                     e.LocationId,
                     e.Position.Title,
                     StartedOn = e.StartedOn.ToShortDateString(),
                     e.WeeklyHours
                 })
                 .Where(e => e.FirstName.ToUpper()
                     .Contains(filter.ToUpper()) ||
                        e.LastName.ToUpper()
                    .Contains(filter.ToUpper()) ||
                        e.Title.ToUpper().Contains(filter.ToUpper()))
                 .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetEmployeesFiltered(filter)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetEmployeesFiltered_LastNameMatch_ShouldReturnFiltered()
        {
            // Arrange
            var filter = await _context.Employees.Select(e => e.LastName).FirstOrDefaultAsync();

            var expected = (object)await _context.Employees
                .Include(e => e.Position)
                 .Select(e => new
                 {
                     e.EmployeeId,
                     e.FirstName,
                     e.LastName,
                     e.LocationId,
                     e.Position.Title,
                     StartedOn = e.StartedOn.ToShortDateString(),
                     e.WeeklyHours
                 })
                 .Where(e => e.FirstName.ToUpper()
                     .Contains(filter.ToUpper()) ||
                        e.LastName.ToUpper()
                    .Contains(filter.ToUpper()) ||
                        e.Title.ToUpper().Contains(filter.ToUpper()))
                 .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetEmployeesFiltered(filter)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetEmployeesFiltered_TitleMatch_ShouldReturnFiltered()
        {
            // Arrange
            var filter = await _context.Employees
                .Include(e=>e.Position)
                .Select(e=>e.Position.Title)
                .FirstOrDefaultAsync();

            var expected = (object)await _context.Employees
                .Include(e => e.Position)
                 .Select(e => new
                 {
                     e.EmployeeId,
                     e.FirstName,
                     e.LastName,
                     e.LocationId,
                     e.Position.Title,
                     StartedOn = e.StartedOn.ToShortDateString(),
                     e.WeeklyHours
                 })
                 .Where(e => e.FirstName.ToUpper()
                     .Contains(filter.ToUpper()) ||
                        e.LastName.ToUpper()
                    .Contains(filter.ToUpper()) ||
                        e.Title.ToUpper().Contains(filter.ToUpper()))
                 .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetEmployeesFiltered(filter)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetEmployeesPaged_HappyFlow_ShouldReturnPaged()
        {
            // Arrange
            var pgSz = 2;
            var pgInd = 1;

            var expected = (object)await _context.Employees
                  .Include(e => e.Position)
                 .Select(e => new
                 {
                     e.EmployeeId,
                     e.FirstName,
                     e.LastName,
                     e.LocationId,
                     e.Position.Title,
                     StartedOn = e.StartedOn.ToShortDateString(),
                     e.WeeklyHours
                 })
                 .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetEmployeesPaged(pgInd, pgSz)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetEmployeesPaged_EndPageIncomplete_ShouldReturnPaged()
        {
            // Arrange
            var empCount = await _context.Employees.CountAsync();
            var pgSz = 1;

            while (true)
            {
                if (empCount % pgSz == 0) pgSz++;
                else break;
            }

            var pgInd = empCount / (pgSz - 1);

            var expected = (object)await _context.Employees
                  .Include(e => e.Position)
                 .Select(e => new
                 {
                     e.EmployeeId,
                     e.FirstName,
                     e.LastName,
                     e.LocationId,
                     e.Position.Title,
                     StartedOn = e.StartedOn.ToShortDateString(),
                     e.WeeklyHours
                 })
                 .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArrayAsync();

            // Act
            var call = (await _controller.GetEmployeesPaged(pgInd, pgSz)).Value;
            var result = (object)call;
            var resCount = (result as IEnumerable<dynamic>).Count();

            // Assert
            result.Should().BeEquivalentTo(expected);
            resCount.Should().BeLessThan(pgSz);
        }

        [TestMethod]
        public async Task GetEmployeesPaged_SizeOver_ShouldReturnPaged()
        {
            // Arrange
            var empCount = await _context.Employees.CountAsync();
            var pgSz = empCount + 1;
            var pgInd = 1;

            var expected = (object)await _context.Employees
                  .Include(e => e.Position)
                 .Select(e => new
                 {
                     e.EmployeeId,
                     e.FirstName,
                     e.LastName,
                     e.LocationId,
                     e.Position.Title,
                     StartedOn = e.StartedOn.ToShortDateString(),
                     e.WeeklyHours
                 })
                 .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetEmployeesPaged(pgInd, pgSz)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetEmployeesPaged_IndexOver_ShouldReturnPaged()
        {
            // Arrange
            var empCount = await _context.Employees.CountAsync();
            var pgInd = empCount + 1;
            var pgSz = 1;

            var expected = (object)await _context.Employees
                  .Include(e => e.Position)
                 .Select(e => new
                 {
                     e.EmployeeId,
                     e.FirstName,
                     e.LastName,
                     e.LocationId,
                     e.Position.Title,
                     StartedOn = e.StartedOn.ToShortDateString(),
                     e.WeeklyHours
                 })
                 .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetEmployeesPaged(pgInd, pgSz)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetEmployeesSorted_ByFirstNameAsc_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "firstname";

            var expected = (object)await _context.Employees
                             .Include(e => e.Position)
                 .Select(e => new
                 {
                     e.EmployeeId,
                     e.FirstName,
                     e.LastName,
                     e.LocationId,
                     e.Position.Title,
                     StartedOn = e.StartedOn.ToShortDateString(),
                     e.WeeklyHours
                 })
                            .OrderBy(e => e.FirstName)
                            .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetEmployeesSorted(sortBy, false)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetEmployeesSorted_ByLastName_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "lastname";

            var expected = (object)await _context.Employees
                             .Include(e => e.Position)
                 .Select(e => new
                 {
                     e.EmployeeId,
                     e.FirstName,
                     e.LastName,
                     e.LocationId,
                     e.Position.Title,
                     StartedOn = e.StartedOn.ToShortDateString(),
                     e.WeeklyHours
                 })
                            .OrderBy(e => e.LastName)
                            .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetEmployeesSorted(sortBy, false)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetEmployeesSorted_ByTitle_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "position";

            var expected = (object)await _context.Employees
                             .Include(e => e.Position)
                 .Select(e => new
                 {
                     e.EmployeeId,
                     e.FirstName,
                     e.LastName,
                     e.LocationId,
                     e.Position.Title,
                     StartedOn = e.StartedOn.ToShortDateString(),
                     e.WeeklyHours
                 })
                            .OrderBy(e => e.FirstName)
                            .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetEmployeesSorted(sortBy, false)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetEmployeesSorted_ByLocation_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "location";

            var expected = (object)await _context.Employees
                             .Include(e => e.Position)
                 .Select(e => new
                 {
                     e.EmployeeId,
                     e.FirstName,
                     e.LastName,
                     e.LocationId,
                     e.Position.Title,
                     StartedOn = e.StartedOn.ToShortDateString(),
                     e.WeeklyHours
                 })
                            .OrderBy(e => e.FirstName)
                            .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetEmployeesSorted(sortBy, false)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetEmployeesSorted_Desc_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "firstname";

            var expected = (object)await _context.Employees
                            .Include(e => e.Position)
                 .Select(e => new
                 {
                     e.EmployeeId,
                     e.FirstName,
                     e.LastName,
                     e.LocationId,
                     e.Position.Title,
                     StartedOn = e.StartedOn.ToShortDateString(),
                     e.WeeklyHours
                 })
                            .OrderByDescending(e => e.FirstName)
                            .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetEmployeesSorted(sortBy, true)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetEmployeesSorted_GarbledText_ShouldDefaultoIdSort()
        {
            // Arrange
            var sortBy = "asd";

            var expected = (object)await _context.Employees
                            .Include(e => e.Position)
                 .Select(e => new
                 {
                     e.EmployeeId,
                     e.FirstName,
                     e.LastName,
                     e.LocationId,
                     e.Position.Title,
                     StartedOn = e.StartedOn.ToShortDateString(),
                     e.WeeklyHours
                 })
                            .OrderBy(e => e.EmployeeId)
                            .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetEmployeesSorted(sortBy, false)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetEmployeesStarted_Before_ShouldReturEmpsSinceBefore()
        {
            // Arrange
            var date = await _context.Employees
                .Select(e => e.StartedOn)
                .OrderBy(e => e)
                .Skip(1)
                .FirstOrDefaultAsync();

            var expected = (object)await _context.Employees
                .Include(e => e.Position)
                 .Select(e => new
                 {
                     e.EmployeeId,
                     e.FirstName,
                     e.LastName,
                     e.LocationId,
                     e.Position.Title,
                     e.StartedOn,
                     e.WeeklyHours
                 })
                 .Where(e =>
                    e.StartedOn < date)
                    .Select(e => new
                    {
                        e.EmployeeId,
                        e.FirstName,
                        e.LastName,
                        e.LocationId,
                        e.Title,
                        StartedOn = e.StartedOn.ToShortDateString(),
                        e.WeeklyHours
                    })
                 .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetEmployeesStarted(date, true)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetEmployeesStarted_After_ShouldReturEmpsSinceAfter()
        {
            // Arrange
            var date = await _context.Employees
                .Select(e => e.StartedOn)
                .OrderBy(e => e)
                .Skip(1)
                .FirstOrDefaultAsync();

            var expected = (object)await _context.Employees
                .Include(e => e.Position)
                 .Select(e => new
                 {
                     e.EmployeeId,
                     e.FirstName,
                     e.LastName,
                     e.LocationId,
                     e.Position.Title,
                     e.StartedOn,
                     e.WeeklyHours
                 })
                 .Where(e =>
                    e.StartedOn >= date)
                    .Select(e => new
                    {
                        e.EmployeeId,
                        e.FirstName,
                        e.LastName,
                        e.LocationId,
                        e.Title,
                        StartedOn = e.StartedOn.ToShortDateString(),
                        e.WeeklyHours
                    })
                 .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetEmployeesStarted(date, false)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        #endregion

        #endregion

        #region Creates

        [TestMethod]
        public async Task CreateEmployee_HappyFlow_ShouldCreateAndReturnEmployee()
        {
            // Arrange
            var employee = new Employee
            {
                FirstName = "Daniel",
                LastName = "Crawford",
                LocationId = (await _context.Locations.FirstOrDefaultAsync()).LocationId,
                PositionId = (await _context.Positions.LastOrDefaultAsync()).PositionId,
                WeeklyHours = 36,
                StartedOn = DateTime.Today.AddDays(-200)
            };

            // Act
            await _controller.CreateEmployee(employee);
            var result = (await _controller.GetEmployeeBasic(employee.EmployeeId)).Value;

            var expected = (object)employee;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task CreateEmployee_TriesToSetId_ShouldReturnBadRequest()
        {
            // Arrange
            var employee = new Employee
            {
                EmployeeId = await _context.Employees.Select(e => e.EmployeeId).LastOrDefaultAsync() + 1,
                FirstName = "John",
                LastName = "Doe",
                LocationId = await _context.Locations.Select(l => l.LocationId).FirstOrDefaultAsync(),
                PositionId = await _context.Positions.Select(p => p.PositionId).LastOrDefaultAsync(),
                StartedOn = DateTime.Today.AddDays(-100),
                WeeklyHours = 32
            };

            // Act
            var result = (await _controller.CreateEmployee(employee)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [DataTestMethod]
        #region Rows
        [DataRow("", "Crawford")]
        [DataRow(" ", "Crawford")]
        [DataRow(null, "Crawford")]
        [DataRow("Daniel", "")]
        [DataRow("Daniel", " ")]
        [DataRow("Daniel", null)]
        [DataRow("Daniel", "012345678911234567892123456789312345678941234567890")]
        [DataRow("012345678911234567892123456789312345678941234567890", "Crawford")]
        #endregion
        public async Task CreateEmployee_WrongNameLengths_ShouldReturnBadRequest(string firstName, string lastName)
        {
            // Arrange
            var employee = new Employee
            {
                FirstName = firstName,
                LastName = lastName,
                Location = null,
                LocationId = (await _context.Locations.LastOrDefaultAsync()).LocationId,
                WeeklyHours = 34,
                Position = null,
                PositionId = (await _context.Positions.FirstOrDefaultAsync()).PositionId,
                StartedOn = DateTime.Today.AddDays(-200),
            };

            // Act
            var result = (await _controller.CreateEmployee(employee)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateEmployee_NoStartingDate_ShouldReturnBadRequest()
        {
            // Arrange
            var employee = new Employee
            {
                EmployeeId = await _context.Employees.Select(e => e.EmployeeId).LastOrDefaultAsync() + 1,
                FirstName = "John",
                LastName = "Doe",
                LocationId = await _context.Locations.Select(l => l.LocationId).FirstOrDefaultAsync(),
                PositionId = await _context.Positions.Select(p => p.PositionId).LastOrDefaultAsync(),
                WeeklyHours = 32
            };

            // Act
            var result = (await _controller.CreateEmployee(employee)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [DataTestMethod]
        [DataRow(0)]
        [DataRow(168)]
        public async Task CreateEmployee_HoursOutOfRange_ShouldReturnBadRequest(int weeklyHours)
        {
            // Arrange
            var employee = new Employee
            {
                FirstName = "Daniel",
                LastName = "Crawford",
                LocationId = (await _context.Locations.LastOrDefaultAsync()).LocationId,
                PositionId = (await _context.Positions.FirstOrDefaultAsync()).PositionId,
                StartedOn = DateTime.Today.AddDays(-200),
                WeeklyHours = weeklyHours
            };

            // Act
            var result = (await _controller.CreateEmployee(employee)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateEmployee_InexistentLocationId_ShouldReturnBadRequest()
        {
            // Arrange
            var employee = new Employee
            {
                FirstName = "Daniel",
                LastName = "Crawford",
                LocationId = (await _context.Locations.LastOrDefaultAsync()).LocationId+1,
                PositionId = (await _context.Positions.FirstOrDefaultAsync()).PositionId,
                StartedOn = DateTime.Today.AddDays(-200),
                WeeklyHours = 36
            };

            // Act
            var result = (await _controller.CreateEmployee(employee)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateEmployee_InexistentPositionId_ShouldReturnBadRequest()
        {
            // Arrange
            var employee = new Employee
            {
                FirstName = "Daniel",
                LastName = "Crawford",
                LocationId = (await _context.Locations.LastOrDefaultAsync()).LocationId,
                PositionId = (await _context.Positions.LastOrDefaultAsync()).PositionId+1,
                StartedOn = DateTime.Today.AddDays(-200),
                WeeklyHours = 36
            };

            // Act
            var result = (await _controller.CreateEmployee(employee)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateEmployee_HasLocation_ShouldReturnBadRequest()
        {
            // Arrange
            var employee = new Employee
            {
                FirstName = "Daniel",
                LastName = "Crawford",
                Location = new Location(),
                LocationId = (await _context.Locations.LastOrDefaultAsync()).LocationId,
                PositionId = (await _context.Positions.FirstOrDefaultAsync()).PositionId,
                StartedOn = DateTime.Today.AddDays(-200),
                WeeklyHours = 36
            };

            // Act
            var result = (await _controller.CreateEmployee(employee)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateEmployee_HasPosition_ShouldReturnBadRequest()
        {
            // Arrange
            var employee = new Employee
            {
                FirstName = "Daniel",
                LastName = "Crawford",
                Position = new Position(),
                LocationId = (await _context.Locations.LastOrDefaultAsync()).LocationId,
                PositionId = (await _context.Positions.LastOrDefaultAsync()).PositionId,
                StartedOn = DateTime.Today.AddDays(-200),
                WeeklyHours = 36
            };

            // Act
            var result = (await _controller.CreateEmployee(employee)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateEmployee_ManagementsNull_ShouldReturnBadRequest()
        {
            // Arrange
            var employee = new Employee
            {
                FirstName = "Daniel",
                LastName = "Crawford",
                LocationId = (await _context.Locations.LastOrDefaultAsync()).LocationId,
                PositionId = (await _context.Positions.LastOrDefaultAsync()).PositionId,
                StartedOn = DateTime.Today.AddDays(-200),
                WeeklyHours = 36,
                Managements = null
            };

            // Act
            var result = (await _controller.CreateEmployee(employee)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateEmployee_ManagementsNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var employee = new Employee
            {
                FirstName = "Daniel",
                LastName = "Crawford",
                LocationId = (await _context.Locations.LastOrDefaultAsync()).LocationId,
                PositionId = (await _context.Positions.LastOrDefaultAsync()).PositionId,
                StartedOn = DateTime.Today.AddDays(-200),
                WeeklyHours = 36,
                Managements = new Management[] {new Management()}
            };

            // Act
            var result = (await _controller.CreateEmployee(employee)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateEmployee_TriesToManageManagedLocation_ShouldReturBadRequest()
        {
            // Arrange
            var locId = await _context.Locations
                .Where(l => l.Managements.Count == 1)
                .Select(l => l.LocationId)
                .FirstOrDefaultAsync();

            var employee = new Employee
            {
                FirstName = "Daniel",
                LastName = "Crawford",
                LocationId = locId,
                PositionId = 1,
                WeeklyHours = 36,
                StartedOn = DateTime.Today.AddDays(-200)
            };

            // Act
            var res = (await _controller.CreateEmployee(employee)).Result;

            // Assert
            res.Should().BeOfType<BadRequestResult>();
        }

        #region Deleted

        //[TestMethod]
        //public async Task CreateEmployee_NewPositionNoMagNoLoc_ShouldCreateAndReturnEmployee()
        //{
        //    // Arrange
        //    var position = new Position
        //    {
        //        Title = "Plumber",
        //        Wage = 24.50m
        //    };

        //    var employee = new Employee
        //    {
        //        FirstName = "Daniel",
        //        LastName = "Crawford",
        //        Location = null,
        //        LocationId = (await _context.Locations.FirstOrDefaultAsync()).LocationId,
        //        WeeklyHours = 36,
        //        Position = position,
        //        StartedOn = DateTime.Today.AddDays(-200),
        //    };

        //    // Act
        //    await _controller.CreateEmployee(employee);
        //    var result = (await _controller.GetEmployeeBasic(employee.EmployeeId)).Value;

        //    // Assert
        //    result.Should().BeEquivalentTo(employee);
        //}

        //[TestMethod]
        //public async Task CreateEmployee_NewLocationNoPosNoMag_ShouldCreateAndReturnEmployee()
        //{
        //    // Arrange
        //    var location = new Location
        //    {
        //        AbreviatedCountry = "FR",
        //        AbreviatedState = "A8",
        //        City = "Sannois",
        //        Street = "26 Kipling Parkway",
        //        OpenSince = new DateTime(2018, 03, 20),
        //        MenuId = (await _context.Menus.FirstOrDefaultAsync()).MenuId,
        //        ScheduleId = (await _context.Schedules.FirstOrDefaultAsync()).ScheduleId 
        //    };

        //    var employee = new Employee
        //    {
        //        FirstName = "Daniel",
        //        LastName = "Crawford",
        //        Location = location,
        //        WeeklyHours = 36,
        //        PositionId = (await _context.Positions.FirstOrDefaultAsync()).PositionId,
        //        StartedOn = DateTime.Today.AddDays(-200),
        //    };

        //    // Act
        //    await _controller.CreateEmployee(employee);
        //    var result = (await _controller.GetEmployeeBasic(employee.EmployeeId)).Value;

        //    // Assert
        //    result.Should().BeEquivalentTo(employee);
        //}


        //[TestMethod]
        //public async Task CreateEmployee_NewManagementExPosExLoc_ShouldCreateAndReturnEmployee()
        //{
        //    // Arrange
        //    var locToManage = new Location
        //    {
        //        AbreviatedCountry = "FR",
        //        AbreviatedState = "B2",
        //        Street = "96 Lyons Plaza",
        //        City = "Épinal",
        //        OpenSince = new DateTime(2018, 7, 24).Date,
        //        MenuId = (await _context.Menus.FirstOrDefaultAsync()).MenuId,
        //        ScheduleId = (await _context.Schedules.FirstOrDefaultAsync()).ScheduleId
        //    };

        //    await _context.Locations.AddAsync(locToManage);
        //    await _context.SaveChangesAsync();

        //    var managements = new Management[] { new Management { LocationId = locToManage.LocationId } };

        //    var employee = new Employee
        //    {
        //        FirstName = "Daniel",
        //        LastName = "Crawford",
        //        LocationId = locToManage.LocationId,
        //        WeeklyHours = 36,
        //        Managements = managements,
        //        PositionId = (await _context.Positions.FirstOrDefaultAsync()).PositionId,
        //        StartedOn = DateTime.Today.AddDays(-200),
        //    };

        //    // Act
        //    var res = (await _controller.CreateEmployee(employee)).Result;
        //    var result = (await _controller.GetEmployeeBasic(employee.EmployeeId)).Value;

        //    // Assert
        //    res.Should().BeOfType<CreatedAtActionResult>();
        //    result.Should().BeEquivalentTo(employee);
        //}

        //[TestMethod]
        //public async Task CreateEmployee_ManagementHasInexistentLocationId_ShouldReturnBadRequest()
        //{
        //    // Arrange
        //    var locationId = (await _context.Locations.LastOrDefaultAsync()).LocationId + 1;

        //    var managements = new Management[] { new Management { LocationId = locationId } };

        //    var employee = new Employee
        //    {
        //        FirstName = "Daniel",
        //        LastName = "Crawford",
        //        LocationId = locationId,
        //        WeeklyHours = 36,
        //        Managements = managements,
        //        PositionId = (await _context.Positions.FirstOrDefaultAsync()).PositionId,
        //        StartedOn = DateTime.Today.AddDays(-200),
        //    };

        //    // Act
        //    var res = (await _controller.CreateEmployee(employee)).Result;

        //    // Assert
        //    res.Should().BeOfType<BadRequestResult>();
        //}

        //[TestMethod]
        //public async Task CreateEmployee_ManagementLocIdInconsistentWithLocId_ShouldReturBadRequest()
        //{
        //    // Arrange
        //    var locToManage = new Location
        //    {
        //        AbreviatedCountry = "FR",
        //        AbreviatedState = "B2",
        //        Street = "96 Lyons Plaza",
        //        City = "Épinal",
        //        OpenSince = new DateTime(2018, 7, 24).Date,
        //        MenuId = (await _context.Menus.FirstOrDefaultAsync()).MenuId,
        //        ScheduleId = (await _context.Schedules.FirstOrDefaultAsync()).ScheduleId
        //    };

        //    await _context.Locations.AddAsync(locToManage);
        //    await _context.SaveChangesAsync();

        //    var managements = new Management[] { new Management { LocationId = locToManage.LocationId } };

        //    var employee = new Employee
        //    {
        //        FirstName = "Daniel",
        //        LastName = "Crawford",
        //        LocationId = await _context.Locations.Select(l => l.LocationId).FirstOrDefaultAsync(),
        //        WeeklyHours = 36,
        //        Managements = managements,
        //        PositionId = (await _context.Positions.FirstOrDefaultAsync()).PositionId,
        //        StartedOn = DateTime.Today.AddDays(-200),
        //    };

        //    // Act
        //    var res = (await _controller.CreateEmployee(employee)).Result;

        //    // Assert
        //    res.Should().BeOfType<BadRequestResult>();
        //}

        //[TestMethod]
        //public async Task CreateEmployee_ManagementLocIdInconsistentWithLocLocId_ShouldReturBadRequest()
        //{
        //    // Arrange
        //    var locToManage = new Location
        //    {
        //        AbreviatedCountry = "FR",
        //        AbreviatedState = "B2",
        //        Street = "96 Lyons Plaza",
        //        City = "Épinal",
        //        OpenSince = new DateTime(2018, 7, 24).Date,
        //        MenuId = (await _context.Menus.FirstOrDefaultAsync()).MenuId,
        //        ScheduleId = (await _context.Schedules.FirstOrDefaultAsync()).ScheduleId
        //    };

        //    await _context.Locations.AddAsync(locToManage);
        //    await _context.SaveChangesAsync();

        //    var managements = new Management[] { new Management
        //        {
        //            LocationId = await _context.Locations.Select(l=>l.LocationId).LastOrDefaultAsync()+1
        //        }
        //    };

        //    var employee = new Employee
        //    {
        //        FirstName = "Daniel",
        //        LastName = "Crawford",
        //        Location = await _context.Locations.FindAsync(locToManage.LocationId),
        //        WeeklyHours = 36,
        //        Managements = managements,
        //        PositionId = (await _context.Positions.FirstOrDefaultAsync()).PositionId,
        //        StartedOn = DateTime.Today.AddDays(-200),
        //    };

        //    // Act
        //    var res = (await _controller.CreateEmployee(employee)).Result;

        //    // Assert
        //    res.Should().BeOfType<BadRequestResult>();
        //}


        //[TestMethod]
        //public async Task CreateEmployee_MultipleManagements_ShouldReturnBadRequest()
        //{
        //    // Arrange
        //    var locToManage1 = new Location
        //    {
        //        AbreviatedCountry = "FR",
        //        AbreviatedState = "B2",
        //        Street = "96 Lyons Plaza",
        //        City = "Épinal",
        //        OpenSince = new DateTime(2018, 7, 24).Date,
        //        MenuId = (await _context.Menus.FirstOrDefaultAsync()).MenuId,
        //        ScheduleId = (await _context.Schedules.FirstOrDefaultAsync()).ScheduleId
        //    };

        //    var locToManage2 = new Location
        //    {
        //        AbreviatedCountry = "FR",
        //        AbreviatedState = "B2",
        //        Street = "95 Lyons Plaza",
        //        City = "Épinal",
        //        OpenSince = new DateTime(2018, 7, 24).Date,
        //        MenuId = (await _context.Menus.FirstOrDefaultAsync()).MenuId,
        //        ScheduleId = (await _context.Schedules.FirstOrDefaultAsync()).ScheduleId
        //    };

        //    await _context.Locations.AddRangeAsync(new Location[] { locToManage1, locToManage2 });
        //    await _context.SaveChangesAsync();

        //    var managements = new Management[]
        //    {
        //        new Management { LocationId = locToManage1.LocationId },
        //        new Management { LocationId = locToManage2.LocationId },
        //    };

        //    var employee = new Employee
        //    {
        //        FirstName = "Daniel",
        //        LastName = "Crawford",
        //        LocationId = locToManage1.LocationId,
        //        WeeklyHours = 36,
        //        Managements = managements,
        //        PositionId = (await _context.Positions.FirstOrDefaultAsync()).PositionId,
        //        StartedOn = DateTime.Today.AddDays(-200),
        //    };

        //    // Act
        //    var res = (await _controller.CreateEmployee(employee)).Result;

        //    // Assert
        //    res.Should().BeOfType<BadRequestResult>();
        //}

        //[TestMethod]
        //public async Task CreateEmployee_NoLocNoLocId_ShouldReturnBadRequest()
        //{
        //    // Arrange
        //    var employee = new Employee
        //    {
        //        FirstName = "Daniel",
        //        LastName = "Crawford",
        //        WeeklyHours = 36,
        //        PositionId = (await _context.Positions.FirstOrDefaultAsync()).PositionId,
        //        StartedOn = DateTime.Today.AddDays(-200)
        //    };

        //    // Act
        //    var res = (await _controller.CreateEmployee(employee)).Result;

        //    // Assert
        //    res.Should().BeOfType<BadRequestResult>();
        //}

        //[TestMethod]
        //public async Task CreateEmployee_NoPosNoPosId_ShouldReturnBadRequest()
        //{
        //    // Arrange
        //    var employee = new Employee
        //    {
        //        FirstName = "Daniel",
        //        LastName = "Crawford",
        //        WeeklyHours = 36,
        //        LocationId = (await _context.Locations.FirstOrDefaultAsync()).LocationId,
        //        StartedOn = DateTime.Today.AddDays(-200)
        //    };

        //    // Act
        //    var res = (await _controller.CreateEmployee(employee)).Result;

        //    // Assert
        //    res.Should().BeOfType<BadRequestResult>();
        //}

        //[TestMethod]
        //public async Task CreateEmployee_MagWithManager_ShouldReturnBadRequest()
        //{
        //    // Arrange
        //    var locToManage = new Location();
        //    await _context.Locations.AddAsync(locToManage);
        //    await _context.SaveChangesAsync();

        //    var managements = new Management[]
        //    {
        //        new Management
        //        {
        //            LocationId = locToManage.LocationId,
        //            Manager = new Employee()
        //        }
        //    };

        //    var employee = new Employee
        //    {
        //        FirstName = "Daniel",
        //        LastName = "Crawford",
        //        WeeklyHours = 36,
        //        LocationId = locToManage.LocationId,
        //        PositionId = await _context.Positions.Select(p => p.PositionId).FirstOrDefaultAsync(),
        //        StartedOn = DateTime.Today.AddDays(-200),
        //        Managements = managements
        //    };

        //    // Act
        //    var res = (await _controller.CreateEmployee(employee)).Result;

        //    // Assert
        //    res.Should().BeOfType<BadRequestResult>();
        //}

        //[TestMethod]
        //public async Task CreateEmployee_MagWithLocation_ShouldReturnBadRequest()
        //{

        //    // Arrange
        //    var locToManage = new Location();
        //    await _context.Locations.AddAsync(locToManage);
        //    await _context.SaveChangesAsync();

        //    var managements = new Management[]
        //    {
        //        new Management
        //        {
        //            Location = new Location()
        //        }
        //    };

        //    var employee = new Employee
        //    {
        //        FirstName = "Daniel",
        //        LastName = "Crawford",
        //        WeeklyHours = 36,
        //        LocationId = locToManage.LocationId,
        //        PositionId = await _context.Positions.Select(p => p.PositionId).FirstOrDefaultAsync(),
        //        StartedOn = DateTime.Today.AddDays(-200),
        //        Managements = managements
        //    };

        //    // Act
        //    var res = (await _controller.CreateEmployee(employee)).Result;

        //    // Assert
        //    res.Should().BeOfType<BadRequestResult>();
        //}

        //[TestMethod]
        //public async Task CreateEmployee_PosWithEmployees_ShouldReturnBadRequest()
        //{
        //    // Arrange
        //    var position = new Position
        //    {
        //        Title = "Plumber",
        //        Wage = 24.5m,
        //        Employees = new Employee[] { new Employee() }
        //    };

        //    var employee = new Employee
        //    {
        //        FirstName = "Daniel",
        //        LastName = "Crawford",
        //        WeeklyHours = 36,
        //        LocationId = await _context.Locations.Select(p => p.LocationId).FirstOrDefaultAsync(),
        //        Position = position,
        //        StartedOn = DateTime.Today.AddDays(-200)
        //    };

        //    // Act
        //    var res = (await _controller.CreateEmployee(employee)).Result;

        //    // Assert
        //    res.Should().BeOfType<BadRequestResult>();
        //}

        //[TestMethod]
        //public async Task CreateEmployee_LocWithEmployees_ShouldReturnBadRequest()
        //{
        //    // Arrange
        //    var location = new Location
        //    {
        //        Employees = new Employee[] { new Employee() }
        //    };

        //    var employee = new Employee
        //    {
        //        FirstName = "Daniel",
        //        LastName = "Crawford",
        //        WeeklyHours = 36,
        //        Location = location,
        //        PositionId = await _context.Positions.Select(p => p.PositionId).LastOrDefaultAsync(),
        //        StartedOn = DateTime.Today.AddDays(-200)
        //    };

        //    // Act
        //    var res = (await _controller.CreateEmployee(employee)).Result;

        //    // Assert
        //    res.Should().BeOfType<BadRequestResult>();
        //}

        //[TestMethod]
        //public async Task CreateEmployee_LocWithManagements_ShouldReturnBadRequest()
        //{
        //    // Arrange
        //    var location = new Location
        //    {
        //        Managements = new Management[] { new Management() }
        //    };

        //    var employee = new Employee
        //    {
        //        FirstName = "Daniel",
        //        LastName = "Crawford",
        //        WeeklyHours = 36,
        //        Location = location,
        //        PositionId = await _context.Positions.Select(p => p.PositionId).LastOrDefaultAsync(),
        //        StartedOn = DateTime.Today.AddDays(-200)
        //    };

        //    // Act
        //    var res = (await _controller.CreateEmployee(employee)).Result;

        //    // Assert
        //    res.Should().BeOfType<BadRequestResult>();
        //}

        //[TestMethod]
        //public async Task CreateEmployee_LocWithSupplyLinks_ShouldReturnBadRequest()
        //{
        //    // Arrange
        //    var location = new Location
        //    {
        //        SupplyLinks = new SupplyLink[] { new SupplyLink() }
        //    };

        //    var employee = new Employee
        //    {
        //        FirstName = "Daniel",
        //        LastName = "Crawford",
        //        WeeklyHours = 36,
        //        Location = location,
        //        PositionId = await _context.Positions.Select(p => p.PositionId).LastOrDefaultAsync(),
        //        StartedOn = DateTime.Today.AddDays(-200)
        //    };

        //    // Act
        //    var res = (await _controller.CreateEmployee(employee)).Result;

        //    // Assert
        //    res.Should().BeOfType<BadRequestResult>();
        //}

        //[TestMethod]
        //public async Task CreateEmployee_LocWithMenu_ShouldReturnBadRequest()
        //{
        //    // Arrange
        //    var location = new Location
        //    {
        //        Menu = new Menu()
        //    };

        //    var employee = new Employee
        //    {
        //        FirstName = "Daniel",
        //        LastName = "Crawford",
        //        WeeklyHours = 36,
        //        Location = location,
        //        PositionId = await _context.Positions.Select(p => p.PositionId).LastOrDefaultAsync(),
        //        StartedOn = DateTime.Today.AddDays(-200)
        //    };

        //    // Act
        //    var res = (await _controller.CreateEmployee(employee)).Result;

        //    // Assert
        //    res.Should().BeOfType<BadRequestResult>();
        //}

        //[TestMethod]
        //public async Task CreateEmployee_LocWithSchedule_ShouldReturnBadRequest()
        //{
        //    // Arrange
        //    var location = new Location
        //    {
        //        Schedule = new Schedule()
        //    };

        //    var employee = new Employee
        //    {
        //        FirstName = "Daniel",
        //        LastName = "Crawford",
        //        WeeklyHours = 36,
        //        Location = location,
        //        PositionId = await _context.Positions.Select(p => p.PositionId).LastOrDefaultAsync(),
        //        StartedOn = DateTime.Today.AddDays(-200)
        //    };

        //    // Act
        //    var res = (await _controller.CreateEmployee(employee)).Result;

        //    // Assert
        //    res.Should().BeOfType<BadRequestResult>();
        //}

        //[TestMethod]
        //public async Task CreateEmployee_LocWithId_ShouldReturnBadRequest()
        //{
        //    // Arrange
        //    var location = new Location
        //    {
        //        LocationId = 1
        //    };

        //    var employee = new Employee
        //    {
        //        FirstName = "Daniel",
        //        LastName = "Crawford",
        //        WeeklyHours = 36,
        //        Location = location,
        //        PositionId = await _context.Positions.Select(p => p.PositionId).LastOrDefaultAsync(),
        //        StartedOn = DateTime.Today.AddDays(-200)
        //    };

        //    // Act
        //    var res = (await _controller.CreateEmployee(employee)).Result;

        //    // Assert
        //    res.Should().BeOfType<BadRequestResult>();
        //}

        //[TestMethod]
        //public async Task CreateEmployee_PosWithId_ShouldReturnBadRequest()
        //{
        //    // Arrange
        //    var position = new Position
        //    {
        //        PositionId = 2
        //    };

        //    var employee = new Employee
        //    {
        //        FirstName = "Daniel",
        //        LastName = "Crawford",
        //        WeeklyHours = 36,
        //        LocationId = await _context.Locations.Select(p => p.LocationId).LastOrDefaultAsync(),
        //        Position = position,
        //        StartedOn = DateTime.Today.AddDays(-200)
        //    };

        //    // Act
        //    var res = (await _controller.CreateEmployee(employee)).Result;

        //    // Assert
        //    res.Should().BeOfType<BadRequestResult>();
        //}

        //[TestMethod]
        //public async Task CreateEmployee_HasManagementButWrongPosId_ShouldReturnBadRequest()
        //{
        //    // Arrange
        //    var location = new Location();
        //    var managements = new Management[] { new Management { LocationId = location.LocationId } };

        //    var employee = new Employee
        //    {
        //        FirstName = "Daniel",
        //        LastName = "Crawford",
        //        WeeklyHours = 36,
        //        LocationId = location.LocationId,
        //        PositionId = await _context.Positions.Where(p => p.PositionId != 1).Select(p => p.PositionId).FirstOrDefaultAsync(),
        //        StartedOn = DateTime.Today.AddDays(-200),
        //        Managements = managements
        //    };

        //    // Act
        //    var res = (await _controller.CreateEmployee(employee)).Result;

        //    // Assert
        //    res.Should().BeOfType<BadRequestResult>();
        //}

        ////Ignored because Managements could be added after the employee.
        //[TestMethod]
        //[Ignore]
        //public async Task CreateEmployee_HasNoManagementsButWrongPosId_ShouldReturnBadRequest()
        //{
        //    // Arrange
        //    var employee = new Employee
        //    {
        //        FirstName = "Daniel",
        //        LastName = "Crawford",
        //        WeeklyHours = 36,
        //        LocationId = await _context.Locations.Select(l => l.LocationId).FirstOrDefaultAsync(),
        //        PositionId = 1,
        //        StartedOn = DateTime.Today.AddDays(-200)
        //    };

        //    // Act
        //    var res = (await _controller.CreateEmployee(employee)).Result;

        //    // Assert
        //    res.Should().BeOfType<BadRequestResult>();
        //}

        #endregion

        #endregion

        #region Updates

        [TestMethod]
        public async Task UpdateEmployee_HappyFlow_ShouldUpdateAndReturnEmployee()
        {
            // Arrange
            var oldEmp = await _context.Employees.FindAsync(GetFirstEmployeeId);
            var oldEmpCopy = new Employee
            {
                EmployeeId = oldEmp.EmployeeId,
                FirstName = oldEmp.FirstName,
                LastName = oldEmp.LastName,
                LocationId = oldEmp.LocationId,
                PositionId = oldEmp.PositionId,
                Location = oldEmp.Location,
                Position = oldEmp.Position,
                Managements = oldEmp.Managements,
                StartedOn = oldEmp.StartedOn,
                WeeklyHours = oldEmp.WeeklyHours
            };
            var employee = new Employee
            {
                FirstName = "Gabe",
                LastName = "Newton",
                StartedOn = oldEmp.StartedOn.AddDays(-10),
                LocationId = await _context.Locations.Select(l => l.LocationId).LastOrDefaultAsync(),
                PositionId = await _context.Positions.Select(p => p.PositionId).LastOrDefaultAsync(),
                WeeklyHours = oldEmp.WeeklyHours + 2
            };

            // Act
            await _controller.UpdateEmployee(GetFirstEmployeeId, employee);
            var result = (await _controller.GetEmployeeBasic(oldEmp.EmployeeId)).Value;

            employee.EmployeeId = oldEmpCopy.EmployeeId;
            employee.Location = await _context.Locations.FindAsync(employee.LocationId);
            employee.Managements = await _context.Managements.Where(m => m.ManagerId == employee.EmployeeId).ToArrayAsync();
            employee.Position = await _context.Positions.FindAsync(employee.PositionId);

            // Assert
            result.Should().BeEquivalentTo(employee);
            result.Should().NotBeEquivalentTo(oldEmpCopy);
        }

        [TestMethod]
        public async Task UpdateEmployee_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var oldEmp = await _context.Employees.FindAsync(GetFirstEmployeeId);
            var oldEmpCopy = new Employee
            {
                EmployeeId = oldEmp.EmployeeId,
                FirstName = oldEmp.FirstName,
                LastName = oldEmp.LastName,
                LocationId = oldEmp.LocationId,
                PositionId = oldEmp.PositionId,
                Location = oldEmp.Location,
                Position = oldEmp.Position,
                Managements = oldEmp.Managements,
                StartedOn = oldEmp.StartedOn,
                WeeklyHours = oldEmp.WeeklyHours
            };

            var emp = new Employee
            {
                FirstName = "John",
                LastName = "Doe",
                StartedOn= oldEmp.StartedOn.AddDays(-10),
                LocationId = await _context.Locations.Select(l => l.LocationId).LastOrDefaultAsync(),
                PositionId = await _context.Positions.Select(p => p.PositionId).LastOrDefaultAsync(),
                WeeklyHours = oldEmp.WeeklyHours+2
            };

            // Act
            var result = await _controller.UpdateEmployee(GenBadEmployeeId, emp);
            var updatedDish = await _context.Employees.FindAsync(GetFirstEmployeeId);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
            updatedDish.Should().BeEquivalentTo(oldEmpCopy);
        }

        [TestMethod]
        public async Task UpdateEmployee_TriesToSetId_ShouldReturnBadRequest()
        {
            // Arrange
            var oldEmp = await _context.Employees.FindAsync(GetFirstEmployeeId);
            var oldEmpCopy = new Employee
            {
                EmployeeId = oldEmp.EmployeeId,
                FirstName = oldEmp.FirstName,
                LastName = oldEmp.LastName,
                LocationId = oldEmp.LocationId,
                PositionId = oldEmp.PositionId,
                Location = oldEmp.Location,
                Position = oldEmp.Position,
                Managements = oldEmp.Managements,
                StartedOn = oldEmp.StartedOn,
                WeeklyHours = oldEmp.WeeklyHours
            };

            var emp = new Employee
            {
                EmployeeId = GenBadEmployeeId,
                FirstName = "John",
                LastName = "Doe",
                StartedOn = oldEmp.StartedOn.AddDays(-10),
                LocationId = await _context.Locations.Select(l => l.LocationId).LastOrDefaultAsync(),
                PositionId = await _context.Positions.Select(p => p.PositionId).LastOrDefaultAsync(),
                WeeklyHours = oldEmp.WeeklyHours + 2
            };

            // Act
            var result = await _controller.UpdateEmployee(oldEmp.EmployeeId, emp);
            var updatedEmp = await _context.Employees.FindAsync(GetFirstEmployeeId);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedEmp.Should().BeEquivalentTo(oldEmpCopy);
        }

        [DataTestMethod]
        #region Rows
        [DataRow("Stu", " ")]
        [DataRow(" ", "Smithson")]
        [DataRow("Stu", "012345678911234567892123456789312345678941234567890")]
        [DataRow("012345678911234567892123456789312345678941234567890", "Smithson")]
        #endregion
        public async Task UpdateEmployee_WrongNameLengths_ShouldReturnBadRequest(string firstName, string lastName)
        {
            // Arrange
            var oldEmp = await _context.Employees.FindAsync(GetFirstEmployeeId);
            var oldEmpCopy = new Employee
            {
                EmployeeId = oldEmp.EmployeeId,
                FirstName = oldEmp.FirstName,
                LastName = oldEmp.LastName,
                LocationId = oldEmp.LocationId,
                PositionId = oldEmp.PositionId,
                Location = oldEmp.Location,
                Position = oldEmp.Position,
                Managements = oldEmp.Managements,
                StartedOn = oldEmp.StartedOn,
                WeeklyHours = oldEmp.WeeklyHours
            };

            var emp = new Employee
            {
                EmployeeId = GenBadEmployeeId,
                FirstName = firstName,
                LastName = lastName,
                StartedOn = oldEmp.StartedOn.AddDays(-10),
                LocationId = await _context.Locations.Select(l => l.LocationId).LastOrDefaultAsync(),
                PositionId = await _context.Positions.Select(p => p.PositionId).LastOrDefaultAsync(),
                WeeklyHours = oldEmp.WeeklyHours + 2
            };

            // Act
            var result = await _controller.UpdateEmployee(oldEmp.EmployeeId, emp);
            var updatedEmp = await _context.Employees.FindAsync(GetFirstEmployeeId);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedEmp.Should().BeEquivalentTo(oldEmpCopy);
        }

        [DataTestMethod]
        [DataRow(-1)]
        [DataRow(168)]
        public async Task UpdateEmployee_HoursOutOfRange_ShouldReturnBadRequest(int weeklyHours)
        {
            // Arrange
            var oldEmp = await _context.Employees.FindAsync(GetFirstEmployeeId);
            var oldEmpCopy = new Employee
            {
                EmployeeId = oldEmp.EmployeeId,
                FirstName = oldEmp.FirstName,
                LastName = oldEmp.LastName,
                LocationId = oldEmp.LocationId,
                PositionId = oldEmp.PositionId,
                Location = oldEmp.Location,
                Position = oldEmp.Position,
                Managements = oldEmp.Managements,
                StartedOn = oldEmp.StartedOn,
                WeeklyHours = oldEmp.WeeklyHours
            };

            var emp = new Employee
            {
                FirstName = "John",
                LastName = "Doe",
                StartedOn = oldEmp.StartedOn.AddDays(-10),
                LocationId = await _context.Locations.Select(l => l.LocationId).LastOrDefaultAsync(),
                PositionId = await _context.Positions.Select(p => p.PositionId).LastOrDefaultAsync(),
                WeeklyHours = weeklyHours
            };

            // Act
            var result = await _controller.UpdateEmployee(oldEmp.EmployeeId, emp);
            var updatedEmp = await _context.Employees.FindAsync(GetFirstEmployeeId);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedEmp.Should().BeEquivalentTo(oldEmpCopy);
        }

        [TestMethod]
        public async Task UpdateEmployee_InexistentLocationId_ShouldReturnBadRequest()
        {
            // Arrange
            var oldEmp = await _context.Employees.FindAsync(GetFirstEmployeeId);
            var oldEmpCopy = new Employee
            {
                EmployeeId = oldEmp.EmployeeId,
                FirstName = oldEmp.FirstName,
                LastName = oldEmp.LastName,
                LocationId = oldEmp.LocationId,
                PositionId = oldEmp.PositionId,
                Location = oldEmp.Location,
                Position = oldEmp.Position,
                Managements = oldEmp.Managements,
                StartedOn = oldEmp.StartedOn,
                WeeklyHours = oldEmp.WeeklyHours
            };

            var emp = new Employee
            {
                FirstName = "John",
                LastName = "Doe",
                StartedOn = oldEmp.StartedOn.AddDays(-10),
                LocationId = await _context.Locations.Select(l => l.LocationId).LastOrDefaultAsync()+1,
                PositionId = await _context.Positions.Select(p => p.PositionId).LastOrDefaultAsync(),
                WeeklyHours = oldEmp.WeeklyHours + 2
            };

            // Act
            var result = await _controller.UpdateEmployee(oldEmp.EmployeeId, emp);
            var updatedEmp = await _context.Employees.FindAsync(GetFirstEmployeeId);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedEmp.Should().BeEquivalentTo(oldEmpCopy);
        }

        [TestMethod]
        public async Task UpdateEmployee_InexistentPositionId_ShouldReturnBadRequest()
        {
            // Arrange
            var oldEmp = await _context.Employees.FindAsync(GetFirstEmployeeId);
            var oldEmpCopy = new Employee
            {
                EmployeeId = oldEmp.EmployeeId,
                FirstName = oldEmp.FirstName,
                LastName = oldEmp.LastName,
                LocationId = oldEmp.LocationId,
                PositionId = oldEmp.PositionId,
                Location = oldEmp.Location,
                Position = oldEmp.Position,
                Managements = oldEmp.Managements,
                StartedOn = oldEmp.StartedOn,
                WeeklyHours = oldEmp.WeeklyHours
            };

            var emp = new Employee
            {
                FirstName = "John",
                LastName = "Doe",
                StartedOn = oldEmp.StartedOn.AddDays(-10),
                LocationId = await _context.Locations.Select(l => l.LocationId).LastOrDefaultAsync(),
                PositionId = await _context.Positions.Select(p => p.PositionId).LastOrDefaultAsync()+1,
                WeeklyHours = oldEmp.WeeklyHours + 2
            };

            // Act
            var result = await _controller.UpdateEmployee(oldEmp.EmployeeId, emp);
            var updatedEmp = await _context.Employees.FindAsync(GetFirstEmployeeId);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedEmp.Should().BeEquivalentTo(oldEmpCopy);
        }

        [TestMethod]
        public async Task UpdateEmployee_HasLocation_ShouldReturnBadRequest()
        {
            // Arrange
            var oldEmp = await _context.Employees.FindAsync(GetFirstEmployeeId);
            var oldEmpCopy = new Employee
            {
                EmployeeId = oldEmp.EmployeeId,
                FirstName = oldEmp.FirstName,
                LastName = oldEmp.LastName,
                LocationId = oldEmp.LocationId,
                PositionId = oldEmp.PositionId,
                Location = oldEmp.Location,
                Position = oldEmp.Position,
                Managements = oldEmp.Managements,
                StartedOn = oldEmp.StartedOn,
                WeeklyHours = oldEmp.WeeklyHours
            };

            var emp = new Employee
            {
                FirstName = "John",
                LastName = "Doe",
                StartedOn = oldEmp.StartedOn.AddDays(-10),
                LocationId = await _context.Locations.Select(l => l.LocationId).LastOrDefaultAsync(),
                PositionId = await _context.Positions.Select(p => p.PositionId).LastOrDefaultAsync(),
                WeeklyHours = oldEmp.WeeklyHours + 2,
                Location = new Location()
            };

            // Act
            var result = await _controller.UpdateEmployee(oldEmp.EmployeeId, emp);
            var updatedEmp = await _context.Employees.FindAsync(GetFirstEmployeeId);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedEmp.Should().BeEquivalentTo(oldEmpCopy);
        }

        [TestMethod]
        public async Task UpdateEmployee_HasPosition_ShouldReturnBadRequest()
        {
            // Arrange
            var oldEmp = await _context.Employees.FindAsync(GetFirstEmployeeId);
            var oldEmpCopy = new Employee
            {
                EmployeeId = oldEmp.EmployeeId,
                FirstName = oldEmp.FirstName,
                LastName = oldEmp.LastName,
                LocationId = oldEmp.LocationId,
                PositionId = oldEmp.PositionId,
                Location = oldEmp.Location,
                Position = oldEmp.Position,
                Managements = oldEmp.Managements,
                StartedOn = oldEmp.StartedOn,
                WeeklyHours = oldEmp.WeeklyHours
            };

            var emp = new Employee
            {
                FirstName = "John",
                LastName = "Doe",
                StartedOn = oldEmp.StartedOn.AddDays(-10),
                LocationId = await _context.Locations.Select(l => l.LocationId).LastOrDefaultAsync(),
                PositionId = await _context.Positions.Select(p => p.PositionId).LastOrDefaultAsync(),
                WeeklyHours = oldEmp.WeeklyHours + 2,
                Position = new Position()
            };

            // Act
            var result = await _controller.UpdateEmployee(oldEmp.EmployeeId, emp);
            var updatedEmp = await _context.Employees.FindAsync(GetFirstEmployeeId);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedEmp.Should().BeEquivalentTo(oldEmpCopy);
        }

        [TestMethod]
        public async Task UpdateEmployee_ManagementsNull_ShouldReturnBadRequest()
        {
            // Arrange
            var oldEmp = await _context.Employees.FindAsync(GetFirstEmployeeId);
            var oldEmpCopy = new Employee
            {
                EmployeeId = oldEmp.EmployeeId,
                FirstName = oldEmp.FirstName,
                LastName = oldEmp.LastName,
                LocationId = oldEmp.LocationId,
                PositionId = oldEmp.PositionId,
                Location = oldEmp.Location,
                Position = oldEmp.Position,
                Managements = oldEmp.Managements,
                StartedOn = oldEmp.StartedOn,
                WeeklyHours = oldEmp.WeeklyHours
            };

            var emp = new Employee
            {
                FirstName = "John",
                LastName = "Doe",
                StartedOn = oldEmp.StartedOn.AddDays(-10),
                LocationId = await _context.Locations.Select(l => l.LocationId).LastOrDefaultAsync(),
                PositionId = await _context.Positions.Select(p => p.PositionId).LastOrDefaultAsync(),
                WeeklyHours = oldEmp.WeeklyHours + 2,
                Managements = null
            };

            // Act
            var result = await _controller.UpdateEmployee(oldEmp.EmployeeId, emp);
            var updatedEmp = await _context.Employees.FindAsync(GetFirstEmployeeId);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedEmp.Should().BeEquivalentTo(oldEmpCopy);
        }

        [TestMethod]
        public async Task UpdateEmployee_ManagementsNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var oldEmp = await _context.Employees.FindAsync(GetFirstEmployeeId);
            var oldEmpCopy = new Employee
            {
                EmployeeId = oldEmp.EmployeeId,
                FirstName = oldEmp.FirstName,
                LastName = oldEmp.LastName,
                LocationId = oldEmp.LocationId,
                PositionId = oldEmp.PositionId,
                Location = oldEmp.Location,
                Position = oldEmp.Position,
                Managements = oldEmp.Managements,
                StartedOn = oldEmp.StartedOn,
                WeeklyHours = oldEmp.WeeklyHours
            };

            var emp = new Employee
            {
                FirstName = "John",
                LastName = "Doe",
                StartedOn = oldEmp.StartedOn.AddDays(-10),
                LocationId = await _context.Locations.Select(l => l.LocationId).LastOrDefaultAsync(),
                PositionId = await _context.Positions.Select(p => p.PositionId).LastOrDefaultAsync(),
                WeeklyHours = oldEmp.WeeklyHours + 2,
                Managements = new Management[] {new Management()}
            };

            // Act
            var result = await _controller.UpdateEmployee(oldEmp.EmployeeId, emp);
            var updatedEmp = await _context.Employees.FindAsync(GetFirstEmployeeId);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedEmp.Should().BeEquivalentTo(oldEmpCopy);
        }

        [TestMethod]
        public async Task UpdateEmployee_TriesToManageManagedLocation_ShouldReturBadRequest()
        {
            // Arrange
            var location = new Location
            {
                AbreviatedCountry = "ES",
                AbreviatedState = "N/A",
                City = "Madrid",
                Street = "14 Parlor Drive",
                MenuId = 1,
                OpenSince = DateTime.Today.AddDays(-200),
                ScheduleId = 1,
                Managements = new Management[] { new Management() }
            };
            await _context.Locations.AddAsync(location);
            await _context.SaveChangesAsync();


            var oldEmp = await _context.Employees.FindAsync(GetFirstEmployeeId);
            var oldEmpCopy = new Employee
            {
                EmployeeId = oldEmp.EmployeeId,
                FirstName = oldEmp.FirstName,
                LastName = oldEmp.LastName,
                LocationId = oldEmp.LocationId,
                PositionId = oldEmp.PositionId,
                Location = oldEmp.Location,
                Position = oldEmp.Position,
                Managements = oldEmp.Managements,
                StartedOn = oldEmp.StartedOn,
                WeeklyHours = oldEmp.WeeklyHours
            };

            var emp = new Employee
            {
                FirstName = "John",
                LastName = "Doe",
                StartedOn = oldEmp.StartedOn.AddDays(-10),
                LocationId = location.LocationId,
                PositionId = 1,
                WeeklyHours = oldEmp.WeeklyHours + 2
            };

            // Act
            var result = await _controller.UpdateEmployee(oldEmp.EmployeeId, emp);
            var updatedEmp = await _context.Employees.FindAsync(GetFirstEmployeeId);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedEmp.Should().BeEquivalentTo(oldEmpCopy);
        }

        #endregion

        [TestMethod]
        public void EmployeeExists_ItDoes_ShoudReturnTrue()
        {
            //Arrange
            var methodInfo = typeof(EmployeesController).GetMethod("EmployeeExists", BindingFlags.NonPublic | BindingFlags.Instance);
            var parameters = new object[] { GetFirstEmployeeId };

            // Act
            var result = ((methodInfo.Invoke(_controller, parameters)) as Task<bool>).Result;

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public void EmployeeExists_ItDoesNot_ShoudReturnFalse()
        {
            //Arrange
            var methodInfo = typeof(EmployeesController).GetMethod("EmployeeExists", BindingFlags.NonPublic | BindingFlags.Instance);
            var parameters = new object[] { GenBadEmployeeId };

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
            _controller = new EmployeesController(_context, _logger);
        }

        /// Helpers
        int GetFirstEmployeeId => _context.Employees
            .Select(d => d.EmployeeId)
            .OrderBy(id => id)
            .FirstOrDefault();
        int GenBadEmployeeId => _context.Employees
            .Select(d => d.EmployeeId)
            .ToArray()
            .OrderBy(id => id)
            .LastOrDefault() + 1;

        private static void Setup()
        {
            _context = InMemoryHelpers.GetContext();
            _controller = new EmployeesController(_context, _logger);
        }
    }
}
