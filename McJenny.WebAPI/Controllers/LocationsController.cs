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
    public class LocationsController : ControllerBase
    {
        private readonly FoodChainsDbContext _context;
        private readonly ILogger<LocationsController> _logger;

        public LocationsController(FoodChainsDbContext context,
            ILogger<LocationsController> logger)
        {
            _context = context;
            _logger = logger;
            _logger.LogDebug("Logger injected into Locations Controller.");
        }

        #region Gets

        #region Basic

        /// <summary>
        /// Get Locations
        /// </summary>
        /// <remarks>
        /// Returns a collection of all the Locations(max 20, unless specified 
        /// otherwise in query), formatted to include:
        ///  the Locations' Id, Abreviated Country, Abreviated State,
        ///  Country, State, City, Street Address, Menu Id, Date of 
        ///  Opening and Schedule.
        /// </remarks>
        // GET: api/Locations
        [HttpGet]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetLocations()
        {
            _logger.LogDebug("Locations Controller fetching all(limited to 20; for more" +
                " entries, query must be used) Locations.");
            return await GetLocationsPaged(1, 20);
        }

        /// <summary>
        /// Get Location
        /// </summary>
        /// <param name="id">Id of the desired Location</param>
        /// <remarks>
        /// Returns the Location corresponding to the given 'id' parameter,
        /// formatted to include: the Location's Id, Abreviated Country, Abreviated State,
        /// Country, State, City, Street Address, Menu Id, Date of 
        /// Opening and Schedule.
        /// If no Location with specified ID exits,
        /// Not Found is returned instead.
        /// </remarks>
        // GET: api/Locations/5
        [HttpGet("{id}")]
        public async Task<ActionResult<dynamic>> GetLocation(int id)
        {
            _logger.LogDebug("Locations Controller fetching Location with ID: " + id);
            if (!await LocationExists(id))
            {
                _logger.LogDebug("Locations Controller found no Location with ID: " + id);
                return NotFound();
            }

            _logger.LogDebug("Locations Controller returning formated Location with ID: " + id);
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

        /// <summary>
        /// Get Location Basic
        /// </summary>
        /// <param name="id">Id of desired Location</param>
        /// <remarks>
        /// Returns the Location object whose Location Id matches the given 'id' 
        /// parameter.
        /// A Location object will contain the Location Id, Abreviated Country, Abreviated State,
        /// Country, State, City, Street Address, Menu Id, ScheduleId, Date of 
        /// Opening, Managements, Employees, Menu, Schedule and Supply Links.
        /// If no Location with specified ID exits,
        /// Not Found is returned instead.
        /// </remarks>
        // GET: api/Locations/5
        [HttpGet("{id}/basic")]
        public async Task<ActionResult<Location>> GetLocationBasic(int id)
        {
            _logger.LogDebug("Locations Controller fetching Location with ID: " + id);
            var location = await _context.Locations.FindAsync(id);
            if (location == null)
            {
                _logger.LogDebug("Locations Controller found no Location with ID: " + id);
                return NotFound();
            }

            _logger.LogDebug("Locations Controller returning basic Location with ID: " + id);
            return location;
        }

        /// <summary>
        /// Get Location Employees
        /// </summary>
        /// <param name="id">Id of desired Location</param>
        /// <remarks>
        /// Returns a collection of Employees currently working
        /// at the Location whose Location Id matches the given
        /// 'id' parameter, formatted to include each Employee's
        /// First Name, Last Name, Employee Id and Position Title.
        /// If no Location with the specified Id exists,
        /// Not Found is returned instead.
        /// </remarks>
        // GET: api/Locations/5/employees
        [HttpGet("{id}/employees")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetEmployees(int id)
        {
            _logger.LogDebug("Locations Controller fetching Employees for Location " +
                "with ID: " + id);
            var location = await _context.Locations.FindAsync(id);
            if (location == null)
            {
                _logger.LogDebug("Locations Controller found no Location with ID: " + id);
                return NotFound();
            }

            _logger.LogDebug("Locations Controller returning Employees for Location " +
                "with ID: " + id);
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

        /// <summary>
        /// Get Location Suppliers
        /// </summary>
        /// <param name="id">Id of desired Location</param>
        /// <remarks>
        /// Returns a collection of the Suppliers currently providing
        /// goods to the Location whose Id matches the given 'id' parameter,
        /// formatted to include: the Supplier Id, Name, Abreviated Country,
        /// Abreviated State and City.
        /// If no Location with the specified Id exists,
        /// Not Found is returned instead.
        /// </remarks>
        // GET: api/Locations/5/suppliers
        [HttpGet("{id}/suppliers")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetSuppliers(int id)
        {
            _logger.LogDebug("Locations Controller fetching Suppliers for Location " +
                "with ID: " + id);
            var location = await _context.Locations.FindAsync(id);
            if (location == null)
            {
                _logger.LogDebug("Locations Controller found no Location with ID: " + id);
                return NotFound();
            }

            var supplierIds = await _context.SupplyLinks.Where(l => l.LocationId == location.LocationId).Select(l => l.SupplierId).ToListAsync();

            _logger.LogDebug("Locations Controller returning Suppliers for Location " +
                "with ID: " + id);
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

        /// <summary>
        /// Get Location Supply Links
        /// </summary>
        /// <param name="id">Id of desired Location</param>
        /// <remarks>
        /// Returns a collection of strings detailing the Supply Link
        /// relationships between the Location whose Id matches the
        /// given 'id' parameter and the Suppliers it works with,
        /// along with the specific categories each supplier provides.
        /// If no Location with the specified Id exists,
        /// Not Found is returned instead.
        /// </remarks>
        // GET: api/Locations/5/links
        [HttpGet("{id}/links")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetLinks(int id)
        {
            _logger.LogDebug("Locations Controller fetching Supply Links for Location " +
                "with ID: " + id);
            var location = await _context.Locations.FindAsync(id);
            if (location == null)
            {
                _logger.LogDebug("Locations Controller found no Location with ID: " + id);
                return NotFound();
            }

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

            _logger.LogDebug("Locations Controller returning Supply Links for Location " +
                "with ID: " + id);
            return results;
        }

        /// <summary>
        /// Get Location Menu
        /// </summary>
        /// <param name="id">Id of desired Location</param>
        /// <remarks>
        /// Returns a collection of Dish objects,
        /// representing the Dishes available at the Location
        /// whose Location Id matches the given 'id' parameter.
        /// If not Location with specified Id exists,
        /// Not Found is returned instead.
        /// </remarks>
        // GET: api/Locations/5/menu
        [HttpGet("{id}/menu")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetItems(int id)
        {
            _logger.LogDebug("Locations Controller fetching Dishes available at " +
                "Location with ID: " + id);
            var location = await _context.Locations.FindAsync(id);
            if (location == null)
            {
                _logger.LogDebug("Locations Controller found no Location with ID: " + id);
                return NotFound();
            }

            var dishIds = await _context.MenuItems
                .Where(i => i.MenuId == location.MenuId)
                .Select(i => i.DishId)
                .ToArrayAsync();

            _logger.LogDebug("Locations Controller returning Dishes available at Location " +
                "with ID: " + id);
            return await _context.Dishes
                .Where(d => dishIds.Contains(d.DishId))
                .Select(d => d.Name)
                .ToArrayAsync();
        }

        #endregion

        #region Advanced

        /// <summary>
        /// Get Location Queried
        /// </summary>
        /// <param name="query">Query String</param>
        /// <remarks>
        /// Returns a list of Locations according to the specified query
        /// Query Keywords: Filter, Open(Optional: Before), SortBy(Optional: Desc), PgInd and PgSz.
        /// Keywords are followed by '=', a string parameter, and, if further keywords
        /// are to be added afterwards, the Ampersand Separator
        /// Filter: Only return Locations where the Abreviated
        /// Country, Abreviated State, Country, State, City or Street
        /// contain the given string.
        /// SortBy: Locations will be ordered in ascending order.
        /// Sort options: 'Country', 'State', 'City'/'Town', 'Street' and 'Menu'.
        /// If the string parameter is anything else the sorting criteria
        /// will be the ID.
        /// Desc: If SortBy is used, and the string parameter is 'true',
        /// Locations wil be returned in Descending Order.
        /// PgInd: If the following int is greater than 0 and the PgSz 
        /// keyword is also present, Locations will be paged and
        /// page 'PgInd' of size 'PgSz' will be returned.
        /// PgSz: If the following int is greater than 0 and the PgInd 
        /// keyword is also present, Location will be paged and
        /// page 'PgInd' of size 'PgSz' will be returned.
        /// Open: Only Locations opened after the date paramaneter
        /// will be returned.
        /// Before: If the 'Open' keyword is used, only 
        /// Locations opened before the date parameter will be returned.
        /// Any of the Keywords can be used or ommited.
        /// Using a keyword more than once will result in only the first occurence being recognized.
        /// If a keyword is followed by an incorrectly formatted parameter, that
        /// keyword will be ignored.
        /// Order of Query Operations: Opened > Filter > Sort > Pagination
        /// Any improperly formatted text will be ignored.
        /// </remarks>
        // GET: api/Locations/query
        [HttpGet("query:{query}")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetLocationsQueried(string query)
        {
            _logger.LogDebug("Employees Controller looking up query...");
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

            //Logging
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
                else if (isSorted || isPaged) returnString.Insert(0, " and ");
                returnString.Insert(0, "Filtered By: " + filter);
            }
            if (isDated)
            {
                if ((isFiltered && isPaged) || (isFiltered && isSorted) || (isSorted && isPaged))
                    returnString.Insert(0, ", ");
                else if (isFiltered || isPaged || isSorted) returnString.Insert(0, " and ");
                returnString.Insert(0, "Opened: " + (before ? "Before" : "After") + ": " + since);
            }
            _logger.LogDebug("Locations Controller returning " +
                (!isPaged && !isSorted && !isFiltered && !isDated? "all " : string.Empty) +
                "Locations" +
                (returnString.Length == 0 ? string.Empty :
                " " + returnString.ToString()) + ".");

            //Logging

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

        [NonAction]
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

        [NonAction]
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

        [NonAction]
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

        [NonAction]
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

        /// <summary>
        /// Update Location
        /// </summary>
        /// <param name="id">Id of the target Location</param>
        /// <param name="location">A Location object whose properties
        /// are to be transfered to the existing Location</param>
        /// <remarks>
        /// Updates the values of an existing Location, whose Id corresponds 
        /// to the given 'id' parameter, using the values from a 'location' parameter.
        /// Only the Abreviated Country, Abreviated State, Country, State, City,
        /// Street Address, Menu Id, Schedule Id and Opening Date of the Location
        /// may be updated through the API.
        /// Its Employees, Menu and Schedule are automatically determined.
        /// For its relationships(Managements and Supply Links) their respective API
        /// Controllers must be used.
        /// The Location Id may not be updated.
        /// The given parameter of type Location may contain an Abreviated Country,
        /// Abreviated State, Country, State, City, Street, MenuId, ScheduleId
        /// and Opening Date.
        /// The Location Id must be 0.
        /// Additionally null objects can be provided for the Menu and Schedule.
        /// Empty collections must be provided for the Managements and Employees.
        /// The Location Id Parameter must correspond to an existing Location, 
        /// otherwise Not Found is returned.
        /// If the Location parameter is incorrect, Bad Request is returned.
        /// Location parameter errors:
        /// (1) The Location Id is not 0, 
        /// (2) The Abreviated Country or Abreviated State strings 
        /// are longer than 4 characters,
        /// (3) The Country, State, City or Street strings are longer than 50 characters,
        /// (4) The Abreviated Country, Abreviated State, City and Street strings
        /// are the same as another Location's,
        /// (5) Managements or Employees are null.
        /// (6) Managements or Employees are not empty collections.
        /// (7) Menu or Schedule are not null.
        /// (8) MenuId does not match an exiting Menu.
        /// (9) ScheduleId does not match an existing Schedule.
        /// (10) Opening Date is later than today.
        /// </remarks>
        // PUT: api/Locations/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLocation(int id, Location location)
        {
            _logger.LogDebug("Locations Controller trying to Update Location with ID: " + id);
            if (!await LocationExists(id))
            {
                _logger.LogDebug("Locations Controller found no Location with ID: " + id);
                return NotFound();
            }

            //Validation

            if (location.LocationId != 0 ||
                    (!string.IsNullOrEmpty(location.AbreviatedCountry) &&
                        (string.IsNullOrWhiteSpace(location.AbreviatedCountry) ||
                        location.AbreviatedCountry.Length > 4)) ||
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
            {
                _logger.LogDebug("Locations Controller found provided 'location' " +
                    "violates the update constraints.");
                return BadRequest();
            }

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
            if(location.MenuId!=0)
            oldLocation.MenuId = location.MenuId;
            if(location.ScheduleId!=0)
            oldLocation.ScheduleId = location.ScheduleId;
            if(location.OpenSince!=new DateTime())
            oldLocation.OpenSince = location.OpenSince;

            await _context.SaveChangesAsync();

            _logger.LogDebug("Locations Controller successfully updated " +
                "Location with ID: " + id);
            return NoContent();
        }
        
        /// <summary>
        /// Create Location
        /// </summary>
        /// <param name="location">A Location to add to the Database</param>
        /// <remarks>
        /// Creates a new Location and inserts it into the Database
        /// using the values from the 'location' parameter.
        /// Only the Abreviated Country, Abreviated State, Country, State, 
        /// City, Street Address, Menu Id, Schedule Id and Opening Date
        /// of the Location may be set.
        /// Menu and Schedule are automatically determined.
        /// For its relationships(Employees and Managements) their respective API
        /// Controllers must be used.
        /// The Location Id may not be set. The Database will automatically
        /// assign it a new Id, equivalent to the last Location Id in the Database + 1.
        /// The given parameter of type Location may contain an Abreviated Country,
        /// Abreviated State, Country, State, City, Street, Menu Id, Schedule Id and
        /// Opening Date.
        /// Null objects for Menu and Schedule may be provided.
        /// Empty collections are required for Managements and Employees.
        /// A Location Id of 0 may also be inluded.
        /// If the Location parameter is incorrect, Bad Request is returned
        /// Dish parameter errors:
        /// (1) The Location Id is not 0, 
        /// (2) The Abreviated Country or Abreviated State strings 
        /// are longer than 4 characters,
        /// (3) The Country, State, City or Street strings are longer than 50 characters,
        /// (4) The Abreviated Country, Abreviated State, City and Street strings
        /// are the same as another Location's,
        /// (5) Managements or Employees are null.
        /// (6) Managements or Employees are not empty collections.
        /// (7) Menu or Schedule are not null.
        /// (8) MenuId does not match an exiting Menu.
        /// (9) ScheduleId does not match an existing Schedule.
        /// (10) Opening Date is later than today.
        /// </remarks>
        // POST: api/Locations
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPost]
        public async Task<ActionResult<Location>> CreateLocation(Location location)
        {
            _logger.LogDebug("Locations Controller trying to create new Location.");

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
            {
                _logger.LogDebug("Locations Controller found that 'location' " +
                    "parameter violates the creation contraints.");
                return BadRequest();
            }

            // Validation

            _context.Locations.Add(location);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Locations Controller successfully created new " +
                "Location with ID: " + location.LocationId);
            return CreatedAtAction("GetLocationBasic", new { id = location.LocationId }, location);
        }

        /// <summary>
        /// Delete Location
        /// </summary>
        /// <param name="id">Id of target Location</param>
        /// <remarks>
        /// Removes a Location from the Database.
        /// If there is no Location, whose Id matches the given 'id' parameter, 
        /// Not Found is returned.
        /// If the desired Location is the last in the Database, and it has no
        /// Employee or Management relationships, it is removed from the Database
        /// and its Location Id will be used by the next Location to be inserted.
        /// If the desired Location is not the last in the Database, and it has no
        /// Employee or Management relationships, it is swapped with the last
        /// Location in the Database before it is removed, giving the last Location in the Database
        /// the Id of the deleted Location and freeing up the last Location's Id for
        /// the next Location to be inserted.
        /// If the desired Location has a Management or any Employees relationships,
        /// it is turned into a blank Location, so a not to violate its relationships.
        /// Its Country will be changed to reflect that fact.
        /// </remarks>
        // DELETE: api/Locations/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<Location>> DeleteLocation(int id)
        {
            _logger.LogDebug("Locations Controller deleting Location with ID: " + id);
            var location = await _context.Locations.FindAsync(id);
            if (location == null)
            {
                _logger.LogDebug("Locations Controller found no Location with ID: " + id);
                return NotFound();
            }

            // If it has Management, Employees or SupplyLinks, turn it into an Empty Location
            if (await _context.Managements.AnyAsync(m => m.LocationId == id)||
                await _context.SupplyLinks.AnyAsync(l=>l.LocationId == id)||
                await _context.Employees.AnyAsync(e=>e.LocationId==id))
            {
                _logger.LogDebug("Locations Controller found that Location " +
                    "with ID:" + id + " has Supply Link, Employee or Management " +
                    "relationships and will turn it into a blank Location " +
                    "instead of deleting it.");

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

                _logger.LogDebug("Locations Controller turned Location with ID: "
                    + id + " into a blank Location.");
                return Ok();
            }

            var lastLocId = await _context.Locations.CountAsync();

            if (id == lastLocId)/*If it's last, delete it*/
            {
                _logger.LogDebug("Locations Controller found that Location " +
                    "with ID: " + id + " has no Supply Link, Employee or Management " +
                    "relationships and is the last in the Database. As " +
                    "such, it will be deleted.");

                _context.Locations.Remove(location);
                await _context.SaveChangesAsync();
                await _context.Database.ExecuteSqlRawAsync
                    ("DBCC CHECKIDENT('[Locations]', RESEED, " + (lastLocId - 1) + ")");
                await _context.SaveChangesAsync();

                _logger.LogDebug("Locations Controller deleted Location with ID: " + id);
                return Ok();
            }

            //If it's not last, swap it with last and del that

            _logger.LogDebug("Locaions Controller found that Location with ID: " +
                id + " has no SupplyLink, Employee or Management relationships and " +
                "is not the last in the Database. As such, it will be swapped with the " +
                "last Location then the last Location will be deleted.");

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

            _logger.LogDebug("Locations Controller turned Location with ID: " + id +
                " into the last Location, then deleted that.");
            return Ok();
        }

        [NonAction]
        private async Task<bool> LocationExists(int id) => await _context.Locations.FindAsync(id)!=null;
    }
}
