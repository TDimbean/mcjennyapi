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
    public class EmployeesIntegrationTests
    {
        private static HttpClient _client;
        private static FoodChainsDbContext _context;
        private static WebApplicationFactory<Startup> _appFactory;

        [ClassInitialize]
        public static void InitClass(TestContext testContext)
        {
            _appFactory = new WebApplicationFactory<Startup>();
            _appFactory.ClientOptions.BaseAddress = new Uri("http://localhost/api/employees");
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
            _appFactory.ClientOptions.BaseAddress = new Uri("http://localhost/api/employees");
            _context = new FoodChainsDbContext();
            _client = _appFactory.CreateClient();

            //var emp1 = _context.Employees.Find(1);
            //var emp2 = _context.Employees.Find(2);

            //if (emp1.FirstName == "Missing" || emp2.FirstName == "Missing")
            //{
            //    var fake = false;
            //    if(!fake)
            //    return;
            //}

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

        #region Gets

        #region Basic

        [TestMethod]
        public async Task GetEmployees_HappyFlow_ShouldReturnEmployees()
        {
            // Arrange
            var expectedObj = await _context.Employees
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
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(string.Empty);
            var content = await result.Content.ReadAsStringAsync();
            //var content = JsonConvert.DeserializeObject(contentString);

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        [Ignore]
        public async Task GetEmployeeCheating_HappyFlow_ShouldReturnEmployee()
        {
            // Arrange
            var employee = await _context.Employees.FindAsync(GetFirstEmployeeId);

            var position = await _context.Positions.SingleOrDefaultAsync(p => p.PositionId == employee.PositionId);

            var expectedObj = (object)new
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
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + GetFirstEmployeeId);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetEmployee_HappyFlow_ShouldReturnEmployee()
        {
            // Arrange
            var empId = (await _context.Employees.FirstOrDefaultAsync()).EmployeeId;
            var employee = await _context.Employees.FindAsync(empId);

            var position = await _context.Positions.FindAsync(employee.PositionId);

            var expected = new
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
            var definition = new
            {
                EmployeeId = 0,
                FirstName = string.Empty,
                LastName = string.Empty,
                Location = string.Empty,
                Position = string.Empty,
                WeeklyHours = 0,
                StartedOn = string.Empty,
                Salary = string.Empty
            };

            // Act
            var result = await _client.GetAsync(_client.BaseAddress + "/" + empId);
            var content = await result.Content.ReadAsStringAsync();
            var emp = JsonConvert.DeserializeAnonymousType(content, definition);

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            emp.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetEmployee_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var empId = GenBadEmployeeId;

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + empId);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task GetEmployeeBasic_HappyFlow_ShouldReturnEmployee()
        {
            // Arrange
            var empId = GetFirstEmployeeId;

            var expected = await _context.Employees.FindAsync(empId);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/" + empId + "/basic");
            var content = await result.Content.ReadAsStringAsync();
            var retrieved = JsonConvert.DeserializeObject(content, typeof(Employee));

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            retrieved.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetEmployeeBasic_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var empId = GenBadEmployeeId;

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/" + empId + "/basic");

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task GetSalary_HappyFlow_ShouldReturnNotFound()
        {
            // Arrange
            var employee = await _context.Employees.FirstOrDefaultAsync();

            var wage = (await _context.Positions.FindAsync(employee.PositionId)).Wage;

            var expectedObj = new
            {
                Weekly = string.Format("{0:C}", wage * employee.WeeklyHours),
                Monthly = string.Format("{0:C}", wage * employee.WeeklyHours * 4),
                Yearly = string.Format("{0:C}", wage * employee.WeeklyHours * 52)
            };
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + 
                GetFirstEmployeeId + "/salary");
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSalary_InexistentId_ShouldReturnNotFound()
        {
            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/" +
                GenBadEmployeeId + "/salary");

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        #endregion

        #region Advanced

        [TestMethod]
        public async Task GetEmployeesFiltered_FirstNameMatch_ShouldReturnFiltered()
        {
            // Arrange
            var filter = await _context.Employees
                .Select(e => e.FirstName)
                .FirstOrDefaultAsync();

            var expectedObj = (object)await _context.Employees
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
                     .Contains(filter.ToUpper())/*||
                     e.LastName.ToUpper()
                     .Contains(filter.ToUpper())*/)
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
        public async Task GetEmployeesFiltered_LastNameMatch_ShouldReturnFiltered()
        {
            // Arrange
            var filter = await _context.Employees
                .Select(e => e.LastName)
                .Distinct()
                .FirstOrDefaultAsync();

            var expectedObj = await _context.Employees
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
                 .Where(e => 
                        e.LastName.ToUpper()
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
        public async Task GetEmployeesFiltered_PositionMatch_ShouldReturnFiltered()
        {
            // Arrange
            var filter = await _context.Employees
                .Include(e=>e.Position)
                .Select(e => e.Position.Title)
                .FirstOrDefaultAsync();

            var expectedObj = await _context.Employees
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
                 .Where(e => e.Title.ToUpper()
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
        public async Task GetEmployeesPaged_HapyFlow_ShouldReturnPaged()
        {
            // Arrange
            var pgSz = 2;
            var pgInd = 1;

            var expectedObj = await _context.Employees
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

            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/query:pgSz=" + pgSz + "&pgInd=" + pgInd);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetEmployeesPaged_EndPageIncomplete_ShouldReturnPaged()
        {
            // Arrange
            var employeeCount = await _context.Employees.CountAsync();
            var pgSz = 1;

            while (true)
            {
                if (employeeCount % pgSz == 0) pgSz++;
                else break;
            }

            var pgInd = employeeCount / (pgSz - 1);

            var expectedObj = await _context.Employees
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
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/query:pgSz=" + pgSz + "&pgInd=" + pgInd);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetEmployeesPaged_SizeOver_ShouldReturnPaged()
        {
            // Arrange
            var employeeCount = await _context.Employees.CountAsync();
            var pgSz = employeeCount + 1;
            var pgInd = 1;

            var expectedObj = await _context.Employees
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
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/query:pgSz=" + pgSz + "&pgInd=" + pgInd);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetEmployeesPaged_IndexOver_ShouldReturnPaged()
        {
            // Arrange
            var employeeCount = await _context.Employees.CountAsync();
            var pgInd = employeeCount + 1;
            var pgSz = 1;

            var expectedObj = await _context.Employees
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
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/query:pgSz=" + pgSz + "&pgInd=" + pgInd);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [DataTestMethod]
        [DataRow(-2, 1)]
        [DataRow(2, -1)]
        public async Task GetEmployeesPaged_HapyFlow_ShouldReturnPaged(int pgSz, int pgInd)
        {
            // Arrange
            var absPgSz = Math.Abs(pgSz);
            var absPgInd = Math.Abs(pgInd);

            var expectedObj = await _context.Employees
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

            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/query:pgSz=" + pgSz + "&pgInd=" + pgInd);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetEmployeesSorted_ByFirstName_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "firstname";

            var expectedObj = (object)await _context.Employees
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
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/query:sortby=" + sortBy);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetEmployeesSorted_ByLastName_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "lastname";

            var expectedObj = (object)await _context.Employees
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
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/query:sortby=" + sortBy);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetEmployeesSorted_ByTitle_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "title";

            var expectedObj = (object)await _context.Employees
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
                            .OrderBy(e => e.Title)
                            .ToArrayAsync();
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/query:sortby=" + sortBy);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetEmployeesSorted_ByLocation_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "location";

            var expectedObj = (object)await _context.Employees
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
                            .OrderBy(e => e.LocationId)
                            .ToArrayAsync();
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/query:sortby=" + sortBy);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetEmployeesSorted_ByGarbleText_ShouldSortByID()
        {
            // Arrange
            var sortBy = "asdsadsac";

            var expectedObj = (object)await _context.Employees
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
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/query:sortby=" + sortBy);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetEmployeesSorted_Descending_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "firstName";

            var expectedObj = (object)await _context.Employees
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
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress + "/query:desc=true&sortby=" + sortBy);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetEmployeesQueried_FilteredAndSorted_ShouldReturnFilteredAndSorted()
        {
            // Arrange
            var sortBy = "title";
            var filter = await GetRecurringName();

            var filtered = await _context.Employees
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

            var expectedObj = filtered
                            .Select(e => new
                            {
                                e.EmployeeId,
                                e.FirstName,
                                e.LastName,
                                e.LocationId,
                                e.Title,
                                e.StartedOn,
                                e.WeeklyHours
                            })
                            .OrderByDescending(e => e.Title)
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
        public async Task GetEmployeesQueried_FilteredAndPaged_ShouldReturnFilteredAndPaged()
        {
            // Arrange
            int pgSz = 2;
            int pgInd = 1;
            var filter = await GetRecurringName();

            var filtered = await _context.Employees
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

            var expectedObj = filtered
                .Select(e => new
                {
                    e.EmployeeId,
                    e.FirstName,
                    e.LastName,
                    e.LocationId,
                    e.Title,
                    e.StartedOn,
                    e.WeeklyHours
                })
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
        public async Task GetEmployeesQueried_SortedAndPaged_ShouldReturnSortedAndPaged()
        {
            // Arrange
            int pgSz = 2;
            int pgInd = 1;
            var sortBy = "firstName";

            var sorted = await _context.Employees
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

            var expectedObj = sorted
                .Select(e => new
                {
                    e.EmployeeId,
                    e.FirstName,
                    e.LastName,
                    e.LocationId,
                    e.Title,
                    e.StartedOn,
                    e.WeeklyHours
                })
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
        public async Task GetEmployeesQueried_FilteredSortedAndPaged_ShouldReturnFilteredSortedAndPaged()
        {
            // Arrange
            int pgSz = 2;
            int pgInd = 1;
            var sortBy = "firstname";
            var filter = await GetRecurringName();

            var filtered = await _context.Employees
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

            var sorted = filtered
                            .Select(e => new
                            {
                                e.EmployeeId,
                                e.FirstName,
                                e.LastName,
                                e.LocationId,
                                e.Title,
                                e.StartedOn,
                                e.WeeklyHours
                            })
                            .OrderByDescending(d => d.FirstName)
                            .ToArray();

            var expectedObj = sorted
                .Select(e => new
                {
                    e.EmployeeId,
                    e.FirstName,
                    e.LastName,
                    e.LocationId,
                    e.Title,
                    e.StartedOn,
                    e.WeeklyHours
                })
                .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArray();
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/query:desc=true&sortby=" + sortBy + "&pgsz=" + pgSz + "&pgind=" + pgInd + "&filter=" + filter);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetEmployeesQueried_Before_ShouldReturnBefore()
        {
            // Arrange
            var date = await _context.Employees
                .Select(e => e.StartedOn)
                .OrderBy(e => e)
                .Skip(1)
                .FirstOrDefaultAsync();

            var expectedObj = (object)await _context.Employees
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
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/query:before=true&started=" + date.ToShortDateString().Replace('/','-'));
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetEmployeesQueried_After_ShouldReturnSince()
        {
            // Arrange
            var date = await _context.Employees
                .Select(e => e.StartedOn)
                .OrderBy(e => e)
                .Skip(1)
                .FirstOrDefaultAsync();

            var expectedObj = (object)await _context.Employees
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
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/query:started=" + date.ToShortDateString().Replace('/','-'));
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetEmployeesQueried_BeforeAndFiltered_ShouldReturnBeforeAndFiltered()
        {
            // Arrange
            var date = await _context.Employees
                .Select(e => e.StartedOn)
                .OrderBy(e => e)
                .Skip(1)
                .FirstOrDefaultAsync();

            var started = await _context.Employees
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

            var filter = started.Select(s => s.Title).FirstOrDefault();

            var expectedObj = started
                .Select(e => new
                {
                    e.EmployeeId,
                    e.FirstName,
                    e.LastName,
                    e.LocationId,
                    e.Title,
                    e.StartedOn,
                    e.WeeklyHours
                })
                 .Where(e => e.FirstName.ToUpper()
                     .Contains(filter.ToUpper()) ||
                        e.LastName.ToUpper()
                    .Contains(filter.ToUpper()) ||
                        e.Title.ToUpper().Contains(filter.ToUpper()))
                 .ToArray();

            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/query:before=true&filter=" + filter + "&started=" + 
                date.ToShortDateString().Replace('/', '-'));
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetEmployeesQueried_BeforeAndPaged_ShouldReturnBeforeAndPaged()
        {
            // Arrange
            var pgSz = 2;
            var pgInd = 1;

            var date = await _context.Employees
                .Select(e => e.StartedOn)
                .OrderBy(e => e)
                .Skip(1)
                .FirstOrDefaultAsync();

            var started = await _context.Employees
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

            var expectedObj = started
                 .Select(e => new
                 {
                     e.EmployeeId,
                     e.FirstName,
                     e.LastName,
                     e.LocationId,
                     e.Title,
                     e.StartedOn,
                     e.WeeklyHours
                 })
                .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArray();
            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/query:before=true&pgsz="+pgSz+"&pgInd="+pgInd+
                "&started=" + date.ToShortDateString().Replace('/', '-'));
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetEmployeesQueried_BeforeAndSorted_ShouldReturnBeforeAndSorted()
        {
            // Arrange
            var sortBy = "firstName";

            var date = await _context.Employees
                .Select(e => e.StartedOn)
                .OrderBy(e => e)
                .Skip(1)
                .FirstOrDefaultAsync();

            var started = await _context.Employees
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

            var expectedObj = started
                            .Select(e => new
                            {
                                e.EmployeeId,
                                e.FirstName,
                                e.LastName,
                                e.LocationId,
                                e.Title,
                                e.StartedOn,
                                e.WeeklyHours
                            })
                            .OrderByDescending(e => e.FirstName)
                            .ToArray();

            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/query:before=true&desc=true&sortby="+sortBy+ "&started=" + 
                date.ToShortDateString().Replace('/', '-'));
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetEmployeesQueried_BeforeFilteredAndPaged_ShouldReturnBeforeFilteredAndPaged()
        {
            // Arrange
            var pgSz = 2;
            var pgInd = 1;

            var date = await _context.Employees
                .Select(e => e.StartedOn)
                .OrderBy(e => e)
                .Skip(1)
                .FirstOrDefaultAsync();

            var started = await _context.Employees
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

            var filter = started.Select(s => s.Title).FirstOrDefault();

            var filtered = started
                .Select(e => new
                {
                    e.EmployeeId,
                    e.FirstName,
                    e.LastName,
                    e.LocationId,
                    e.Title,
                    e.StartedOn,
                    e.WeeklyHours
                })
                 .Where(e => e.FirstName.ToUpper()
                     .Contains(filter.ToUpper()) ||
                        e.LastName.ToUpper()
                    .Contains(filter.ToUpper()) ||
                        e.Title.ToUpper().Contains(filter.ToUpper()))
                 .ToArray();

            var expectedObj = filtered
                .Select(e => new
                {
                    e.EmployeeId,
                    e.FirstName,
                    e.LastName,
                    e.LocationId,
                    e.Title,
                    e.StartedOn,
                    e.WeeklyHours
                })
                .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArray();

            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/query:before=true&filter=" + filter + "&pgsz="+pgSz+"&pgInd="+pgInd+
                "&started=" + date.ToShortDateString().Replace('/', '-'));
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetEmployeesQueried_BeforeFilteredAndSorted_ShouldReturnBeforeFilteredAndSorted()
        {
            // Arrange
            var sortBy = "position";

            var date = await _context.Employees
                .Select(e => e.StartedOn)
                .OrderBy(e => e)
                .Skip(1)
                .FirstOrDefaultAsync();

            var started = await _context.Employees
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

            var filter = started.Select(s => s.Title).FirstOrDefault();

            var filtered = started
                .Select(e => new
                {
                    e.EmployeeId,
                    e.FirstName,
                    e.LastName,
                    e.LocationId,
                    e.Title,
                    e.StartedOn,
                    e.WeeklyHours
                })
                 .Where(e => e.FirstName.ToUpper()
                     .Contains(filter.ToUpper()) ||
                        e.LastName.ToUpper()
                    .Contains(filter.ToUpper()) ||
                        e.Title.ToUpper().Contains(filter.ToUpper()))
                 .ToArray();

            var expectedObj = filtered
                .Select(e => new
                {
                    e.EmployeeId,
                    e.FirstName,
                    e.LastName,
                    e.LocationId,
                    e.Title,
                    e.StartedOn,
                    e.WeeklyHours
                })
                            .OrderByDescending(e => e.Title)
                            .ToArray();

            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/query:desc=true&before=true&filter=" + filter + "&sortby=" + sortBy+
                "&started=" + date.ToShortDateString().Replace('/', '-'));
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetEmployeesQueried_BeforeSortedAndPaged_ShouldReturnBeforeSortedAndPaged()
        {
            // Arrange
            var pgSz = 2;
            var pgInd = 1;
            var sortBy = "position";

            var date = await _context.Employees
                .Select(e => e.StartedOn)
                .OrderBy(e => e)
                .Skip(1)
                .FirstOrDefaultAsync();

            var started = await _context.Employees
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

            var sorted = started
                .Select(e => new
                {
                    e.EmployeeId,
                    e.FirstName,
                    e.LastName,
                    e.LocationId,
                    e.Title,
                    e.StartedOn,
                    e.WeeklyHours
                })
                            .OrderByDescending(e => e.Title)
                            .ToArray();

            var expectedObj = sorted
                .Select(e => new
                {
                    e.EmployeeId,
                    e.FirstName,
                    e.LastName,
                    e.LocationId,
                    e.Title,
                    e.StartedOn,
                    e.WeeklyHours
                })
                .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArray();

            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/query:desc=true&before=true&sortby=" + sortBy + 
                "&pgsz=" + pgSz + "&pgInd=" + pgInd +
                "&started=" + date.ToShortDateString().Replace('/', '-'));
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetEmployeesQueried_BeforeFilteredSortedAndPaged_ShouldReturnBeforeFilteredSortedAndPaged()
        {
            // Arrange
            var pgSz = 2;
            var pgInd = 1;
            var sortBy = "position";

            var date = await _context.Employees
                .Select(e => e.StartedOn)
                .OrderBy(e => e)
                .Skip(1)
                .FirstOrDefaultAsync();

            var started = await _context.Employees
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

            var filter = started.Select(s => s.Title).FirstOrDefault();

            var filtered = started
                .Select(e => new
                {
                    e.EmployeeId,
                    e.FirstName,
                    e.LastName,
                    e.LocationId,
                    e.Title,
                    e.StartedOn,
                    e.WeeklyHours
                })
                 .Where(e => e.FirstName.ToUpper()
                     .Contains(filter.ToUpper()) ||
                        e.LastName.ToUpper()
                    .Contains(filter.ToUpper()) ||
                        e.Title.ToUpper().Contains(filter.ToUpper()))
                 .ToArray();

            var sorted = filtered
                .Select(e => new
                {
                    e.EmployeeId,
                    e.FirstName,
                    e.LastName,
                    e.LocationId,
                    e.Title,
                    e.StartedOn,
                    e.WeeklyHours
                })
                            .OrderByDescending(e => e.Title)
                            .ToArray();

            var expectedObj = sorted
                .Select(e => new
                {
                    e.EmployeeId,
                    e.FirstName,
                    e.LastName,
                    e.LocationId,
                    e.Title,
                    e.StartedOn,
                    e.WeeklyHours
                })
                .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArray();

            var expected = JsonConvert.SerializeObject(expectedObj);

            // Act
            var result = await _client.GetAsync(requestUri: _client.BaseAddress +
                "/query:desc=true&before=true&filter="+filter+
                "&sortby=" + sortBy + 
                "&pgsz=" + pgSz + 
                "&pgInd=" + pgInd+
                "&started=" + date.ToShortDateString().Replace('/', '-'));
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        #endregion

        #endregion

        #region Updates

        [TestMethod]
        public async Task UpdateEmployee_HappyFlow_ShouldUpdateEmployee()
        {
            // Arrange
            var empId = GetFirstEmployeeId;

            /* Save a copy of the original entry to compare against*/
            var oldEmp = await _context.Employees.FindAsync(empId);
            var oldEmpCopy = new Employee
            {
                EmployeeId = oldEmp.EmployeeId,
                StartedOn = oldEmp.StartedOn,
                FirstName=oldEmp.FirstName,
                LastName = oldEmp.LastName,
                Location = oldEmp.Location,
                LocationId = oldEmp.LocationId,
                Managements = oldEmp.Managements,
                Position = oldEmp.Position,
                PositionId = oldEmp.PositionId,
                WeeklyHours = oldEmp.WeeklyHours
            };

            var emp = new Employee
            {
                FirstName = "Gabe",
                LastName = "Newton",
                StartedOn = oldEmp.StartedOn.AddDays(-10),
                LocationId = await _context.Locations
                    .Select(l => l.LocationId)
                    .FirstOrDefaultAsync(l=>l!=oldEmpCopy.LocationId),
                PositionId = await _context.Positions
                    .Select(p => p.PositionId)
                    .FirstOrDefaultAsync(p=>p!=oldEmpCopy.PositionId),
                WeeklyHours = oldEmp.WeeklyHours + 2
            };
            var empContent = new StringContent(JsonConvert.SerializeObject(emp),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + empId, empContent);

            /* Retrieve the (allegedly) updated entry*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + empId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newEmp = JsonConvert.DeserializeObject(newCont, typeof(Employee));

            /* Complete the entry we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            emp.EmployeeId = oldEmpCopy.EmployeeId;
            emp.Location = oldEmpCopy.Location;
            emp.Managements = oldEmpCopy.Managements;

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            newEmp.Should().NotBeEquivalentTo(oldEmpCopy);
            newEmp.Should().BeEquivalentTo(emp);

            // Clean-up
            emp = new Employee
            {
                FirstName = oldEmpCopy.FirstName,
                LastName = oldEmpCopy.LastName,
                StartedOn = oldEmpCopy.StartedOn,
                LocationId = oldEmpCopy.LocationId,
                PositionId = oldEmpCopy.PositionId,
                WeeklyHours = oldEmpCopy.WeeklyHours
            };

            empContent = new StringContent(JsonConvert.SerializeObject(emp),
                Encoding.UTF8, "application/json");
            result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + empId, empContent);
            updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + empId + "/basic");
            newCont = await updCall.Content.ReadAsStringAsync();
            newEmp = JsonConvert.DeserializeObject(newCont, typeof(Employee));

            result.IsSuccessStatusCode.Should().BeTrue();
            newEmp.Should().BeEquivalentTo(oldEmpCopy);
        }

        [TestMethod]
        public async Task UpdateEmployee_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var empId = GetFirstEmployeeId;

            /* Save a copy of the original entry to compare against*/
            var oldEmp = await _context.Employees.FindAsync(empId);
            var oldEmpCopy = new Employee
            {
                EmployeeId = oldEmp.EmployeeId,
                StartedOn = oldEmp.StartedOn,
                FirstName = oldEmp.FirstName,
                LastName = oldEmp.LastName,
                Location = oldEmp.Location,
                LocationId = oldEmp.LocationId,
                Managements = oldEmp.Managements,
                Position = oldEmp.Position,
                PositionId = oldEmp.PositionId,
                WeeklyHours = oldEmp.WeeklyHours
            };

            var emp = new Employee
            {
                FirstName = "Gabe",
                LastName = "Newton",
                StartedOn = oldEmp.StartedOn.AddDays(-10),
                LocationId = await _context.Locations
                    .Select(l => l.LocationId)
                    .FirstOrDefaultAsync(l => l != oldEmpCopy.LocationId),
                PositionId = await _context.Positions
                    .Select(p => p.PositionId)
                    .FirstOrDefaultAsync(p => p != oldEmpCopy.PositionId),
                WeeklyHours = oldEmp.WeeklyHours + 2
            };
            var empContent = new StringContent(JsonConvert.SerializeObject(emp),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" +
                GenBadEmployeeId, empContent);

            /* Retrieve the (allegedly) updated entry*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + empId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newEmp = JsonConvert.DeserializeObject(newCont, typeof(Employee));

            /* Complete the entry we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            emp.EmployeeId = oldEmpCopy.EmployeeId;
            emp.Location = oldEmpCopy.Location;
            emp.Managements = oldEmpCopy.Managements;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
            newEmp.Should().BeEquivalentTo(oldEmpCopy);
            newEmp.Should().NotBeEquivalentTo(emp);
        }

        [TestMethod]
        public async Task UpdateEmployee_TriesToSetID_ShouldReturnBadRequest()
        {
            // Arrange
            var empId = GetFirstEmployeeId;

            /* Save a copy of the original entry to compare against*/
            var oldEmp = await _context.Employees.FindAsync(empId);
            var oldEmpCopy = new Employee
            {
                EmployeeId = oldEmp.EmployeeId,
                StartedOn = oldEmp.StartedOn,
                FirstName = oldEmp.FirstName,
                LastName = oldEmp.LastName,
                Location = oldEmp.Location,
                LocationId = oldEmp.LocationId,
                Managements = oldEmp.Managements,
                Position = oldEmp.Position,
                PositionId = oldEmp.PositionId,
                WeeklyHours = oldEmp.WeeklyHours
            };

            var emp = new Employee
            {
                EmployeeId = GenBadEmployeeId,
                FirstName = "Gabe",
                LastName = "Newton",
                StartedOn = oldEmp.StartedOn.AddDays(-10),
                LocationId = await _context.Locations
                    .Select(l => l.LocationId)
                    .FirstOrDefaultAsync(l => l != oldEmpCopy.LocationId),
                PositionId = await _context.Positions
                    .Select(p => p.PositionId)
                    .FirstOrDefaultAsync(p => p != oldEmpCopy.PositionId),
                WeeklyHours = oldEmp.WeeklyHours + 2
            };
            var empContent = new StringContent(JsonConvert.SerializeObject(emp),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + empId, empContent);

            /* Retrieve the (allegedly) updated entry*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + empId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newEmp = JsonConvert.DeserializeObject(newCont, typeof(Employee));

            /* Complete the entry we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            emp.EmployeeId = oldEmpCopy.EmployeeId;
            emp.Location = oldEmpCopy.Location;
            emp.Managements = oldEmpCopy.Managements;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newEmp.Should().BeEquivalentTo(oldEmpCopy);
            newEmp.Should().NotBeEquivalentTo(emp);
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
            var empId = GetFirstEmployeeId;

            /* Save a copy of the original entry to compare against*/
            var oldEmp = await _context.Employees.FindAsync(empId);
            var oldEmpCopy = new Employee
            {
                EmployeeId = oldEmp.EmployeeId,
                StartedOn = oldEmp.StartedOn,
                FirstName = oldEmp.FirstName,
                LastName = oldEmp.LastName,
                Location = oldEmp.Location,
                LocationId = oldEmp.LocationId,
                Managements = oldEmp.Managements,
                Position = oldEmp.Position,
                PositionId = oldEmp.PositionId,
                WeeklyHours = oldEmp.WeeklyHours
            };

            var emp = new Employee
            {
                FirstName = firstName,
                LastName = lastName,
                StartedOn = oldEmp.StartedOn.AddDays(-10),
                LocationId = await _context.Locations
                    .Select(l => l.LocationId)
                    .FirstOrDefaultAsync(l => l != oldEmpCopy.LocationId),
                PositionId = await _context.Positions
                    .Select(p => p.PositionId)
                    .FirstOrDefaultAsync(p => p != oldEmpCopy.PositionId),
                WeeklyHours = oldEmp.WeeklyHours + 2
            };
            var empContent = new StringContent(JsonConvert.SerializeObject(emp),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + empId, empContent);

            /* Retrieve the (allegedly) updated entry*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + empId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newEmp = JsonConvert.DeserializeObject(newCont, typeof(Employee));

            /* Complete the entry we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            emp.EmployeeId = oldEmpCopy.EmployeeId;
            emp.Location = oldEmpCopy.Location;
            emp.Managements = oldEmpCopy.Managements;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newEmp.Should().BeEquivalentTo(oldEmpCopy);
            newEmp.Should().NotBeEquivalentTo(emp);
        }

        [DataTestMethod]
        [DataRow(-1)]
        [DataRow(168)]
        public async Task UpdateEmployee_HoursOutOfRange_ShouldReturnBadRequest(int hours)
        {
            // Arrange
            var empId = GetFirstEmployeeId;

            /* Save a copy of the original entry to compare against*/
            var oldEmp = await _context.Employees.FindAsync(empId);
            var oldEmpCopy = new Employee
            {
                EmployeeId = oldEmp.EmployeeId,
                StartedOn = oldEmp.StartedOn,
                FirstName = oldEmp.FirstName,
                LastName = oldEmp.LastName,
                Location = oldEmp.Location,
                LocationId = oldEmp.LocationId,
                Managements = oldEmp.Managements,
                Position = oldEmp.Position,
                PositionId = oldEmp.PositionId,
                WeeklyHours = oldEmp.WeeklyHours
            };

            var emp = new Employee
            {
                FirstName = "Gabe",
                LastName = "Newton",
                StartedOn = oldEmp.StartedOn.AddDays(-10),
                LocationId = await _context.Locations
                    .Select(l => l.LocationId)
                    .FirstOrDefaultAsync(l => l != oldEmpCopy.LocationId),
                PositionId = await _context.Positions
                    .Select(p => p.PositionId)
                    .FirstOrDefaultAsync(p => p != oldEmpCopy.PositionId),
                WeeklyHours = hours
            };
            var empContent = new StringContent(JsonConvert.SerializeObject(emp),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + empId, empContent);

            /* Retrieve the (allegedly) updated entry*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + empId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newEmp = JsonConvert.DeserializeObject(newCont, typeof(Employee));

            /* Complete the entry we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            emp.EmployeeId = oldEmpCopy.EmployeeId;
            emp.Location = oldEmpCopy.Location;
            emp.Managements = oldEmpCopy.Managements;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newEmp.Should().BeEquivalentTo(oldEmpCopy);
            newEmp.Should().NotBeEquivalentTo(emp);
        }

        [TestMethod]
        public async Task UpdateEmployee_InexistentLocationId_ShouldReturnBadRequest()
        {
            // Arrange
            var empId = GetFirstEmployeeId;

            /* Save a copy of the original entry to compare against*/
            var oldEmp = await _context.Employees.FindAsync(empId);
            var oldEmpCopy = new Employee
            {
                EmployeeId = oldEmp.EmployeeId,
                StartedOn = oldEmp.StartedOn,
                FirstName = oldEmp.FirstName,
                LastName = oldEmp.LastName,
                Location = oldEmp.Location,
                LocationId = oldEmp.LocationId,
                Managements = oldEmp.Managements,
                Position = oldEmp.Position,
                PositionId = oldEmp.PositionId,
                WeeklyHours = oldEmp.WeeklyHours
            };

            var emp = new Employee
            {
                FirstName = "Gabe",
                LastName = "Newton",
                StartedOn = oldEmp.StartedOn.AddDays(-10),
                LocationId = (await _context.Locations
                    .Select(l => l.LocationId)
                    .ToArrayAsync())
                    .LastOrDefault() + 1,
                PositionId = await _context.Positions
                    .Select(p => p.PositionId)
                    .FirstOrDefaultAsync(p => p != oldEmpCopy.PositionId),
                WeeklyHours = oldEmp.WeeklyHours + 2
            };
            var empContent = new StringContent(JsonConvert.SerializeObject(emp),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + empId, empContent);

            /* Retrieve the (allegedly) updated entry*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + empId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newEmp = JsonConvert.DeserializeObject(newCont, typeof(Employee));

            /* Complete the entry we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            emp.EmployeeId = oldEmpCopy.EmployeeId;
            emp.Location = oldEmpCopy.Location;
            emp.Managements = oldEmpCopy.Managements;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newEmp.Should().BeEquivalentTo(oldEmpCopy);
            newEmp.Should().NotBeEquivalentTo(emp);
        }

        [TestMethod]
        public async Task UpdateEmployee_InexistentPositionId_ShouldReturnBadRequest()
        {
            // Arrange
            var empId = GetFirstEmployeeId;

            /* Save a copy of the original entry to compare against*/
            var oldEmp = await _context.Employees.FindAsync(empId);
            var oldEmpCopy = new Employee
            {
                EmployeeId = oldEmp.EmployeeId,
                StartedOn = oldEmp.StartedOn,
                FirstName = oldEmp.FirstName,
                LastName = oldEmp.LastName,
                Location = oldEmp.Location,
                LocationId = oldEmp.LocationId,
                Managements = oldEmp.Managements,
                Position = oldEmp.Position,
                PositionId = oldEmp.PositionId,
                WeeklyHours = oldEmp.WeeklyHours
            };

            var emp = new Employee
            {
                FirstName = "Gabe",
                LastName = "Newton",
                StartedOn = oldEmp.StartedOn.AddDays(-10),
                LocationId = await _context.Locations
                    .Select(l => l.LocationId)
                    .FirstOrDefaultAsync(l => l != oldEmpCopy.LocationId),
                PositionId = (await _context.Positions
                    .Select(p => p.PositionId)
                    .ToArrayAsync())
                    .LastOrDefault()+1,
                WeeklyHours = oldEmp.WeeklyHours + 2
            };
            var empContent = new StringContent(JsonConvert.SerializeObject(emp),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + empId, empContent);

            /* Retrieve the (allegedly) updated entry*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + empId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newEmp = JsonConvert.DeserializeObject(newCont, typeof(Employee));

            /* Complete the entry we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            emp.EmployeeId = oldEmpCopy.EmployeeId;
            emp.Location = oldEmpCopy.Location;
            emp.Managements = oldEmpCopy.Managements;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newEmp.Should().BeEquivalentTo(oldEmpCopy);
            newEmp.Should().NotBeEquivalentTo(emp);
        }

        [TestMethod]
        public async Task UpdateEmployee_TriesToSetLocation_ShouldReturnBadRequest()
        {
            // Arrange
            var empId = GetFirstEmployeeId;

            /* Save a copy of the original entry to compare against*/
            var oldEmp = await _context.Employees.FindAsync(empId);
            var oldEmpCopy = new Employee
            {
                EmployeeId = oldEmp.EmployeeId,
                StartedOn = oldEmp.StartedOn,
                FirstName = oldEmp.FirstName,
                LastName = oldEmp.LastName,
                Location = oldEmp.Location,
                LocationId = oldEmp.LocationId,
                Managements = oldEmp.Managements,
                Position = oldEmp.Position,
                PositionId = oldEmp.PositionId,
                WeeklyHours = oldEmp.WeeklyHours
            };

            var emp = new Employee
            {
                FirstName = "Gabe",
                LastName = "Newton",
                StartedOn = oldEmp.StartedOn.AddDays(-10),
                LocationId = await _context.Locations
                    .Select(l => l.LocationId)
                    .FirstOrDefaultAsync(l => l != oldEmpCopy.LocationId),
                PositionId = await _context.Positions
                    .Select(p => p.PositionId)
                    .FirstOrDefaultAsync(p => p != oldEmpCopy.PositionId),
                WeeklyHours = oldEmp.WeeklyHours + 2,
                Location = new Location()
            };
            var empContent = new StringContent(JsonConvert.SerializeObject(emp),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + empId, empContent);

            /* Retrieve the (allegedly) updated entry*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + empId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newEmp = JsonConvert.DeserializeObject(newCont, typeof(Employee));

            /* Complete the entry we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            emp.EmployeeId = oldEmpCopy.EmployeeId;
            emp.Location = oldEmpCopy.Location;
            emp.Managements = oldEmpCopy.Managements;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newEmp.Should().BeEquivalentTo(oldEmpCopy);
            newEmp.Should().NotBeEquivalentTo(emp);
        }

        [TestMethod]
        public async Task UpdateEmployee_TriesToSetPosition_ShouldReturnBadRequest()
        {
            // Arrange
            var empId = GetFirstEmployeeId;

            /* Save a copy of the original entry to compare against*/
            var oldEmp = await _context.Employees.FindAsync(empId);
            var oldEmpCopy = new Employee
            {
                EmployeeId = oldEmp.EmployeeId,
                StartedOn = oldEmp.StartedOn,
                FirstName = oldEmp.FirstName,
                LastName = oldEmp.LastName,
                Location = oldEmp.Location,
                LocationId = oldEmp.LocationId,
                Managements = oldEmp.Managements,
                Position = oldEmp.Position,
                PositionId = oldEmp.PositionId,
                WeeklyHours = oldEmp.WeeklyHours
            };

            var emp = new Employee
            {
                FirstName = "Gabe",
                LastName = "Newton",
                StartedOn = oldEmp.StartedOn.AddDays(-10),
                LocationId = await _context.Locations
                    .Select(l => l.LocationId)
                    .FirstOrDefaultAsync(l => l != oldEmpCopy.LocationId),
                PositionId = await _context.Positions
                    .Select(p => p.PositionId)
                    .FirstOrDefaultAsync(p => p != oldEmpCopy.PositionId),
                WeeklyHours = oldEmp.WeeklyHours + 2,
                Position = new Position()
            };
            var empContent = new StringContent(JsonConvert.SerializeObject(emp),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + empId, empContent);

            /* Retrieve the (allegedly) updated entry*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + empId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newEmp = JsonConvert.DeserializeObject(newCont, typeof(Employee));

            /* Complete the entry we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            emp.EmployeeId = oldEmpCopy.EmployeeId;
            emp.Location = oldEmpCopy.Location;
            emp.Managements = oldEmpCopy.Managements;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newEmp.Should().BeEquivalentTo(oldEmpCopy);
            newEmp.Should().NotBeEquivalentTo(emp);
        }

        [TestMethod]
        public async Task UpdateEmployee_ManagementsNull_ShouldReturnBadRequest()
        {
            // Arrange
            var empId = GetFirstEmployeeId;

            /* Save a copy of the original entry to compare against*/
            var oldEmp = await _context.Employees.FindAsync(empId);
            var oldEmpCopy = new Employee
            {
                EmployeeId = oldEmp.EmployeeId,
                StartedOn = oldEmp.StartedOn,
                FirstName = oldEmp.FirstName,
                LastName = oldEmp.LastName,
                Location = oldEmp.Location,
                LocationId = oldEmp.LocationId,
                Managements = oldEmp.Managements,
                Position = oldEmp.Position,
                PositionId = oldEmp.PositionId,
                WeeklyHours = oldEmp.WeeklyHours
            };

            var emp = new Employee
            {
                FirstName = "Gabe",
                LastName = "Newton",
                StartedOn = oldEmp.StartedOn.AddDays(-10),
                LocationId = await _context.Locations
                    .Select(l => l.LocationId)
                    .FirstOrDefaultAsync(l => l != oldEmpCopy.LocationId),
                PositionId = await _context.Positions
                    .Select(p => p.PositionId)
                    .FirstOrDefaultAsync(p => p != oldEmpCopy.PositionId),
                WeeklyHours = oldEmp.WeeklyHours + 2,
                Managements = null
            };
            var empContent = new StringContent(JsonConvert.SerializeObject(emp),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + empId, empContent);

            /* Retrieve the (allegedly) updated entry*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + empId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newEmp = JsonConvert.DeserializeObject(newCont, typeof(Employee));

            /* Complete the entry we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            emp.EmployeeId = oldEmpCopy.EmployeeId;
            emp.Location = oldEmpCopy.Location;
            emp.Managements = oldEmpCopy.Managements;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newEmp.Should().BeEquivalentTo(oldEmpCopy);
            newEmp.Should().NotBeEquivalentTo(emp);
        }

        [TestMethod]
        public async Task UpdateEmployee_ManagementsNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var empId = GetFirstEmployeeId;

            /* Save a copy of the original entry to compare against*/
            var oldEmp = await _context.Employees.FindAsync(empId);
            var oldEmpCopy = new Employee
            {
                EmployeeId = oldEmp.EmployeeId,
                StartedOn = oldEmp.StartedOn,
                FirstName = oldEmp.FirstName,
                LastName = oldEmp.LastName,
                Location = oldEmp.Location,
                LocationId = oldEmp.LocationId,
                Managements = oldEmp.Managements,
                Position = oldEmp.Position,
                PositionId = oldEmp.PositionId,
                WeeklyHours = oldEmp.WeeklyHours
            };

            var emp = new Employee
            {
                FirstName = "Gabe",
                LastName = "Newton",
                StartedOn = oldEmp.StartedOn.AddDays(-10),
                LocationId = await _context.Locations
                    .Select(l => l.LocationId)
                    .FirstOrDefaultAsync(l => l != oldEmpCopy.LocationId),
                PositionId = await _context.Positions
                    .Select(p => p.PositionId)
                    .FirstOrDefaultAsync(p => p != oldEmpCopy.PositionId),
                WeeklyHours = oldEmp.WeeklyHours + 2,
                Managements = new Management[] {new Management()}
            };
            var empContent = new StringContent(JsonConvert.SerializeObject(emp),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + empId, empContent);

            /* Retrieve the (allegedly) updated entry*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + empId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newEmp = JsonConvert.DeserializeObject(newCont, typeof(Employee));

            /* Complete the entry we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            emp.EmployeeId = oldEmpCopy.EmployeeId;
            emp.Location = oldEmpCopy.Location;
            emp.Managements = oldEmpCopy.Managements;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newEmp.Should().BeEquivalentTo(oldEmpCopy);
            newEmp.Should().NotBeEquivalentTo(emp);
        }

        [TestMethod]
        public async Task UpdateEmployee_TriesToManageManagedLocation_ShouldReturnBadRequest()
        {
            #region Fixes
            //{
            //// Fixes
            //var broknEmp = await _context.Employees
            //    .FirstOrDefaultAsync();
            //
            //broknEmp.FirstName = "Keene";
            //broknEmp.LastName = "Larchiere";
            //broknEmp.LocationId = 1;
            //broknEmp.StartedOn = new DateTime(2019, 11, 19);
            //broknEmp.WeeklyHours = 56;
            //broknEmp.PositionId = 1;
            //
            //await _context.SaveChangesAsync();
            //
            //var brokenLoc = await _context.Locations.Skip(1).FirstOrDefaultAsync();
            //var emp2 = await _context.Employees.FindAsync(2);
            //brokenLoc.Employees = new List<Employee>{ emp2 };
            //await _context.SaveChangesAsync();
            //}
            #endregion

            // Arrange
            var empId = GetFirstEmployeeId;
            var managedLocId = (await _context.Locations
                .Include(l => l.Managements)
                .FirstOrDefaultAsync(l =>
                    l.Managements.Count() != 0 &&
                    l.Managements
                .SingleOrDefault().ManagerId != empId))
                .LocationId;


            /* Save a copy of the original entry to compare against*/
            var oldEmp = await _context.Employees.FindAsync(empId);
            var oldEmpCopy = new Employee
            {
                EmployeeId = oldEmp.EmployeeId,
                StartedOn = oldEmp.StartedOn,
                FirstName = oldEmp.FirstName,
                LastName = oldEmp.LastName,
                Location = oldEmp.Location,
                LocationId = oldEmp.LocationId,
                Managements = oldEmp.Managements,
                Position = oldEmp.Position,
                PositionId = oldEmp.PositionId,
                WeeklyHours = oldEmp.WeeklyHours
            };

            var emp = new Employee
            {
                FirstName = "Gabe",
                LastName = "Newton",
                StartedOn = oldEmp.StartedOn.AddDays(-10),
                WeeklyHours = oldEmp.WeeklyHours + 2,
                LocationId = managedLocId,
                PositionId = 1
            };
            var empContent = new StringContent(JsonConvert.SerializeObject(emp),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PutAsync(requestUri: _client.BaseAddress + "/" + empId, empContent);

            /* Retrieve the (allegedly) updated entry*/
            var updCall = await _client.GetAsync(requestUri: _client.BaseAddress + "/" + empId + "/basic");
            var newCont = await updCall.Content.ReadAsStringAsync();
            var newEmp = JsonConvert.DeserializeObject(newCont, typeof(Employee));

            /* Complete the entry we sent in the request with imutable properties 
             * that would've 500'd the Request, but are nonetheless necessary
             * for comparison to results*/
            emp.EmployeeId = oldEmpCopy.EmployeeId;
            emp.Location = oldEmpCopy.Location;
            emp.Managements = oldEmpCopy.Managements;

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
            newEmp.Should().BeEquivalentTo(oldEmpCopy);
            newEmp.Should().NotBeEquivalentTo(emp);
        }

        #endregion

        #region Deletes

        [TestMethod]
        public async Task DeleteEmployee_InexistendId_ShouldReturnNotFound()
        {
            // Arrange
            var empId = await _context.Employees.CountAsync() + 1;

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + empId);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [DataTestMethod]
        [DataRow(false, false)]
        [DataRow(true, true)]
        public async Task DeleteEmployee_HasRelations_ShouldTurnToBlank(bool isCap, bool isFirst)
        {
            // Arrange
            var empId = 0;
            if (!isCap) empId = 2;
            else
            {
                if (isFirst) empId = 1;
                else return;
            }
            var emp = await _context.Employees.FindAsync(empId);
            var empCopy = new Employee
            {
                FirstName = emp.FirstName,
                LastName = emp.LastName,
                StartedOn = emp.StartedOn,
                LocationId = emp.LocationId,
                PositionId = emp.PositionId,
                WeeklyHours = emp.WeeklyHours
            };
            var empFix = new StringContent(JsonConvert.SerializeObject(empCopy),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + empId);
            var getRes = await _client.GetAsync(_client.BaseAddress + "/" + empId + "/basic");
            var getCont = await getRes.Content.ReadAsStringAsync();
            var getEmp = JsonConvert.DeserializeObject(getCont, typeof(Employee)) as Employee;

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            getEmp.FirstName.Should().Contain("Missing");
            getEmp.StartedOn.Should().Be(new DateTime());
            getEmp.WeeklyHours.Should().Be(0);
            getEmp.LocationId.Should().Be(1);
            getEmp.PositionId.Should().Be(2);

            // Clean-up
            await _client.PutAsync(_client.BaseAddress + "/" + empId, empFix);
        }

        [TestMethod]
        public async Task DeleteEmployee_LastNoRelationships_ShouldDelete()
        {
            // Arrange
            var empId = await _context.Employees.CountAsync() + 1;
            var emp = new Employee
            {
                FirstName = "Dilan",
                LastName = "Gonman",
                StartedOn = DateTime.Today.AddDays(-10),
                WeeklyHours = 20,
                LocationId = 1,
                PositionId = 2
            };
            await _context.Employees.AddAsync(emp);
            await _context.SaveChangesAsync();

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + empId);
            var getRes = await _client.GetAsync(_client.BaseAddress + "/" + empId + "/basic");

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            getRes.IsSuccessStatusCode.Should().BeFalse();
            getRes.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task DeleteEmployee_NotLastNoRelationships_ShouldSwapWithLastAndDelThat()
        {
            // Arrange
            var empId = await _context.Employees.CountAsync() + 1;
            var delEmp = new Employee
            {
                FirstName = "Delete",
                LastName = "Me",
                StartedOn = DateTime.Today.AddDays(-10),
                WeeklyHours = 20,
                LocationId = 1,
                PositionId = 2
            };
            var swpEmp = new Employee
            {
                FirstName = "Dilan",
                LastName = "Gonman",
                StartedOn = DateTime.Today.AddDays(-10),
                WeeklyHours = 20,
                LocationId = 1,
                PositionId = 2
            };
            var newName = swpEmp.FirstName;
            await _context.Employees.AddRangeAsync(new Employee[] { delEmp,swpEmp });
            await _context.SaveChangesAsync();

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + empId);
            var getRes = await _client.GetAsync(_client.BaseAddress + "/" + empId + "/basic");
            var getCont = await getRes.Content.ReadAsStringAsync();
            var getEmp = JsonConvert.DeserializeObject(getCont, typeof(Employee)) as Employee;

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            getEmp.FirstName.Should().BeEquivalentTo(newName);

            // Clean-up
            await _client.DeleteAsync(_client.BaseAddress + "/" + empId);
        }

        [TestMethod]
        public async Task DeleteEmployee_NotLastNoRel_ShouldMigrateManagement()
        {
            // Arrange
            var manId = await _context.Managements.CountAsync() + 1;
            var locId = await _context.Locations.CountAsync() + 1;
            var empId = await _context.Employees.CountAsync() + 1;

            var loc = new Location
            {
                ScheduleId = 1,
                State = "DelMe",
                Street = "DelMe",
                AbreviatedState = "N/A",
                OpenSince = new DateTime(),
                AbreviatedCountry = "XX",
                City = "DelMe",
                Country = "DelMe",
                MenuId = 1
            };
            var delEmp = new Employee 
            { 
                FirstName = "Delete",
                LastName = "Me",
                StartedOn = new DateTime(),
                LocationId = 1,
                WeeklyHours = 1,
                PositionId = 2
            };
            var swapEmp = new Employee 
            {
                FirstName = "Trafalgar",
                LastName = "Law",
                StartedOn = new DateTime(),
                LocationId = locId,
                WeeklyHours = 34,
                PositionId = 1
            };
            var man = new Management { LocationId = locId, ManagerId = empId + 1 };

            await _context.Locations.AddAsync(loc);
            await _context.SaveChangesAsync();
            await _context.Employees.AddRangeAsync(new Employee[] { delEmp, swapEmp });
            await _context.SaveChangesAsync();

            await _context.Managements.AddAsync(man);
            await _context.SaveChangesAsync();

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + empId);

            var getManRes = await _client.GetAsync("http://localhost/api/managements/" +
                manId + "/basic");
            var getManCont = await getManRes.Content.ReadAsStringAsync();
            var getMan = JsonConvert.DeserializeObject(getManCont,
                typeof(Management)) as Management;

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            getMan.ManagerId.Should().Be(empId);

            // Clean-up
            await _client.DeleteAsync("http://localhost/api/managements/" + manId);
            await _client.DeleteAsync(_client.BaseAddress + "/" + empId);
            await _client.DeleteAsync("http://localhost/api/locations/" + locId);
        }

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
                PositionId = await _context.Positions.CountAsync(),
                WeeklyHours = 36,
                StartedOn = DateTime.Today.AddDays(-200)
            };
            var empCont = new StringContent(JsonConvert.SerializeObject(employee),
                Encoding.UTF8, "application/json");
            var empId = await _context.Employees.CountAsync() + 1;

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, empCont);
            var getRes = await _client.GetAsync(_client.BaseAddress + "/" + empId + "/basic");
            var getCont = await getRes.Content.ReadAsStringAsync();
            var getEmp = JsonConvert.DeserializeObject(getCont, typeof(Employee)) as Employee;

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            getEmp.FirstName.Should().Be(employee.FirstName);
            getEmp.LastName.Should().Be(employee.LastName);
            getEmp.LocationId.Should().Be(employee.LocationId);
            getEmp.PositionId.Should().Be(employee.PositionId);
            getEmp.WeeklyHours.Should().Be(employee.WeeklyHours);
            getEmp.StartedOn.Should().Be(employee.StartedOn);

            // Clean-up
            await _client.DeleteAsync(_client.BaseAddress + "/" + empId);
        }

        [TestMethod]
        public async Task CreateEmployee_TriesToSetId_ShouldReturnBadRequest()
        {
            // Arrange
            var employee = new Employee
            {
                FirstName = "Daniel",
                LastName = "Crawford",
                LocationId = (await _context.Locations.FirstOrDefaultAsync()).LocationId,
                PositionId = await _context.Positions.CountAsync(),
                WeeklyHours = 36,
                StartedOn = DateTime.Today.AddDays(-200),
                EmployeeId = GenBadEmployeeId
            };
            var empCont = new StringContent(JsonConvert.SerializeObject(employee),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, empCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
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
                LocationId = (await _context.Locations.FirstOrDefaultAsync()).LocationId,
                PositionId = await _context.Positions.CountAsync(),
                WeeklyHours = 36,
                StartedOn = DateTime.Today.AddDays(-200)
            };
            var empCont = new StringContent(JsonConvert.SerializeObject(employee),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, empCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateEmployee_NoStartingDate_ShouldReturnBadRequest()
        {
            // Arrange
            var employee = new Employee
            {
                FirstName = "Daniel",
                LastName = "Crawford",
                LocationId = (await _context.Locations.FirstOrDefaultAsync()).LocationId,
                PositionId = await _context.Positions.CountAsync(),
                WeeklyHours = 36
            };
            var empCont = new StringContent(JsonConvert.SerializeObject(employee),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, empCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
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
                LocationId = (await _context.Locations.FirstOrDefaultAsync()).LocationId,
                PositionId = await _context.Positions.CountAsync(),
                StartedOn = DateTime.Today.AddDays(-200),
                WeeklyHours = weeklyHours
            };
            var empCont = new StringContent(JsonConvert.SerializeObject(employee),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, empCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateEmployee_InexistentLocationId_ShouldReturnBadRequest()
        {
            // Arrange
            var employee = new Employee
            {
                FirstName = "Daniel",
                LastName = "Crawford",
                PositionId = await _context.Positions.CountAsync(),
                WeeklyHours = 36,
                StartedOn = DateTime.Today.AddDays(-200),
                LocationId = await _context.Locations.CountAsync()+1
            };
            var empCont = new StringContent(JsonConvert.SerializeObject(employee),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, empCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateEmployee_InexistentPositionId_ShouldReturnBadRequest()
        {
            // Arrange
            var employee = new Employee
            {
                FirstName = "Daniel",
                LastName = "Crawford",
                LocationId = (await _context.Locations.FirstOrDefaultAsync()).LocationId,
                WeeklyHours = 36,
                StartedOn = DateTime.Today.AddDays(-200),
                PositionId = await _context.Positions.CountAsync()+1
            };
            var empCont = new StringContent(JsonConvert.SerializeObject(employee),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, empCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateEmployee_HasLocation_ShouldReturnBadRequest()
        {
            // Arrange
            var employee = new Employee
            {
                FirstName = "Daniel",
                LastName = "Crawford",
                LocationId = (await _context.Locations.FirstOrDefaultAsync()).LocationId,
                PositionId = await _context.Positions.CountAsync(),
                WeeklyHours = 36,
                StartedOn = DateTime.Today.AddDays(-200),
                Location = new Location()
            };
            var empCont = new StringContent(JsonConvert.SerializeObject(employee),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, empCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateEmployee_HasPosition_ShouldReturnBadRequest()
        {
            // Arrange
            var employee = new Employee
            {
                FirstName = "Daniel",
                LastName = "Crawford",
                LocationId = (await _context.Locations.FirstOrDefaultAsync()).LocationId,
                PositionId = await _context.Positions.CountAsync(),
                WeeklyHours = 36,
                StartedOn = DateTime.Today.AddDays(-200),
                Position = new Position()
            };
            var empCont = new StringContent(JsonConvert.SerializeObject(employee),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, empCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateEmployee_ManagementsNull_ShouldReturnBadRequest()
        {
            // Arrange
            var employee = new Employee
            {
                FirstName = "Daniel",
                LastName = "Crawford",
                LocationId = (await _context.Locations.FirstOrDefaultAsync()).LocationId,
                PositionId = await _context.Positions.CountAsync(),
                WeeklyHours = 36,
                StartedOn = DateTime.Today.AddDays(-200),
                Managements = null
            };
            var empCont = new StringContent(JsonConvert.SerializeObject(employee),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, empCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CreateEmployee_ManagementsNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var employee = new Employee
            {
                FirstName = "Daniel",
                LastName = "Crawford",
                LocationId = (await _context.Locations.FirstOrDefaultAsync()).LocationId,
                PositionId = await _context.Positions.CountAsync(),
                WeeklyHours = 36,
                StartedOn = DateTime.Today.AddDays(-200),
                Managements = new Management[] {new Management()}
            };
            var empCont = new StringContent(JsonConvert.SerializeObject(employee),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, empCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
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
                PositionId = 1,
                WeeklyHours = 36,
                StartedOn = DateTime.Today.AddDays(-200),
                LocationId = locId
            };
            var empCont = new StringContent(JsonConvert.SerializeObject(employee),
                Encoding.UTF8, "application/json");

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, empCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);
        }

        #endregion

        //Helpers
        private int GenBadEmployeeId => _context.Employees
                .AsEnumerable()
                .Select(e => e.EmployeeId)
                .LastOrDefault() + 1;
        private int GetFirstEmployeeId => _context.Employees
            .Select(e => e.EmployeeId)
            .FirstOrDefault();
        private async Task<string> GetRecurringName()
        {
            var names = new List<(string name, int count)>();

            var id = 0;
            while (true)
            {
                var emp = await _context.Employees.Select(e => new { e.EmployeeId, e.FirstName }).Skip(id).FirstOrDefaultAsync();
                if (emp == null) break;
                id++;
                if (names.Any(n => n.name == emp.FirstName))
                {
                    var oldRec = names.SingleOrDefault(n => n.name == emp.FirstName);

                    if (oldRec.count == 2) return oldRec.name;
                    
                    names.Add((oldRec.name, oldRec.count + 1));
                    names.Remove(oldRec);
                }
                else names.Add((emp.FirstName, 1));
            }

            return "ERROR";
        }
    }
}
