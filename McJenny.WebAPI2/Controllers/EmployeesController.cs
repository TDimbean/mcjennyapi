using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using McJenny.WebAPI.Data.Models;
using McJenny.WebAPI.Helpers;

namespace McJenny.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeesController : ControllerBase
    {
        private readonly FoodChainsDbContext _context;

        public EmployeesController(FoodChainsDbContext context) => _context = context;

        #region Gets

        #region Basic

        // GET: api/Employees
        [HttpGet]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetEmployees()
            => await GetEmployeesPaged(1, 20);

        //GET: api/employees/5
        [HttpGet("{id}/basic")]
        public async Task<ActionResult<Employee>> GetEmployeeBasic(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            return employee;
        }

        // GET: api/Employees/5
        [HttpGet("{id}")]
        public async Task<ActionResult<dynamic>> GetEmployee(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            var position = await _context.Positions.SingleOrDefaultAsync(p => p.PositionId == employee.PositionId);

            return new
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
        }

        // GET: api/Employees/5/salary
        [HttpGet("{id}/salary")]
        public async Task<ActionResult<dynamic>> GetSalary(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            var wage = (await _context.Positions.FindAsync(employee.PositionId)).Wage;

            return new
            {
                Weekly = string.Format("{0:C}", wage * employee.WeeklyHours),
                Monthly = string.Format("{0:C}", wage * employee.WeeklyHours * 4),
                Yearly = string.Format("{0:C}", wage * employee.WeeklyHours * 52)
            };
        }

        #endregion

        #region Advanced

        // GET: api/Employees/query:{query}
        [HttpGet("query:{query}")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetEmployeesQueried(string query)
        {
            var quer = query.ToUpper().Replace("QUERY:", string.Empty);

            bool isPaged, isFiltered, isSorted, desc, before, isDated;

            var pgInd = 0;
            var pgSz = 0;
            var filter = string.Empty;
            var sortBy = string.Empty;
            var since = new DateTime();

            isPaged = quer.Contains("PGIND=") && quer.Contains("PGSZ=");
            isFiltered = quer.Contains("FILTER=");
            isSorted = quer.Contains("SORTBY=");
            isDated = quer.Contains("STARTED=");
            desc = quer.Contains("DESC=TRUE");
            before = quer.Contains("BEFORE=TRUE");

            if (isPaged)
            {
                var indStr = quer.QueryToParam("PGIND");
                var szStr = quer.QueryToParam("PGSZ");

                var indParses = int.TryParse(indStr, out pgInd);
                var szParses = int.TryParse(szStr, out pgSz);

                if (!indParses || !szParses) isPaged = false;
                else
                {
                    pgInd = Math.Abs(pgInd);
                    pgSz = pgSz == 0 ? 10 : Math.Abs(pgSz);
                }
            }

            if (isDated)
            {
                var dateStr = quer.QueryToParam("STARTED");

                var dateParses = DateTime.TryParse(dateStr, out since);

                if (!dateParses) isDated = false;
            }

            if (isFiltered) filter = quer.QueryToParam("FILTER");
            if (isSorted) sortBy = quer.QueryToParam("SORTBY");

            return (isPaged, isSorted, isFiltered, isDated)
switch
            {
                (false, false, true, false) => await GetEmployeesFiltered(filter),
                (false, true, false, false) => await GetEmployeesSorted(sortBy, desc),
                (true, false, false, false) => await GetEmployeesPaged(pgInd, pgSz),
                (false, true, true, false) => await GetEmployeesSorted(sortBy, desc,
(await GetEmployeesFiltered(filter)).Value),
                (true, false, true, false) => await GetEmployeesPaged(pgInd, pgSz,
(await GetEmployeesFiltered(filter)).Value),
                (true, true, false, false) => await GetEmployeesPaged(pgInd, pgSz,
(await GetEmployeesSorted(sortBy, desc)).Value),
                (true, true, true, false) => await GetEmployeesPaged(pgInd, pgSz,
(await GetEmployeesSorted(sortBy, desc,
(await GetEmployeesFiltered(filter)).Value)).Value),
                (false, false, false, true) => await GetEmployeesStarted(since, before),
                (false, false, true, true) => await GetEmployeesFiltered(filter,
(await GetEmployeesStarted(since, before)).Value),
                (false, true, false, true) => await GetEmployeesSorted(sortBy, desc,
(await GetEmployeesStarted(since, before)).Value),
                (true, false, false, true) => await GetEmployeesPaged(pgInd, pgSz,
(await GetEmployeesStarted(since, before)).Value),
                (false, true, true, true) => await GetEmployeesSorted(sortBy, desc,
(await GetEmployeesFiltered(filter,
(await GetEmployeesStarted(since, before)).Value)).Value),
                (true, false, true, true) => await GetEmployeesPaged(pgInd, pgSz,
(await GetEmployeesFiltered(filter,
(await GetEmployeesStarted(since, before)).Value)).Value),
                (true, true, false, true) => await GetEmployeesPaged(pgInd, pgSz,
(await GetEmployeesSorted(sortBy, desc,
(await GetEmployeesStarted(since, before)).Value)).Value),
                (true, true, true, true) => await GetEmployeesPaged(pgInd, pgSz,
(await GetEmployeesSorted(sortBy, desc,
(await GetEmployeesFiltered(filter,
(await GetEmployeesStarted(since, before)).Value)).Value)).Value),
                _ => await GetEmployees(),
            };
        }

        public async Task<ActionResult<IEnumerable<dynamic>>> GetEmployeesFiltered(string filter, IEnumerable<dynamic> col = null)
        => col == null ?
                (dynamic)await _context.Employees
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
                 .ToArrayAsync() :
                (dynamic)col
                 .Where(e => e.FirstName.ToUpper()
                     .Contains(filter.ToUpper()) ||
                        e.LastName.ToUpper()
                    .Contains(filter.ToUpper()) ||
                        e.Title.ToUpper().Contains(filter.ToUpper()))
                        .ToArray();

        public async Task<ActionResult<IEnumerable<dynamic>>> GetEmployeesPaged
                            (int pgInd, int pgSz, IEnumerable<dynamic> col = null)
            => col == null ?
            (dynamic)await _context.Employees
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
                .ToArrayAsync() :
            (dynamic)col
                .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArray();

        public async Task<ActionResult<IEnumerable<dynamic>>> GetEmployeesSorted
                (string sortBy, bool? desc, IEnumerable<dynamic> col = null)
            => col == null ?
                sortBy.ToUpper() switch
                {
                    "FIRSTNAME" => desc.GetValueOrDefault() ?
                        (dynamic)await _context.Employees
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
                            .ToArrayAsync() :
                        (dynamic)await _context.Employees
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
                            .ToArrayAsync(),
                    "LASTNAME" => desc.GetValueOrDefault() ?
                        (dynamic)await _context.Employees
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
                            .OrderByDescending(e => e.LastName)
                            .ToArrayAsync() :
                        (dynamic)await _context.Employees
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
                            .ToArrayAsync(),
                    var sort when sort == "POSITION" || sort == "TITLE"
                            => desc.GetValueOrDefault() ?
                        (dynamic)await _context.Employees
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
                            .OrderByDescending(e => e.Title)
                            .ToArrayAsync() :
                        (dynamic)await _context.Employees
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
                            .ToArrayAsync(),
                    "LOCATION" => desc.GetValueOrDefault() ?
                        (dynamic)await _context.Employees
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
                            .OrderByDescending(e => e.LocationId)
                            .ToArrayAsync() :
                        (dynamic)await _context.Employees
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
                            .ToArrayAsync(),
                    _ => desc.GetValueOrDefault() ?
                        (dynamic)await _context.Employees
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
                            .OrderByDescending(e => e.EmployeeId)
                            .ToArrayAsync() :
                        (dynamic)await _context.Employees
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
                            .ToArrayAsync()
                } :
                  sortBy.ToUpper() switch
                  {
                      "FIRSTNAME" => desc.GetValueOrDefault() ?
                          (dynamic)col
                              .OrderByDescending(e => e.FirstName)
                      .ToArray() :
                          (dynamic)col
                              .OrderBy(e => e.FirstName)
                                .ToArray(),
                      "LASTNAME" => desc.GetValueOrDefault() ?
                          (dynamic)col
                              .OrderByDescending(e => e.LastName)
                      .ToArray() :
                          (dynamic)col
                              .OrderBy(e => e.LastName)
                                .ToArray(),
                      "LOCATION" => desc.GetValueOrDefault() ?
                          (dynamic)col
                              .OrderByDescending(e => e.LocationId)
                      .ToArray() :
                          (dynamic)col
                              .OrderBy(e => e.LocationId)
                                .ToArray(),
                      var sort when sort == "POSITION" || sort == "TITLE"
                      => desc.GetValueOrDefault() ?
                          (dynamic)col
                              .OrderByDescending(e => e.Title).ToArray() :
                          (dynamic)col
                              .OrderBy(e => e.Title)
                                .ToArray(),
                      _ => desc.GetValueOrDefault() ?
                          (dynamic)col
                              .OrderByDescending(e => e.EmployeeId)
                      .ToArray() :
                          (dynamic)col
                              .ToArray()
                  };

        public async Task<ActionResult<IEnumerable<dynamic>>> GetEmployeesStarted
                (DateTime since, bool before)
            => await _context.Employees
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
                 .Where(e => before ?
                    e.StartedOn < since :
                    e.StartedOn >= since)
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

        #endregion

        #endregion

        // PUT: api/Employees/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEmployee(int id, Employee employee)
        {
            if (!await EmployeeExists(id)) return NotFound();

            //Validation

            if (employee.EmployeeId != 0 ||
                (!string.IsNullOrEmpty(employee.FirstName) &&
                    (string.IsNullOrWhiteSpace(employee.FirstName) ||
                    employee.FirstName.Length > 50)) ||
                (!string.IsNullOrEmpty(employee.LastName) &&
                    (string.IsNullOrWhiteSpace(employee.LastName) ||
                    employee.LastName.Length > 50)) ||
                employee.StartedOn == new DateTime() ||
                employee.WeeklyHours < 0 ||
                employee.WeeklyHours >= 168 ||
                (employee.LocationId != 0 &&
                    (await _context.Locations.FindAsync(employee.LocationId)) == null) ||
                (employee.PositionId != 0 &&
                    (await _context.Positions.FindAsync(employee.PositionId)) == null) ||
                employee.Managements == null || employee.Managements.Count != 0 ||
                employee.Location != null || employee.Position != null ||
                ((await _context.Locations
                    .Include(l => l.Managements)
                    .FirstOrDefaultAsync(l => l.LocationId == employee.LocationId))
                 .Managements.Any(l => l.ManagementId != id)
                 && employee.PositionId==1))
                return BadRequest();

            //var management = (await _context.Locations
            //    .Include(l => l.Managements)
            //    .FirstOrDefaultAsync(l => l.LocationId == employee.LocationId))
            //    .Managements.SingleOrDefault();

            //if (management!=null && management.ManagerId!=id && employee.PositionId==1)
            //    return BadRequest();

            //Validation

            var oldEmp = await _context.Employees.FindAsync(id);

            if (!string.IsNullOrEmpty(employee.FirstName) &&
                !string.IsNullOrWhiteSpace(employee.FirstName))
                oldEmp.FirstName = employee.FirstName;
            if (!string.IsNullOrEmpty(employee.LastName) &&
                !string.IsNullOrWhiteSpace(employee.LastName))
                oldEmp.LastName = employee.LastName;
            if (employee.PositionId != 0)
                oldEmp.PositionId = employee.PositionId;
            if (employee.LocationId != 0)
                oldEmp.LocationId = employee.LocationId;
            if (employee.StartedOn.Year != 1)
                oldEmp.StartedOn = employee.StartedOn;
            if (employee.WeeklyHours != 0)
                oldEmp.WeeklyHours = employee.WeeklyHours;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/Employees
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPost]
        public async Task<ActionResult<Employee>> CreateEmployee(Employee employee)
        {
            /// Validation

            if (employee.EmployeeId != 0 ||
                string.IsNullOrEmpty(employee.FirstName) ||
                string.IsNullOrWhiteSpace(employee.FirstName) ||
                employee.FirstName.Length > 50 ||
                string.IsNullOrEmpty(employee.LastName) ||
                string.IsNullOrWhiteSpace(employee.LastName) ||
                employee.LastName.Length > 50 ||
                employee.StartedOn.Year == 1 ||
                employee.WeeklyHours <= 0 ||
                employee.WeeklyHours >= 168 ||
                (await _context.Locations.FindAsync(employee.LocationId)) == null ||
                (await _context.Positions.FindAsync(employee.PositionId)) == null ||
                employee.Managements == null || employee.Managements.Count != 0 ||
                employee.Location != null || employee.Position != null ||
                (employee.PositionId == 1 &&
                (await _context.Managements.AnyAsync(m=>m.LocationId==employee.LocationId))))
                return BadRequest();

            //if (employee.PositionId==1)
            //{
            //    var location = await _context.Locations.FindAsync(employee.LocationId);
            //    if (location.Managements.Count != 0) return BadRequest();
            //}

            /// Validation

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            //return CreatedAtAction("GetEmployeeBasic", new { id = employee.EmployeeId }, employee);
            return Ok();
        }

        // DELETE: api/Employees/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<Employee>> DeleteEmployee(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            // If it has Management, turn it into a blank employee
            if (await _context.Managements.AnyAsync(m => m.ManagerId== id))
            {
                employee.FirstName = "Missing";
                employee.LastName = DateTime.Now.ToShortDateString() + " - " + id;
                employee.LocationId = 1;
                employee.PositionId = 2;
                employee.WeeklyHours = 0;
                employee.StartedOn = new DateTime();
                await _context.SaveChangesAsync();
                return Ok();
            }

            var lastEmpId = await _context.Employees.CountAsync();

            if (id == lastEmpId)/*If it's last, delete it*/
            {
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
                await _context.Database.ExecuteSqlRawAsync
                    ("DBCC CHECKIDENT('[Employees]', RESEED, " + (lastEmpId - 1) + ")");
                await _context.SaveChangesAsync();
                return Ok();
            }

            //If it's not last, swap it with last and del that

            /*Migrate Management from Last Employee to empId*/
            var managements = await _context.Managements
                    .Where(m => m.ManagerId == lastEmpId)
                    .ToArrayAsync();

            if (managements.Length>0)
            {
                var management = managements.SingleOrDefault();
                management.ManagerId = id;
                await _context.SaveChangesAsync();
            }

            /*Swap and del last*/
            var lastEmp = await _context.Employees.FindAsync(lastEmpId);
            employee.FirstName = lastEmp.FirstName;
            employee.LastName = lastEmp.LastName;
            employee.LocationId = lastEmp.LocationId;
            employee.PositionId = lastEmp.PositionId;
            employee.StartedOn = lastEmp.StartedOn;
            employee.WeeklyHours = lastEmp.WeeklyHours;

            _context.Employees.Remove(lastEmp);
            await _context.SaveChangesAsync();
            await _context.Database.ExecuteSqlRawAsync
                ("DBCC CHECKIDENT('[Employees]', RESEED, " + (lastEmpId - 1) + ")");
            await _context.SaveChangesAsync();
            return Ok();
        }

        private async Task<bool> EmployeeExists(int id)
            => (await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == id)) != null;
    }
}
