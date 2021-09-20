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
    public class SuppliersController : ControllerBase
    {
        private readonly FoodChainsDbContext _context;
        private readonly ILogger<SuppliersController> _logger;

        public SuppliersController(FoodChainsDbContext context,
            ILogger<SuppliersController> logger)
        {
            _context = context;
            _logger = logger;
            _logger.LogDebug("Logger injected into Suppliers Controller.");
        }

        #region Gets

        #region Basic

        /// <summary>
        /// Get Suppliers
        /// </summary>
        /// <remarks>
        /// Returns a collection of strings(20 max, unless queried otherwise)
        /// formatted to include each Supplier's Id, Name, Abreviated Country,
        /// Abreviated State and City.
        /// </remarks>
        // GET: api/Suppliers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<string>>> GetSuppliers()
        {
            _logger.LogDebug("Suppliers Controller fetching all Suppliers(" +
                "20 max, unless queried otherwise.)");
            return await _context.Suppliers
            .Take(20)
            .Select(s =>
            string.Format("({0}) {1}, {2}{3}, {4}",
                s.SupplierId, s.Name, s.AbreviatedCountry,
                s.AbreviatedState == "N/A" ? string.Empty : ", " + s.AbreviatedState,
                s.City
                ))
            .ToListAsync();
        }

        /// <summary>
        /// Get Supplier
        /// </summary>
        /// <param name="id">Id of desired Supplier</param>
        /// <remarks>
        /// Returns the Supplier whose Id matches the 'id' parameter, formated
        /// to include the Supplier Id, Name, Abrevited Country, Abreviated State
        /// and City.
        /// If no Supplier matching the 'id' parameter exists, 
        /// Not Found is returned instead.
        /// </remarks>
        // GET: api/Suppliers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<dynamic>> GetSupplier(int id)
        {
            _logger.LogDebug("Suppliers Controller fetching Supplier with ID: " + id);

            if (!await SupplierExists(id))
            {
                _logger.LogDebug("Suppliers Controller found no Supplier with ID: " + id);
                return NotFound();
            }

            _logger.LogDebug("Suppliers controller returning formated Schedule with ID: " + id);
            return await _context.Suppliers
                .Select(s => new
                {
                    s.SupplierId,
                    s.Name,
                    s.AbreviatedCountry,
                    s.AbreviatedState,
                    s.City
                })
                .FirstOrDefaultAsync(s => s.SupplierId == id);
        }

        /// <summary>
        /// Get Supplier Basic
        /// </summary>
        /// <param name="id">Id of desired Supplier</param>
        /// <remarks>
        /// Returns a Supplier object of the Supplier whose Supplier Id
        /// matches the 'id' parameter.
        /// If no such Supplier exists, Not Found is returned instead.
        /// </remarks>
        // GET: api/Suppliers/5/basic
        [HttpGet("{id}/basic")]
        public async Task<ActionResult<Supplier>> GetSupplierBasic(int id)
        {
            _logger.LogDebug("Suppliers Controller fetching Supplier with ID: " + id);

            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                _logger.LogDebug("Suppliers Controller found no Supplier with ID: " + id);
                return NotFound();
            }

            _logger.LogDebug("Suppliers Controller returning basic Supplier with ID: " + id);
            return supplier;
        }

        /// <summary>
        /// Get Supplier Categories
        /// </summary>
        /// <param name="id">Id of desired Supplier</param>
        /// <remarks>
        /// Returns a collection of the Supply Categories the Suppplier
        /// whose Supplier Id matches the 'id' parameter stocks, formted 
        /// to include each Supply Category's Id and Name.
        /// If no Supplier whose Id matches the provided 'id' parameter exists,
        /// Not Found is returned instead.
        /// </remarks>
        // GET: api/Suppliers/5/stocks
        [HttpGet("{id}/stocks")]
        public async Task<ActionResult<dynamic>> GetSupplyCategories(int id)
        {
            _logger.LogDebug("Suppliers Controller fetching Supply Categories" +
                "stocked by Supplier with ID: " + id);

            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                _logger.LogDebug("Suppliers Controller found no Supplier with ID: " + id);
                return NotFound();
            }

            _logger.LogDebug("Suppliers Controller returning Supply Categories stocked" +
                "by Supplier with ID: " + id);
            return await _context.SupplyCategories
                .Where(c => GetSupplyCategoryIdsAsync(id).Result
                    .Contains(c.SupplyCategoryId))
                .Select(c => new { c.SupplyCategoryId, c.Name })
                .ToArrayAsync();
        }

        /// <summary>
        /// Get Supplier Locations
        /// </summary>
        /// <param name="id">Id of desired Supplier</param>
        /// <remarks>
        /// Returns a collection of strings of the Locations serviced by the Supplier
        /// whose Supplier Id matches the 'id' parameter, formated to include
        /// each Location's Id, Abreviated Country, Abreviated State, City and Street.
        /// If no Supplier whose Supplier Id matches the given 'id' parameter exists,
        /// Not Found is returned.
        /// </remarks>
        // GET: api/Suppliers/5/locations
        [HttpGet("{id}/locations")]
        public async Task<ActionResult<IEnumerable<string>>> GetLocations(int id)
        {
            _logger.LogDebug("Suppliers Controller fetching Locations supplied by " +
                "Supplier with ID: " + id);

            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                _logger.LogDebug("Suppliers Controller found no Supplier with ID: " + id);
                return NotFound();
            }

            _logger.LogDebug("Suppliers Controller returning Locations supplied by " +
                "Supplier with ID: " + id);
            return await _context.Locations
                .Where(l => GetSuppliedLocationIdsAsync(id).Result
                    .Contains(l.LocationId))
                .Select(l => string.Format("({0}), {1}, {2}{3}, {4}",
                    l.LocationId,
                    l.AbreviatedCountry,
                    l.AbreviatedState == "N/A" ? string.Empty :
                    l.AbreviatedState + ", ",
                    l.City,
                    l.Street))
                .ToArrayAsync();
        }

        /// <summary>
        /// Get Supplier Links
        /// </summary>
        /// <param name="id">Id of desired Supplier</param>
        /// <remarks>
        /// Returns a collection of strings explaining the Supply Link
        /// relationship between Locations, the Supply Categories provided them and the Supplier
        /// whose Id matches the given 'id' parameter, formated to include
        /// each Supply Category's Id and Name, as well
        /// as each Location's Id, Abreviated Country, Abreviated State, City and Street
        /// Address.
        /// If no Supplier whose Id matches the given 'id' parameter exists,
        /// Not Found is returned instead.
        /// </remarks>
        // GET: api/Suppliers/5/links
        [HttpGet("{id}/links")]
        public async Task<ActionResult<IEnumerable<string>>> GetSupplierLinks(int id)
        {
            _logger.LogDebug("Suppliers Controller fetching Supply " +
                "Links for Supplier with ID: " + id);
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                _logger.LogDebug("Suppliers Controller found no " +
                    "Supplier with ID: " + id);
                return NotFound();
            }

            var supplyLinks = await _context.SupplyLinks
                .Where(l => l.SupplierId == id)
                .ToArrayAsync();

            var catIds = new List<int>();
            var locIds = new List<int>();

            foreach (var link in supplyLinks)
            {
                catIds.Add(link.SupplyCategoryId);
                locIds.Add(link.LocationId);
            };

            catIds = catIds.Distinct().ToList();
            locIds = locIds.Distinct().ToList();

            var cats = await _context.SupplyCategories
                .Select(c => new
                {
                    c.SupplyCategoryId,
                    c.Name
                })
                .Where(c => catIds.Contains(c.SupplyCategoryId))
                .ToArrayAsync();

            var locations = await _context.Locations
                .Select(l => new
                {
                    l.LocationId,
                    l.AbreviatedCountry,
                    l.AbreviatedState,
                    l.City,
                    l.Street
                })
                .Where(l => locIds.Contains(l.LocationId))
                .ToArrayAsync();

            var links = new string[supplyLinks.Length];

            for (int i = 0; i < links.Length; i++)
            {
                var cat = cats.FirstOrDefault(c => c.SupplyCategoryId == supplyLinks[i].SupplyCategoryId);
                var loc = locations.FirstOrDefault(l => l.LocationId == supplyLinks[i].LocationId);

                links[i] = string.Format("(Supply Link: {0}): ({1}) {2} " +
                    "supplied to ({3}) {4}, {5}{6}, {7}.",
                    i,
                    cat.SupplyCategoryId,
                    cat.Name,
                    loc.LocationId,
                    loc.AbreviatedCountry,
                    loc.AbreviatedState,
                    loc.City,
                    loc.Street);
            }

            _logger.LogDebug("Suppliers Controller returning Supply " +
                "Links for Supplier with ID: " + id);
            return links;
        }

        //    {
        //    // GET: api/Suppliers/5/links
        //    [HttpGet("{id}/links")]
        //    public async Task<ActionResult<IEnumerable<string>>> GetSupplyLinks(int id)
        //    {
        //        var supplier = await _context.Suppliers.FindAsync(id);
        //        if (supplier == null) return NotFound();

        //        var dbLinks = await _context.SupplyLinks.ToArrayAsync();
        //        var locationIds = new List<int>();
        //        var cats = await _context.SupplyCategories.Select(c => new { c.SupplyCategoryId, c.Name }).ToArrayAsync();

        //        foreach (var dbLink in dbLinks)
        //            if (!locationIds.Contains(dbLink.LocationId)) locationIds.Add(dbLink.LocationId);

        //        var locations = await _context.Locations.Where(l => locationIds.Contains(l.LocationId)).ToArrayAsync();

        //        var statements = new string[dbLinks.Length];

        //        for (int i = 0; i < dbLinks.Length; i++)
        //        {
        //            var location = locations.SingleOrDefault(l => l.LocationId == dbLinks[i].LocationId);

        //            statements[i] = string.Format("{0} ({1}), {2}, {3}{4} supplies {5}, {6}{7}, {8} ({9}) with {10} ({11})",
        //            supplier.Name, id, supplier.AbreviatedCountry,
        //            supplier.AbreviatedState == "N/A" ? string.Empty : supplier.AbreviatedState + ", ",
        //            supplier.City, location.AbreviatedCountry,
        //            location.AbreviatedState == "N/A" ? string.Empty : location.AbreviatedState + ", ",
        //            location.City, location.Street, dbLinks[i].LocationId,
        //            cats.SingleOrDefault(c => c.SupplyCategoryId == dbLinks[0].SupplyCategoryId).Name,
        //            dbLinks[0].SupplyCategoryId);
        //        }
        //        return statements;

        //    }
        //}

        #endregion

        #region Advanced

        /// <summary>
        /// Get Suppliers Queried
        /// </summary>
        /// <param name="query">Query String</param>
        /// <remarks>
        /// Returns a collection of Suppliers according to the 'query' parameter, formated
        /// to include each Supplier's Id, Name, Abreviated Country, Abreviated State,
        /// Country, State and City.
        /// Query Keywords: Filter, SortBy(Optional: Desc), PgInd and PgSz.
        /// Keywords are followed by '=', a string parameter, and, if further keywords
        /// are to be added afterwards, the Ampersand Separator.
        /// Filter: Only return Suppliers where the Name, Abreviated Country,
        /// Abreviated State, Country, State or City contain the provided string.
        /// SortBy: Suppliers will be ordered in ascending order by the provided
        /// sorting criteria.
        /// Sorting Criteria include 'Name', 'Country', 'State' and 'City'. 
        /// Any other string will result in an ID sort.
        /// Desc: If SortBy is used, and the string parameter is 'true',
        /// Suppliers wil be returned in Descending Order.
        /// PgInd: If the following int is greater than 0 and the PgSz 
        /// keyword is also present, Suppliers will be paged and
        /// page 'PgInd' of size 'PgSz' will be returned.
        /// PgSz: If the following int is greater than 0 and the PgInd 
        /// keyword is also present, Suppliers will be paged and
        /// page 'PgInd' of size 'PgSz' will be returned.
        /// Any of the Keywords can be used or ommited.
        /// Using a keyword more than once will result in only the first 
        /// occurence being recognized.
        /// If a keyword is followed by an incorrectly formatted parameter, 
        /// the keyword will be ignored.
        /// Order of Query Operations: Filter > Sort > Pagination
        /// Any improperly formatted text will be ignored.
        /// </remarks>
        // GET: api/Suppliers/query
        [HttpGet("query:{query}")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetSuppliersQueried(string query)
        {
            _logger.LogDebug("Suppliers Controller looking up query...");
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
                else if (isSorted || isPaged) returnString.Insert(0, " and ");
                returnString.Insert(0, "Filtered By: " + filter);
            }
            _logger.LogDebug("Employees Controller returning " +
                (!isPaged && !isSorted && !isFiltered ? "all " : string.Empty) +
                "Employees" +
                (returnString.Length == 0 ? string.Empty :
                " " + returnString.ToString()) + ".");


            return (isPaged, isSorted, isFiltered)
            switch
            {
                (false, false, true) => await GetSuppliersFiltered(filter),
                (false, true, false) => await GetSuppliersSorted(sortBy, desc),
                (true, false, false) => await GetSuppliersPaged(pgInd, pgSz),
                (false, true, true) => await GetSuppliersSorted(sortBy, desc,
                    (await GetSuppliersFiltered(filter)).Value),
                (true, false, true) => await GetSuppliersPaged(pgInd, pgSz,
                    (await GetSuppliersFiltered(filter)).Value),
                (true, true, false) => await GetSuppliersPaged(pgInd, pgSz,
                    (await GetSuppliersSorted(sortBy, desc)).Value),
                (true, true, true) => await GetSuppliersPaged(pgInd, pgSz,
                    (await GetSuppliersSorted(sortBy, desc,
                    (await GetSuppliersFiltered(filter)).Value)).Value),
                _ => await GetSuppliersPaged(0, 20),
            };
        }

        [NonAction]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetSuppliersFiltered(string filter, IEnumerable<dynamic> col = null)
        => col == null ?
                    (dynamic)await _context.Suppliers
                     .Select(s => new
                     {
                         s.SupplierId,
                         s.Name,
                         s.AbreviatedCountry,
                         s.AbreviatedState,
                         s.Country,
                         s.State,
                         s.City
                     })
                     .Where(s => s.Name.ToUpper()
                         .Contains(filter.ToUpper()) ||
                        s.AbreviatedCountry.ToUpper()
                            .Contains(filter.ToUpper()) ||
                        s.AbreviatedState.ToUpper()
                            .Contains(filter.ToUpper()) ||
                        s.Country.ToUpper()
                            .Contains(filter.ToUpper()) ||
                        s.State.ToUpper()
                            .Contains(filter.ToUpper()) ||
                        s.City.ToUpper()
                            .Contains(filter.ToUpper()))
                     .ToArrayAsync() :
                (dynamic)col
                 .Where(s => s.Name.ToUpper()
                     .Contains(filter.ToUpper()) ||
                    s.AbreviatedCountry.ToUpper()
                        .Contains(filter.ToUpper()) ||
                    s.AbreviatedState.ToUpper()
                        .Contains(filter.ToUpper()) ||
                    s.Country.ToUpper()
                        .Contains(filter.ToUpper()) ||
                    s.State.ToUpper()
                        .Contains(filter.ToUpper()) ||
                    s.City.ToUpper()
                        .Contains(filter.ToUpper()))
                        .ToArray();

        [NonAction]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetSuppliersPaged(int pgInd, int pgSz, IEnumerable<dynamic> col = null)
            => col == null ?
            (dynamic)await _context.Suppliers
                .Select(s => new
                {
                    s.SupplierId,
                    s.Name,
                    s.AbreviatedCountry,
                    s.AbreviatedState,
                    s.Country,
                    s.State,
                    s.City
                })
                .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArrayAsync() :
            (dynamic)col
                .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArray();

        [NonAction]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetSuppliersSorted(string sortBy, bool? desc, IEnumerable<dynamic> col = null)
            => col == null ?
                sortBy.ToUpper() switch
                {
                    "NAME" => desc.GetValueOrDefault() ?
                        (dynamic)await _context.Suppliers
                            .Select(s => new
                            {
                                s.SupplierId,
                                s.Name,
                                s.AbreviatedCountry,
                                s.AbreviatedState,
                                s.Country,
                                s.State,
                                s.City
                            })
                            .OrderByDescending(s => s.Name).ToArrayAsync() :
                        (dynamic)await _context.Suppliers
                            .Select(s => new
                            {
                                s.SupplierId,
                                s.Name,
                                s.AbreviatedCountry,
                                s.AbreviatedState,
                                s.Country,
                                s.State,
                                s.City
                            })
                            .OrderBy(s => s.Name).ToArrayAsync(),
                    "COUNTRY" => desc.GetValueOrDefault() ?
                        (dynamic)await _context.Suppliers
                            .Select(s => new
                            {
                                s.SupplierId,
                                s.Name,
                                s.AbreviatedCountry,
                                s.AbreviatedState,
                                s.Country,
                                s.State,
                                s.City
                            })
                            .OrderByDescending(s => s.AbreviatedCountry).ToArrayAsync() :
                        (dynamic)await _context.Suppliers
                            .Select(s => new
                            {
                                s.SupplierId,
                                s.Name,
                                s.AbreviatedCountry,
                                s.AbreviatedState,
                                s.Country,
                                s.State,
                                s.City
                            })
                            .OrderBy(s => s.AbreviatedCountry)
                            .ToArrayAsync(),
                    "STATE" => desc.GetValueOrDefault() ?
                        (dynamic)await _context.Suppliers
                            .Select(s => new
                            {
                                s.SupplierId,
                                s.Name,
                                s.AbreviatedCountry,
                                s.AbreviatedState,
                                s.Country,
                                s.State,
                                s.City
                            })
                            .OrderByDescending(s => s.AbreviatedState).ToArrayAsync() :
                        (dynamic)await _context.Suppliers
                            .Select(s => new
                            {
                                s.SupplierId,
                                s.Name,
                                s.AbreviatedCountry,
                                s.AbreviatedState,
                                s.Country,
                                s.State,
                                s.City
                            })
                            .OrderBy(s => s.AbreviatedState)
                            .ToArrayAsync(),
                    "CITY" => desc.GetValueOrDefault() ?
                        (dynamic)await _context.Suppliers
                            .Select(s => new
                            {
                                s.SupplierId,
                                s.Name,
                                s.AbreviatedCountry,
                                s.AbreviatedState,
                                s.Country,
                                s.State,
                                s.City
                            })
                            .OrderByDescending(s => s.City)
                            .ToArrayAsync() :
                        (dynamic)await _context.Suppliers
                            .Select(s => new
                            {
                                s.SupplierId,
                                s.Name,
                                s.AbreviatedCountry,
                                s.AbreviatedState,
                                s.Country,
                                s.State,
                                s.City
                            })
                            .OrderBy(s => s.City)
                            .ToArrayAsync(),
                    _ => desc.GetValueOrDefault() ?
                        (dynamic)await _context.Suppliers
                            .Select(s => new
                            {
                                s.SupplierId,
                                s.Name,
                                s.AbreviatedCountry,
                                s.AbreviatedState,
                                s.Country,
                                s.State,
                                s.City
                            })
                            .OrderByDescending(s => s.SupplierId)
                            .ToArrayAsync() :
                        (dynamic)await _context.Suppliers
                            .Select(s => new
                            {
                                s.SupplierId,
                                s.Name,
                                s.AbreviatedCountry,
                                s.AbreviatedState,
                                s.Country,
                                s.State,
                                s.City
                            })
                            .ToArrayAsync()
                } :
                  sortBy.ToUpper() switch
                  {
                      "NAME" => desc.GetValueOrDefault() ?
                          (dynamic)col
                              .OrderByDescending(s => s.Name)
                              .ToArray() :
                          (dynamic)col
                              .OrderBy(s => s.Name)
                                .ToArray(),
                      "COUNTRY" => desc.GetValueOrDefault() ?
                          (dynamic)col
                              .OrderByDescending(s => s.AbreviatedCountry)
                              .ToArray() :
                          (dynamic)col
                              .OrderBy(s => s.AbreviatedCountry)
                                .ToArray(),
                      "STATE" => desc.GetValueOrDefault() ?
                          (dynamic)col
                              .OrderByDescending(s => s.AbreviatedState)
                              .ToArray() :
                          (dynamic)col
                              .OrderBy(s => s.AbreviatedState)
                                .ToArray(),
                      "CITY" => desc.GetValueOrDefault() ?
                          (dynamic)col
                              .OrderByDescending(s => s.City)
                              .ToArray() :
                          (dynamic)col
                              .OrderBy(s => s.City)
                                .ToArray(),
                      _ => desc.GetValueOrDefault() ?
                          (dynamic)col
                              .OrderByDescending(s => s.SupplierId)
                      .ToArray() :
                          (dynamic)col
                              .ToArray()
                  };

        #endregion

        #endregion

        /// <summary>
        /// Update Supplier
        /// </summary>
        /// <param name="id">Id of target Supplier</param>
        /// <param name="supplier">A Supplier object whose properties 
        /// are to be transfered to the existing Suppplier</param>
        /// <remarks>
        /// Updates the values of an existing Supplier, whose Id corresponds 
        /// to the given 'id' parameter,
        /// using the values from the 'supplier' parameter.
        /// Only the Name, Abreviated Country, Abreviated State, Country, State and City
        /// of the Supplier may be updated through this API controller.
        /// For its Supplier Stocks and Supply Links relationships, their respective
        /// API controllers must be used.
        /// The Supplier Id may not be updated.
        /// The given parameter of type Supplier may contain a Name, Abreviated Country,
        /// Abreviated State, Country, State and City.
        /// Additionally empty collections Supplier Stocks and Supply Links, as well as a 
        /// Supplier Id of 0 may be included.
        /// The 'id' Parameter must correspond to an existing Supplier, otherwise
        /// Not Found is returned.
        /// If the 'supplier' parameter is incorrect, Bad Request is returned.
        /// Supplier parameter errors:
        /// (1) The Supplier Id is not 0,
        /// (2) The Abreviated Country or Abreviated State strings 
        /// are longer than 4 characters,
        /// (3) The Name, Country, State or City strings are 
        /// longer than 50 characters,
        /// (4) Supply Links or Supplier Stocks are null,
        /// (5) Supply Links or Supplier Stocks are not empty collections.
        /// (6) Another Supplier with the same Name exists.
        /// </remarks>
        // PUT: api/Suppliers/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSupplier(int id, Supplier supplier)
        {
            _logger.LogDebug("Suppliers Controller trying to update Supplier " +
                "with ID: " + id);
            if (!await SupplierExists(id))
            {
                _logger.LogDebug("Suppliers Controller found no Supplier with ID: " + id);
                return NotFound();
            }

            //Validation

            if (supplier.SupplierId != 0 ||
                (!string.IsNullOrEmpty(supplier.AbreviatedCountry) &&
                    (string.IsNullOrWhiteSpace(supplier.AbreviatedCountry) ||
                    supplier.AbreviatedCountry.Length > 4)) ||
                (!string.IsNullOrEmpty(supplier.AbreviatedState) &&
                    (string.IsNullOrWhiteSpace(supplier.AbreviatedState) ||
                    supplier.AbreviatedState.Length > 4)) ||
                (!string.IsNullOrEmpty(supplier.City) &&
                    (string.IsNullOrWhiteSpace(supplier.City) ||
                    supplier.City.Length > 50)) ||
                (!string.IsNullOrEmpty(supplier.Country) &&
                    (string.IsNullOrWhiteSpace(supplier.Country) ||
                    supplier.Country.Length > 50)) ||
                (!string.IsNullOrEmpty(supplier.State) &&
                    (string.IsNullOrWhiteSpace(supplier.State) ||
                    supplier.State.Length > 50)) ||
                (!string.IsNullOrEmpty(supplier.Name) &&
                    (string.IsNullOrWhiteSpace(supplier.Name) ||
                    supplier.Name.Length > 50)) ||
                (!string.IsNullOrEmpty(supplier.Name) &&
                await _context.Suppliers
                    .AnyAsync(s => s.Name == supplier.Name &&
                    s.SupplierId != id)) ||
                supplier.SupplierStocks == null || supplier.SupplierStocks.Count != 0 ||
                supplier.SupplyLinks == null || supplier.SupplyLinks.Count != 0)
            {
                _logger.LogDebug("Suppliers Controller found provided 'supplier' " +
                    "parameter to violate update constraints.");
                return BadRequest();
            }

            //Validation

            var oldSupplier = await _context.Suppliers.FindAsync(id);

            if (!string.IsNullOrEmpty(supplier.AbreviatedCountry))
            oldSupplier.AbreviatedCountry = supplier.AbreviatedCountry;
            if (!string.IsNullOrEmpty(supplier.AbreviatedState))
            oldSupplier.AbreviatedState = supplier.AbreviatedState;
            if (!string.IsNullOrEmpty(supplier.City))
            oldSupplier.City = supplier.City;
            if (!string.IsNullOrEmpty(supplier.Name))
            oldSupplier.Name = supplier.Name;
            if (!string.IsNullOrEmpty(supplier.Country))
            oldSupplier.Country = supplier.Country;
            if (!string.IsNullOrEmpty(supplier.State))
            oldSupplier.State = supplier.State;

            await _context.SaveChangesAsync();

            _logger.LogDebug("Suppliers Controller successfully updated" +
                " Supplier with ID: " + id);
            return NoContent();
        }

        /// <summary>
        /// Create Supplier
        /// </summary>
        /// <param name="supplier">A Supplier object 
        /// to be added to the Database</param>
        /// <remarks>
        /// Creates a new Supplier and inserts it into the Database
        /// using the values from the 'supplier' parameter.
        /// Only the Name, Abreviated Country, Abreviated State, Country, State
        /// and City of the Supplier may be set.
        /// For its Supply Links and Supplier Stocks relationships their 
        /// respective API controllers must be used.
        /// The Supplier Id may not be set. The Databse will automatically
        /// assign it a new Id, equivalent to the last Supplier Id in the Database + 1.
        /// The given parameter of type Supplier must contain a Name, Abreviated Country,
        /// Abreviated State, Country, State and City.
        /// Additionally, it may contain empty collections
        /// of Supply Links and Supplier Stocks as well as a Supplier Id of 0.
        /// If the 'supplier' parameter is incorrect, Bad Request is returned.
        /// Supplier parameter errors:
        /// (1) The Supplier Id is not 0, 
        /// (2) The Name, Abreviated Country, Abreviated State, Country,
        /// State or City strings are empty,
        /// (3) The Abreviated Country or Abreviated State strings
        /// are longer than 4 characters,
        /// (4) The Name, Country, State or City strings are
        /// longer than 50 characters,
        /// (5) The Name string is the same as another Supplier's Name,
        /// (6) Supply Links or Supplier Stocks are null,
        /// (7) Supply Links or Supplier Stocks are not empty collections.
        /// </remarks>
        // POST: api/Suppliers
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPost]
        public async Task<ActionResult<Supplier>> CreateSupplier(Supplier supplier)
        {
            _logger.LogDebug("Suppliers Controller trying to create new Supplier.");

            //Validation

            if (supplier.SupplierId != 0 ||
                string.IsNullOrEmpty(supplier.AbreviatedCountry) ||
                string.IsNullOrWhiteSpace(supplier.AbreviatedCountry) ||
                supplier.AbreviatedCountry.Length > 4 ||
                string.IsNullOrEmpty(supplier.AbreviatedState) ||
                string.IsNullOrWhiteSpace(supplier.AbreviatedState) ||
                supplier.AbreviatedState.Length > 4 ||
                string.IsNullOrEmpty(supplier.City) ||
                string.IsNullOrWhiteSpace(supplier.City) ||
                supplier.City.Length > 50 ||
                string.IsNullOrEmpty(supplier.Country) ||
                string.IsNullOrWhiteSpace(supplier.Country) ||
                supplier.Country.Length > 50 ||
                string.IsNullOrEmpty(supplier.State) ||
                string.IsNullOrWhiteSpace(supplier.State) ||
                supplier.State.Length > 50 ||
                string.IsNullOrEmpty(supplier.Name) ||
                string.IsNullOrWhiteSpace(supplier.Name) ||
                supplier.Name.Length > 50 ||
                await _context.Suppliers.AnyAsync(s => s.Name == supplier.Name) ||
                supplier.SupplierStocks == null || supplier.SupplierStocks.Count != 0 ||
                supplier.SupplyLinks == null || supplier.SupplyLinks.Count != 0)
            {
                _logger.LogDebug("Suppliers Controller found provided 'supplier' " +
                    "parameted to violate creation contraints.");
                return BadRequest();
            }

            //Validation

            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Suppliers Controller successfully created new Supplier " +
                "at ID: " + supplier.SupplierId);
            return CreatedAtAction("GetSupplierBasic", new { id = supplier.SupplierId }, supplier);
        }
        
        /// <summary>
        /// Delete Supplier
        /// </summary>
        /// <param name="id">Id of target Supplier</param>
        /// <remarks>
        /// Removes a Supplier from the Database.
        /// If there is no Supplier, whose Id matches the given 'id' parameter, 
        /// Not Found is returned.
        /// If the desired Suppler is the last in the Database, and it has no
        /// Supply Link or Supplier Stocks relationships, it is removed from the Database
        /// and its Supplier Id will be used by the next Supplier to be inserted.
        /// If the desired Supplier is not the last in the Database, and it has no
        /// Supply Links or Supplier Stocks relationships, it is swapped with the last
        /// Supplier in the Database, taking on its Supply Links and Supplier Stocks before,
        /// the last Supplier is deleted freeing up the last Supplier Id for
        /// the next Supplier to be inserted.
        /// If the desired Supplier has any Supply Links or Supplier Stocks relationships,
        /// it is turned into a blank Supplier, so a not to violate its relationships.
        /// Its Name will be changed to reflect that fact.
        /// </remarks>
        // DELETE: api/Suppliers/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<Supplier>> DeleteSupplier(int id)
        {
            _logger.LogDebug("Suppliers Controller trying to delete Supplier " +
                "with ID: " + id);

            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                _logger.LogDebug("Suppliers Controller found no Supplier " +
                    "with ID: " + id);
                return NotFound();
            }

            // If it has SupplyStocks or SupplyLinks, turn it into an Empty Supplier
            if (await _context.SupplierStocks.AnyAsync(s => s.SupplierId == id) ||
                await _context.SupplyLinks.AnyAsync(l => l.SupplierId == id))
            {
                _logger.LogDebug("Suppliers Controller found Supplier with ID: " +
                    +id + " has Supplier Stocks or Supply Link relationships. As such, " +
                    "intead of being deleted it will be turned into a blank Supplier.");
                supplier.Name = string.Format("{0} : {1}",
                    "Empty", DateTime.Today.ToShortDateString());
                supplier.AbreviatedCountry = "XX";
                supplier.AbreviatedState = "XX";
                supplier.Country = "None";
                supplier.State = "None";
                supplier.City = "None";
                await _context.SaveChangesAsync();

                _logger.LogDebug("Suppliers Controller successfully turned Supplier " +
                    "with ID: " + id + " into a blank Supplier.");
                return Ok();
            }

            var lastSupId = await _context.Suppliers.CountAsync();

            if (id == lastSupId)/*If it's last, delete it*/
            {
                _logger.LogDebug("Suppliers Controller found Supplier with ID: " +
                    +id + " to have no Supply Link or Supplier Stock relationships and " +
                    "to be last in the Database. As such, it will be deleted.");

                _context.Suppliers.Remove(supplier);
                await _context.SaveChangesAsync();
                await _context.Database.ExecuteSqlRawAsync
                    ("DBCC CHECKIDENT('[Suppliers]', RESEED, " + (lastSupId - 1) + ")");
                await _context.SaveChangesAsync();

                _logger.LogDebug("Suppliers Controlelr successfully deleted Supplier " +
                    "with ID: " + id);
                return Ok();
            }

            //If it's not last, swap it with last and del that

            _logger.LogDebug("Suppliers Controller found Supplier with ID: " + id +
                " to have no Supplier Link or Supplier Stock relationships and " +
                "to appear before the last Supplier in the Database. As such, it " +
                "will be swapped with the last Supplier before thah one will be deleted.");

            /*Migrate Stocks and Links from Last Supplier to supId*/
            var stocks = await _context.SupplierStocks
                .Where(s => s.SupplierId == lastSupId)
                .ToArrayAsync();
            var links = await _context.SupplyLinks
                .Where(s => s.SupplierId == lastSupId)
                .ToArrayAsync();

            foreach (var stock in stocks)
                stock.SupplierId = id;

            foreach (var link in links)
                link.SupplierId = id;

            await _context.SaveChangesAsync();

            /*Swap and del last*/
            var lastSup = await _context.Suppliers.FindAsync(lastSupId);
            supplier.AbreviatedCountry = lastSup.AbreviatedCountry;
            supplier.AbreviatedState = lastSup.AbreviatedState;
            supplier.Country = lastSup.Country;
            supplier.State = lastSup.State;
            supplier.City = lastSup.City;
            supplier.Name = lastSup.Name;

            _context.Suppliers.Remove(lastSup);
            await _context.SaveChangesAsync();
            await _context.Database.ExecuteSqlRawAsync
                ("DBCC CHECKIDENT('[Suppliers]', RESEED, " + (lastSupId - 1) + ")");
            await _context.SaveChangesAsync();

            _logger.LogDebug("Suppliers Controller successfully turned Supplier with ID: "
                + id + " into last Supplier and deleted that one.");
            return Ok();
        }
        
        [NonAction]
        private async Task<bool> SupplierExists(int id)
            => await _context.Suppliers.FindAsync(id)!=null;

        //Helpers

        [NonAction]
        private async Task<int[]> GetSupplyCategoryIdsAsync(int id)
        => await _context.SupplierStocks
                .Where(ss => ss.SupplierId == id)
                .Select(ss => ss.SupplyCategoryId)
                .ToArrayAsync();

        [NonAction]
        private async Task<int[]> GetSuppliedLocationIdsAsync(int id)
        => await _context.SupplyLinks
                .Where(l => l.SupplierId == id)
                .Select(l => l.LocationId)
                .ToArrayAsync();
    }
}
