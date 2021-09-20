using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using McJenny.WebAPI.Data.Models;

namespace McJenny.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ManagementsController : ControllerBase
    {
        private readonly FoodChainsDbContext _context;
        public ManagementsController(FoodChainsDbContext context) => _context = context;

        #region Gets

        #region Basic

        // GET: api/Managements
        [HttpGet]
        public async Task<ActionResult<IEnumerable<string>>> GetManagements()
        {
            var managements = await _context.Managements
                .Select(m=>new {m.ManagementId, m.ManagerId, m.LocationId })
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

            var result = new string[managements.Length];
            for (int i = 0; i < managements.Length; i++)
            {
                var emp = employees.SingleOrDefault(e => e.EmployeeId == managements[i].ManagerId);
                var loc = locations.SingleOrDefault(l => l.LocationId == managements[i].LocationId);

                result[i] = string.Format("Management [{0}]: ({1}) {2}, {3} manages ({4}) {5}, {6}{7}, {8}",
                    managements[i].ManagementId,
                    managements[i].ManagerId,
                    emp.LastName, emp.FirstName,
                    managements[i].LocationId,
                    loc.AbreviatedCountry,
                    loc.AbreviatedState == "N/A" ? string.Empty : loc.AbreviatedState + ", ",
                    loc.City, loc.Street);
            }

            return result;
        }

        // GET: api/Managements/5
        [HttpGet("{id}")]
        public async Task<ActionResult<string>> GetManagement(int id)
        {
            var management = await _context.Managements.FindAsync(id);

            if (management == null) return NotFound();

            var employee = await _context.Employees.FindAsync(management.ManagerId);
            var location = await _context.Locations.FindAsync(management.LocationId);

            if (employee == null || location == null) return NotFound();

            return string.Format("({0}) {1}, {2} manages ({3}) {4}, {5}{6}, {7}",
                    management.ManagerId,
                    employee.LastName, employee.FirstName,
                    management.LocationId,
                    location.AbreviatedCountry,
                    location.AbreviatedState == "N/A" ? string.Empty :
                    location.AbreviatedState + ", ",
                    location.City,
                    location.Street);
        }

        // GET: api/Managements/5/basic
        [HttpGet("{id}/basic")]
        public async Task<ActionResult<Management>> GetManagementBasic(int id)
        {
            var management = await _context.Managements.FindAsync(id);

            if (management == null) return NotFound();

            return management;
        }

        #endregion

        #endregion

        // POST: api/Managements
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPost]
        public async Task<ActionResult<Management>> CreateManagement(Management management)
        {
            //Validation

            if (management.ManagementId != 0 ||
                management.Location != null || management.Manager != null ||
                await _context.Employees.FindAsync(management.ManagerId) == null ||
                await _context.Locations.FindAsync(management.LocationId) == null ||
                await _context.Managements.AnyAsync(m => 
                    m.LocationId == management.LocationId) ||
                await _context.Managements.AnyAsync(m =>
                    m.ManagerId == management.ManagerId)||
                (await _context.Employees.FindAsync(management.ManagerId)).PositionId!=1)
                return BadRequest();

            //Validation

            _context.Managements.Add(management);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetManagementBasic", new { id = management.ManagementId }, management);
        }

        // DELETE: api/Managements/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<Management>> DeleteManagement(int id)
        {
            var management = await _context.Managements.FindAsync(id);
            if (management == null) return NotFound();

            var lastManId = await _context.Managements.CountAsync();

            if (id == lastManId)
            {
                _context.Managements.Remove(management);
                await _context.SaveChangesAsync();
                await _context.Database.ExecuteSqlRawAsync
                    ("DBCC CHECKIDENT('[Managements]', RESEED, " + (lastManId - 1) + ")");
                await _context.SaveChangesAsync();
                return Ok();
            }

            var lastMan = await _context.Managements.FindAsync(lastManId);
            management.LocationId = lastMan.LocationId;
            management.ManagerId = lastMan.ManagerId;

            _context.Managements.Remove(lastMan);
            await _context.SaveChangesAsync();
            await _context.Database.ExecuteSqlRawAsync
                ("DBCC CHECKIDENT('[Managements]', RESEED, " + (lastManId - 1) + ")");
            await _context.SaveChangesAsync();
            return Ok();
        }

        private async Task<bool> ManagementExists(int id)
            => await _context.Managements.AnyAsync(e => e.ManagementId == id);
    }
}
