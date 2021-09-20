using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using McJenny.WebAPI.Data.Models;
using Microsoft.Extensions.Logging;

namespace McJenny.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ManagementsController : ControllerBase
    {
        private readonly FoodChainsDbContext _context;
        private readonly ILogger<ManagementsController> _logger;

        public ManagementsController(FoodChainsDbContext context,
            ILogger<ManagementsController> logger)
        {
            _context = context;
            _logger = logger;
            _logger.LogDebug("Logger injected into Managements Controller.");
        }

        #region Gets

        #region Basic

        /// <summary>
        /// Get Managements
        /// </summary>
        /// <remarks>
        /// Returns a collection of strings explaining the Management
        /// relationships between the Locations and Employees that manage them.
        /// </remarks>
        // GET: api/Managements
        [HttpGet]
        public async Task<ActionResult<IEnumerable<string>>> GetManagements()
        {
            _logger.LogDebug("Managements Controller fetching all Managements...");
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

            _logger.LogDebug("Managements Controller returning all Managements.");
            return result;
        }

        /// <summary>
        /// Get Management
        /// </summary>
        /// <param name="id">Id of target Management</param>
        /// <remarks>
        /// Returns a string explaining the Management relationship
        /// between a Location of Location Id and Employee of Manager Id.
        /// If no Management with specified 'id' exists,
        /// Not Found is returned.
        /// </remarks>
        // GET: api/Managements/5
        [HttpGet("{id}")]
        public async Task<ActionResult<string>> GetManagement(int id)
        {
            _logger.LogDebug("Managements Controller fetching Management with ID: " + id);
            var management = await _context.Managements.FindAsync(id);

            if (management == null)
            {
                _logger.LogDebug("Managements Controller found no Management with ID: " + id);
                return NotFound();
            }

            var employee = await _context.Employees.FindAsync(management.ManagerId);
            var location = await _context.Locations.FindAsync(management.LocationId);

            if (employee == null || location == null) return NotFound();

            _logger.LogDebug("Managements Controller returning formated Management " +
                "with ID: " + id);
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

        /// <summary>
        /// Get Management Basic
        /// </summary>
        /// <param name="id">Id of target Management</param>
        /// <remarks>
        /// Returns a Management object whose Management Id
        /// matches the 'id' parameter.
        /// If no Management with specified 'id' exists
        /// Not Found is returned
        /// </remarks>
        // GET: api/Managements/5/basic
        [HttpGet("{id}/basic")]
        public async Task<ActionResult<Management>> GetManagementBasic(int id)
        {
            _logger.LogDebug("Managements Controller fetching Management with ID: " + id);
            var management = await _context.Managements.FindAsync(id);

            if (management == null)
            {
                _logger.LogDebug("Managements Controller found no Management with ID: " + id);
                return NotFound();
            }

            _logger.LogDebug("Managements Controller returning basic Management with ID: " + id);
            return management;
        }

        #endregion

        #endregion

        /// <summary>
        /// Create Management
        /// </summary>
        /// <param name="management">Management to Create</param>
        /// <remarks>
        /// Creates a new Management and inserts it into the Database
        /// using the values from the provided management parameter.
        /// Only the Manager Id and Location Id of the Management may be set.
        /// Its Manager and Location will be automatically set based on the
        /// Manager Id and Loction Id.
        /// The Mnagement Id may not be set. The Databse will automatically
        /// assign it a new Id, equivalent to the last Management Id in the Database + 1.
        /// The given parameter of type Management must contain a Manager Id and
        /// Location Id, belonging to a valid Employee and Location.
        /// If the 'management' parameter is incorrect, Bad Request is returned.
        /// Management parameter errors:
        /// (1) The Management Id is not 0, 
        /// (2) The Manager Id does not match any Employee's Employee Id,
        /// (3) The Location Id does not match any Location's Location Id,
        /// (4) The Manager Id matches an Employee that already manages a different Location,
        /// (5) The Location Id matches a Location that is already managed by a different Employee,
        /// (6) Another Management with the same Manager Id and Location Id values exists.
        /// </remarks>
        // POST: api/Managements
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPost]
        public async Task<ActionResult<Management>> CreateManagement(Management management)
        {
            _logger.LogDebug("Managements Controller trying to create new Management.");

            //Validation

            if (management.ManagementId != 0 ||
                management.Location != null || management.Manager != null ||
                await _context.Employees.FindAsync(management.ManagerId) == null ||
                await _context.Locations.FindAsync(management.LocationId) == null ||
                await _context.Managements.AnyAsync(m =>
                    m.LocationId == management.LocationId) ||
                await _context.Managements.AnyAsync(m =>
                    m.ManagerId == management.ManagerId) ||
                (await _context.Employees.FindAsync(management.ManagerId)).PositionId != 1)
            {
                _logger.LogDebug("Managements Controller found provided 'management' " +
                    "object to violate creation constraints.");
                return BadRequest();
            }

            //Validation

            _context.Managements.Add(management);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Managements Controller successfully created new Management " +
                "at ID: " + management.ManagementId);
            return CreatedAtAction("GetManagementBasic", new { id = management.ManagementId }, management);
        }

        /// <summary>
        /// Delete Management
        /// </summary>
        /// <param name="id">Id of target Management</param>
        /// <remarks>
        /// Removes a Management from the Database.
        /// If there is no Management, whose Id matches the given 'id' parameter, 
        /// Not Found is returned.
        /// If the desired Management is the last in the Database it is removed from the Database
        /// and its Management Id will be used by the next Management to be inserted.
        /// If the desired Management is not the last in the Database, it is swapped with the last
        /// Management in the Database before it is removed, taking on the last Management's
        /// Location Id and Manager Id before removing it and freeing up the last 
        /// Management's Id for the next Management to be inserted.
        /// </remarks>
        // DELETE: api/Managements/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<Management>> DeleteManagement(int id)
        {
            _logger.LogDebug("Managements Controller trying to delete Management with ID: " + id);
            var management = await _context.Managements.FindAsync(id);
            if (management == null)
            {
                _logger.LogDebug("Managements Controller found no Management with ID: " + id);
                return NotFound();
            }

            var lastManId = await _context.Managements.CountAsync();

            if (id == lastManId)
            {
                _logger.LogDebug("Managements Controller found Management with ID: " +
                    id + " to be the last Management and will delete it.");
                _context.Managements.Remove(management);
                await _context.SaveChangesAsync();
                await _context.Database.ExecuteSqlRawAsync
                    ("DBCC CHECKIDENT('[Managements]', RESEED, " + (lastManId - 1) + ")");
                await _context.SaveChangesAsync();

                _logger.LogDebug("Managements Controller successfully deleted Management " +
                    "with ID: " + id);
                return Ok();
            }

            _logger.LogDebug("Managements Controller found Management with ID: " + id +
                " not to be the last in the Database. As such, it will swap it with the last " +
                "Management and delete that one instead.");

            var lastMan = await _context.Managements.FindAsync(lastManId);
            management.LocationId = lastMan.LocationId;
            management.ManagerId = lastMan.ManagerId;

            _context.Managements.Remove(lastMan);
            await _context.SaveChangesAsync();
            await _context.Database.ExecuteSqlRawAsync
                ("DBCC CHECKIDENT('[Managements]', RESEED, " + (lastManId - 1) + ")");
            await _context.SaveChangesAsync();

            _logger.LogDebug("Managements Controller successfully turned Management with ID: " +
                id + " into the last Management and deleted that.");
            return Ok();
        }

        [NonAction]
        private async Task<bool> ManagementExists(int id)
            => await _context.Managements.AnyAsync(e => e.ManagementId == id);
    }
}
