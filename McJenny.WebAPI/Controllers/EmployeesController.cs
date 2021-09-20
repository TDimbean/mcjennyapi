using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using McJenny.WebAPI.Data.Models;
using McJenny.WebAPI.Helpers;
using Microsoft.Extensions.Logging;
using System.Text;

namespace McJenny.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeesController : ControllerBase
    {
        private readonly FoodChainsDbContext _context;
        private readonly ILogger<EmployeesController> _logger;
        
        public EmployeesController(FoodChainsDbContext context, 
            ILogger<EmployeesController> logger)
        {
            _context = context;
            _logger = logger;
            _logger.LogDebug("Logger injected into Employees Controller.");
        }

        #region Gets

        #region Basic

        /// <summary>
        /// Get Employees
        /// </summary>
        /// <remarks>
        /// Returns a collection of all Employees(20 max, unless specified otherwise with query string).
        /// Each item in the collection contins the Employee Id,
        /// First and Last Names, Title of Position, Date when the employee started working,
        /// the Id of the Location where the Employee works and the number of hours the 
        /// Employee works each week
        /// </remarks>
        // GET: api/Employees
        [HttpGet]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetEmployees()
            => await GetEmployeesPaged(1, 20);

        /// <summary>
        /// Get Employee Basic
        /// </summary>
        /// <param name="id">Id of Desired Employee</param>
        /// <remarks>
        /// Returns the Employee, whose Employee Id corresponds to the given Id parameter.
        /// The returned object contains the Employee Id, First and Last Names,
        /// Weekly Hours, Date of employment, the Id of the Location where the employee works,
        /// An empty Managements collection and empty Position and Location objects.
        /// If no employee with given Id exists, Not Found is returned instead
        /// </remarks>
        //GET: api/employees/5
        [HttpGet("{id}/basic")]
        public async Task<ActionResult<Employee>> GetEmployeeBasic(int id)
        {
            _logger.LogDebug("Employees Controller fetching Employee with ID: " + id);

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                _logger.LogDebug("Employees Controller found no Employee with ID: " + id);
                return NotFound();
            }

            _logger.LogDebug("Employees Controller returning Employee with ID: " + id);
            return employee;
        }

        /// <summary>
        /// Get Employee
        /// </summary>
        /// <param name="id">Id of Desired Employee</param>
        /// <remarks>
        /// Returns the Employee, whose Employee Id corresponds to the given Id parameter.
        /// The returned object contains the Employee Id, First and Last Names,
        /// Weekly Hours, Date of employment, the Id of the Location where the employee works,
        /// the Title of the Employee's position, their weekly salary,
        /// An empty Managements collection and empty Position and Location objects.
        /// If no employee with given Id exists, Not Found is returned instead
        /// </remarks>
        // GET: api/Employees/5
        [HttpGet("{id}")]
        public async Task<ActionResult<dynamic>> GetEmployee(int id)
        {
            _logger.LogDebug("Employees Controller fetching Employee with ID: " + id);
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                _logger.LogDebug("Employees Controller found no Employee with ID: " + id);
                return NotFound();
            }

            var position = await _context.Positions.SingleOrDefaultAsync(p => p.PositionId == employee.PositionId);

            _logger.LogDebug("Employees Controller returning Employee with ID: " + id);
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

        /// <summary>
        /// Get Employee Salary
        /// </summary>
        /// <param name="id">Id of Employee</param>
        /// <remarks>
        /// Returns a string collection describing the Weekly, Monthly and Early
        /// Salaries of the Employee whose Id matches the given parameter
        /// If no Employee with given Id exists, Not Found is returned instead.
        /// </remarks>
        // GET: api/Employees/5/salary
        [HttpGet("{id}/salary")]
        public async Task<ActionResult<dynamic>> GetSalary(int id)
        {
            _logger.LogDebug("Employee Controller fetching Salary for Employee with ID: " + id);
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                _logger.LogDebug("Employee Controller found no Employee with ID: " + id);
                return NotFound();
            }

            var wage = (await _context.Positions.FindAsync(employee.PositionId)).Wage;

            _logger.LogDebug("Employee Controller returnin Salary for Employee with ID: " + id);
            return new
            {
                Weekly = string.Format("{0:C}", wage * employee.WeeklyHours),
                Monthly = string.Format("{0:C}", wage * employee.WeeklyHours * 4),
                Yearly = string.Format("{0:C}", wage * employee.WeeklyHours * 52)
            };
        }

        #endregion

        #region Advanced

        /// <summary>
        /// Get Employees Queried
        /// </summary>
        /// <remarks>
        /// Returns a list of Employees according to the specified query
        /// Query Keywords: Filter, Started(Optional: Before), SortBy(Optional: Desc), PgInd and PgSz.
        /// Keywords are followed by '=', a string parameter, and, if further keywords
        /// are to be added afterwards, the Ampersand Separator
        /// Filter: Only return Employees where the First Name,
        /// Last Name or Position Title contains the given string.
        /// SortBy: Employees will be ordered in ascending order by the given parameter;
        /// 'FirstName', 'LastName', 'Title'/'Position' and 'Location' are valid Sort Targets.
        /// Any other parameter will result in an ID sort.
        /// Desc: If SortBy is used, and the string parameter is 'true',
        /// Employees wil be returned in Descending Order.
        /// PgInd: If the following int is greater than 0 and the PgSz 
        /// keyword is also present, Employees will be paged and
        /// page 'PgInd' of size 'PgSz' will be returned.
        /// PgSz: If the following int is greater than 0 and the PgInd 
        /// keyword is also present, Employees will be paged and
        /// page 'PgInd' of size 'PgSz' will be returned.
        /// Started: Only returns Employees that started working after the
        /// following parameter, which must parse to a valid Date
        /// Before: If Started is used and the following parameter is 'true'
        /// , only Employees that started working before the Started Parameter
        /// will be returned.
        /// Any of the Keywords can be used or ommited.
        /// Using a keyword more than once will result in only the occurence being recognized.
        /// If a keyword is followed by an incorrectly formatted parameter, the keyword will be ignored.
        /// Order of Query Operations: Started > Filter > Sort > Pagination
        /// Any improperly formatted text will be ignored.
        /// </remarks>
        // GET: api/Employees/query:{query}
        [HttpGet("query:{query}")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetEmployeesQueried(string query)
        {
            _logger.LogDebug("Employees Controller looking up query...");
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

            var returnString = new StringBuilder(!isPaged ? string.Empty :
                    "Paginated: Page " + pgInd + " of Size " + pgSz);
            if (isSorted)
            {
                if (returnString.Length > 0) returnString.Insert(0, " and ");
                returnString.Insert(0, "Sorted: By " + sortBy + " In " +
                    (desc ? "Descending" : "Ascending") + " Order");
            }
            if (isFiltered)
            {
                if (isSorted && isPaged) returnString.Insert(0, ", ");
                else if (isSorted|| isPaged) returnString.Insert(0, " and ");
                returnString.Insert(0, "Filtered By: " + filter);
            }
            if(isDated)
            {
                if ((isFiltered && isPaged) || (isFiltered && isSorted) || (isSorted && isPaged))
                    returnString.Insert(0, ", ");
                else if (isFiltered || isPaged || isSorted) returnString.Insert(0, " and ");
                returnString.Insert(0, "Started: " + (before ? "Before" : "After") + ": " + since);
            }
            _logger.LogDebug("Employees Controller returning " +
                (!isPaged && !isSorted && !isFiltered && !isDated? "all " : string.Empty) +
                "Employees" +
                (returnString.Length == 0 ? string.Empty :
                " " + returnString.ToString()) + ".");

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


        [NonAction]
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


        [NonAction]
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


        [NonAction]
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


        [NonAction]
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

        /// <summary>
        /// Update Employee
        /// </summary>
        /// <param name="id">The Id of the target Employee</param>
        /// <param name="employee">An Employee object whose properties are to be transfered 
        /// to the existing Employee</param>
        /// <remarks>
        /// Updates the values of an existing Employee, whose Id corresponds to the given parameter,
        /// using the values from the 'employee' parameter.
        /// Only the First Name, Last Name, Location Id, Position Id, Weekly Hours
        /// and Starting Date of the Employee may be updated through the API.
        /// The Location and Position is automatically changed by changing the 
        /// LocationId and PositionId values.
        /// For its Management relationship the Managements API 
        /// Controller must be used.
        /// The Employee Id may not be updated.
        /// The given parameter of type Employee may contain a First Name,
        /// Last Name, Location Id, Position Id, Weekly Hours and
        /// Starting Date.
        /// If any of these are their data type's default value they will
        /// be ignored.
        /// Additionally empty objects for the Position and Location, as well
        /// as an empty collection of Managements must be provided, along
        /// with an Employee Id of 0
        /// The Employee Id Parameter must correspond to an existing Employee, otherwise
        /// Not Found is returned
        /// If the Employee Parameter is incorrect, Bad Request is returned
        /// Employee parameter errors:
        /// (1) The Employee Id is not 0, 
        /// (2) The First Name or Last Name strings are empty,
        /// (3) The First Name or Last Name strings are longer than 50 characters,
        /// (4) Starting Date is the default value of DateTime,
        /// (5) Managements is null
        /// (6) Managements is not an empty collection
        /// (7) Location or Position are not null
        /// (8) Weekly Hours is less than 0 or more tha 167
        /// (9) The Location Id does not correspond to a Location
        /// (10) The Position Id does not correspond to a Position
        /// (11) The Position Id corresponds to the 'Manager' position,
        /// but the Location Id corresponds to a Location that already
        /// has a different Employee as Manager.
        /// </remarks>
        // PUT: api/Employees/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEmployee(int id, Employee employee)
        {
            _logger.LogDebug("Employees Controller trying to update Employee with ID: " + id);
            if (!await EmployeeExists(id))
            {
                _logger.LogDebug("Employees Controller found no Eployee with ID: " + id);
                return NotFound();
            }

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
                 && employee.PositionId == 1))
            {
                _logger.LogDebug("Employees Controller determinde provided 'employee'" +
                    " parameter to violate the update constraints.");
                return BadRequest();
            }

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

            _logger.LogDebug("Employees Controller succesfully updated Employee with ID: " + id);
            return NoContent();
        }

        /// <summary>
        /// Create Employee
        /// </summary>
        /// <param name="employee">An Employee object to be added to the Database</param>
        /// <remarks>
        /// Creates a new Employee and inserts it into the Database
        /// using the values from the given 'employee' parameter.
        /// Only the First Name, Last Name, Position Id, Location Id, Weekly Hours
        /// and Starting Date of the Employee may be set.
        /// Its Position and Location will be automatically determined by the
        /// given Position Id and Location Id.
        /// For its Management relationship the Managements API 
        /// Controller must be used.
        /// The Employee Id may not be set. The Databse will automatically
        /// assign it a new Id, equivalent to the last Employee Id in the Database + 1.
        /// The given parameter of type Employee must contain a First Name,
        /// Last Name, Location Id, Position Id, Weekly Hours and Starting Date.
        /// Additionally empty objects for the Position and Location, as well as
        /// an empty collection
        /// of Managements and an Employee Id of 0.
        /// If the Employee parameter is incorrect, Bad Request is returned
        /// Employee parameter errors:
        /// (1) The Employee Id is not 0, 
        /// (2) The First Name or Last Name strings are empty,
        /// (3) The First Name or Last Name strings are longer than 50 characters,
        /// (4) The Starting Date is equivalent to the default value of DateTime,
        /// (5) Managements is null,
        /// (6) Managements is not empty collections.
        /// (7) Weekly Hours are less than 0 or more than 167,
        /// (8) Position or Location are not empty objects,
        /// (9) The Position Id does not correspond to an existing Position,
        /// (10) The Location Id does not correspond to an existing Location,
        /// (11) The Position Id corresponds to the 'Manager' position,
        /// but the Location Id corresponds to a Location with
        /// a different Employee as its Manager.
        /// </remarks>
        // POST: api/Employees
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPost]
        public async Task<ActionResult<Employee>> CreateEmployee(Employee employee)
        {
            _logger.LogDebug("Employees Controller trying to create new Employee...");

            // Validation

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
                (await _context.Managements.AnyAsync(m => m.LocationId == employee.LocationId))))
            {
                _logger.LogDebug("Employees controller found provided 'employee' " +
                    "parameter to violate creation constraints.");
                return BadRequest();
            }

            //if (employee.PositionId==1)
            //{
            //    var location = await _context.Locations.FindAsync(employee.LocationId);
            //    if (location.Managements.Count != 0) return BadRequest();
            //}

            // Validation

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Employees Controller successfully created Employee " +
                "at ID: " + employee.EmployeeId);
            //return CreatedAtAction("GetEmployeeBasic", new { id = employee.EmployeeId }, employee);
            return Ok();
        }

        /// <summary>
        /// Delete Employee
        /// </summary>
        /// <param name="id">Id of target Employee</param>
        /// <remarks>
        /// Removes an Employee from the Database.
        /// If there is no Employee, whose Id matches the given Id parameter, 
        /// Not Found is returned.
        /// If the desired Employee is the last in the Database, and it has no
        /// Management relationship, it is removed from the Database
        /// and its Employee Id will be used by the next Employee to be inserted.
        /// If the desired Employee is not the last in the Database, and it has no
        /// Management relationship, it is swapped with the last
        /// Employee in the Database before it is removed, giving the last Employee in the Database
        /// the Id of the deleted Employee and freeing up the last Employee's Id for
        /// the next Employee to be inserted.
        /// If the desired Employee has a Management relationship,
        /// it is turned into a blank Employee, so as not to violate its relationship.
        /// Its First and Last Names will be changed to reflect that fact.
        /// </remarks>
        // DELETE: api/Employees/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<Employee>> DeleteEmployee(int id)
        {
            _logger.LogDebug("Employees Controller deleting Employee with ID: " + id);

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                _logger.LogDebug("Employees Controller found no Employee with ID: " + id);
                return NotFound();
            }

            // If it has Management, turn it into a blank employee
            if (await _context.Managements.AnyAsync(m => m.ManagerId== id))
            {
                _logger.LogDebug("Employee Controller found Employee with ID: " + id +
                    " to contain a Management relationship and will turn it into " +
                    "a blank Employee rather than deleting it.");
                employee.FirstName = "Missing";
                employee.LastName = DateTime.Now.ToShortDateString() + " - " + id;
                employee.LocationId = 1;
                employee.PositionId = 2;
                employee.WeeklyHours = 0;
                employee.StartedOn = new DateTime();
                await _context.SaveChangesAsync();
                _logger.LogDebug("Employee Controller turned Employee with ID: " + id + " " +
                    "into a blank Employee.");
                return Ok();
            }

            var lastEmpId = await _context.Employees.CountAsync();

            if (id == lastEmpId)/*If it's last, delete it*/
            {
                _logger.LogDebug("Employee Controller found Employee with ID: " + id +
                    " to have no Management relationship and be last in the database. " +
                    "As such it will delete it.");
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
                await _context.Database.ExecuteSqlRawAsync
                    ("DBCC CHECKIDENT('[Employees]', RESEED, " + (lastEmpId - 1) + ")");
                await _context.SaveChangesAsync();
                _logger.LogDebug("Employee Controller successfully deleted Employee with ID: " + id);
                return Ok();
            }

            //If it's not last, swap it with last and del that

            _logger.LogDebug("Employees Controller found that Employee with ID: " + id + " has no " +
                "Management relationship and appears before the last Employee. It will switch places "+
                "with the last Employee and delete that one instead.");
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
            _logger.LogDebug("Employees Controller succesfully turned Employee with ID: " + id
                + " into the last Employee, and removed the last Employee from its position.");
            return Ok();
        }

        [NonAction]
        private async Task<bool> EmployeeExists(int id)
            => (await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == id)) != null;
    }
}
