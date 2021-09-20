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
    public class ManagementsIntegrationTests
    {
        private static HttpClient _client;
        private static FoodChainsDbContext _context;
        private static WebApplicationFactory<Startup> _appFactory;

        [ClassInitialize]
        public static void InitClass(TestContext testContext)
        {
            _appFactory = new WebApplicationFactory<Startup>();
            _appFactory.ClientOptions.BaseAddress = new Uri("http://localhost/api/managements");
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
            _appFactory.ClientOptions.BaseAddress = new Uri("http://localhost/api/managements");
            _client = _appFactory.CreateClient();

            var fked = HelperFuck.MessedUpDB(_context);
            if (fked)
            {
                var fake = false;
            }
        }

        #region Deletes

        [TestMethod]
        public async Task DeleteManagement_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var manId = GenBadManagementId;

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + manId);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task DeleteManagement_Last_ShouldDelete()
        {
            // Arrange
            var manId = GenBadManagementId;

            var locCount = await _context.Locations.CountAsync();
            var empCount = await _context.Employees.CountAsync();

            var unused = new Tuple<int, int>(0, 0);

            for (int i = 1; i <= locCount; i++)
            {
                for (int j = 1; j <= empCount; j++)
                {
                    if (!await _context.Managements.AnyAsync(m =>
                         m.LocationId == i &&
                         m.ManagerId == j))
                    {
                        unused = new Tuple<int, int>(i, j);
                        break;
                    }
                }
                if (unused.Item1 != 0) break;
            }

            var management = new Management
            {
                LocationId = unused.Item1,
                ManagerId = unused.Item2
            };
            await _context.Managements.AddAsync(management);
            await _context.SaveChangesAsync();

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + manId);
            var getRes = await _client.GetAsync(_client.BaseAddress + "/" + manId);

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            getRes.IsSuccessStatusCode.Should().BeFalse();
            getRes.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task DeleteManagement_NotLast_ShouldSwitchWithLastThenDelThat()
        {
            // Arrange
            var manId = GenBadManagementId;

            var locCount = await _context.Locations.CountAsync();
            var empCount = await _context.Employees.CountAsync();

            var unused1 = new Tuple<int, int>(0, 0);
            var unused2 = new Tuple<int, int>(0, 0);

            for (int i = 1; i <= locCount; i++)
            {
                for (int j = 1; j <= empCount; j++)
                {
                    if (!await _context.Managements.AnyAsync(m =>
                         m.LocationId == i &&
                         m.ManagerId == j))
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

            var man1 = new Management
            {
                LocationId = unused1.Item1,
                ManagerId = unused1.Item2
            };
            var man2 = new Management
            {
                LocationId = unused2.Item1,
                ManagerId = unused2.Item2
            };
            await _context.Managements.AddRangeAsync(new Management[] { man1, man2 });
            await _context.SaveChangesAsync();

            // Act
            var result = await _client.DeleteAsync(_client.BaseAddress + "/" + manId);
            var getRes = await _client.GetAsync(_client.BaseAddress + "/" + manId + "/basic");
            var getCont = await getRes.Content.ReadAsStringAsync();
            var getItm = JsonConvert.DeserializeObject(getCont, typeof(Management)) as Management;

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            getItm.LocationId.Should().Be(man2.LocationId);
            getItm.ManagerId.Should().Be(man2.ManagerId);

            // Clean-up
            await _client.DeleteAsync(_client.BaseAddress + "/" + manId);
        }

        #endregion

        #region Creates

        [TestMethod]
        public async Task CreateManagement_HappyFlow_ShouldCreateAndReturnManagement()
        {
            // Arrange
            var empId = await _context.Employees.CountAsync() + 1;
            var locId = await _context.Locations.CountAsync() + 1;
            var manId = await _context.Managements.CountAsync() + 1;

            var emp = new Employee
            {
                FirstName = "Delete",
                LastName = "Me",
                StartedOn = DateTime.Today.AddDays(-3),
                LocationId = locId,
                PositionId = 1,
                WeeklyHours = 34,
            };
            var loc = new Location
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "YY",
                ScheduleId = 1,
                OpenSince = DateTime.Today.AddDays(-5),
                Country = "Delete Me",
                State = "Capous",
                Street = "10 Temporary Avenue",
                City = "Termina",
                MenuId = 1
            };
            var man = new Management
            {
                ManagerId = empId,
                LocationId = locId
            };

            var manCont = new StringContent(JsonConvert.SerializeObject(man),
                Encoding.UTF8, "application/json");

            await _context.Locations.AddAsync(loc);
            await _context.SaveChangesAsync();
            await _context.Employees.AddAsync(emp);
            await _context.SaveChangesAsync();

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, manCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();

            // Clean-up
            await _client.DeleteAsync(_client.BaseAddress + "/" + manId);
            await _client.DeleteAsync("http://localhost/api/employees" + "/" + empId);
            await _client.DeleteAsync("http://localhost/api/locations" + "/" + locId);
        }

        [TestMethod]
        public async Task CreateManagement_TriesToSetId_ShouldReturnBadRequest()
        {
            // Arrange
            var empId = await _context.Employees.CountAsync() + 1;
            var locId = await _context.Locations.CountAsync() + 1;
            var manId = await _context.Managements.CountAsync() + 1;

            var emp = new Employee
            {
                FirstName = "Delete",
                LastName = "Me",
                StartedOn = DateTime.Today.AddDays(-3),
                LocationId = locId,
                PositionId = 1,
                WeeklyHours = 34,
            };
            var loc = new Location
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "YY",
                ScheduleId = 1,
                OpenSince = DateTime.Today.AddDays(-5),
                Country = "Delete Me",
                State = "Capous",
                Street = "10 Temporary Avenue",
                City = "Termina",
                MenuId = 1
            };
            var man = new Management
            {
                ManagerId = empId,
                LocationId = locId,
                ManagementId = manId
            };

            var manCont = new StringContent(JsonConvert.SerializeObject(man),
                Encoding.UTF8, "application/json");

            await _context.Locations.AddAsync(loc);
            await _context.SaveChangesAsync();
            await _context.Employees.AddAsync(emp);
            await _context.SaveChangesAsync();

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, manCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);

            // Clean-up
            await _client.DeleteAsync("http://localhost/api/employees" + "/" + empId);
            await _client.DeleteAsync("http://localhost/api/locations" + "/" + locId);
        }

        [TestMethod]
        public async Task CreateManagement_HasLocation_ShouldReturnBadRequest()
        {
            // Arrange
            var empId = await _context.Employees.CountAsync() + 1;
            var locId = await _context.Locations.CountAsync() + 1;
            var manId = await _context.Managements.CountAsync() + 1;

            var emp = new Employee
            {
                FirstName = "Delete",
                LastName = "Me",
                StartedOn = DateTime.Today.AddDays(-3),
                LocationId = locId,
                PositionId = 1,
                WeeklyHours = 34,
            };
            var loc = new Location
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "YY",
                ScheduleId = 1,
                OpenSince = DateTime.Today.AddDays(-5),
                Country = "Delete Me",
                State = "Capous",
                Street = "10 Temporary Avenue",
                City = "Termina",
                MenuId = 1
            };
            var man = new Management
            {
                ManagerId = empId,
                LocationId = locId,
                Location = new Location()
            };

            var manCont = new StringContent(JsonConvert.SerializeObject(man),
                Encoding.UTF8, "application/json");

            await _context.Locations.AddAsync(loc);
            await _context.SaveChangesAsync();
            await _context.Employees.AddAsync(emp);
            await _context.SaveChangesAsync();

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, manCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);

            // Clean-up
            await _client.DeleteAsync("http://localhost/api/employees" + "/" + empId);
            await _client.DeleteAsync("http://localhost/api/locations" + "/" + locId);
        }

        [TestMethod]
        public async Task CreateManagement_HasManager_ShouldReturnBadRequest()
        {
            // Arrange
            var empId = await _context.Employees.CountAsync() + 1;
            var locId = await _context.Locations.CountAsync() + 1;
            var manId = await _context.Managements.CountAsync() + 1;

            var emp = new Employee
            {
                FirstName = "Delete",
                LastName = "Me",
                StartedOn = DateTime.Today.AddDays(-3),
                LocationId = locId,
                PositionId = 1,
                WeeklyHours = 34,
            };
            var loc = new Location
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "YY",
                ScheduleId = 1,
                OpenSince = DateTime.Today.AddDays(-5),
                Country = "Delete Me",
                State = "Capous",
                Street = "10 Temporary Avenue",
                City = "Termina",
                MenuId = 1
            };
            var man = new Management
            {
                ManagerId = empId,
                LocationId = locId,
                Manager = new Employee()
            };

            var manCont = new StringContent(JsonConvert.SerializeObject(man),
                Encoding.UTF8, "application/json");

            await _context.Locations.AddAsync(loc);
            await _context.SaveChangesAsync();
            await _context.Employees.AddAsync(emp);
            await _context.SaveChangesAsync();

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, manCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);

            // Clean-up
            await _client.DeleteAsync("http://localhost/api/employees" + "/" + empId);
            await _client.DeleteAsync("http://localhost/api/locations" + "/" + locId);
        }

        [TestMethod]
        public async Task CreateManagement_BadLocationId_ShouldReturnBadRequest()
        {
            // Arrange
            var empId = await _context.Employees.CountAsync() + 1;
            var locId = await _context.Locations.CountAsync() + 1;

            var emp = new Employee
            {
                FirstName = "Delete",
                LastName = "Me",
                StartedOn = DateTime.Today.AddDays(-3),
                LocationId = locId,
                PositionId = 1,
                WeeklyHours = 34,
            };
            var loc = new Location
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "YY",
                ScheduleId = 1,
                OpenSince = DateTime.Today.AddDays(-5),
                Country = "Delete Me",
                State = "Capous",
                Street = "10 Temporary Avenue",
                City = "Termina",
                MenuId = 1
            };
            var man = new Management
            {
                ManagerId = empId,
                LocationId = locId + 1
            };

            var manCont = new StringContent(JsonConvert.SerializeObject(man),
                Encoding.UTF8, "application/json");

            await _context.Locations.AddAsync(loc);
            await _context.SaveChangesAsync();
            await _context.Employees.AddAsync(emp);
            await _context.SaveChangesAsync();

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, manCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);

            // Clean-up
            await _client.DeleteAsync("http://localhost/api/employees" + "/" + empId);
            await _client.DeleteAsync("http://localhost/api/locations" + "/" + locId);
        }

        [TestMethod]
        public async Task CreateManagement_BadManagerId_ShouldReturnBadRequest()
        {
            // Arrange
            var empId = await _context.Employees.CountAsync() + 1;
            var locId = await _context.Locations.CountAsync() + 1;
            var manId = await _context.Managements.CountAsync() + 1;

            var emp = new Employee
            {
                FirstName = "Delete",
                LastName = "Me",
                StartedOn = DateTime.Today.AddDays(-3),
                LocationId = locId,
                PositionId = 1,
                WeeklyHours = 34,
            };
            var loc = new Location
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "YY",
                ScheduleId = 1,
                OpenSince = DateTime.Today.AddDays(-5),
                Country = "Delete Me",
                State = "Capous",
                Street = "10 Temporary Avenue",
                City = "Termina",
                MenuId = 1
            };
            var man = new Management
            {
                ManagerId = empId + 1,
                LocationId = locId
            };

            var manCont = new StringContent(JsonConvert.SerializeObject(man),
                Encoding.UTF8, "application/json");

            await _context.Locations.AddAsync(loc);
            await _context.SaveChangesAsync();
            await _context.Employees.AddAsync(emp);
            await _context.SaveChangesAsync();

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, manCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);

            // Clean-up
            await _client.DeleteAsync("http://localhost/api/employees" + "/" + empId);
            await _client.DeleteAsync("http://localhost/api/locations" + "/" + locId);
        }

        [TestMethod]
        public async Task CreateManagement_EmployeeManagesAnotherLocation_ShouldReturnBadRequest()
        {
            // Arrange
            var empId = await _context.Employees.CountAsync() + 1;
            var locId = await _context.Locations.CountAsync() + 1;
            var manId = await _context.Managements.CountAsync() + 1;

            var emp = new Employee
            {
                FirstName = "Delete",
                LastName = "Me",
                StartedOn = DateTime.Today.AddDays(-3),
                LocationId = locId,
                PositionId = 1,
                WeeklyHours = 34,
            };
            var loc = new Location
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "YY",
                ScheduleId = 1,
                OpenSince = DateTime.Today.AddDays(-5),
                Country = "Delete Me",
                State = "Capous",
                Street = "10 Temporary Avenue",
                City = "Termina",
                MenuId = 1
            };
            var loc2 = new Location
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "YY",
                ScheduleId = 1,
                OpenSince = DateTime.Today.AddDays(-5),
                Country = "Delete Me 2",
                State = "Capous",
                Street = "11 Temporary Avenue",
                City = "Termina",
                MenuId = 1
            };
            var man = new Management
            {
                ManagerId = empId,
                LocationId = locId + 1
            };

            var manCont = new StringContent(JsonConvert.SerializeObject(man),
                Encoding.UTF8, "application/json");

            await _context.Locations.AddRangeAsync(new Location[] { loc, loc2 });
            await _context.SaveChangesAsync();
            await _context.Employees.AddAsync(emp);
            await _context.SaveChangesAsync();
            var exMan = new Management
            {
                ManagerId = empId,
                LocationId = locId
            };
            await _context.Managements.AddAsync(exMan);
            await _context.SaveChangesAsync();

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, manCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);

            // Clean-up
            await _client.DeleteAsync(_client.BaseAddress + "/" + manId);
            await _client.DeleteAsync("http://localhost/api/employees" + "/" + empId);
            await _client.DeleteAsync("http://localhost/api/locations" + "/" + (locId + 1));
            await _client.DeleteAsync("http://localhost/api/locations" + "/" + locId);
        }

        [TestMethod]
        public async Task CreateManagement_LocationAlreadyManaged_ShouldReturnBadRequest()
        {
            // Arrange
            var empId = await _context.Employees.CountAsync() + 1;
            var locId = await _context.Locations.CountAsync() + 1;
            var manId = await _context.Managements.CountAsync() + 1;

            var emp = new Employee
            {
                FirstName = "Delete",
                LastName = "Me",
                StartedOn = DateTime.Today.AddDays(-3),
                LocationId = locId,
                PositionId = 1,
                WeeklyHours = 34,
            };
            var emp2 = new Employee
            {
                FirstName = "Delete",
                LastName = "Me2",
                StartedOn = DateTime.Today.AddDays(-3),
                LocationId = locId,
                PositionId = 1,
                WeeklyHours = 34,
            };
            var loc = new Location
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "YY",
                ScheduleId = 1,
                OpenSince = DateTime.Today.AddDays(-5),
                Country = "Delete Me",
                State = "Capous",
                Street = "10 Temporary Avenue",
                City = "Termina",
                MenuId = 1
            };
            var man = new Management
            {
                ManagerId = empId + 1,
                LocationId = locId
            };
            var exMan = new Management
            {
                ManagerId = empId,
                LocationId = locId
            };

            var manCont = new StringContent(JsonConvert.SerializeObject(man),
                Encoding.UTF8, "application/json");

            await _context.Locations.AddAsync(loc);
            await _context.SaveChangesAsync();
            await _context.Employees.AddRangeAsync(new Employee[] { emp, emp2 });
            await _context.SaveChangesAsync();
            await _context.Managements.AddAsync(exMan);
            await _context.SaveChangesAsync();

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, manCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);

            // Clean-up
            var res1 = await _client.DeleteAsync(_client.BaseAddress + "/" + manId);
            var res2 = await _client.DeleteAsync("http://localhost/api/employees" + "/" + empId);
            var res3 = await _client.DeleteAsync("http://localhost/api/employees" + "/" + empId);
            var res4 = await _client.DeleteAsync("http://localhost/api/locations" + "/" + locId);

            res1.IsSuccessStatusCode.Should().BeTrue();
            res2.IsSuccessStatusCode.Should().BeTrue();
            res3.IsSuccessStatusCode.Should().BeTrue();
            res4.IsSuccessStatusCode.Should().BeTrue();
        }

        [TestMethod]
        public async Task CreateManagement_ManagerWithWrongPosition_ShouldReturnBadRequest()
        {
            // Arrange
            var empId = await _context.Employees.CountAsync() + 1;
            var locId = await _context.Locations.CountAsync() + 1;
            var manId = await _context.Managements.CountAsync() + 1;

            var emp = new Employee
            {
                FirstName = "Delete",
                LastName = "Me",
                StartedOn = DateTime.Today.AddDays(-3),
                LocationId = locId,
                PositionId = 2,
                WeeklyHours = 34
            };
            var loc = new Location
            {
                AbreviatedCountry = "XX",
                AbreviatedState = "YY",
                ScheduleId = 1,
                OpenSince = DateTime.Today.AddDays(-5),
                Country = "Delete Me",
                State = "Capous",
                Street = "10 Temporary Avenue",
                City = "Termina",
                MenuId = 1
            };
            var man = new Management
            {
                ManagerId = empId,
                LocationId = locId,
                ManagementId = manId
            };

            var manCont = new StringContent(JsonConvert.SerializeObject(man),
                Encoding.UTF8, "application/json");

            await _context.Locations.AddAsync(loc);
            await _context.SaveChangesAsync();
            await _context.Employees.AddAsync(emp);
            await _context.SaveChangesAsync();

            // Act
            var result = await _client.PostAsync(_client.BaseAddress, manCont);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.BadRequest);

            // Clean-up
            await _client.DeleteAsync("http://localhost/api/employees" + "/" + empId);
            await _client.DeleteAsync("http://localhost/api/locations" + "/" + locId);
        }

        #endregion

        #region Gets

        [TestMethod]
        public async Task GetManagements_HappyFlow_ShouldReturnAllManagements()
        {
            // Arrange
            var managements = await _context.Managements.ToArrayAsync();

            var locIds = managements.Select(m => m.LocationId).Distinct().ToArray();
            var empIds = managements.Select(m => m.ManagerId).Distinct().ToArray();

            var locations = await _context.Locations
                .Where(l => locIds.Contains(l.LocationId))
                .ToArrayAsync();

            var employees = await _context.Employees
                .Where(e => empIds.Contains(e.EmployeeId))
                .Select(e => new { e.EmployeeId, e.FirstName, e.LastName })
                .ToArrayAsync();

            var expected = new string[managements.Length];
            for (int i = 0; i < managements.Length; i++)
            {
                var emp = employees.SingleOrDefault(e => e.EmployeeId == managements[i].ManagerId);
                var loc = locations.SingleOrDefault(l => l.LocationId == managements[i].LocationId);

                expected[i] = string.Format("Management [{0}]: ({1}) {2}, {3} manages ({4}) {5}, {6}{7}, {8}",
                    managements[i].ManagementId,
                    managements[i].ManagerId,
                    emp.LastName, emp.FirstName,
                    managements[i].LocationId,
                    loc.AbreviatedCountry,
                    loc.AbreviatedState == "N/A" ? string.Empty : loc.AbreviatedState + ", ",
                    loc.City, loc.Street);
            }

            // Act
            var result = await _client.GetAsync(string.Empty);
            var content = await result.Content.ReadAsStringAsync();
            var links = JsonConvert.DeserializeObject(content, typeof(List<string>)) as List<string>;

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            links.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetManagement_HappyFlow_ShouldReturnManagement()
        {
            // Arrange
            var manId = GetFirstManagementId;
            var management = await _context.Managements.FindAsync(manId);

            var employee = await _context.Employees.FindAsync(management.ManagerId);
            var location = await _context.Locations.FindAsync(management.LocationId);

            var expected = string.Format("({0}) {1}, {2} manages ({3}) {4}, {5}{6}, {7}",
                    management.ManagerId,
                    employee.LastName, employee.FirstName,
                    management.LocationId,
                    location.AbreviatedCountry,
                    location.AbreviatedState == "N/A" ? string.Empty :
                    location.AbreviatedState + ", ",
                    location.City,
                    location.Street);


            // Act
            var result = await _client.GetAsync(_client.BaseAddress + "/" + manId);
            var content = await result.Content.ReadAsStringAsync();

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            content.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetManagement_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var badId = GenBadManagementId;

            // Act
            var result = await _client.GetAsync(_client.BaseAddress + "/" + badId);

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task GetManagementBasic_HappyFlow_ShouldReturnRequirement()
        {
            // Arrange
            var manId = GetFirstManagementId;
            var expected = await _context.Managements.FindAsync(manId);

            // Act
            var result = await _client.GetAsync(_client.BaseAddress + "/" + manId + "/basic");
            var content = await result.Content.ReadAsStringAsync();
            var man = JsonConvert.DeserializeObject(content, typeof(Management)) as Management;

            // Assert
            result.IsSuccessStatusCode.Should().BeTrue();
            man.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetManagementBasic_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var badId = GenBadManagementId;

            // Act
            var result = await _client.GetAsync(_client.BaseAddress + "/" + badId + "/basic");

            // Assert
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().BeEquivalentTo(HttpStatusCode.NotFound);
        }

        #endregion

        //Heplers

        private int GenBadManagementId => _context.Managements.Count() + 1;

        private int GetFirstManagementId => _context.Managements.First().ManagementId;
    }
}
