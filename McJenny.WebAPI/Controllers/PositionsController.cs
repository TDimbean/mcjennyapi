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
    public class PositionsController : ControllerBase
    {
        private readonly FoodChainsDbContext _context;
        private readonly ILogger<PositionsController> _logger;

        public PositionsController(FoodChainsDbContext context,
            ILogger<PositionsController> logger)
        {
            _context = context;
            _logger = logger;
            _logger.LogDebug("Logger injected into Positions Controller.");
        }

        #region Gets

        #region Basic

        /// <summary>
        /// Get Positions
        /// </summary>
        /// <remarks>
        /// Returns a collection of all Positions, formatted to includ
        /// Position Id, Title and Hourly Wage.
        /// </remarks>
        // GET: api/Positions
        [HttpGet]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetPositions()
        {
            _logger.LogDebug("Positions Controller fetching all Positions(20 max, " +
                "unless queried otherwise.)");

            return await _context.Positions
            .Select(p => new
            {
                ID = p.PositionId,
                p.Title,
                HourlyWage = string.Format("{0:C}", p.Wage)
            })
            .Take(20)
            .ToListAsync();
        }

        /// <summary>
        /// Get Position
        /// </summary>
        /// <param name="id">Id of desired Position</param>
        /// <remarks>
        /// Returns the Title and Hourly Wage of the Position whose Id
        /// matches the 'id' parameter.
        /// If no such Position exists, Not Found is returned instead.
        /// </remarks>
        // GET: api/Positions/5
        [HttpGet("{id}")]
        public async Task<ActionResult<dynamic>> GetPosition(int id)
        {
            _logger.LogDebug("Positions Controller fetching Position with ID: " + id);
            if (!await PositionExists(id))
            {
                _logger.LogDebug("Positions Controller returning formated " +
                    "Position with ID:" + id);
                return NotFound();
            }

            _logger.LogDebug("Positions Controller returning formated" +
                "Position with ID: " + id);
            return (await _context.Positions
                .Select(p => new
                {
                    p.PositionId,
                    res = new
                    {
                        p.Title,
                        HourlyWage = string.Format("{0:C}", p.Wage)
                    }
                })
                .FirstOrDefaultAsync(p => p.PositionId == id)).res;
        }

        /// <summary>
        /// Get Position Basic
        /// </summary>
        /// <param name="id">Id of desired Position</param>
        /// <remarks>
        /// Returns a Position object whose Position Id matches the
        /// given 'id' parameter. It contains the Position Id, Title,
        /// Hourly Wage and an empty list of Employees.
        /// If no Position whose Id matches the 'id' parameter exists,
        /// Not Found is returned instead.
        /// </remarks>
        // GET: api/Positions/5/basic
        [HttpGet("{id}/basic")]
        public async Task<ActionResult<Position>> GetPositionBasic(int id)
        {
            _logger.LogDebug("Positions Controller fetching Position with ID: " + id);
            var position = await _context.Positions.FindAsync(id);
            if (position == null)
            {
                _logger.LogDebug("Positions Controller found no Position " +
                    "with ID: " + id);
                return NotFound();
            }

            _logger.LogDebug("Positions Controller returning basic Position with ID: " + id);
            return position;
        }

        /// <summary>
        /// Get Position Employees
        /// </summary>
        /// <param name="id">Id of desired Position</param>
        /// <remarks>
        /// Returns a collection of strings identifying
        /// the Employees who occupy the Position whose Id matches
        /// the given 'id' parameter, formated to incud each Employees
        /// Id, as well as the First and Last Names.
        /// If no Position with specified Id exists,
        /// Not Found is returned instead.
        /// </remarks>
        // GET: api/Positions/5/employees
        [HttpGet("{id}/employees")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetEmployees(int id)
        {
            _logger.LogDebug("Positions Controller fetching Employees " +
                "for Position with ID: " + id);
            var position = await _context.Positions.FindAsync(id);
            if (position == null)
            {
                _logger.LogDebug("Positions Controller found no Position with ID: " + id);
                return NotFound();
            }

            _logger.LogDebug("Positions Controller returning Employees for " +
                "Position with ID: " + id);
            return await _context.Employees
                .Where(e => e.PositionId == id)
                .Select(e => string.Format("({0}) {1}, {2}", e.EmployeeId, e.LastName, e.FirstName))
                .ToArrayAsync();
        }

        #endregion

        #region Advanced

        /// <summary>
        /// Get Positions Queried
        /// </summary>
        /// <param name="query">Query String</param>
        /// <remarks>
        /// Returns a list of Positions according to the specified query
        /// Query Keywords: Filter, SortBy(Optional: Desc), PgInd and PgSz.
        /// Keywords are followed by '=', a string parameter, and, if further keywords
        /// are to be added afterwards, the Ampersand Separator
        /// Filter: Only return Positions where the Title
        /// contains the given string.
        /// SortBy: Positions will be ordered in ascending order by a criteria
        /// matching the string parameter.
        /// Sort Options: 'Title'/'Name' and 'Wage'/'Salary'.
        /// If the string parameter is anything else, the Positions
        /// will be sorted by their Position IDs.
        /// Desc: If SortBy is used, and the string parameter is 'true',
        /// Positions wil be returned in Descending Order.
        /// PgInd: If the following int is greater than 0 and the PgSz 
        /// keyword is also present, Positions will be paged and
        /// page 'PgInd' of size 'PgSz' will be returned.
        /// PgSz: If the following int is greater than 0 and the PgInd 
        /// keyword is also present, Positions will be paged and
        /// page 'PgInd' of size 'PgSz' will be returned.
        /// Any of the Keywords can be used or ommited.
        /// Using a keyword more than once will result in only the first occurence being recognized.
        /// If a keyword is followed by an incorrectly formatted parameter, the keyword will be ignored.
        /// Order of Query Operations: Filter > Sort > Pagination
        /// Any improperly formatted text will be ignored.
        /// </remarks>
        // GET: api/Positions/query
        [HttpGet("query:{query}")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetPositionsQueried(string query)
        {
            _logger.LogDebug("Positions Controller looking up query...");

            var quer = query.ToUpper().Replace("QUERY:", string.Empty);

            bool isPaged, isFiltered, isSorted, desc;

            var pgInd = 0;
            var pgSz = 0;
            var filter = string.Empty;
            var sortBy = string.Empty;

            isPaged = quer.Contains("PGIND=") && quer.Contains("PGSZ=");
            isFiltered = quer.Contains("FILTER=");
            isSorted = quer.Contains("SORTBY=");
            desc = quer.Contains("DESC=TRUE");

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
                else if (isFiltered || isPaged) returnString.Insert(0, " and ");
                returnString.Insert(0, "Filtered By: " + filter);
            }
            _logger.LogDebug("Positions Controller returning " +
                (!isPaged && !isSorted && !isFiltered  ? "all " : string.Empty) +
                "Positions" +
                (returnString.Length == 0 ? string.Empty :
                " " + returnString.ToString()) + ".");

            return (isPaged, isSorted, isFiltered)
            switch
            {
                (false, false, true) => await GetPositionsFiltered(filter),
                (false, true, false) => await GetPositionsSorted(sortBy, desc),
                (true, false, false) => await GetPositionsPaged(pgInd, pgSz),
                (false, true, true) => await GetPositionsSorted(sortBy, desc,
                    (await GetPositionsFiltered(filter)).Value),
                (true, false, true) => await GetPositionsPaged(pgInd, pgSz,
                    (await GetPositionsFiltered(filter)).Value),
                (true, true, false) => await GetPositionsPaged(pgInd, pgSz,
                    (await GetPositionsSorted(sortBy, desc)).Value),
                (true, true, true) => await GetPositionsPaged(pgInd, pgSz,
                    (await GetPositionsSorted(sortBy, desc,
                    (await GetPositionsFiltered(filter)).Value)).Value),
                _ => await GetPositions(),
            };
        }

        [NonAction]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetPositionsFiltered(string filter, IEnumerable<dynamic> col = null)
        => col == null ?
                (dynamic)await _context.Positions
                 .Select(p => new { p.PositionId, p.Title, p.Wage })
                 .Where(p => p.Title.ToUpper()
                     .Contains(filter.ToUpper()))
                 .ToArrayAsync() :
                (dynamic)col
                    .Where(p => p.Title.ToUpper()
                     .Contains(filter.ToUpper()))
                        .ToArray();

        [NonAction]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetPositionsPaged(int pgInd, int pgSz, IEnumerable<dynamic> col = null)
            => col == null ?
            (dynamic)await _context.Positions
                .Select(p => new { p.PositionId, p.Title, p.Wage })
                .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArrayAsync() :
            (dynamic)col
                .Select(p => new { p.PositionId, p.Title, p.Wage })
                .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArray();

        [NonAction]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetPositionsSorted(string sortBy, bool? desc, IEnumerable<dynamic> col = null)
            => col == null ?
                sortBy.ToUpper() switch
                {
                    var sort when sort == "TITLE" || sort == "NAME"
                    => desc.GetValueOrDefault() ?
                        (dynamic)await _context.Positions
                            .Select(p => new { p.PositionId, p.Title, p.Wage })
                            .OrderByDescending(p => p.Title)
                    .ToArrayAsync() :
                        (dynamic)await _context.Positions
                            .Select(p => new { p.PositionId, p.Title, p.Wage })
                            .OrderBy(p => p.Title)
                    .ToArrayAsync(),
                    var sort when sort == "WAGE" || sort == "SALARY"
                    => desc.GetValueOrDefault() ?
                        (dynamic)await _context.Positions
                            .Select(p => new { p.PositionId, p.Title, p.Wage })
                            .OrderByDescending(p => p.Wage)
                    .ToArrayAsync() :
                        (dynamic)await _context.Positions
                            .Select(p => new { p.PositionId, p.Title, p.Wage })
                            .OrderBy(p => p.Wage)
                    .ToArrayAsync(),
                    _ => desc.GetValueOrDefault() ?
                        (dynamic)await _context.Positions
                            .Select(p => new { p.PositionId, p.Title, p.Wage })
                            .OrderByDescending(p => p.PositionId).ToArrayAsync() :
                        (dynamic)await _context.Positions
                            .Select(p => new { p.PositionId, p.Title, p.Wage })
                            .ToArrayAsync()
                } :
                  sortBy.ToUpper() switch
                  {
                      var sort when sort == "TITLE" || sort == "NAME"
                     => desc.GetValueOrDefault() ?
                          (dynamic)col
                              .OrderByDescending(p => p.Title).ToArray() :
                          (dynamic)col
                              .OrderBy(p => p.Title).ToArray(),
                      var sort when sort == "WAGE" || sort == "SALARY"
                      => desc.GetValueOrDefault() ?
                          (dynamic)col
                              .OrderByDescending(p => p.Wage)
                      .ToArray() :
                          (dynamic)col
                              .OrderBy(p => p.Wage)
                      .ToArray(),
                      _ => desc.GetValueOrDefault() ?
                          (dynamic)col
                              .OrderByDescending(p => p.PositionId).ToArray() :
                          (dynamic)col
                              .ToArray()
                  };

        #endregion

        #endregion

        /// <summary>
        /// Update Position
        /// </summary>
        /// <param name="id">Id of the target Position</param>
        /// <param name="position">A Position object whose properties 
        /// are to be transfered to the existing Position</param>
        /// <remarks>
        /// Updates the values of an existing Position, whose Id 
        /// corresponds to the given 'id' parameter,
        /// using the values from the 'position' parameter.
        /// Only the Title and Wage of the Position may be updated through this API.
        /// Its Employees are determined by which Employees have this Position's
        /// Id as their Position Id and can be changed through the Employee's API
        /// controller.
        /// The Position Id may not be manually updated.
        /// The given parameter of type Position may contain a Title string and Wage decimal.
        /// It may additionally contain an empty collection of Employees and a
        /// Position Id of 0.
        /// The 'id' Parameter must correspond to an existing Position, otherwise
        /// Not Found is returned.
        /// If the 'position' parameter is incorrect, Bad Request is returned
        /// Position parameter errors:
        /// (1) The Position Id is not 0, 
        /// (2) The Title string is longer than 50 characters,
        /// (3) The Title string is the same as another Position's Title,
        /// (4) Employees is null,
        /// (5) Employees is not an empty collection,
        /// (6) Wage is lower than 0m.
        /// </remarks>
        // PUT: api/Positions/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePosition(int id, Position position)
        {
            _logger.LogDebug("Positions Controller trying to update " +
                "Position with ID: " + id);
            if (!await PositionExists(id))
            {
                _logger.LogDebug("Positions Controller found no Position " +
                    "with ID: " + id);
                return NotFound();
            }

            //Validation

            if (position.PositionId != 0 ||
                (!string.IsNullOrEmpty(position.Title) &&
                    (string.IsNullOrWhiteSpace(position.Title) ||
                    position.Title.Length > 50)) ||
                (position.Title != null &&
                (await _context.Positions.AnyAsync(p =>
                    p.PositionId != id &&
                    p.Title.ToUpper() == position.Title.ToUpper()))) ||
                position.Wage < 0m ||
                position.Employees == null || position.Employees.Count > 0)
            {
                _logger.LogDebug("Positions Controller found provided " +
                    "'position' parameter to violate update constraints.");
                return BadRequest();
            }

            /*Wage can be 0 for unpayed interns*/

            //Validation

            var oldPosition = await _context.Positions.FindAsync(id);

            if (!string.IsNullOrEmpty(position.Title))
                oldPosition.Title = position.Title;
            if (position.Wage != 0) 
                oldPosition.Wage = position.Wage;

            await _context.SaveChangesAsync();

            _logger.LogDebug("Positions Controller successfully updated " +
                "Position with ID: " + id);
            return NoContent();
        }

        /// <summary>
        /// Create Position
        /// </summary>
        /// <param name="position">A Position object to be added to the Database</param>
        /// <remarks>
        /// Creates a new Position and inserts it into the Database
        /// using the values from the 'position' parameter.
        /// Only the Title and Wage of the Position may be set.
        /// The Employees are automatically determined by which Employees'
        /// Position Id matches the Position's, and may only be changed through
        /// the Employee API controller.
        /// The Position Id may not be set. The Databse will automatically
        /// assign it a new Id, equivalent to the last Position Id in the Database + 1.
        /// The given parameter of type Position must contain a Title string and
        /// Wage decimal. It may additionally hold an empty collection
        /// of Employees and a Position Id of 0.
        /// If the 'position' parameter is incorrect, Bad Request will be returned.
        /// Position parameter errors:
        /// (1) The Position Id is not 0, 
        /// (2) The Title string is empty,
        /// (3) The Title string is longer than 50 characters,
        /// (4) The Title string is the same as another Position's Title,
        /// (5) Employees is null,
        /// (6) Employees is not an empty collection,
        /// (7) Wage is smaller than 0m.
        /// </remarks>
        // POST: api/Positions
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPost]
        public async Task<ActionResult<Position>> CreatePosition(Position position)
        {
            _logger.LogDebug("Positions Controller trying to create new Position.");

            //Validation

            if (position.PositionId != 0 ||
                string.IsNullOrEmpty(position.Title) ||
                string.IsNullOrWhiteSpace(position.Title) ||
                position.Title.Length > 50 ||
                await _context.Positions.AnyAsync(p => p.Title.ToUpper() == position.Title.ToUpper()) ||
                position.Wage <= 0m ||
                position.Employees == null || position.Employees.Count > 0)
            {
                _logger.LogDebug("Positions Controller found provided " +
                    "'position' parameter to violate creation constraints.");
                return BadRequest();
            }

            //Validation

            _context.Positions.Add(position);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Positions Controller successsfully created Position" +
                "at ID: " + position.PositionId);
            return CreatedAtAction("GetPositionBasic", new { id = position.PositionId }, position);
        }

        /// <summary>
        /// Delete Position
        /// </summary>
        /// <param name="id">Id of target Position</param>
        /// <remarks>
        /// Removes a Position from the Database.
        /// If there is no Position, whose Id matches the given 'id' parameter, 
        /// Not Found is returned.
        /// If the desired Position is the last in the Database, and it has no
        /// Employees, it is removed from the Database
        /// and its Position Id will be used by the next Position to be inserted.
        /// If the desired Position is not the last in the Database, and it has no
        /// Menu Item or Dish Requirement relationships,Employees it is swapped with the last
        /// Position, taking on its Title, Wage and Employees, and the last Position is removed,
        /// freeing up the last Positin's Id for
        /// the next Position to be inserted.
        /// If the desired Position Employees,
        /// it is turned into a blank Position, so a not to violate its relationships.
        /// Its Title will be changed to reflect that fact.
        /// </remarks>
        // DELETE: api/Positions/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<Position>> DeletePosition(int id)
        {
            _logger.LogDebug("Positions Controller trying to delete " +
                "Position with ID: " + id);

            var position = await _context.Positions.FindAsync(id);
            if (position == null)
            {
                _logger.LogDebug("Positions Controller found no Position with ID: " + id);
                return NotFound();
            }

            // If it has employees, turn it into a blank position
            if (await _context.Employees.AnyAsync(e => e.PositionId == id))
            {
                _logger.LogDebug("Positions Controller found Position with " +
                    "ID: " + id + " has Employees depening on it. As such, instead of" +
                    " being deleted, it will be turned into a blank Position.");
                position.Title = string.Format("Unassigned {0}:{1}", 
                    DateTime.Now.ToShortDateString(), id);
                position.Wage = 0m;
                await _context.SaveChangesAsync();

                _logger.LogDebug("Positions Controller turned Position with ID: " +
                    id + " into a blank Position");
                return Ok();
            }

            var lastPosId = await _context.Positions.CountAsync();

            if (id == lastPosId)/*If it's last, delete it*/
            {
                _logger.LogDebug("Positions Controller found Position with ID: " +
                    id + " to have no Employee relationships and be the last in " +
                    "the Database. As such, it will be deleted.");
                _context.Positions.Remove(position);
                await _context.SaveChangesAsync();
                await _context.Database.ExecuteSqlRawAsync
                    ("DBCC CHECKIDENT('[Positions]', RESEED, " + (lastPosId - 1) + ")");
                await _context.SaveChangesAsync();

                _logger.LogDebug("Positions Controller succesfully deleted " +
                    "Position with ID: " + id);
                return Ok();
            }

            //If it's not last, swap it with last and del that

            _logger.LogDebug("Positions Controller found Position with ID: " + id +
                " has no Employee relationships and is not the last in the Database. " +
                "As such, it will swap places with the last Position and that one will be deleted.");

            /*Migrate Employees from Last Position to posId*/
            var employeesInLastPos = await _context.Employees
                    .Where(e => e.PositionId == lastPosId)
                    .ToArrayAsync();

            foreach (var emp in employeesInLastPos)
                emp.PositionId = id;

            await _context.SaveChangesAsync();

            /*Swap and del last*/
            var lastPos = await _context.Positions.FindAsync(lastPosId);
            position.Title = lastPos.Title;
            position.Wage = lastPos.Wage;

            _context.Positions.Remove(lastPos);
            await _context.SaveChangesAsync();
            await _context.Database.ExecuteSqlRawAsync
                ("DBCC CHECKIDENT('[Positions]', RESEED, " + (lastPosId - 1) + ")");
            await _context.SaveChangesAsync();

            _logger.LogDebug("Positions Controller successfully turned Position with " +
                "ID: " + id + " into the last Position, then deleted the last one.");
            return Ok();
        }

        [NonAction]
        private async Task<bool> PositionExists(int id)=> await _context.Positions.FindAsync(id)!=null;
    }
}
