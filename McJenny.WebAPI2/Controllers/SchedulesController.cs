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
    public class SchedulesController : ControllerBase
    {
        private readonly FoodChainsDbContext _context;
        public SchedulesController(FoodChainsDbContext context) => _context = context;

        #region Gets

        #region Basic

        // GET: api/Schedules
        [HttpGet]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetSchedules()
        => await _context.Schedules
            .Select(s=>new { s.ScheduleId, s.TimeTable})
            .Take(20)
            .ToListAsync();

        // GET: api/Schedules/5
        [HttpGet("{id}")]
        public async Task<ActionResult<dynamic>> GetSchedule(int id)
        {
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null) return NotFound();

            return new
            {
                schedule.ScheduleId,
                schedule.TimeTable
            };
        }

        // GET: api/Schedules/5/basic
        [HttpGet("{id}/basic")]
        public async Task<ActionResult<Schedule>> GetScheduleBasic(int id)
        {
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null) return NotFound();

            return schedule;
        }

        // GET: api/Schedules/5/locations
        [HttpGet("{id}/locations")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetLocations(int id)
        {
            if (!await ScheduleExists(id)) return NotFound();

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

        // PUT: api/Schedules/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSchedule(int id, Schedule schedule)
        {
            if (!await ScheduleExists(id)) return NotFound();

            // Validation

            if (schedule.ScheduleId != 0 ||
                string.IsNullOrEmpty(schedule.TimeTable)||
            (!string.IsNullOrEmpty(schedule.TimeTable) &&
                (schedule.TimeTable.Length > 200 ||
                string.IsNullOrWhiteSpace(schedule.TimeTable))) ||
            schedule.Locations == null || schedule.Locations.Count != 0)
                return BadRequest();

            // Validation

            var oldSchedule = await _context.Schedules.FindAsync(id);

            oldSchedule.TimeTable = schedule.TimeTable;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/Schedules
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPost]
        public async Task<ActionResult<Schedule>> CreateSchedule(Schedule schedule)
        {
            //Validation

            if (schedule.ScheduleId != 0 ||
                string.IsNullOrEmpty(schedule.TimeTable) ||
                string.IsNullOrWhiteSpace(schedule.TimeTable) ||
                schedule.TimeTable.Length > 200 ||
                schedule.Locations == null || schedule.Locations.Count != 0)
                return BadRequest();
            
            //Validation

            _context.Schedules.Add(schedule);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetScheduleBasic", new { id = schedule.ScheduleId }, schedule);
        }

        // DELETE: api/Schedules/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<Schedule>> DeleteSchedules(int id)
        {
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null) return NotFound();

            // If it has Locations, turn it into a blank schedule
            if (await _context.Locations.AnyAsync(l => l.ScheduleId == id))
            {
                schedule.TimeTable = string.Format("Not Set {0}:{1}",
                    DateTime.Now.ToShortDateString(), id);
                await _context.SaveChangesAsync();
                return Ok();
            }

            var lastSchId = await _context.Schedules.CountAsync();

            if (id == lastSchId)/*If it's last, delete it*/
            {
                _context.Schedules.Remove(schedule);
                await _context.SaveChangesAsync();
                await _context.Database.ExecuteSqlRawAsync
                    ("DBCC CHECKIDENT('[Schedules]', RESEED, " + (lastSchId - 1) + ")");
                await _context.SaveChangesAsync();
                return Ok();
            }

            //If it's not last, swap it with last and del that

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
            return Ok();
        }

        private async Task<bool> ScheduleExists(int id)=> await _context.Schedules.FindAsync(id)!=null;
    }
}
