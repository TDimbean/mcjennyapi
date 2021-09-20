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
    public class LocationsController : ControllerBase
    {
        private readonly FoodChainsDbContext _context;

        public LocationsController(FoodChainsDbContext context) => _context = context;

        #region Gets

        #region Basic

        // GET: api/Locations
        [HttpGet]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetLocations() 
            => await GetLocationsPaged(1, 20);

        // GET: api/Locations/5
        [HttpGet("{id}")]
        public async Task<ActionResult<dynamic>> GetLocation(int id)
        {
            if (!await LocationExists(id)) return NotFound();

            return await _context.Locations
            .Include(l => l.Schedule)
            .Select(l => new
            {
                l.LocationId,
                l.AbreviatedCountry,
                l.AbreviatedState,
                l.Country,
                l.State,
                l.City,
                l.Street,
                l.Schedule.TimeTable,
                l.MenuId,
                OpenSince = l.OpenSince.ToShortDateString()
            })
            .FirstOrDefaultAsync(l => l.LocationId == id);
        }

        // GET: api/Locations/5
        [HttpGet("{id}/basic")]
        public async Task<ActionResult<Location>> GetLocationBasic(int id)
        {
            var location = await _context.Locations.FindAsync(id);
            if (location == null) return NotFound();

            return location;
        }

        // GET: api/Locations/5/employees
        [HttpGet("{id}/employees")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetEmployees(int id)
        {
            var location = await _context.Locations.FindAsync(id);
            if (location == null) return NotFound();

            return await _context.Employees
                .Include(e => e.Position)
                .Select(e => new
                {
                    e.LocationId,
                    e.EmployeeId,
                    e.FirstName,
                    e.LastName,
                    e.Position.Title
                })
                .Where(e => e.LocationId == id)
                .Select(e => new
                {
                    e.EmployeeId,
                    e.FirstName,
                    e.LastName,
                    e.Title
                })
                .ToArrayAsync();
        }

        // GET: api/Locations/5/suppliers
        [HttpGet("{id}/suppliers")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetSuppliers(int id)
        {
            var location = await _context.Locations.FindAsync(id);
            if (location == null) return NotFound();

            var supplierIds = await _context.SupplyLinks.Where(l => l.LocationId == location.LocationId).Select(l => l.SupplierId).ToListAsync();
            return await _context.Suppliers.Where(s => supplierIds.Contains(s.SupplierId))
                .Select(s => new
                {
                    s.SupplierId,
                    s.Name,
                    s.AbreviatedCountry,
                    s.AbreviatedState,
                    s.City
                })
                .ToArrayAsync();
        }

        // GET: api/Locations/5/links
        [HttpGet("{id}/links")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetLinks(int id)
        {
            var location = await _context.Locations.FindAsync(id);
            if (location == null) return NotFound();

            var links = await _context.SupplyLinks.Where(sl => sl.LocationId == id)
                .Select(sl => new { sl.SupplierId, sl.SupplyCategoryId }).ToArrayAsync();

            var categories = await _context.SupplyCategories.Select(c => new
            {
                c.SupplyCategoryId,
                c.Name
            }).ToArrayAsync();

            var supplierIds = new List<int>();
            foreach (var link in links) if (!supplierIds.Contains(link.SupplierId)) supplierIds.Add(link.SupplierId);

            var suppliers = await _context.Suppliers.Where(s => supplierIds.Contains(s.SupplierId))
                .Select(s => new
                {
                    s.SupplierId,
                    s.Name,
                    s.AbreviatedCountry,
                    s.AbreviatedState,
                    s.City
                }).ToArrayAsync();

            var results = new string[links.Length];
            for (int i = 0; i < results.Length; i++)
            {
                var supplier = suppliers.SingleOrDefault(s => s.SupplierId == links[i].SupplierId);

                results[i] = string.Format("({0}) {1} supplied by {2}, {3}, {4}{5} ({6})",
                    links[i].SupplyCategoryId,
                     categories.SingleOrDefault(c => c.SupplyCategoryId == links[i].SupplyCategoryId).Name,
                     supplier.Name, supplier.AbreviatedCountry,
                     supplier.AbreviatedState == "N/A" ? string.Empty : supplier.AbreviatedState + ", ",
                     supplier.City, links[i].SupplierId
                    );
            }

            return results;
        }

        // GET: api/Locations/5/menu
        [HttpGet("{id}/menu")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetItems(int id)
        {
            var location = await _context.Locations.FindAsync(id);
            if (location == null) return NotFound();

            var dishIds = await _context.MenuItems.Where(i => i.MenuId == location.MenuId).Select(i => i.DishId).ToArrayAsync();

            return await _context.Dishes.Where(d => dishIds.Contains(d.DishId)).Select(d => d.Name).ToArrayAsync();
        }

        #endregion

        #region Advanced

        // GET: api/Locations/query
        [HttpGet("query:{query}")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetLocationsQueried(string query)
        {
            var quer = query.ToUpper().Replace("QUERY:", string.Empty);

            bool isPaged, isDated, isFiltered, isSorted, desc, before;

            var pgInd = 0;
            var pgSz = 0;
            var filter = string.Empty;
            var sortBy = string.Empty;
            var since = new DateTime();

            isPaged = quer.Contains("PGIND=") && quer.Contains("PGSZ=");
            isDated = quer.Contains("OPEN=");
            isFiltered = quer.Contains("FILTER=");
            isSorted = quer.Contains("SORTBY=");
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
                    pgInd = pgInd == 0 ? 1 : Math.Abs(pgInd);
                    pgSz = pgSz == 0 ? 10 : Math.Abs(pgSz);
                }
            }

            if (isDated)
            {
                var dateStr = quer.QueryToParam("OPEN");

                var dateParses = DateTime.TryParse(dateStr, out since);

                if (!dateParses) isDated = false;
            }

            if (isFiltered) filter = quer.QueryToParam("FILTER");
            if (isSorted) sortBy = quer.QueryToParam("SORTBY");

            return (isPaged, isSorted, isFiltered, isDated)
switch
            {
                (false, false, true, false) => await GetLocationsFiltered(filter),
                (false, true, false, false) => await GetLocationsSorted(sortBy, desc),
                (true, false, false, false) => await GetLocationsPaged(pgInd, pgSz),
                (false, true, true, false) => await GetLocationsSorted(sortBy, desc,
(await GetLocationsFiltered(filter)).Value),
                (true, false, true, false) => await GetLocationsPaged(pgInd, pgSz,
(await GetLocationsFiltered(filter)).Value),
                (true, true, false, false) => await GetLocationsPaged(pgInd, pgSz,
(await GetLocationsSorted(sortBy, desc)).Value),
                (true, true, true, false) => await GetLocationsPaged(pgInd, pgSz,
(await GetLocationsSorted(sortBy, desc,
(await GetLocationsFiltered(filter)).Value)).Value),
                (false, false, false, true) => await GetLocationsOpen(since, before),
                (false, false, true, true) => await GetLocationsFiltered(filter,
(await GetLocationsOpen(since, before)).Value),
                (false, true, false, true) => await GetLocationsSorted(sortBy, desc,
(await GetLocationsOpen(since, before)).Value),
                (true, false, false, true) => await GetLocationsPaged(pgInd, pgSz,
(await GetLocationsOpen(since, before)).Value),
                (false, true, true, true) => await GetLocationsSorted(sortBy, desc,
(await GetLocationsFiltered(filter,
(await GetLocationsOpen(since, before)).Value)).Value),
                (true, false, true, true) => await GetLocationsPaged(pgInd, pgSz,
(await GetLocationsFiltered(filter,
(await GetLocationsOpen(since, before)).Value)).Value),
                (true, true, false, true) => await GetLocationsPaged(pgInd, pgSz,
(await GetLocationsSorted(sortBy, desc,
(await GetLocationsOpen(since, before)).Value)).Value),
                (true, true, true, true) => await GetLocationsPaged(pgInd, pgSz,
(await GetLocationsSorted(sortBy, desc,
(await GetLocationsFiltered(filter,
(await GetLocationsOpen(since, before)).Value)).Value)).Value),
                _ => await GetLocations(),
            };
        }

        public async Task<ActionResult<IEnumerable<dynamic>>> GetLocationsFiltered
            (string filter, IEnumerable<dynamic> col = null)
        => col == null ?
                (dynamic)await _context.Locations
                 .Include(l => l.Schedule)
                 .Select(l => new
                 {
                     l.LocationId,
                     l.AbreviatedCountry,
                     l.AbreviatedState,
                     l.Country,
                     l.State,
                     l.City,
                     l.Street,
                     l.MenuId,
                     OpenSince = l.OpenSince.ToShortDateString(),
                     l.Schedule.TimeTable
                 })
                 .Where(l =>
                    l.AbreviatedCountry.ToUpper()
                        .Contains(filter.ToUpper()) ||
                    l.AbreviatedState.ToUpper()
                        .Contains(filter.ToUpper()) ||
                    l.Country.ToUpper()
                        .Contains(filter.ToUpper()) ||
                    l.State.ToUpper()
                        .Contains(filter.ToUpper()) ||
                    l.City.ToUpper()
                        .Contains(filter.ToUpper()) ||
                    l.Street.ToUpper()
                        .Contains(filter.ToUpper()))
                 .ToArrayAsync() :
                (dynamic)col
                 .Where(l =>
                    l.AbreviatedCountry.ToUpper()
                        .Contains(filter.ToUpper()) ||
                    l.AbreviatedState.ToUpper()
                        .Contains(filter.ToUpper()) ||
                    l.Country.ToUpper()
                        .Contains(filter.ToUpper()) ||
                    l.State.ToUpper()
                        .Contains(filter.ToUpper()) ||
                    l.City.ToUpper()
                        .Contains(filter.ToUpper()) ||
                    l.Street.ToUpper()
                        .Contains(filter.ToUpper()))
                .ToArray();

        public async Task<ActionResult<IEnumerable<dynamic>>> GetLocationsPaged
            (int pgInd, int pgSz, IEnumerable<dynamic> col = null)
            => col == null ?
            (dynamic)await _context.Locations
                .Include(l => l.Schedule)
                 .Select(l => new
                 {
                     l.LocationId,
                     l.AbreviatedCountry,
                     l.AbreviatedState,
                     l.Country,
                     l.State,
                     l.City,
                     l.Street,
                     l.MenuId,
                     OpenSince = l.OpenSince.ToShortDateString(),
                     l.Schedule.TimeTable
                 })
                .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArrayAsync() :
            (dynamic)col
                .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArray();

        public async Task<ActionResult<IEnumerable<dynamic>>> GetLocationsSorted
            (string sortBy, bool? desc, IEnumerable<dynamic> col = null)
            => col == null ?
                sortBy.ToUpper() switch
                {
                    "COUNTRY" => desc.GetValueOrDefault() ?
                        (dynamic)await _context.Locations
                            .Include(l => l.Schedule)
                            .Select(l => new
                            {
                                l.LocationId,
                                l.AbreviatedCountry,
                                l.AbreviatedState,
                                l.Country,
                                l.State,
                                l.City,
                                l.Street,
                                l.MenuId,
                                OpenSince = l.OpenSince.ToShortDateString(),
                                l.Schedule.TimeTable
                            })
                            .OrderByDescending(l => l.AbreviatedCountry)
                            .ToArrayAsync() :
                        (dynamic)await _context.Locations
                            .Include(l => l.Schedule)
                            .Select(l => new
                            {
                                l.LocationId,
                                l.AbreviatedCountry,
                                l.AbreviatedState,
                                l.Country,
                                l.State,
                                l.City,
                                l.Street,
                                l.MenuId,
                                OpenSince = l.OpenSince.ToShortDateString(),
                                l.Schedule.TimeTable
                            })
                            .OrderBy(l => l.AbreviatedCountry)
                            .ToArrayAsync(),
                    "STATE" => desc.GetValueOrDefault() ?
                    (dynamic)await _context.Locations
                        .Include(l => l.Schedule)
                        .Select(l => new
                        {
                            l.LocationId,
                            l.AbreviatedCountry,
                            l.AbreviatedState,
                            l.Country,
                            l.State,
                            l.City,
                            l.Street,
                            l.MenuId,
                            OpenSince = l.OpenSince.ToShortDateString(),
                            l.Schedule.TimeTable
                        })
                        .OrderByDescending(l => l.AbreviatedState)
                        .ToArrayAsync() :
                    (dynamic)await _context.Locations
                        .Include(l => l.Schedule)
                        .Select(l => new
                        {
                            l.LocationId,
                            l.AbreviatedCountry,
                            l.AbreviatedState,
                            l.Country,
                            l.State,
                            l.City,
                            l.Street,
                            l.MenuId,
                            OpenSince = l.OpenSince.ToShortDateString(),
                            l.Schedule.TimeTable
                        })
                        .OrderBy(l => l.AbreviatedState)
                        .ToArrayAsync(),
                    var sort when sort == "CITY" || sort == "TOWN"
                    => desc.GetValueOrDefault() ?
                    (dynamic)await _context.Locations
                        .Include(l => l.Schedule)
                        .Select(l => new
                        {
                            l.LocationId,
                            l.AbreviatedCountry,
                            l.AbreviatedState,
                            l.Country,
                            l.State,
                            l.City,
                            l.Street,
                            l.MenuId,
                            OpenSince = l.OpenSince.ToShortDateString(),
                            l.Schedule.TimeTable
                        })
                        .OrderByDescending(l => l.City)
                        .ToArrayAsync() :
                    (dynamic)await _context.Locations
                        .Include(l => l.Schedule)
                        .Select(l => new
                        {
                            l.LocationId,
                            l.AbreviatedCountry,
                            l.AbreviatedState,
                            l.Country,
                            l.State,
                            l.City,
                            l.Street,
                            l.MenuId,
                            OpenSince = l.OpenSince.ToShortDateString(),
                            l.Schedule.TimeTable
                        })
                        .OrderBy(l => l.City)
                        .ToArrayAsync(),
                    "STREET" => desc.GetValueOrDefault() ?
                    (dynamic)await _context.Locations
                        .Include(l => l.Schedule)
                        .Select(l => new
                        {
                            l.LocationId,
                            l.AbreviatedCountry,
                            l.AbreviatedState,
                            l.Country,
                            l.State,
                            l.City,
                            l.Street,
                            l.MenuId,
                            OpenSince = l.OpenSince.ToShortDateString(),
                            l.Schedule.TimeTable
                        })
                        .OrderByDescending(l => l.Street)
                        .ToArrayAsync() :
                    (dynamic)await _context.Locations
                        .Include(l => l.Schedule)
                        .Select(l => new
                        {
                            l.LocationId,
                            l.AbreviatedCountry,
                            l.AbreviatedState,
                            l.Country,
                            l.State,
                            l.City,
                            l.Street,
                            l.MenuId,
                            OpenSince = l.OpenSince.ToShortDateString(),
                            l.Schedule.TimeTable
                        })
                        .OrderBy(l => l.Street)
                        .ToArrayAsync(),
                    "MENU" => desc.GetValueOrDefault() ?
                    (dynamic)await _context.Locations
                        .Include(l => l.Schedule)
                        .Select(l => new
                        {
                            l.LocationId,
                            l.AbreviatedCountry,
                            l.AbreviatedState,
                            l.Country,
                            l.State,
                            l.City,
                            l.Street,
                            l.MenuId,
                            OpenSince = l.OpenSince.ToShortDateString(),
                            l.Schedule.TimeTable
                        })
                        .OrderByDescending(l => l.MenuId)
                        .ToArrayAsync() :
                    (dynamic)await _context.Locations
                        .Include(l => l.Schedule)
                        .Select(l => new
                        {
                            l.LocationId,
                            l.AbreviatedCountry,
                            l.AbreviatedState,
                            l.Country,
                            l.State,
                            l.City,
                            l.Street,
                            l.MenuId,
                            OpenSince = l.OpenSince.ToShortDateString(),
                            l.Schedule.TimeTable
                        })
                        .OrderBy(l => l.MenuId)
                        .ToArrayAsync(),
                    _ => desc.GetValueOrDefault() ?
                        (dynamic)await _context.Locations
                            .Include(l => l.Schedule)
                            .Select(l => new
                            {
                                l.LocationId,
                                l.AbreviatedCountry,
                                l.AbreviatedState,
                                l.Country,
                                l.State,
                                l.City,
                                l.Street,
                                l.MenuId,
                                OpenSince = l.OpenSince.ToShortDateString(),
                                l.Schedule.TimeTable
                            })
                            .OrderByDescending(l => l.LocationId)
                            .ToArrayAsync() :
                        (dynamic)await _context.Locations
                            .Include(l => l.Schedule)
                            .Select(l => new
                            {
                                l.LocationId,
                                l.AbreviatedCountry,
                                l.AbreviatedState,
                                l.Country,
                                l.State,
                                l.City,
                                l.Street,
                                l.MenuId,
                                OpenSince = l.OpenSince.ToShortDateString(),
                                l.Schedule.TimeTable
                            })
                            .ToArrayAsync()
                } :
                  sortBy.ToUpper() switch
                  {
                      "COUNTRY" => desc.GetValueOrDefault() ?
                          (dynamic)col
                              .OrderByDescending(l => l.AbreviatedCountry)
                      .ToArray() :
                          (dynamic)col
                              .OrderBy(l => l.AbreviatedCountry)
                      .ToArray(),
                      "STATE" => desc.GetValueOrDefault() ?
                          (dynamic)col
                              .OrderByDescending(l => l.AbreviatedState)
                      .ToArray() :
                          (dynamic)col
                              .OrderBy(l => l.AbreviatedState)
                      .ToArray(),
                      var sort when sort == "CITY" || sort == "TOWN" => desc.GetValueOrDefault() ?
                          (dynamic)col
                              .OrderByDescending(l => l.City)
                      .ToArray() :
                          (dynamic)col
                              .OrderBy(l => l.City)
                      .ToArray(),
                      "STREET" => desc.GetValueOrDefault() ?
                          (dynamic)col
                              .OrderByDescending(l => l.Street)
                      .ToArray() :
                          (dynamic)col
                              .OrderBy(l => l.Street)
                      .ToArray(),
                      "MENU" => desc.GetValueOrDefault() ?
                          (dynamic)col
                              .OrderByDescending(l => l.MenuId)
                      .ToArray() :
                          (dynamic)col
                              .OrderBy(l => l.MenuId)
                      .ToArray(),
                      _ => desc.GetValueOrDefault() ?
                          (dynamic)col
                              .OrderByDescending(l => l.LocationId)
                      .ToArray() :
                          (dynamic)col.OrderBy(l => l.LocationId)
                              .ToArray()
                  };

        public async Task<ActionResult<IEnumerable<dynamic>>> GetLocationsOpen
            (DateTime since, bool before)
            => await _context.Locations
                .Include(l => l.Schedule)
                 .Select(l => new
                 {
                     l.LocationId,
                     l.AbreviatedCountry,
                     l.AbreviatedState,
                     l.Country,
                     l.State,
                     l.City,
                     l.Street,
                     l.MenuId,
                     l.OpenSince,
                     l.Schedule.TimeTable
                 })
                 .Where(l => before ?
                    l.OpenSince < since :
                    l.OpenSince >= since)
                 .Select(l => new
                 {
                     l.LocationId,
                     l.AbreviatedCountry,
                     l.AbreviatedState,
                     l.Country,
                     l.State,
                     l.City,
                     l.Street,
                     l.MenuId,
                     OpenSince = l.OpenSince.ToShortDateString(),
                     l.TimeTable
                 }).ToArrayAsync();

        #endregion

        #endregion

        // PUT: api/Locations/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLocation(int id, Location location)
        {
            if (!await LocationExists(id)) return NotFound();

            //Validation

            if (location.LocationId != 0 ||
                    (!string.IsNullOrEmpty(location.AbreviatedCountry) &&
                        (string.IsNullOrWhiteSpace(location.AbreviatedCountry) ||
                        location.AbreviatedCountry.Length > 4))  ||
                    (!string.IsNullOrEmpty(location.AbreviatedState) &&
                        (string.IsNullOrWhiteSpace(location.AbreviatedState) ||
                        location.AbreviatedState.Length > 4)) ||
                    (!string.IsNullOrEmpty(location.Country) &&
                        (string.IsNullOrWhiteSpace(location.Country) ||
                        location.Country.Length > 50)) ||
                    (!string.IsNullOrEmpty(location.State) &&
                        (string.IsNullOrWhiteSpace(location.State) ||
                        location.State.Length > 50)) ||
                    (!string.IsNullOrEmpty(location.City) &&
                        (string.IsNullOrWhiteSpace(location.City) ||
                        location.City.Length > 50)) ||
                    (!string.IsNullOrEmpty(location.Street) &&
                        (string.IsNullOrWhiteSpace(location.Street) ||
                        location.Street.Length > 50)) ||
                    (!string.IsNullOrEmpty(location.AbreviatedCountry) &&
                    !string.IsNullOrEmpty(location.AbreviatedState) &&
                    !string.IsNullOrWhiteSpace(location.City) &&
                    !string.IsNullOrEmpty(location.Street) &&
                    await _context.Locations.AnyAsync(l =>
                        l.AbreviatedCountry == location.AbreviatedCountry &&
                        l.AbreviatedState == location.AbreviatedState &&
                        l.City == location.City &&
                        l.Street == location.Street)) ||
                    await _context.Menus.FindAsync(location.MenuId) == null ||
                    await _context.Schedules.FindAsync(location.ScheduleId) == null ||
                    location.Menu != null ||
                    location.Schedule != null ||
                    location.Managements == null ||
                    location.Managements.Count != 0 ||
                    location.SupplyLinks == null ||
                    location.SupplyLinks.Count != 0)
                return BadRequest();

            //Validation

            var oldLocation = await _context.Locations.FindAsync(id);

            if(!string.IsNullOrEmpty(location.AbreviatedCountry))
            oldLocation.AbreviatedCountry = location.AbreviatedCountry;
            if(!string.IsNullOrEmpty(location.AbreviatedState ))
            oldLocation.AbreviatedState = location.AbreviatedState;
            if(!string.IsNullOrEmpty(location.City ))
            oldLocation.City = location.City;
            if(!string.IsNullOrEmpty(location.Country ))
            oldLocation.Country = location.Country;
            if(!string.IsNullOrEmpty(location.State ))
            oldLocation.State = location.State;
            if(!string.IsNullOrEmpty(location.Street ))
            oldLocation.Street = location.Street;
            if(location.LocationId!=0)
            oldLocation.LocationId = location.LocationId;
            if(location.MenuId!=0)
            oldLocation.MenuId = location.MenuId;
            if(location.ScheduleId!=0)
            oldLocation.ScheduleId = location.ScheduleId;
            if(location.OpenSince!=new DateTime())
            oldLocation.OpenSince = location.OpenSince;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/Locations
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPost]
        public async Task<ActionResult<Location>> CreateLocation(Location location)
        {
            // Validation
            if (
                    location.LocationId != 0 ||
                    string.IsNullOrEmpty(location.AbreviatedCountry) ||
                    string.IsNullOrWhiteSpace(location.AbreviatedCountry) ||
                    location.AbreviatedCountry.Length > 4 ||
                    string.IsNullOrEmpty(location.AbreviatedState) ||
                    string.IsNullOrWhiteSpace(location.AbreviatedState) ||
                    location.AbreviatedState.Length > 4 ||
                    string.IsNullOrEmpty(location.Country) ||
                    string.IsNullOrWhiteSpace(location.Country) ||
                    location.Country.Length > 50 ||
                    string.IsNullOrEmpty(location.State) ||
                    string.IsNullOrWhiteSpace(location.State) ||
                    location.State.Length > 50 ||
                    string.IsNullOrEmpty(location.City) ||
                    string.IsNullOrWhiteSpace(location.City) ||
                    location.City.Length > 50 ||
                    string.IsNullOrEmpty(location.Street) ||
                    string.IsNullOrWhiteSpace(location.Street) ||
                    location.Street.Length > 50 ||
                    await _context.Locations.AnyAsync(l =>
                        l.AbreviatedCountry == location.AbreviatedCountry &&
                        l.AbreviatedState == location.AbreviatedState &&
                        l.City == location.City &&
                        l.Street==location.Street)||
                    location.OpenSince.Year==1 ||
                    await _context.Menus.FindAsync(location.MenuId) == null ||
                    await _context.Schedules.FindAsync(location.ScheduleId) == null ||
                    location.Menu != null ||
                    location.Schedule != null ||
                    location.Managements == null ||
                    location.Managements.Count != 0 ||
                    location.SupplyLinks == null ||
                    location.SupplyLinks.Count != 0
                )
                return BadRequest();

            // Validation

            _context.Locations.Add(location);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetLocationBasic", new { id = location.LocationId }, location);
        }

        // DELETE: api/Locations/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<Location>> DeleteLocation(int id)
        {
            var location = await _context.Locations.FindAsync(id);
            if (location == null)  return NotFound();

            // If it has Management, Employees or SupplyLinks, turn it into an Empty Location
            if (await _context.Managements.AnyAsync(m => m.LocationId == id)||
                await _context.SupplyLinks.AnyAsync(l=>l.LocationId == id)||
                await _context.Employees.AnyAsync(e=>e.LocationId==id))
            {
                location.MenuId = 1;
                location.ScheduleId = 1;
                location.OpenSince = new DateTime();
                location.AbreviatedCountry = "XX";
                location.AbreviatedState = "XX";
                location.Country = string.Format("{0} : {1}", 
                    "Empty", DateTime.Today.ToShortDateString());
                location.State = "None";
                location.City = "None";
                location.Street = "None";
                await _context.SaveChangesAsync();
                return Ok();
            }

            var lastLocId = await _context.Locations.CountAsync();

            if (id == lastLocId)/*If it's last, delete it*/
            {
                _context.Locations.Remove(location);
                await _context.SaveChangesAsync();
                await _context.Database.ExecuteSqlRawAsync
                    ("DBCC CHECKIDENT('[Locations]', RESEED, " + (lastLocId - 1) + ")");
                await _context.SaveChangesAsync();
                return Ok();
            }

            //If it's not last, swap it with last and del that

            /*Migrate Management, Employees and Links from Last Position to posId*/
            var managements = await _context.Managements
                .Where(m => m.LocationId == lastLocId)
                .ToArrayAsync();
            var employees = await _context.Employees
                .Where(e => e.LocationId == lastLocId)
                .ToArrayAsync();
            var links = await _context.SupplyLinks
                .Where(l => l.LocationId == lastLocId)
                .ToArrayAsync();

            if (managements.Length > 0)
            {
                var management = managements.SingleOrDefault();
                management.LocationId = id;
            }
            
            foreach (var emp in employees)
                emp.LocationId = id;

            foreach (var link in links)
                link.LocationId = id;
            
            await _context.SaveChangesAsync();

            /*Swap and del last*/
            var lastLoc = await _context.Locations.FindAsync(lastLocId);
            location.AbreviatedCountry = lastLoc.AbreviatedCountry;
            location.AbreviatedState = lastLoc.AbreviatedState;
            location.Country = lastLoc.Country;
            location.State = lastLoc.State;
            location.City = lastLoc.City;
            location.Street = lastLoc.Street;
            location.ScheduleId = lastLoc.ScheduleId;
            location.MenuId = lastLoc.MenuId;
            location.OpenSince = lastLoc.OpenSince;
            await _context.SaveChangesAsync();

            _context.Locations.Remove(lastLoc);
            await _context.SaveChangesAsync();
            await _context.Database.ExecuteSqlRawAsync
                ("DBCC CHECKIDENT('[Locations]', RESEED, " + (lastLocId - 1) + ")");
            await _context.SaveChangesAsync();
            return Ok();
        }

        private async Task<bool> LocationExists(int id) => await _context.Locations.FindAsync(id)!=null;
    }
}
