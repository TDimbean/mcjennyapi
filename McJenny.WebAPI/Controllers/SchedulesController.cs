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
    public class SchedulesController : ControllerBase
    {
        private readonly FoodChainsDbContext _context;
        private readonly ILogger<SchedulesController> _logger;
        public SchedulesController(FoodChainsDbContext context,
            ILogger<SchedulesController> logger)
        {
            _context = context;
            _logger = logger;
            _logger.LogDebug("Logger injected into Schedules Controller.");
        }

        #region Gets

        #region Basic

        /// <summary>
        /// Get Schedules
        /// </summary>
        /// <remarks>
        /// Returns a collection of all Schedules(max 20, unless queried otherwise)
        /// forated to incude the Schedule Id and Timetable.
        /// </remarks>
        // GET: api/Schedules
        [HttpGet]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetSchedules()
        {
            _logger.LogDebug("Schedules Controller fetching all Schedules" +
                "(max of 20, unless queried otherwise)");
            return await _context.Schedules
            .Select(s => new { s.ScheduleId, s.TimeTable })
            .Take(20)
            .ToListAsync();
        }

        /// <summary>
        /// Get Shedule
        /// </summary>
        /// <param name="id">Id of desired Schedule</param>
        /// <remarks>
        /// Returns the Schedule whose Id matches the given 'id' parameter.
        /// If no such Schedule exists, Not Found is returned instead.
        /// </remarks>
        // GET: api/Schedules/5
        [HttpGet("{id}")]
        public async Task<ActionResult<dynamic>> GetSchedule(int id)
        {
            _logger.LogDebug("Schedules Controller fetching Schedule " +
                "with ID: " + id);
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null)
            {
                _logger.LogDebug("Schedules Controller found no Schedule " +
                    "with ID: " + id);
                return NotFound();
            }

            _logger.LogDebug("Scheudles Controller returning formated Schedule " +
                "with ID: " + id);
            return new
            {
                schedule.ScheduleId,
                schedule.TimeTable
            };
        }

        /// <summary>
        /// Get Schedule Basic
        /// </summary>
        /// <param name="id">Id of desired Schedule</param>
        /// <remarks>
        /// Returns a Schedule object from the Schedule whose Id matches
        /// the given 'id' parameter. It Includes a Schedule Id, Timetable 
        /// and an empty collection of Locations.
        /// If no Schedule whose Id matches the 'id' parameter exists,
        /// Not Found is returned instead.
        /// </remarks>
        // GET: api/Schedules/5/basic
        [HttpGet("{id}/basic")]
        public async Task<ActionResult<Schedule>> GetScheduleBasic(int id)
        {
            _logger.LogDebug("Schedules Controller fetching Schedule with " +
                "ID: " + id);
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null)
            {
                _logger.LogDebug("Schedules Controller found no Schedule " +
                    "with ID: " + id);
                return NotFound();
            }

            _logger.LogDebug("Schedules Controller returning basic Schedule " +
                "with ID: " + id);
            return schedule;
        }

        /// <summary>
        /// Get Schedule Locations
        /// </summary>
        /// <param name="id">If of desired Schedule</param>
        /// <remarks>
        /// Returns a collection of the Locations that implement the Schedule
        /// whose Id matches the given 'id' parameter, formated to include
        /// each Location's Id, Abreviated Country, Abreviated State,
        /// City and Street.
        /// If no Schedule whose Id matches the given 'id' exists,
        /// Not Found is returned instead.
        /// </remarks>
        // GET: api/Schedules/5/locations
        [HttpGet("{id}/locations")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetLocations(int id)
        {
            _logger.LogDebug("Schedules Controller fetching Locations " +
                "that implement Schedule with ID: " + id);
            if (!await ScheduleExists(id))
            {
                _logger.LogDebug("Schedules Controller found no Schedule " +
                    "with ID: " + id);
                return NotFound();
            }

            _logger.LogDebug("Schedules Controller returning Locations for" +
                " Schedule with ID: " + id);
            return await _context.Locations
                .Select(l => new
                {
                    l.ScheduleId,
                    res = new
                    {
                        l.LocationId,
                        l.AbreviatedCountry,
                        l.AbreviatedState,
                        l.City,
                        l.Street
                    }
                })
                .Where(l => l.ScheduleId == id)
                .Select(l => l.res)
                .ToArrayAsync();
        }

        #endregion

        #endregion

        /// <summary>
        /// Update Schedule
        /// </summary>
        /// <param name="id">Id of target Schedule</param>
        /// <param name="schedule">A Schedule object whose properties are
        /// to be transfered to the existing Schedule</param>
        /// <remarks>
        /// Updates the values of an existing Schedule, whose Id corresponds 
        /// to the given 'id' parameter, using the values from the 'schedule' parameter.
        /// Only the Timetable of the Schedule may be updated through this API.
        /// Its Locations are automatically determined based on which Locations' 
        /// Schedule Id matches the Schedule's and can only be changed through the
        /// Location's API Controller.
        /// The Schedule Id may not be updated.
        /// The given parameter of type Schedule may contain a Timetable, an empty collection
        /// of Locations and a Schedule Id of 0.
        /// The 'id' parameter must correspond to an existing Schedule, otherwise
        /// Not Found is returned.
        /// If the Schedule parameter is incorrect, Bad Request is returned.
        /// Schedule parameter errors:
        /// (1) The Schedule Id is not 0, 
        /// (2) The Timetable string is longer than 50 characters,
        /// (3) Locations is null,
        /// (4) Locations is not an empty collection.
        /// </remarks>
        // PUT: api/Schedules/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSchedule(int id, Schedule schedule)
        {
            _logger.LogDebug("Schedules Controller trying to update Schedule " +
                "with ID: " + id);
            if (!await ScheduleExists(id))
            {
                _logger.LogDebug("Schedules Controller found no Schedule with " +
                    "ID: " + id);
                return NotFound();
            }

            // Validation

            if (schedule.ScheduleId != 0 ||
                string.IsNullOrEmpty(schedule.TimeTable) ||
            (!string.IsNullOrEmpty(schedule.TimeTable) &&
                (schedule.TimeTable.Length > 200 ||
                string.IsNullOrWhiteSpace(schedule.TimeTable))) ||
            schedule.Locations == null || schedule.Locations.Count != 0)
            {
                _logger.LogDebug("Schedules Controller found provided " +
                    "'schedule' parameter to violate update constraints.");
                return BadRequest();
            }

            // Validation

            var oldSchedule = await _context.Schedules.FindAsync(id);

            oldSchedule.TimeTable = schedule.TimeTable;

            await _context.SaveChangesAsync();

            _logger.LogDebug("Schedules Controller successfully updated " +
                "Schedule with ID: " + id);
            return NoContent();
        }

        /// <summary>
        /// Create Schedule
        /// </summary>
        /// <param name="schedule">A Schedule object to be added to the Database</param>
        /// <remarks>
        /// Creates a new Schedule and inserts it into the Database
        /// using the values from the 'schedule' parameter.
        /// Only the Timetable of the Schedule may be set.
        /// The Locations are automatically determined by which Locations have
        /// a Schedule Id matching the Schedule's Id. This can only be changed using
        /// the Locations API controller.
        /// The Schedule Id may not be set. The Databse will automatically
        /// assign it a new Id, equivalent to the last Schedule Id in the Database + 1.
        /// The given parameter of type Schedule must contain a Timetable string.
        /// Additionally, it may contain an empty collection
        /// of Locations and a Schedule Id of 0.
        /// If the 'schedule' parameter is incorrect, Bad Request is returned.
        /// Schedule parameter errors:
        /// (1) The Schedule Id is not 0, 
        /// (2) The Timetable string is empty,
        /// (3) The Timetable string is longer than 50 characters,
        /// (4) Locations is null,
        /// (5) Locations is not an empty collection.
        /// </remarks>
        // POST: api/Schedules
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPost]
        public async Task<ActionResult<Schedule>> CreateSchedule(Schedule schedule)
        {
            _logger.LogDebug("Schedules Controller trying to create new Schedule.");

            //Validation

            if (schedule.ScheduleId != 0 ||
                string.IsNullOrEmpty(schedule.TimeTable) ||
                string.IsNullOrWhiteSpace(schedule.TimeTable) ||
                schedule.TimeTable.Length > 200 ||
                schedule.Locations == null || schedule.Locations.Count != 0)
            {
                _logger.LogDebug("Schedules Controller found provided 'schedule' " +
                    "paramtere to violate creation constraints.");
                return BadRequest();
            }
            
            //Validation

            _context.Schedules.Add(schedule);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Schedues Controller successfully created new Schedule at ID: " +
                schedule.ScheduleId);
            return CreatedAtAction("GetScheduleBasic", new { id = schedule.ScheduleId }, schedule);
        }

        /// <summary>
        /// Delete Schedule
        /// </summary>
        /// <param name="id">Id of target Schedule</param>
        /// <remarks>
        /// Removes a Schedule from the Database.
        /// If there is no Schedule, whose Id matches the given 'id' parameter, 
        /// Not Found is returned.
        /// If the desired Schedule is the last in the Database, and it has no
        /// Locations, it is removed from the Database
        /// and its Schedule Id will be used by the next Schedule to be inserted.
        /// If the desired Schedule is not the last in the Database, and it has no
        /// Locations, it takes on the last Schedule's Timetable and Locations, 
        /// and the last Schedule is deleted instead, freeing up its Id
        /// for the next Schedule to be inserted.
        /// If the target Schedule has any Positions depending on it,
        /// it is turned into a blank Schedule, so a not to violate its relationships.
        /// Its Timetable will be changed to reflect that fact.
        /// </remarks>
        // DELETE: api/Schedules/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<Schedule>> DeleteSchedules(int id)
        {
            _logger.LogDebug("Schedules Controller trying to delete " +
                "Schedule with ID: " + id);

            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null)
            {
                _logger.LogDebug("Schedules Controller found no Schedule " +
                    "with ID: " + id);
                return NotFound();
            }

            // If it has Locations, turn it into a blank schedule
            if (await _context.Locations.AnyAsync(l => l.ScheduleId == id))
            {
                _logger.LogDebug("Schedules Controller found Schedule with " +
                    "ID: " + id + " to have Locations that depend on it. Instead of " +
                    "being deleted, it will be turned into a blank Schedule.");

                schedule.TimeTable = string.Format("Not Set {0}:{1}",
                    DateTime.Now.ToShortDateString(), id);
                await _context.SaveChangesAsync();

                _logger.LogDebug("Schedules Controller successfully " +
                    "turned Schedule with ID: " + id + " into a blank Schedule");
                return Ok();
            }

            var lastSchId = await _context.Schedules.CountAsync();

            if (id == lastSchId)/*If it's last, delete it*/
            {
                _logger.LogDebug("Schedules Controller found SChedule with ID: " +
                    id + " to have no Locations depending on it and to be the last " +
                    "in the Database. As such, it will be deleted.");

                _context.Schedules.Remove(schedule);
                await _context.SaveChangesAsync();
                await _context.Database.ExecuteSqlRawAsync
                    ("DBCC CHECKIDENT('[Schedules]', RESEED, " + (lastSchId - 1) + ")");
                await _context.SaveChangesAsync();

                _logger.LogDebug("Schedules Controller successfully deleted Schedule " +
                    "with ID: " + id);
                return Ok();
            }

            //If it's not last, swap it with last and del that

            _logger.LogDebug("Schedules Controller found Schedule with ID: " + id + " has" +
                " no Locations depending on it and is not the last in the Database. As such, " +
                "it will swap it with the last Schedule and delete that one instead.");


            /*Migrate Locations from Last Schedule to locId*/

            var locationsWithLastSch = await _context.Locations
                    .Where(l => l.ScheduleId == lastSchId)
                    .ToArrayAsync();

            foreach (var loc in locationsWithLastSch)
                loc.ScheduleId= id;

            await _context.SaveChangesAsync();

            /*Swap and del last*/
            var lastSch = await _context.Schedules.FindAsync(lastSchId);
            schedule.TimeTable = lastSch.TimeTable;

            _context.Schedules.Remove(lastSch);
            await _context.SaveChangesAsync();
            await _context.Database.ExecuteSqlRawAsync
                ("DBCC CHECKIDENT('[Schedules]', RESEED, " + (lastSchId - 1) + ")");
            await _context.SaveChangesAsync();

            _logger.LogDebug("Schedules Controller successfully turned Schedule with " +
                "ID: " + id + " into last Schedule and deleted that one.");
            return Ok();
        }

        [NonAction]
        private async Task<bool> ScheduleExists(int id)=> await _context.Schedules.FindAsync(id)!=null;
    }
}
