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
    public class PositionsController : ControllerBase
    {
        private readonly FoodChainsDbContext _context;

        public PositionsController(FoodChainsDbContext context) => _context = context;

        #region Gets

        #region Basic

        // GET: api/Positions
        [HttpGet]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetPositions()
        => await _context.Positions
            .Select(p => new 
            {
                ID = p.PositionId,
                p.Title,
                HourlyWage = string.Format("{0:C}", p.Wage)
            })
            .Take(20)
            .ToListAsync();

        // GET: api/Positions/5
        [HttpGet("{id}")]
        public async Task<ActionResult<dynamic>> GetPosition(int id)
        {
            if (!await PositionExists(id)) return NotFound();

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

        // GET: api/Positions/5/basic
        [HttpGet("{id}/basic")]
        public async Task<ActionResult<Position>> GetPositionBasic(int id)
        {
            var position = await _context.Positions.FindAsync(id);
            if (position == null) return NotFound();

            return position;
        }

        // GET: api/Positions/5/employees
        [HttpGet("{id}/employees")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetEmployees(int id)
        {
            var position = await _context.Positions.FindAsync(id);
            if (position == null) return NotFound();

            return await _context.Employees
                .Where(e => e.PositionId == id)
                .Select(e => string.Format("({0}) {1}, {2}", e.EmployeeId, e.LastName, e.FirstName))
                .ToArrayAsync();
        }

        #endregion

        #region Advanced

        // GET: api/Positions/query
        [HttpGet("query:{query}")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetPositionsQueried(string query)
        {
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

        // PUT: api/Positions/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePosition(int id, Position position)
        {
            if (!await PositionExists(id)) return NotFound();

            //Validation

            if (position.PositionId != 0 ||
                (!string.IsNullOrEmpty(position.Title) &&
                    (string.IsNullOrWhiteSpace(position.Title) ||
                    position.Title.Length > 50)) ||
                (position.Title!=null&&
                (await _context.Positions.AnyAsync(p => 
                    p.PositionId != id &&
                    p.Title.ToUpper() == position.Title.ToUpper())))||
                position.Wage < 0m ||
                position.Employees == null || position.Employees.Count > 0)
                return BadRequest();

            /*Wage can be 0 for unpayed interns*/

            //Validation

            var oldPosition = await _context.Positions.FindAsync(id);

            if (!string.IsNullOrEmpty(position.Title))
                oldPosition.Title = position.Title;
            if (position.Wage != 0) 
                oldPosition.Wage = position.Wage;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/Positions
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPost]
        public async Task<ActionResult<Position>> CreatePosition(Position position)
        {
            //Validation

            if (position.PositionId != 0 ||
                string.IsNullOrEmpty(position.Title) ||
                string.IsNullOrWhiteSpace(position.Title) ||
                position.Title.Length > 50 ||
                await _context.Positions.AnyAsync(p=>p.Title.ToUpper()==position.Title.ToUpper())||
                position.Wage <= 0m ||
                position.Employees == null || position.Employees.Count > 0)
                return BadRequest();

            //Validation

            _context.Positions.Add(position);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetPositionBasic", new { id = position.PositionId }, position);
        }

        // DELETE: api/Positions/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<Position>> DeletePosition(int id)
        {
            var position = await _context.Positions.FindAsync(id);
            if (position == null) return NotFound();

            // If it has empoyees, turn it into a blank position
            if (await _context.Employees.AnyAsync(e => e.PositionId == id))
            {
                position.Title = string.Format("Unassigned {0}:{1}", 
                    DateTime.Now.ToShortDateString(), id);
                position.Wage = 0m;
                await _context.SaveChangesAsync();
                return Ok();
            }

            var lastPosId = await _context.Positions.CountAsync();

            if (id == lastPosId)/*If it's last, delete it*/
            {
                _context.Positions.Remove(position);
                await _context.SaveChangesAsync();
                await _context.Database.ExecuteSqlRawAsync
                    ("DBCC CHECKIDENT('[Positions]', RESEED, " + (lastPosId - 1) + ")");
                await _context.SaveChangesAsync();
                return Ok();
            }

            //If it's not last, swap it with last and del that

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
            return Ok();
        }

        private async Task<bool> PositionExists(int id)=> await _context.Positions.FindAsync(id)!=null;

    }
}
